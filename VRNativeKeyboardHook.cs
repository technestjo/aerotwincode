using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using RTLTMPro;
using UnityEngine.UI;

[RequireComponent(typeof(TMP_InputField))]
public class VRNativeKeyboardHook : MonoBehaviour, IPointerClickHandler, ISelectHandler, IDeselectHandler
{
    private TMP_InputField inputField;
    private TouchScreenKeyboard keyboard;
    private MonoBehaviour xrKeyboardInstance; // Stores the XRI Keyboard so we can poll its text

    [Tooltip("Check this if this field takes Arabic/RTL text to enable right-to-left layout in the VR OS keyboard")]
    public bool isArabicInputField = false;

    // A flag to ensure we only subscribe to the XRI keyboard's events once
    private bool hasSubscribedToXRIEvents = false;
    
    // Static tracker to enforce only one field accepts XRI Keyboard text at a time
    public static TMP_InputField globalActiveVRField;
    
    private GeminiVoiceToText localVoiceToText;
    private string lastXrText = "";
    private string lastInputText = "";

    // --- Performance Caching ---
    private Transform cachedMicTransform;
    private Transform cachedInputFieldChild;
    private RTLTextMeshPro cachedRtlOverlayText;
    private bool isReferencesCached = false;

    void Awake()
    {
        inputField = GetComponent<TMP_InputField>();
        
        // Prevent the physical PC keyboard from intercepting or locking the UI
        inputField.shouldHideMobileInput = true;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OpenVRKeyboard();
    }

    public void OnSelect(BaseEventData eventData)
    {
        // When a new field is selected, ensure other instances release the keyboard reference
        foreach (var hook in FindObjectsByType<VRNativeKeyboardHook>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (hook != this) hook.ClearKeyboardReferences();
        }

        OpenVRKeyboard();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        // Only close the native OS keyboard always on deselect
        CloseNativeVRKeyboard();
        // We DO NOT call ClearKeyboardReferences here because in VR, focus shifts to the keyboard itself,
        // and we want to keep syncing while the keyboard is open even if the input field isn't technically 'selected'.
    }
    
    public void ClearKeyboardReferences()
    {
        xrKeyboardInstance = null;
    }

    private void OpenVRKeyboard()
    {
        if (TryOpenXRISpatialKeyboard())
            return;

        // Fallback to Native OS Keyboard
        if (keyboard != null && keyboard.status == TouchScreenKeyboard.Status.Visible)
            return;

#if UNITY_ANDROID && !UNITY_EDITOR
        // For Meta Quest, TouchScreenKeyboard.Open triggers the native OS systemic keyboard.
        // We set it to default. If you need numbers only, you can change the TouchScreenKeyboardType.
        TouchScreenKeyboardType keyboardType = TouchScreenKeyboardType.Default;
        
        keyboard = TouchScreenKeyboard.Open(
            inputField.text, 
            keyboardType, 
            false, // autocorrect
            false, // multiline
            false, // secure
            false, // alert
            isArabicInputField ? "Type your message..." : "Enter text...",
            0 // character limit
        );
#else
        Debug.Log("VRNativeKeyboardHook: TouchScreenKeyboard is only active on Android/Quest builds.");
#endif
    }

    private bool TryOpenXRISpatialKeyboard()
    {
        System.Type xrKeyboardType = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.Samples.SpatialKeyboard.XRKeyboard, Unity.XR.Interaction.Toolkit.Samples.SpatialKeyboard");
        if (xrKeyboardType == null) return false;

        var keyboardObj = Object.FindFirstObjectByType(xrKeyboardType, FindObjectsInactive.Include) as MonoBehaviour;
        if (keyboardObj != null)
        {
            var openMethod = xrKeyboardType.GetMethod("Open", new System.Type[] { typeof(TMP_InputField), typeof(bool) });
            if (openMethod != null)
            {
                openMethod.Invoke(keyboardObj, new object[] { inputField, false });
                
                // Position it nicely in front of the camera (user)
                Transform camEnv = Camera.main != null ? Camera.main.transform : null;
                if (camEnv != null)
                {
                    Vector3 targetPos = camEnv.position + camEnv.forward * 0.7f + Vector3.down * 0.35f;
                    keyboardObj.transform.position = targetPos;
                    // Make it face the user
                    keyboardObj.transform.forward = (keyboardObj.transform.position - camEnv.position).normalized;
                }
                
                xrKeyboardInstance = keyboardObj; // Store reference for Update loop
                globalActiveVRField = inputField; // Lock sync to THIS field globally!
                lastXrText = inputField.text;
                lastInputText = inputField.text;
                
                // Clear cache on new open to force re-discovery
                isReferencesCached = false;
                cachedMicTransform = null;
                cachedInputFieldChild = null;
                cachedRtlOverlayText = null;
                
                // Inject our Arabic font into all TextMeshProUGUI keys to stop "Missing Character" \u06F0 warnings
                if (isArabicInputField && inputField != null && inputField.textComponent != null && inputField.textComponent.font != null)
                {
                    var allKeyTexts = keyboardObj.GetComponentsInChildren<TextMeshProUGUI>(true);
                    foreach (var keyText in allKeyTexts)
                    {
                        keyText.font = inputField.textComponent.font;
                    }
                }
                
                return true;
            }
        }
        return false;
    }

    private void CloseNativeVRKeyboard()
    {
        if (keyboard != null)
        {
            keyboard.active = false;
            keyboard = null;
        }
    }

    private void CloseXRISpatialKeyboard()
    {
        if (xrKeyboardInstance != null)
        {
            var closeMethod = xrKeyboardInstance.GetType().GetMethod("Close", new System.Type[] {  });
            if (closeMethod != null) closeMethod.Invoke(xrKeyboardInstance, null);
            xrKeyboardInstance = null;
            hasSubscribedToXRIEvents = false;
            
            if (globalActiveVRField == inputField)
                globalActiveVRField = null;
        }
    }

    void Update()
    {
        // 1. Sync Native OS Keyboard
        if (keyboard != null)
        {
            try
            {
                if (keyboard.status == TouchScreenKeyboard.Status.Visible)
                {
                    // Sync the native OS keyboard text back down continuously into the Unity UI
                    if (inputField != null && inputField.text != keyboard.text)
                    {
                        inputField.text = keyboard.text;
                        // Force TMP_InputField internal validation/events to trigger
                        inputField.ForceLabelUpdate();
                    }

                    // Detect if the user pressed the "Enter/Submit" or "Done" button on the VR keyboard
                    if (keyboard.status == TouchScreenKeyboard.Status.Done || keyboard.status == TouchScreenKeyboard.Status.Canceled)
                    {
                        CloseNativeVRKeyboard();
                        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
                    }
                }
            }
            catch (System.Exception)
            {
                // Catch any internal Unity TouchScreenKeyboard null ref exceptions 
                // that happen in the Editor when the module isn't fully loaded.
                CloseNativeVRKeyboard();
            }
        }

        // 2. Sync XRI Spatial Keyboard
        if (xrKeyboardInstance != null && inputField != null)
        {
            // Enforce singleton receiver!
            if (globalActiveVRField != null && globalActiveVRField != inputField)
            {
                return;
            }

            var textProp = xrKeyboardInstance.GetType().GetProperty("text");
            if (textProp != null)
            {
                // Cache discovery logic once per keyboard session
                if (!isReferencesCached)
                {
                    cachedInputFieldChild = FindChildRecursive(xrKeyboardInstance.transform, "Input Field");
                    cachedMicTransform = FindChildRecursive(xrKeyboardInstance.transform, "Mic");
                    isReferencesCached = true;
                }

                Transform inputFieldChild = cachedInputFieldChild;
                TMP_InputField xrInputFieldObj = (inputFieldChild != null) ? inputFieldChild.GetComponent<TMP_InputField>() : null;

                string xrText = textProp.GetValue(xrKeyboardInstance) as string ?? "";
                string uiText = inputField.text ?? "";

                bool vrChanged = (xrText != lastXrText);
                bool pcChanged = (uiText != lastInputText);

                    if (vrChanged && !pcChanged)
                    {
                        // VR Hand Typed
                        if (inputField.text != xrText)
                        {
                            inputField.text = xrText;
                            inputField.ForceLabelUpdate();
                        }
                        // Store EXACT settled texts to prevent infinite formatting loops
                        lastXrText = xrText;
                        lastInputText = inputField.text;
                    }
                    else if (pcChanged && !vrChanged)
                    {
                        // Physical PC Typed
                        if (xrText != uiText)
                        {
                            textProp.SetValue(xrKeyboardInstance, uiText);
                        }
                        lastInputText = uiText;
                        lastXrText = textProp.GetValue(xrKeyboardInstance) as string ?? "";
                    }
                    else if (vrChanged && pcChanged)
                    {
                        // Both changed frame-perfect? Prefer PC hardware overrides
                        if (xrText != uiText)
                        {
                            textProp.SetValue(xrKeyboardInstance, uiText);
                        }
                        lastInputText = uiText;
                        lastXrText = textProp.GetValue(xrKeyboardInstance) as string ?? "";
                    }
                    else
                    {
                        // No new typings this frame. Keep trackers fresh to catch drift.
                        lastXrText = xrText;
                        lastInputText = uiText;
                    }
                    
                    if (isArabicInputField)
                    {
                        if (xrInputFieldObj != null && xrInputFieldObj.textComponent != null)
                            {
                                // Since standard TMPro ignores RTL rendering, we use RTLTextMeshPro directly.
                                if (cachedRtlOverlayText == null && inputFieldChild != null)
                                {
                                    Transform rtlOverlay = inputFieldChild.Find("XR_RTL_Overlay");
                                    if (rtlOverlay == null)
                                    {
                                        GameObject go = new GameObject("XR_RTL_Overlay");
                                        Transform parentToUse = xrInputFieldObj.textViewport != null ? xrInputFieldObj.textViewport : inputFieldChild;
                                        go.transform.SetParent(parentToUse, false);

                                        RectTransform origRt = xrInputFieldObj.textComponent.GetComponent<RectTransform>();
                                        RectTransform rt = go.AddComponent<RectTransform>();
                                        rt.anchorMin = origRt.anchorMin; rt.anchorMax = origRt.anchorMax;
                                        rt.offsetMin = origRt.offsetMin; rt.offsetMax = origRt.offsetMax;
                                        rt.pivot = origRt.pivot;
                                        rt.sizeDelta = origRt.sizeDelta;

                                        cachedRtlOverlayText = go.AddComponent<RTLTextMeshPro>();
                                        cachedRtlOverlayText.fontSize = xrInputFieldObj.textComponent.fontSize;
                                        cachedRtlOverlayText.color = xrInputFieldObj.textComponent.color;
                                        cachedRtlOverlayText.alignment = TextAlignmentOptions.Right; 
                                        cachedRtlOverlayText.textWrappingMode = TextWrappingModes.NoWrap;
                                        cachedRtlOverlayText.overflowMode = TextOverflowModes.Overflow;
                                        cachedRtlOverlayText.isRightToLeftText = true;
                                        cachedRtlOverlayText.Farsi = false;

                                        if (inputField.textComponent != null && inputField.textComponent.font != null)
                                            cachedRtlOverlayText.font = inputField.textComponent.font;

                                        // Fully hide the original text renderer
                                        xrInputFieldObj.textComponent.color = new Color(0, 0, 0, 0);
                                    }
                                    else
                                    {
                                        cachedRtlOverlayText = rtlOverlay.GetComponent<RTLTextMeshPro>();
                                    }
                                }

                                if (cachedRtlOverlayText != null)
                                {
                                    cachedRtlOverlayText.text = xrInputFieldObj.text; 
                                }
                            }
                        }
                    }

            // 3. Hook onto the physical "Mic" UI Button
            if (!hasSubscribedToXRIEvents)
            {
                Transform micTransform = cachedMicTransform;
                if (micTransform != null)
                {
                    // Force it to be visible in case the XRLayout system hid it
                    micTransform.gameObject.SetActive(true);
                    
                    // Force the button to be interactable, as XRKeyboardLayout might have disabled it
                    var btn = micTransform.GetComponent<UnityEngine.UI.Button>();
                    if (btn != null)
                    {
                        btn.interactable = true;
                        
                        // If it's specifically an XRKeyboardKey, it might have faded alpha. Force it.
                        var xrKey = micTransform.GetComponent("XRKeyboardKey");
                        if (xrKey != null)
                        {
                            var setInteractableMethod = xrKey.GetType().GetMethod("SetButtonInteractable", new System.Type[] { typeof(bool) });
                            if (setInteractableMethod != null)
                            {
                                setInteractableMethod.Invoke(xrKey, new object[] { true });
                            }
                        }

                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(() => {
                            Debug.Log("[VRNativeKeyboardHook] Mic clicked. Resolving voice component...");
                            
                            // 1. Try to find a global voice system for consistent state
                            var chatController = Object.FindFirstObjectByType<ChatControllerRTL>();
                            if (chatController != null && chatController.voiceToText != null)
                            {
                                // IMPORTANT: We don't just ToggleRecording() because the global one might 
                                // be hardcoded to the chat input field. 
                                // Instead, we ensure THIS hook manages its own transcription for ITS field.
                            }

                            // 2. Setup/Enforce local voice component
                            if (localVoiceToText == null)
                            {
                                localVoiceToText = gameObject.AddComponent<GeminiVoiceToText>();
                                
                                localVoiceToText.Setup(
                                    "",
                                    onComplete: (trText) => {
                                        if (inputField != null)
                                        {
                                            inputField.text += (inputField.text.Length > 0 ? " " : "") + trText;
                                            inputField.ForceLabelUpdate();
                                        }
                                    },
                                    onStart: () => { Debug.Log("[VRNativeKeyboardHook] Recording Started."); },
                                    onStop: () => { Debug.Log("[VRNativeKeyboardHook] Recording Stopped."); },
                                    onErr: (err) => { Debug.LogError("[VRNativeKeyboardHook] Voice Error: " + err); }
                                );
                            }
                            
                            localVoiceToText.ToggleRecording();
                        });
                        hasSubscribedToXRIEvents = true; 
                    }
                }
            }

            // Continuous visual feedback check
            if (hasSubscribedToXRIEvents && xrKeyboardInstance != null && localVoiceToText != null)
            {
                Transform micTransform = cachedMicTransform;
                if (micTransform != null)
                {
                    micTransform.localScale = Vector3.one;
                    var iconText = micTransform.GetComponentInChildren<TextMeshProUGUI>();
                    var iconImages = micTransform.GetComponentsInChildren<UnityEngine.UI.Image>();
                    
                    bool isRec = localVoiceToText.IsRecording();
                    Color targetColor = isRec ? Color.red : new Color(0.9f, 0.9f, 0.9f, 1f);

                    float fillScale = 1f;
                    if (isRec)
                    {
                        float vol = localVoiceToText.GetCurrentVolume();
                        fillScale = Mathf.Clamp(0.7f + (vol / 1.5f), 0.7f, 1.35f); 
                    }
                    Vector3 dynScale = new Vector3(fillScale, fillScale, 1f);

                    if (iconText != null) { iconText.color = targetColor; iconText.rectTransform.localScale = dynScale; }
                    foreach (var img in iconImages)
                    {
                        if (img.gameObject != micTransform.gameObject) { img.color = targetColor; img.rectTransform.localScale = dynScale; }
                    }
                }
            }
        }
    }

    private Transform FindChildRecursive(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName) return child;
            
            Transform found = FindChildRecursive(child, childName);
            if (found != null) return found;
        }
        return null;
    }
}
