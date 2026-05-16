using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using RTLTMPro;
using UnityEngine.Networking;

public enum ChatLang
{
    EN,
    AR
}

public class ChatRuntimeUIBuilderRTL : MonoBehaviour
{
    [Header("Placement")]
    public Vector3 worldPosition = new Vector3(3.56f, 2.95f, 3.2f);
    public Vector3 worldEuler = new Vector3(0f, 486.796f, 0f);
    public float worldScale = 0.01f;

    [Header("Font")]
    public TMP_FontAsset defaultFont;

    [Header("Language")]
    public ChatLang startLanguage = ChatLang.EN;

    [Header("Panel Size")]
    public float panelWidth = 940f;
    public float panelHeight = 1140f;

    [Header("Layout Heights")]
    public float headerHeight = 130f;
    public float messagesHeight = 840f;
    public float quickButtonsHeight = 72f;
    public float typingHeight = 34f;
    public float inputHeight = 120f;

    [Header("Message Settings")]
    public float bubbleWidth = 560f;
    public float messageFontSize = 24f;
    public float messageMinHeight = 52f;
    public float messagePaddingHorizontal = 14f;
    public float messagePaddingVertical = 10f;
    public float messageSpacing = 10f;

    [Header("Scrollbar")]
    [Range(1f, 40f)] public float scrollbarWidth = 8f;

    [Header("Buttons")]
    public float mainButtonFontSize = 18f;
    public float sendButtonWidth = 120f;
    public float sendButtonFontSize = 20f;

    [Header("Header Fonts")]
    public float titleFontSize = 36f;
    public float subtitleFontSize = 20f;

    [Header("Graphics")]
    [Tooltip("Drop the TechNest / AI Logo Sprite here")]
    public Sprite teamLogo;

    [Header("Colors")]
    public Color panelColor = new Color(0.05f, 0.06f, 0.08f, 0.78f);
    public Color panelOutlineColor = new Color(0.25f, 0.6f, 1f, 0.35f);
    public Color sectionColor = new Color(0.12f, 0.14f, 0.18f, 0.60f);
    public Color messagesBgColor = new Color(0.08f, 0.09f, 0.12f, 0.40f);
    public Color inputBgColor = new Color(0.06f, 0.07f, 0.10f, 0.75f);
    public Color buttonColor = new Color(0.18f, 0.55f, 1f, 0.28f);
    public Color sendButtonColor = new Color(0.18f, 0.55f, 1f, 0.70f);
    public Color userBubbleColor = new Color(0.18f, 0.55f, 1f, 0.35f);
    public Color aiBubbleColor = new Color(0.10f, 0.11f, 0.14f, 0.70f);
    public Color textColor = new Color(0.95f, 0.97f, 1f, 0.95f);
    public Color subTextColor = new Color(0.8f, 0.9f, 1f, 0.78f);

    [Header("Demo Context")]
    public string faultType = "Oil Pressure Drop";
    public string currentStep = "Scan Completed";
    public float pressure = 24.7f;
    public float temp = 615f;
    public float vibration = 2.1f;
    public bool leak = false;


    private Canvas canvas;
    private RectTransform rootPanel;
    private ScrollRect scrollRect;
    private RectTransform messagesContent;
    private Scrollbar verticalScrollbar;
    private GameObject _canvasGo;

    private TMP_InputField inputField;
    private Button sendButton;
    private Button btnStop;
    private Button voiceButton;
    private Button btnNext;
    private Button btnExplain;
    private Button btnSafety;
    private Button btnEvaluate;
    private Button btnAttach;
    private Button btnDoctor;
    private Button btnOffline;
    private Button btnLang;
    private Button btnMode;
    private Button btnGoRobot;
    private Button btnTTS;

    private RTLTextMeshPro headerTitle;
    private RTLTextMeshPro headerSub;
    private RTLTextMeshPro typingText;
    private RTLTextMeshPro footerText;

    private ChatControllerRTL controller;
    private ChatMessageRowPrefabRTL rowPrefab;

    void Awake()
    {
        EnsureEventSystem();
        TMP_FontAsset[] allFonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        foreach (var font in allFonts)
        {
            if (font.name.Equals("segoeui SDF Arabic", System.StringComparison.OrdinalIgnoreCase))
            {
                defaultFont = font;
                break;
            }
        }

        BuildCanvas();
        BuildRoot();
        BuildHeader();
        BuildMessagesArea();
        BuildQuickButtons();
        BuildTyping();
        BuildInput();
        BuildFooter();
        BuildRowPrefab();
        BuildController();

        canvas.transform.position = worldPosition;
        canvas.transform.rotation = Quaternion.Euler(worldEuler);
    }
    void LateUpdate()
    {
        if (_canvasGo == null) return;
        _canvasGo.transform.position = worldPosition;
        _canvasGo.transform.rotation = Quaternion.Euler(worldEuler);
        _canvasGo.transform.localScale = Vector3.one * worldScale;
    }

    void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }
    }

    void Update()
    {
        if (controller != null)
        {
            controller.faultType = faultType;
            controller.currentStep = currentStep;
            controller.pressure = pressure;
            controller.temp = temp;
            controller.vibration = vibration;
            controller.leak = leak;
        }
    }

    void BuildCanvas()
    {
        _canvasGo = new GameObject("ChatCanvas");
        var go = _canvasGo;
        go.transform.SetParent(transform, false);

        canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        Camera cam = Camera.main;
        if (cam == null) cam = Object.FindFirstObjectByType<Camera>();
        canvas.worldCamera = cam;

        if (cam == null)
        {
            Debug.LogWarning("[ChatRuntimeUIBuilderRTL] No camera found. Falling back to Screen Space Overlay.");
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        go.AddComponent<CanvasScaler>();
        go.AddComponent<GraphicRaycaster>();

        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(panelWidth + 40f, panelHeight + 40f);
        go.transform.localScale = Vector3.one * worldScale;

        EnsureVRRaycasters(go);
    }

    void BuildRoot()
    {
        rootPanel = CreateRect("RootPanel", canvas.transform);
        rootPanel.sizeDelta = new Vector2(panelWidth, panelHeight);

        var img = rootPanel.gameObject.AddComponent<Image>();
        img.color = panelColor;

        var outline = rootPanel.gameObject.AddComponent<Outline>();
        outline.effectColor = panelOutlineColor;
        outline.effectDistance = new Vector2(2f, -2f);

        var v = rootPanel.gameObject.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(18, 18, 18, 18);
        v.spacing = 12;
        v.childControlWidth = true;
        v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;
    }

    void BuildHeader()
    {
        var header = CreateRect("Header", rootPanel);
        header.gameObject.AddComponent<LayoutElement>().preferredHeight = headerHeight;
        header.gameObject.AddComponent<Image>().color = sectionColor;

        headerTitle = CreateRTLText("Title", header, "AI Maintenance Assistant", titleFontSize, TextAlignmentOptions.Left);
        headerTitle.fontStyle = FontStyles.Bold;
        Stretch(headerTitle.rectTransform, new Vector2(18, 8), new Vector2(-140, -8), new Vector2(0, 0.4f), new Vector2(1, 1));

        headerSub = CreateRTLText("Sub", header, "Hybrid Chat â€¢ Diagnosis â€¢ Guided Steps", subtitleFontSize, TextAlignmentOptions.Left);
        headerSub.color = subTextColor;
        Stretch(headerSub.rectTransform, new Vector2(18, 8), new Vector2(-140, 0), new Vector2(0, 0), new Vector2(1, 0.5f));

        var langRect = CreateRect("LangButton", header);
        Stretch(langRect, Vector2.zero, Vector2.zero, new Vector2(0.86f, 0.2f), new Vector2(0.98f, 0.85f));

        var img = langRect.gameObject.AddComponent<Image>();
        img.color = sendButtonColor;

        btnLang = langRect.gameObject.AddComponent<Button>();
        btnLang.targetGraphic = img;

        var txt = CreateRTLText("LangLabel", langRect, "AR/EN", 20f, TextAlignmentOptions.Center);
        txt.fontStyle = FontStyles.Bold;
        Stretch(txt.rectTransform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one);
    }

    void BuildMessagesArea()
    {
        var row = CreateRect("MessagesRow", rootPanel);
        row.gameObject.AddComponent<LayoutElement>().preferredHeight = messagesHeight;

        var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 8;
        h.childControlWidth = true;
        h.childControlHeight = true;
        h.childForceExpandWidth = false;
        h.childForceExpandHeight = true;

        var scrollRoot = CreateRect("MessagesScroll", row);
        var scrollLE = scrollRoot.gameObject.AddComponent<LayoutElement>();
        scrollLE.flexibleWidth = 1;
        scrollLE.minWidth = 100f;
        scrollLE.preferredWidth = panelWidth - scrollbarWidth - 80f;

        scrollRoot.gameObject.AddComponent<Image>().color = messagesBgColor;

        scrollRect = scrollRoot.gameObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 35f;

        var viewport = CreateRect("Viewport", scrollRoot);
        Stretch(viewport, new Vector2(8, 8), new Vector2(-8, -8), Vector2.zero, Vector2.one);
        viewport.gameObject.AddComponent<Image>().color = new Color(1, 1, 1, 0.01f);
        viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;

        messagesContent = CreateRect("Content", viewport);
        messagesContent.anchorMin = new Vector2(0, 1);
        messagesContent.anchorMax = new Vector2(1, 1);
        messagesContent.pivot = new Vector2(0.5f, 1f);

        var v = messagesContent.gameObject.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(4, 4, 4, 4);
        v.spacing = (int)messageSpacing;
        v.childControlWidth = true;
        v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;

        var fit = messagesContent.gameObject.AddComponent<ContentSizeFitter>();
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = viewport;
        scrollRect.content = messagesContent;

        var sb = CreateRect("Scrollbar", row);
        var sbLe = sb.gameObject.AddComponent<LayoutElement>();
        sbLe.minWidth = scrollbarWidth;
        sbLe.preferredWidth = scrollbarWidth;
        sbLe.flexibleWidth = 0;

        sb.gameObject.AddComponent<Image>().color = new Color(0.12f, 0.14f, 0.18f, 0.40f);

        verticalScrollbar = sb.gameObject.AddComponent<Scrollbar>();
        verticalScrollbar.direction = Scrollbar.Direction.BottomToTop;

        var handle = CreateRect("Handle", sb);
        Stretch(handle, new Vector2(0, 2), new Vector2(0, -2), Vector2.zero, Vector2.one);
        var handleImg = handle.gameObject.AddComponent<Image>();
        handleImg.color = new Color(0.18f, 0.55f, 1f, 0.55f);

        verticalScrollbar.targetGraphic = handleImg;
        verticalScrollbar.handleRect = handle;

        scrollRect.verticalScrollbar = verticalScrollbar;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
        scrollRect.verticalScrollbarSpacing = 0;
    }

    void BuildQuickButtons()
    {
        var row = CreateRect("QuickButtons", rootPanel);
        row.gameObject.AddComponent<LayoutElement>().preferredHeight = quickButtonsHeight;
        row.gameObject.AddComponent<Image>().color = sectionColor;

        var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.padding = new RectOffset(8, 8, 8, 8);
        h.spacing = 8;
        h.childControlWidth = true;
        h.childControlHeight = true;
        h.childForceExpandWidth = true;
        h.childForceExpandHeight = true;

        btnNext = CreateButton(row, "Next Step");
        btnExplain = CreateButton(row, "Explain");
        btnMode = CreateButton(row, "Context Mode");
        btnSafety = CreateButton(row, "Safety");
        btnEvaluate = CreateButton(row, "Final Evaluation");
        btnAttach = CreateButton(row, "Attach Scan");
        btnDoctor = CreateButton(row, "Send To Doctor");
        btnOffline = CreateButton(row, "Go Offline");
        btnGoRobot = CreateButton(row, "Go Robot");
        btnTTS = CreateButton(row, "Sound: ON");
    }

    void BuildTyping()
    {
        var area = CreateRect("TypingArea", rootPanel);
        area.gameObject.AddComponent<LayoutElement>().preferredHeight = typingHeight;

        typingText = CreateRTLText("TypingText", area, "", 18f, TextAlignmentOptions.Left);
        typingText.color = subTextColor;
        Stretch(typingText.rectTransform, new Vector2(8, 0), new Vector2(-8, 0), Vector2.zero, Vector2.one);
    }

    void BuildInput()
    {
        var row = CreateRect("InputArea", rootPanel);
        row.gameObject.AddComponent<LayoutElement>().preferredHeight = inputHeight;

        var imgBg = row.gameObject.AddComponent<Image>();
        imgBg.color = new Color(0, 0, 0, 0);

        var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.padding = new RectOffset(6, 6, 6, 6);
        h.spacing = 12;
        h.childControlWidth = true;
        h.childControlHeight = true;
        h.childForceExpandWidth = false;
        h.childForceExpandHeight = true;

        var inputRoot = CreateRect("InputRoot", row);
        inputRoot.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
        var inputImg = inputRoot.gameObject.AddComponent<Image>();
        inputImg.color = aiBubbleColor;
        inputImg.type = Image.Type.Sliced;
        inputImg.sprite = CreateRoundedRectSprite(24);

        inputField = BuildInputField(inputRoot);

        var voiceRoot = CreateRect("VoiceButton", row);
        var voiceLe = voiceRoot.gameObject.AddComponent<LayoutElement>();
        voiceLe.preferredWidth = sendButtonWidth;
        voiceLe.flexibleWidth = 0;

        var voiceImg = voiceRoot.gameObject.AddComponent<Image>();
        voiceImg.color = userBubbleColor;
        voiceImg.type = Image.Type.Sliced;
        voiceImg.sprite = CreateRoundedRectSprite(24);

        voiceButton = voiceRoot.gameObject.AddComponent<Button>();
        voiceButton.targetGraphic = voiceImg;

        var voiceTxt = CreateRTLText("VoiceLabel", voiceRoot, "ØµÙˆØª", sendButtonFontSize, TextAlignmentOptions.Center);
        voiceTxt.fontStyle = FontStyles.Bold;
        Stretch(voiceTxt.rectTransform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one);

        var sendRoot = CreateRect("SendButton", row);
        var sendLe = sendRoot.gameObject.AddComponent<LayoutElement>();
        sendLe.preferredWidth = sendButtonWidth;
        sendLe.flexibleWidth = 0;

        var sendImg = sendRoot.gameObject.AddComponent<Image>();
        sendImg.color = userBubbleColor;
        sendImg.type = Image.Type.Sliced;
        sendImg.sprite = CreateRoundedRectSprite(24);

        sendButton = sendRoot.gameObject.AddComponent<Button>();
        sendButton.targetGraphic = sendImg;

        var txt = CreateRTLText("SendLabel", sendRoot, "Send", sendButtonFontSize, TextAlignmentOptions.Center);
        txt.fontStyle = FontStyles.Bold;
        Stretch(txt.rectTransform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one);
        var stopRoot = CreateRect("StopButton", row);
        var stopLe = stopRoot.gameObject.AddComponent<LayoutElement>();
        stopLe.preferredWidth = sendButtonWidth;
        stopLe.flexibleWidth = 0;

        var stopImg = stopRoot.gameObject.AddComponent<Image>();
        stopImg.color = new Color(0.8f, 0.15f, 0.25f, 0.9f);
        stopImg.type = Image.Type.Sliced;
        stopImg.sprite = CreateRoundedRectSprite(24);

        btnStop = stopRoot.gameObject.AddComponent<Button>();
        btnStop.targetGraphic = stopImg;

        var stopTxt = CreateRTLText("StopLabel", stopRoot, "Stop", sendButtonFontSize, TextAlignmentOptions.Center);
        stopTxt.fontStyle = FontStyles.Bold;
        Stretch(stopTxt.rectTransform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one);

        stopRoot.gameObject.SetActive(false);
    }

    TMP_InputField BuildInputField(RectTransform parent)
    {
        var root = CreateRect("TMP_InputField", parent);
        Stretch(root, new Vector2(16, 8), new Vector2(-16, -8), Vector2.zero, Vector2.one);
        root.gameObject.AddComponent<Image>().color = new Color(1, 1, 1, 0);

        var field = root.gameObject.AddComponent<TMP_InputField>();
        field.lineType = TMP_InputField.LineType.MultiLineNewline;
        field.characterLimit = 2000;
        field.onFocusSelectAll = false;
        field.customCaretColor = true;
        field.caretColor = textColor;
        field.caretWidth = 3;
        var hook = root.gameObject.AddComponent<VRNativeKeyboardHook>();
        hook.isArabicInputField = (startLanguage == ChatLang.AR);

        var textArea = CreateRect("TextArea", root);
        Stretch(textArea, new Vector2(0, 0), new Vector2(0, 0), Vector2.zero, Vector2.one);
        textArea.gameObject.AddComponent<RectMask2D>();

        var placeholder = CreateRTLText("Placeholder", textArea, "Type your message...", 20f, TextAlignmentOptions.TopLeft);
        Stretch(placeholder.rectTransform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one);
        placeholder.color = new Color(0.8f, 0.85f, 0.95f, 0.35f);

        var hiddenTextGO = new GameObject("HiddenNativeText");
        hiddenTextGO.transform.SetParent(textArea, false);
        var hiddenText = hiddenTextGO.AddComponent<TextMeshProUGUI>();
        hiddenText.text = "";
        hiddenText.fontSize = messageFontSize;
        hiddenText.alignment = TextAlignmentOptions.TopLeft;
        hiddenText.color = new Color(0, 0, 0, 0);
        hiddenText.textWrappingMode = TextWrappingModes.Normal;
        hiddenText.overflowMode = TextOverflowModes.Overflow;
        Stretch(hiddenText.rectTransform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one);

        var visibleTextGO = new GameObject("VisibleRTLText");
        visibleTextGO.transform.SetParent(textArea, false);
        var visibleText = visibleTextGO.AddComponent<RTLTextMeshPro>();
        visibleText.text = "";
        visibleText.fontSize = messageFontSize;
        visibleText.alignment = TextAlignmentOptions.TopLeft;
        visibleText.color = textColor;
        visibleText.isRightToLeftText = true;
        visibleText.Farsi = false;
        visibleText.textWrappingMode = TextWrappingModes.Normal;
        visibleText.overflowMode = TextOverflowModes.Overflow;
        Stretch(visibleText.rectTransform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one);

        if (defaultFont != null)
        {
            hiddenText.font = defaultFont;
            visibleText.font = defaultFont;
        }

        field.textViewport = textArea;
        field.textComponent = hiddenText;
        field.placeholder = placeholder as TextMeshProUGUI;

        var controllerCmp = transform.GetComponentInChildren<ChatControllerRTL>(true);
        if (controllerCmp == null)
        {
            var go = new GameObject("ChatController");
            go.transform.SetParent(transform, false);
            controllerCmp = go.AddComponent<ChatControllerRTL>();
        }

        return field;
    }

    void BuildFooter()
    {
        var row = CreateRect("FooterArea", rootPanel);
        row.gameObject.AddComponent<LayoutElement>().preferredHeight = 36f;

        var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.childAlignment = TextAnchor.MiddleCenter;
        h.childControlWidth = true;
        h.childControlHeight = true;
        h.childForceExpandWidth = false;
        h.childForceExpandHeight = false;
        h.spacing = 8f;

        var logoRect = CreateRect("FooterLogo", row);
        var logoImg = logoRect.gameObject.AddComponent<Image>();
        logoImg.preserveAspect = true;

        var logoLE = logoRect.gameObject.AddComponent<LayoutElement>();
        logoLE.preferredHeight = 24f;
        logoLE.minHeight = 24f;

        if (teamLogo != null)
        {
            logoImg.sprite = teamLogo;
            float aspect = teamLogo.rect.width / teamLogo.rect.height;
            logoLE.preferredWidth = 24f * aspect;
            logoLE.minWidth = 24f * aspect;
        }
        else
        {
            logoLE.preferredWidth = 24f;
            logoLE.minWidth = 24f;
        }

        footerText = CreateRTLText("FooterText", row, "Built with care by TechNest Team", 16f, TextAlignmentOptions.Right);
        footerText.color = new Color(subTextColor.r, subTextColor.g, subTextColor.b, 0.5f);
        Stretch(footerText.rectTransform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one);
    }

    void BuildRowPrefab()
    {
        var root = new GameObject("RowPrefab");
        root.SetActive(false);

        root.AddComponent<RectTransform>();
        var rootLE = root.AddComponent<LayoutElement>();
        rootLE.minHeight = 20f;

        var h = root.AddComponent<HorizontalLayoutGroup>();
        h.childControlWidth = true;
        h.childControlHeight = true;
        h.childForceExpandWidth = true;
        h.childForceExpandHeight = false;
        h.spacing = 0;

        var left = new GameObject("LeftSpacer");
        left.transform.SetParent(root.transform, false);
        var leftLE = left.AddComponent<LayoutElement>();
        leftLE.flexibleWidth = 1;

        var bubble = new GameObject("Bubble");
        bubble.transform.SetParent(root.transform, false);

        var bubbleImg = bubble.AddComponent<Image>();
        bubbleImg.color = aiBubbleColor;
        bubbleImg.type = Image.Type.Sliced;
        bubbleImg.sprite = CreateRoundedRectSprite(24);

        var shadow = bubble.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.2f);
        shadow.effectDistance = new Vector2(2f, -2f);

        var bubbleLE = bubble.AddComponent<LayoutElement>();
        bubbleLE.preferredWidth = bubbleWidth;
        bubbleLE.minHeight = messageMinHeight;
        bubbleLE.flexibleWidth = 0;

        var bubbleV = bubble.AddComponent<VerticalLayoutGroup>();
        bubbleV.padding = new RectOffset((int)messagePaddingHorizontal, (int)messagePaddingHorizontal, (int)messagePaddingVertical, (int)messagePaddingVertical);
        bubbleV.childControlWidth = true;
        bubbleV.childControlHeight = true;
        bubbleV.childForceExpandWidth = true;
        bubbleV.childForceExpandHeight = false;

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(bubble.transform, false);

        var txt = textGO.AddComponent<RTLTextMeshPro>();
        txt.text = "";
        txt.fontSize = messageFontSize;
        txt.alignment = TextAlignmentOptions.TopLeft;
        txt.textWrappingMode = TextWrappingModes.Normal;
        txt.overflowMode = TextOverflowModes.Overflow;
        txt.color = textColor;
        txt.isRightToLeftText = true;
        txt.Farsi = false;
        if (defaultFont != null) txt.font = defaultFont;

        var right = new GameObject("RightSpacer");
        right.transform.SetParent(root.transform, false);
        var rightLE = right.AddComponent<LayoutElement>();
        rightLE.flexibleWidth = 1;

        var avatar = new GameObject("Avatar");
        avatar.transform.SetParent(root.transform, false);
        var avatarImg = avatar.AddComponent<Image>();
        avatarImg.preserveAspect = true;

        var avatarLE = avatar.AddComponent<LayoutElement>();
        avatarLE.preferredHeight = 45f;
        avatarLE.minHeight = 45f;

        if (teamLogo != null)
        {
            avatarImg.sprite = teamLogo;
            float aspect = teamLogo.rect.width / teamLogo.rect.height;
            avatarLE.preferredWidth = 45f * aspect;
            avatarLE.minWidth = 45f * aspect;
        }
        else
        {
            avatarLE.preferredWidth = 45f;
            avatarLE.minWidth = 45f;
        }

        avatarLE.flexibleWidth = 0;
        avatarLE.flexibleHeight = 0;
        avatar.transform.SetAsLastSibling();

        rowPrefab = root.AddComponent<ChatMessageRowPrefabRTL>();
        rowPrefab.rootLE = rootLE;
        rowPrefab.leftSpacer = leftLE;
        rowPrefab.rightSpacer = rightLE;
        rowPrefab.bubbleLE = bubbleLE;
        rowPrefab.bubbleImg = bubbleImg;
        rowPrefab.avatarImg = avatarImg;
        rowPrefab.avatarLE = avatarLE;
        rowPrefab.text = txt;
        rowPrefab.userBubbleColor = userBubbleColor;
        rowPrefab.aiBubbleColor = aiBubbleColor;
        rowPrefab.textPaddingH = messagePaddingHorizontal;
        rowPrefab.textPaddingV = messagePaddingVertical;
    }

    void BuildController()
    {
        var go = new GameObject("ChatController");
        go.transform.SetParent(transform, false);

        controller = go.AddComponent<ChatControllerRTL>();
        controller.defaultFont = defaultFont;
        controller.startLanguage = startLanguage;

        controller.headerTitle = headerTitle;
        controller.headerSub = headerSub;

        controller.inputField = inputField;
        controller.sendButton = sendButton;
        controller.btnStop = btnStop;
        controller.voiceButton = voiceButton;
        controller.scrollRect = scrollRect;
        controller.messagesParent = messagesContent;
        controller.rowPrefab = rowPrefab;
        controller.typingText = typingText;
        controller.footerText = footerText;

        controller.btnNext = btnNext;
        controller.btnExplain = btnExplain;
        controller.btnMode = btnMode;
        controller.btnSafety = btnSafety;
        controller.btnEvaluate = btnEvaluate;
        controller.btnAttach = btnAttach;
        controller.btnDoctor = btnDoctor;
        controller.btnOffline = btnOffline;
        controller.btnLang = btnLang;
        controller.btnGoRobot = btnGoRobot;
        controller.btnTTS = btnTTS;

        controller.faultType = faultType;
        controller.currentStep = currentStep;
        controller.pressure = pressure;
        controller.temp = temp;
        controller.vibration = vibration;
        controller.leak = leak;

        controller.Init();
    }

    RectTransform CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.localScale = Vector3.one;
        return rt;
    }

    void Stretch(RectTransform rt, Vector2 offsetMin, Vector2 offsetMax, Vector2 anchorMin, Vector2 anchorMax)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
    }

    RTLTextMeshPro CreateRTLText(string name, Transform parent, string value, float fontSize, TextAlignmentOptions alignment)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var t = go.AddComponent<RTLTextMeshPro>();
        t.text = value;
        t.fontSize = fontSize;
        t.alignment = alignment;
        t.color = textColor;
        t.isRightToLeftText = true;
        t.Farsi = false;
        if (defaultFont != null) t.font = defaultFont;

        return t;
    }

    Button CreateButton(RectTransform parent, string label)
    {
        var go = new GameObject(label.Replace(" ", "") + "Btn");
        go.transform.SetParent(parent, false);

        var img = go.AddComponent<Image>();
        img.color = buttonColor;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        var txt = CreateRTLText("Label", go.transform, label, mainButtonFontSize, TextAlignmentOptions.Center);
        txt.fontStyle = FontStyles.Bold;
        Stretch(txt.rectTransform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one);

        return btn;
    }

    Sprite CreateRoundedRectSprite(int radius)
    {
        int size = radius * 3;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                if ((x < radius && y < radius && Vector2.Distance(new Vector2(x, y), new Vector2(radius, radius)) > radius) ||
                    (x > size - radius && y < radius && Vector2.Distance(new Vector2(x, y), new Vector2(size - radius, radius)) > radius) ||
                    (x < radius && y > size - radius && Vector2.Distance(new Vector2(x, y), new Vector2(radius, size - radius)) > radius) ||
                    (x > size - radius && y > size - radius && Vector2.Distance(new Vector2(x, y), new Vector2(size - radius, size - radius)) > radius))
                {
                    pixels[y * size + x] = new Color(1, 1, 1, 0);
                }
                else
                {
                    pixels[y * size + x] = Color.white;
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
    }

    void EnsureVRRaycasters(GameObject canvasGo)
    {
        System.Type xrRaycaster = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster, Unity.XR.Interaction.Toolkit");
        if (xrRaycaster != null && canvasGo.GetComponent(xrRaycaster) == null)
        {
            canvasGo.AddComponent(xrRaycaster);
        }

        System.Type ovrRaycaster = System.Type.GetType("OVRRaycaster, Assembly-CSharp");
        if (ovrRaycaster != null && canvasGo.GetComponent(ovrRaycaster) == null)
        {
            canvasGo.AddComponent(ovrRaycaster);
        }
    }
}

public class ChatMessageRowPrefabRTL : MonoBehaviour
{
    public LayoutElement rootLE;
    public LayoutElement leftSpacer;
    public LayoutElement rightSpacer;
    public LayoutElement bubbleLE;
    public Image bubbleImg;
    public RTLTextMeshPro text;
    public Image avatarImg;
    public LayoutElement avatarLE;

    public Color userBubbleColor;
    public Color aiBubbleColor;
    public float textPaddingH;
    public float textPaddingV;

    public void Set(bool isUser, string content, TMP_FontAsset font, bool rtl)
    {
        if (font != null) text.font = font;
        text.text = content;
        text.alignment = rtl ? TextAlignmentOptions.TopRight : TextAlignmentOptions.TopLeft;

        bubbleImg.color = isUser ? userBubbleColor : aiBubbleColor;
        avatarImg.gameObject.SetActive(!isUser);

        if (isUser)
        {
            leftSpacer.flexibleWidth = 1;
            rightSpacer.flexibleWidth = 0;
        }
        else
        {
            if (rtl)
            {
                avatarImg.transform.SetAsLastSibling();
                leftSpacer.flexibleWidth = 1;
                rightSpacer.flexibleWidth = 0;
            }
            else
            {
                avatarImg.transform.SetAsFirstSibling();
                leftSpacer.flexibleWidth = 0;
                rightSpacer.flexibleWidth = 1;
            }
        }

        Canvas.ForceUpdateCanvases();
        text.ForceMeshUpdate();

        float usableWidth = Mathf.Max(180f, bubbleLE.preferredWidth - (textPaddingH * 2f));
        Vector2 pref = text.GetPreferredValues(text.text, usableWidth, 0f);
        float finalHeight = Mathf.Max(52f, pref.y + (textPaddingV * 2f));

        bubbleLE.minHeight = finalHeight;
        bubbleLE.preferredHeight = finalHeight;
        rootLE.minHeight = finalHeight;
        rootLE.preferredHeight = finalHeight;

        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);
    }
}

public class ChatControllerRTL : MonoBehaviour
{
    [Header("Font")]
    public TMP_FontAsset defaultFont;

    [Header("Language")]
    public ChatLang startLanguage = ChatLang.EN;
    public ChatLang currentLanguage = ChatLang.EN;

    [Header("UI")]
    public RTLTextMeshPro headerTitle;
    public RTLTextMeshPro headerSub;
    public TMP_InputField inputField;
    public Button sendButton;
    public Button btnStop;
    public Button voiceButton;
    public ScrollRect scrollRect;
    public Transform messagesParent;
    public ChatMessageRowPrefabRTL rowPrefab;
    public RTLTextMeshPro typingText;
    public RTLTextMeshPro footerText;

    public Button btnNext;
    public Button btnExplain;
    public Button btnMode;
    public Button btnSafety;
    public Button btnEvaluate;
    public Button btnAttach;
    public Button btnDoctor;
    public Button btnOffline;
    public Button btnLang;
    public Button btnGoRobot;
    public Button btnTTS;

    bool isVoiceOverEnabled = true;

    [Header("Context")]
    public string faultType;
    public string currentStep;
    public float pressure;
    public float temp;
    public float vibration;
    public bool leak;

    bool busy;
    Coroutine typingRoutine;
    Coroutine currentRequestCoroutine;
    UnityWebRequest currentRequest;
    bool isFreeMode = false;
    bool isTestOffline = false;
    public GeminiVoiceToText voiceToText;

    public void Init()
    {
        currentLanguage = startLanguage;

        voiceToText = gameObject.AddComponent<GeminiVoiceToText>();
        voiceToText.Setup(
            "",
            onComplete: (text) =>
            {
                inputField.text += (inputField.text.Length > 0 ? " " : "") + text;
                UpdateVoiceUI();
            },
            onStart: UpdateVoiceUI,
            onStop: UpdateVoiceUI,
            onErr: (err) =>
            {
                Debug.LogError(err);
                UpdateVoiceUI();
            }
        );

        voiceButton.onClick.AddListener(() => voiceToText.ToggleRecording());
        sendButton.onClick.AddListener(SendMessageFromInput);
        if (btnStop != null) btnStop.onClick.AddListener(CancelAIRequest);
        btnNext.onClick.AddListener(() => OnQuickAction("NEXT"));
        btnExplain.onClick.AddListener(() => OnQuickAction("EXPLAIN"));
        btnSafety.onClick.AddListener(() => OnQuickAction("SAFETY"));
        btnEvaluate.onClick.AddListener(OnRequestEvaluation);
        btnAttach.onClick.AddListener(OnAttachScan);
        btnDoctor.onClick.AddListener(OnSendToDoctor);
        btnOffline.onClick.AddListener(ToggleOfflineTest);
        btnLang.onClick.AddListener(ToggleLanguage);
        btnMode.onClick.AddListener(ToggleMode);
        if (btnGoRobot != null) btnGoRobot.onClick.AddListener(OnGoRobot);
        if (btnTTS != null) btnTTS.onClick.AddListener(ToggleTTS);

        inputField.onValueChanged.AddListener(OnInputChanged);

        ApplyLanguage();
        UpdateVoiceUI();
        AddAI(T("READY"));
    }

    void UpdateVoiceUI()
    {
        bool rtl = currentLanguage == ChatLang.AR;
        if (voiceToText != null && voiceToText.IsRecording())
        {
            var img = voiceButton.GetComponent<Image>();
            if (img != null) img.color = new Color(0.85f, 0.15f, 0.25f, 0.95f);
            var txt = voiceButton.GetComponentInChildren<RTLTextMeshPro>();
            if (txt != null)
            {
                txt.isRightToLeftText = rtl;
                txt.text = rtl ? "ØªÙˆÙ‚Ù" : "Stop";
            }
        }
        else
        {
            var img = voiceButton.GetComponent<Image>();
            if (img != null) img.color = rowPrefab.userBubbleColor;
            var txt = voiceButton.GetComponentInChildren<RTLTextMeshPro>();
            if (txt != null)
            {
                txt.isRightToLeftText = rtl;
                txt.text = rtl ? "ØµÙˆØª" : "Voice";
            }
        }
    }

    void OnInputChanged(string text)
    {
        if (inputField != null && inputField.textViewport != null)
        {
            var visibleTextTransform = inputField.textViewport.Find("VisibleRTLText");
            if (visibleTextTransform != null)
            {
                var visibleText = visibleTextTransform.GetComponent<RTLTextMeshPro>();
                if (visibleText != null)
                {
                    visibleText.text = text;
                }
            }
        }
    }

    void ToggleMode()
    {
        isFreeMode = !isFreeMode;
        ApplyLanguage();

        string modeMsgEn = isFreeMode ? "Switched to Free Mode." : "Switched to Context Mode.";
        string modeMsgAr = isFreeMode ? "ØªÙ… Ø§Ù„ØªØ­ÙˆÙŠÙ„ Ø¥Ù„Ù‰ Ø§Ù„ÙˆØ¶Ø¹ Ø§Ù„Ø­Ø±." : "ØªÙ… Ø§Ù„ØªØ­ÙˆÙŠÙ„ Ø¥Ù„Ù‰ ÙˆØ¶Ø¹ Ø§Ù„Ø³ÙŠØ§Ù‚.";

        AddAI(currentLanguage == ChatLang.EN ? modeMsgEn : modeMsgAr);
    }

    void ToggleOfflineTest()
    {
        isTestOffline = !isTestOffline;
        string msgEn = isTestOffline ? "Simulated Offline Mode Enabled." : "Simulated Offline Mode Disabled.";
        string msgAr = isTestOffline ? "ØªÙ… ØªÙØ¹ÙŠÙ„ Ù…Ø­Ø§ÙƒØ§Ø© ÙˆØ¶Ø¹ Ø¹Ø¯Ù… Ø§Ù„Ø§ØªØµØ§Ù„." : "ØªÙ… Ø¥Ù„ØºØ§Ø¡ Ù…Ø­Ø§ÙƒØ§Ø© ÙˆØ¶Ø¹ Ø¹Ø¯Ù… Ø§Ù„Ø§ØªØµØ§Ù„.";
        AddAI(currentLanguage == ChatLang.EN ? msgEn : msgAr);
        ApplyLanguage();
    }

    void ToggleTTS()
    {
        isVoiceOverEnabled = !isVoiceOverEnabled;
        ApplyLanguage();

        if (!isVoiceOverEnabled)
        {
            var src = gameObject.GetComponent<AudioSource>();
            if (src != null) src.Stop();
        }
    }

    void ToggleLanguage()
    {
        currentLanguage = currentLanguage == ChatLang.EN ? ChatLang.AR : ChatLang.EN;
        ApplyLanguage();
        AddAI(currentLanguage == ChatLang.EN ? "Switched to English." : "ØªÙ… Ø§Ù„ØªØ­ÙˆÙŠÙ„ Ø¥Ù„Ù‰ Ø§Ù„Ø¹Ø±Ø¨ÙŠØ©.");
    }

    void ApplyLanguage()
    {
        bool rtl = currentLanguage == ChatLang.AR;

        headerTitle.isRightToLeftText = rtl;
        headerSub.isRightToLeftText = rtl;
        if (footerText != null) footerText.isRightToLeftText = rtl;

        headerTitle.text = T("TITLE");
        headerSub.text = T("SUB");
        if (footerText != null) footerText.text = T("FOOTER");

        SetButtonText(sendButton, T("SEND"), rtl);
        if (btnStop != null) SetButtonText(btnStop, T("STOP"), rtl);
        SetButtonText(btnSafety, T("SAFETY"), rtl);
        SetButtonText(btnEvaluate, T("EVALUATE"), rtl);
        SetButtonText(btnNext, T("NEXT"), rtl);
        SetButtonText(btnExplain, T("EXPLAIN"), rtl);
        SetButtonText(btnMode, T(isFreeMode ? "MODE_FREE" : "MODE_CONTEXT"), rtl);
        SetButtonText(btnSafety, T("SAFETY"), rtl);
        SetButtonText(btnAttach, T("ATTACH"), rtl);
        SetButtonText(btnDoctor, T("DOCTOR"), rtl);
        SetButtonText(btnOffline, T(isTestOffline ? "GO_ONLINE" : "GO_OFFLINE"), rtl);
        if (btnGoRobot != null) SetButtonText(btnGoRobot, T("GO_ROBOT"), rtl);
        if (btnTTS != null) SetButtonText(btnTTS, T(isVoiceOverEnabled ? "TTS_ON" : "TTS_OFF"), rtl);

        UpdateVoiceUI();

        inputField.textComponent.alignment = rtl ? TextAlignmentOptions.TopRight : TextAlignmentOptions.TopLeft;

        var visibleTextTransform = inputField.textViewport.Find("VisibleRTLText");
        if (visibleTextTransform != null)
        {
            var visibleText = visibleTextTransform.GetComponent<RTLTextMeshPro>();
            if (visibleText != null)
            {
                visibleText.isRightToLeftText = rtl;
                visibleText.alignment = rtl ? TextAlignmentOptions.TopRight : TextAlignmentOptions.TopLeft;
            }
        }

        if (inputField.placeholder is RTLTextMeshPro ph)
        {
            ph.isRightToLeftText = rtl;
            ph.alignment = rtl ? TextAlignmentOptions.TopRight : TextAlignmentOptions.TopLeft;
            ph.text = T("PLACEHOLDER");
        }
    }

    void SetButtonText(Button btn, string value, bool rtl)
    {
        var txt = btn.GetComponentInChildren<RTLTextMeshPro>(true);
        if (txt != null)
        {
            txt.isRightToLeftText = rtl;
            txt.text = value;
        }
    }

    void SendMessageFromInput()
    {
        if (busy) return;
        if (string.IsNullOrWhiteSpace(inputField.text)) return;

        string msg = inputField.text;
        inputField.text = "";

        inputField.ActivateInputField();
        bool isArabic = false;
        foreach (char c in msg) if (c >= 0x0600 && c <= 0x06FF) { isArabic = true; break; }

        ChatLang detectedLang = isArabic ? ChatLang.AR : ChatLang.EN;
        if (currentLanguage != detectedLang)
        {
            currentLanguage = detectedLang;
            ApplyLanguage();
        }

        AddUser(msg);
        if (ActivityLogger.Instance != null) ActivityLogger.Instance.LogEvent("Chat_Message_Sent", msg);

        if (currentRequestCoroutine != null) StopCoroutine(currentRequestCoroutine);
        currentRequestCoroutine = StartCoroutine(SendGeminiRequest(msg));
    }

    public void CancelAIRequest()
    {
        if (!busy) return;

        if (currentRequestCoroutine != null) StopCoroutine(currentRequestCoroutine);
        if (currentRequest != null)
        {
            currentRequest.Abort();
            currentRequest.Dispose();
            currentRequest = null;
        }

        StopTyping();
        busy = false;
        UpdateInputState();

        AddAI(currentLanguage == ChatLang.AR ? "<color=#ff4d4d>[ØªÙ… Ø¥ÙŠÙ‚Ø§Ù ØªÙˆÙ„ÙŠØ¯ Ø§Ù„Ø±Ø¯ Ø¨Ù†Ø§Ø¡Ù‹ Ø¹Ù„Ù‰ Ø·Ù„Ø¨Ùƒ]</color>" : "<color=#ff4d4d>[AI generation stopped by user]</color>");
    }

    void UpdateInputState()
    {
        if (sendButton != null) sendButton.gameObject.SetActive(!busy);
        if (btnStop != null) btnStop.gameObject.SetActive(busy);
        if (voiceButton != null) voiceButton.interactable = !busy;
    }

    void OnQuickAction(string key)
    {
        string text = T(key);
        if (ActivityLogger.Instance != null)
            ActivityLogger.Instance.LogEvent("Chat_QuickAction", text);

        AddUser(text);
        if (currentRequestCoroutine != null) StopCoroutine(currentRequestCoroutine);
        currentRequestCoroutine = StartCoroutine(SendGeminiRequest(text));
    }

    void OnAttachScan()
    {
        if (busy) return;
        StartCoroutine(CaptureAndAnalyzeScreen());
    }

    IEnumerator CaptureAndAnalyzeScreen()
    {
        yield return new WaitForEndOfFrame();
        Texture2D screenTx = ScreenCapture.CaptureScreenshotAsTexture();
        byte[] imgBytes = screenTx.EncodeToJPG(75);
        Destroy(screenTx);

        string msg = currentLanguage == ChatLang.EN
            ? $"Please analyze this view.\nFault: {faultType}\nStep: {currentStep}\nPressure: {pressure}\nTemp: {temp}\nVibration: {vibration}\nLeak: {leak}"
            : $"Ø§Ù„Ø±Ø¬Ø§Ø¡ ØªØ­Ù„ÙŠÙ„ Ù‡Ø°Ù‡ Ø§Ù„Ø´Ø§Ø´Ø©.\nØ§Ù„Ø¹Ø·Ù„: {faultType}\nØ§Ù„Ù…Ø±Ø­Ù„Ø©: {currentStep}\nØ§Ù„Ø¶ØºØ·: {pressure}\nØ§Ù„Ø­Ø±Ø§Ø±Ø©: {temp}\nØ§Ù„Ø§Ù‡ØªØ²Ø§Ø²: {vibration}\nØªØ³Ø±ÙŠØ¨: {leak}";
        if (ActivityLogger.Instance != null)
            ActivityLogger.Instance.LogEvent("Analyze_View_Triggered", "Trainee requested visual AI analysis of the current view.");

        AddUser(msg);
        if (currentRequestCoroutine != null) StopCoroutine(currentRequestCoroutine);
        currentRequestCoroutine = StartCoroutine(SendGeminiRequest(msg, imgBytes));
    }


    void OnSendToDoctor()
    {
        var exporter = gameObject.GetComponent<GameAPIExporter>();
        if (exporter == null)
        {
            exporter = gameObject.AddComponent<GameAPIExporter>();
        }
        exporter.traineeName = PlayerPrefs.GetString("TraineeName", "Guest Trainee");
        exporter.doctorCode = PlayerPrefs.GetString("DoctorCode", "DOC-0000");

        string fullChatLog = "";
        foreach (Transform child in messagesParent)
        {
            var txtObj = child.GetComponentInChildren<RTLTextMeshPro>(true);
            if (txtObj != null) fullChatLog += txtObj.text + "\n";
        }
        if (ActivityLogger.Instance != null)
            ActivityLogger.Instance.LogEvent("Report_Sent_To_Doctor", "Trainee submitted their final report and chat log to the portal.");

        exporter.SubmitReport(faultType, pressure, temp, vibration, leak, fullChatLog);
        string msg;
        if (currentLanguage == ChatLang.EN)
        {
            msg = $"Doctor Evaluation Request:\nPlease evaluate my performance based on this data:\nFault: {faultType}\nStep: {currentStep}\nPressure: {pressure}\nTemp: {temp}\nVibration: {vibration}\nLeak: {leak}";
        }
        else
        {
            msg = $"Ø·Ù„Ø¨ ØªÙ‚ÙŠÙŠÙ… Ù…Ù† Ø§Ù„Ø¯ÙƒØªÙˆØ±:\nØ£Ø±Ø¬Ùˆ ØªÙ‚ÙŠÙŠÙ… Ø£Ø¯Ø§Ø¦ÙŠ Ø¨Ù†Ø§Ø¡Ù‹ Ø¹Ù„Ù‰ Ù‡Ø°Ù‡ Ø§Ù„Ø¨ÙŠØ§Ù†Ø§Øª:\nØ§Ù„Ø¹Ø·Ù„: {faultType}\nØ§Ù„Ù…Ø±Ø­Ù„Ø©: {currentStep}\nØ§Ù„Ø¶ØºØ·: {pressure}\nØ§Ù„Ø­Ø±Ø§Ø±Ø©: {temp}\nØ§Ù„Ø§Ù‡ØªØ²Ø§Ø²: {vibration}\nØªØ³Ø±ÙŠØ¨: {leak}";
        }

        AddUser(msg);
        if (currentRequestCoroutine != null) StopCoroutine(currentRequestCoroutine);
        currentRequestCoroutine = StartCoroutine(SendGeminiRequest(msg));
    }

    void OnGoRobot()
    {
        if (busy) return;

        var robot = Object.FindFirstObjectByType<RobotController>();
        if (robot != null)
        {
            robot.TriggerRobotAIMovement();
            AddAI(currentLanguage == ChatLang.AR ? "ØªÙ… Ø¥Ø±Ø³Ø§Ù„ Ø§Ù„Ø±ÙˆØ¨ÙˆØª Ù„Ù„Ù…Ø³Ø§Ø± Ø§Ù„Ù…Ø­Ø¯Ø¯." : "Robot sent to the specified path.");
        }
        else
        {
            AddAI(currentLanguage == ChatLang.AR ? "Ù„Ù… ÙŠØªÙ… Ø§Ù„Ø¹Ø«ÙˆØ± Ø¹Ù„Ù‰ Ø§Ù„Ø±ÙˆØ¨ÙˆØª ÙÙŠ Ø§Ù„Ù…Ø´Ù‡Ø¯!" : "Robot not found in the scene!");
        }
    }

    IEnumerator SendGeminiRequest(string userText, byte[] imageBytes = null)
    {
        busy = true;
        UpdateInputState();
        StartTyping();
        if (isTestOffline || Application.internetReachability == NetworkReachability.NotReachable)
        {
            yield return new WaitForSeconds(1.5f);
            StopTyping();
            string localReply = LocalAirplaneBot.GetResponse(userText, currentLanguage, faultType, currentStep, pressure, temp, vibration, leak);
            AddAI(localReply);
            busy = false;
            UpdateInputState();
            yield break;
        }

        string contextPrompt = "";
        string traineeName = PlayerPrefs.GetString("TraineeName", "Trainee");

        if (isFreeMode)
        {
            contextPrompt = currentLanguage == ChatLang.AR
                ? $"Ø£Ù†Øª Ù…Ø³Ø§Ø¹Ø¯ ØµÙŠØ§Ù†Ø© Ø·ÙŠØ±Ø§Ù† Ø°ÙƒÙŠ. Ø§Ù„Ù…ØªØ¯Ø±Ø¨ Ø§Ù„Ø°ÙŠ ØªØªØ­Ø¯Ø« Ù…Ø¹Ù‡ Ø§Ù„Ø¢Ù† Ø§Ø³Ù…Ù‡ '{traineeName}'. Ø£Ø¬Ø¨ Ø¹Ù„Ù‰ Ø£Ø³Ø¦Ù„ØªÙ‡ Ø¨Ø·Ø±ÙŠÙ‚Ø© ÙˆØ¯ÙŠØ© ÙˆØ§Ø­ØªØ±Ø§ÙÙŠØ© ÙˆÙ†Ø§Ø¯ÙŠÙ‡ Ø¨Ø§Ø³Ù…Ù‡.\nØ³Ø¤Ø§Ù„ Ø§Ù„Ù…ØªØ¯Ø±Ø¨: "
                : $"You are a smart aviation maintenance assistant. The trainee you are talking to is named '{traineeName}'. Answer warmly, professionally, and use their name.\nTrainee Question: ";
        }
        else
        {
            contextPrompt = currentLanguage == ChatLang.AR
                ? $"Ø£Ù†Øª Ù…Ø³Ø§Ø¹Ø¯ ØµÙŠØ§Ù†Ø© Ø·ÙŠØ±Ø§Ù† Ø°ÙƒÙŠ. Ø§Ù„Ù…ØªØ¯Ø±Ø¨ Ø§Ø³Ù…Ù‡ '{traineeName}'. Ù‚Ù… Ø¨Ø§Ù„Ø±Ø¯ Ø¨Ø£Ø³Ù„ÙˆØ¨ Ø§Ø­ØªØ±Ø§ÙÙŠ Ø¨Ù†Ø§Ø¡Ù‹ Ø¹Ù„Ù‰ Ù‡Ø°Ø§ Ø§Ù„Ø³ÙŠØ§Ù‚ ÙˆÙ†Ø§Ø¯ÙŠÙ‡ Ø¨Ø§Ø³Ù…Ù‡:\nØ§Ù„Ø¹Ø·Ù„: {faultType}\nØ§Ù„Ù…Ø±Ø­Ù„Ø©: {currentStep}\nØ§Ù„Ø¶ØºØ·: {pressure}\nØ§Ù„Ø­Ø±Ø§Ø±Ø©: {temp}\nØ§Ù„Ø§Ù‡ØªØ²Ø§Ø²: {vibration}\nØ³Ø¤Ø§Ù„ Ø§Ù„Ù…ØªØ¯Ø±Ø¨: "
                : $"You are a smart aviation maintenance assistant. Trainee's name is '{traineeName}'. Reply professionally using their name based on this context:\nFault: {faultType}\nStep: {currentStep}\nPressure: {pressure}\nTemp: {temp}\nVibration: {vibration}\nTrainee Question: ";
        }

        string finalPrompt = contextPrompt + userText;

        string url = "https://aerotwin.onrender.com/api/ai/proxy";

        string jsonPayload;
        string aiProvider = PlayerPrefs.GetString("AI_Provider", "gemini");

        if (imageBytes != null && aiProvider != "deepseek")
        {
            string base64Image = System.Convert.ToBase64String(imageBytes);
            jsonPayload = "{\"contents\": [{\"parts\": [{\"text\": \"" + EscapeJsonString(finalPrompt) + "\"}, {\"inlineData\": {\"mimeType\": \"image/jpeg\", \"data\": \"" + base64Image + "\"}}]}]}";
        }
        else
        {
            jsonPayload = "{\"contents\": [{\"parts\": [{\"text\": \"" + EscapeJsonString(finalPrompt) + "\"}]}]}";
        }

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        currentRequest = request;

        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("x-app-secret", "AeroTwin_Device_Stream_Secure_Key_9988");
        request.SetRequestHeader("x-ai-provider", PlayerPrefs.GetString("AI_Provider", "gemini"));
        int maxAttempts = 3;
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            yield return request.SendWebRequest();

            bool isOverloaded = request.responseCode == 503 ||
                (request.result == UnityWebRequest.Result.Success &&
                 request.downloadHandler.text.Contains("high demand"));

            if (!isOverloaded || attempt >= maxAttempts) break;

            Debug.LogWarning($"[Ghost Instructor] API overloaded (attempt {attempt}/{maxAttempts}). Retrying in 3s...");
            yield return new WaitForSeconds(3f);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
        }

        StopTyping();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string aiProv = request.GetResponseHeader("X-AI-Provider-Used");
            if (!string.IsNullOrEmpty(aiProv)) Debug.Log("<color=magenta>[AI System - Chat] Active Model Used: " + aiProv.ToUpper() + "</color>");

            string responseJson = request.downloadHandler.text;
            if (responseJson.Contains("high demand") || responseJson.Contains("503"))
            {
                AddAI(currentLanguage == ChatLang.AR
                    ? "Ø§Ù„Ø®Ø¯Ù…Ø© Ù…Ø´ØºÙˆÙ„Ø© Ø­Ø§Ù„ÙŠØ§Ù‹ØŒ Ø§Ù„Ø±Ø¬Ø§Ø¡ Ø§Ù„Ù…Ø­Ø§ÙˆÙ„Ø© Ù…Ø¬Ø¯Ø¯Ø§Ù‹ Ø¨Ø¹Ø¯ Ù„Ø­Ø¸Ø©."
                    : "The AI service is currently busy. Please try again in a moment.");
            }
            else
            {
                string replyText = ExtractGeminiResponse(responseJson);
                AddAI(replyText);
            }
        }
        else
        {
            Debug.LogError("[Ghost Instructor] API Failure: " + request.error + "\n" + request.downloadHandler.text);
            AddAI(currentLanguage == ChatLang.AR ? "Ø¹Ø°Ø±Ù‹Ø§ØŒ Ø­Ø¯Ø« Ø®Ø·Ø£ ÙÙŠ Ø§Ù„Ø§ØªØµØ§Ù„ Ø¨Ø§Ù„Ø®Ø¯Ù…Ø©." : "Sorry, an error occurred while connecting to the service.");
        }

        request.Dispose();
        currentRequest = null;
        busy = false;
        UpdateInputState();
    }

    string EscapeJsonString(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        return text.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
    }

    string ExtractGeminiResponse(string json)
    {
        try
        {
            if (json.Contains("\"error\"")) return "AI Error: " + json;
            int textKeyIdx = json.IndexOf("\"text\"");
            if (textKeyIdx != -1)
            {
                int colonIdx = json.IndexOf(":", textKeyIdx);
                if (colonIdx != -1)
                {
                    int startQuoteIdx = json.IndexOf("\"", colonIdx);
                    if (startQuoteIdx != -1)
                    {
                        int startIndex = startQuoteIdx + 1;
                        int endIndex = -1;
                        for (int i = startIndex; i < json.Length; i++)
                        {
                            if (json[i] == '\"' && json[i - 1] != '\\')
                            {
                                endIndex = i;
                                break;
                            }
                        }

                        if (endIndex != -1)
                        {
                            string rawText = json.Substring(startIndex, endIndex - startIndex);
                            return rawText.Replace("\\\"", "\"").Replace("\\n", "\n").Replace("\\r", "\r").Replace("\\\\", "\\").Trim();
                        }
                    }
                }
            }
        }
        catch (System.Exception e) { Debug.LogError("Parse Err: " + e.Message); }

        return currentLanguage == ChatLang.AR ? "ÙØ´Ù„ ØªØ­Ù„ÙŠÙ„ Ø§Ù„Ø±Ø¯. Ø§Ù„ØªÙØ§ØµÙŠÙ„: " + json : "Failed to parse response. Raw: " + json;
    }
    void AddUser(string msg)
    {
        string sender = currentLanguage == ChatLang.EN ? "You" : "Ø£Ù†Øª";
        AddMessage(sender, msg, true);
    }

    public void AddAI(string msg)
    {
        string sender = currentLanguage == ChatLang.EN ? "Tec" : "ØªÙŠÙƒ";
        AddMessage(sender, msg, false);
        StartCoroutine(PlayTTS(msg));
    }

    IEnumerator PlayTTS(string text)
    {
        if (!isVoiceOverEnabled) yield break;

        string langCode = currentLanguage == ChatLang.AR ? "ar" : "en";
        if (text.Length > 190) text = text.Substring(0, 190);

        string url = $"https://translate.google.com/translate_tts?ie=UTF-8&client=tw-ob&q={System.Uri.EscapeDataString(text)}&tl={langCode}";
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG))
        {
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success)
            {
                var clip = DownloadHandlerAudioClip.GetContent(www);
                var src = gameObject.GetComponent<AudioSource>();
                if (src == null) src = gameObject.AddComponent<AudioSource>();

                src.Stop();
                src.clip = clip;
                src.Play();
            }
        }
    }

    void AddMessage(string sender, string content, bool isUser)
    {
        bool rtl = currentLanguage == ChatLang.AR;

        string final;
        final = "<color=#5ac8fa><b>" + sender + "</b></color>\n" + content;

        var row = Instantiate(rowPrefab, messagesParent);
        row.gameObject.SetActive(true);
        row.Set(isUser, final, defaultFont, rtl);

        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
        Canvas.ForceUpdateCanvases();
    }

    void StartTyping()
    {
        if (typingRoutine != null) StopCoroutine(typingRoutine);
        typingRoutine = StartCoroutine(TypingAnim());
    }

    void StopTyping()
    {
        if (typingRoutine != null) StopCoroutine(typingRoutine);
        typingRoutine = null;
        typingText.text = "";
    }

    IEnumerator TypingAnim()
    {
        int dots = 0;
        while (true)
        {
            dots = (dots + 1) % 4;
            bool rtl = currentLanguage == ChatLang.AR;
            if (typingText is RTLTextMeshPro rtlTxt)
                rtlTxt.isRightToLeftText = rtl;

            typingText.text = (currentLanguage == ChatLang.EN ? "AI is typing" : "Ø§Ù„Ù…Ø³Ø§Ø¹Ø¯ ÙŠÙƒØªØ¨") + new string('.', dots);
            yield return new WaitForSeconds(0.25f);
        }
    }

    void OnRequestEvaluation()
    {
        if (busy) return;
        AddAI(currentLanguage == ChatLang.AR ? "Ø¬Ø§Ø±ÙŠ ØªØ­Ø¶ÙŠØ± Ø§Ù„ØªÙ‚ÙŠÙŠÙ… Ø§Ù„Ù†Ù‡Ø§Ø¦ÙŠ Ø¨Ù†Ø§Ø¡Ù‹ Ø¹Ù„Ù‰ Ø£Ø¯Ø§Ø¤Ùƒ..." : "Preparing final evaluation based on your performance...");

        string prompt = "You are a senior aviation examiner overseeing a VR engine failure simulation. Evaluate the trainee's performance based on the following activity log. " +
                        "Check if they followed safety procedures (like fire extinguishing), identified the correct faults, and communicated with the AI professionally. " +
                        "Note: Engine failure training is critical. Provide a summary and a score out of 100 in both Arabic and English.";

        if (ActivityLogger.Instance != null)
        {
            busy = true;
            UpdateInputState();
            StartTyping();

            ActivityLogger.Instance.SendToAIForEvaluation("", prompt, (result) =>
            {
                StopTyping();
                busy = false;
                UpdateInputState();
                string reply = result.Contains("{") ? ExtractGeminiResponse(result) : result;
                AddAI(reply);
            });
        }
        else
        {
            AddAI(currentLanguage == ChatLang.AR ? "Ù†Ø¸Ø§Ù… Ø³Ø¬Ù„ Ø§Ù„Ø£Ø­Ø¯Ø§Ø« Ù…ÙÙ‚ÙˆØ¯!" : "Activity Logger system missing!");
        }
    }

    string T(string key)
    {
        if (currentLanguage == ChatLang.EN)
        {
            switch (key)
            {
                case "TITLE": return "AI Maintenance Engineer";
                case "SUB": return "Hybrid Chat â€¢ Diagnosis â€¢ Advanced Walkthrough";
                case "SEND": return "Send";
                case "STOP": return "Stop AI";
                case "NEXT": return "Next Step";
                case "EXPLAIN": return "Explain";
                case "MODE_CONTEXT": return "Context Mode";
                case "MODE_FREE": return "Free Mode";
                case "SAFETY": return "Safety";
                case "EVALUATE": return "Final Evaluation";
                case "ATTACH": return "Analyze View";
                case "DOCTOR": return "Send To Doctor";
                case "GO_OFFLINE": return "Test Offline";
                case "GO_ONLINE": return "Test Online";
                case "GO_ROBOT": return "Go Robot";
                case "TTS_ON": return "Sound: ON";
                case "TTS_OFF": return "Sound: OFF";
                case "PLACEHOLDER": return "Type your message...";
                case "FOOTER": return "Built with care by TechNest Team";
                case "READY": return "Ready. Ask me anything, or press Next / Attach.";
            }
        }
        else
        {
            switch (key)
            {
                case "TITLE": return "Ø§Ù„Ù…Ù‡Ù†Ø¯Ø³ Ø§Ù„Ø°ÙƒÙŠ Ù„Ù„ØµÙŠØ§Ù†Ø©";
                case "SUB": return "Ù…Ø­Ø§Ø¯Ø«Ø© Ù‡Ø¬ÙŠÙ†Ø© â€¢ ØªØ´Ø®ÙŠØµ Ø¯Ù‚ÙŠÙ‚ â€¢ Ø­Ù„ÙˆÙ„ Ø¹Ù„Ù…ÙŠØ© Ù…ØªÙƒØ§Ù…Ù„Ø©";
                case "SEND": return "Ø¥Ø±Ø³Ø§Ù„";
                case "STOP": return "Ø¥ÙŠÙ‚Ø§Ù Ø§Ù„Ø±Ø¯";
                case "NEXT": return "Ø§Ù„Ø®Ø·ÙˆØ© Ø§Ù„ØªØ§Ù„ÙŠØ©";
                case "EXPLAIN": return "Ø´Ø±Ø­";
                case "MODE_CONTEXT": return "ÙˆØ¶Ø¹ Ø§Ù„Ù…Ø´Ø±ÙˆØ¹";
                case "MODE_FREE": return "Ø§Ù„ÙˆØ¶Ø¹ Ø§Ù„Ø­Ø±";
                case "SAFETY": return "Ø³Ù„Ø§Ù…Ø©";
                case "EVALUATE": return "Ø§Ù„ØªÙ‚ÙŠÙŠÙ… Ø§Ù„Ù†Ù‡Ø§Ø¦ÙŠ";
                case "ATTACH": return "ØªØ­Ù„ÙŠÙ„ Ø§Ù„Ø´Ø§Ø´Ø©";
                case "DOCTOR": return "Ø¥Ø±Ø³Ø§Ù„ Ù„Ù„Ø¯ÙƒØªÙˆØ±";
                case "GO_OFFLINE": return "Ù…Ø­Ø§ÙƒØ§Ø© Ø§Ù„Ø§ÙˆÙÙ„Ø§ÙŠÙ†";
                case "GO_ONLINE": return "Ù…Ø­Ø§ÙƒØ§Ø© Ø§Ù„Ø§ÙˆÙ†Ù„Ø§ÙŠÙ†";
                case "GO_ROBOT": return "ØªØ­Ø±ÙŠÙƒ Ø§Ù„Ø±ÙˆØ¨ÙˆØª";
                case "TTS_ON": return "Ø§Ù„ØµÙˆØª: Ø´ØºØ§Ù„";
                case "TTS_OFF": return "Ø§Ù„ØµÙˆØª: Ù…Ø·ÙØ£";
                case "PLACEHOLDER": return "Ø§ÙƒØªØ¨ Ø±Ø³Ø§Ù„ØªÙƒ...";
                case "FOOTER": return "ØµÙÙ†Ø¹ Ø¨ÙˆØ§Ø³Ø·Ø© ÙØ±ÙŠÙ‚ TechNest";
                case "READY": return "Ø¬Ø§Ù‡Ø². Ø§Ø³Ø£Ù„Ù†ÙŠ Ø£Ùˆ Ø§Ø¶ØºØ· Ø¹Ù„Ù‰ Ø§Ù„Ø®Ø·ÙˆØ© Ø§Ù„ØªØ§Ù„ÙŠØ© Ø£Ùˆ Ø¥Ø±ÙØ§Ù‚ Ø§Ù„ÙØ­Øµ.";
            }
        }

        return key;
    }
}
