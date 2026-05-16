using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;

public class LiveStreamManager : MonoBehaviour
{
    [Header("Stream Settings")]
    [SerializeField] private string streamEndpoint = "https://aerotwin.onrender.com/api/device/stream";
    [SerializeField] private string deviceSecret = "AeroTwin_Device_Stream_Secure_Key_9988";

    [Tooltip("Recommended for Quest 2: 2 - 5")]
    [Range(1f, 10f)] public float frameRate = 4f;

    [Tooltip("Recommended for Quest 2: 30 - 45")]
    [Range(1, 100)] public int imageQuality = 40;

    [Tooltip("Recommended for Quest 2 streaming preview: 640x360 or 512x288")]
    public Vector2Int targetResolution = new Vector2Int(640, 360);

    [Tooltip("If true, uses unscaled time")]
    public bool useRealtimeWait = true;

    [Tooltip("Optional extra delay after each upload attempt")]
    [Range(0f, 1f)] public float extraUploadCooldown = 0.05f;

    [Header("Target Camera")]
    public Camera streamCamera;

    [Tooltip("Assign only the layers you actually want to stream")]
    public LayerMask streamingLayers = ~0;

    [Header("Optional References")]
    [SerializeField] private ActivityLogger activityLogger;

    private string deviceId;
    private bool isStreaming = true;
    private bool isUploading = false;

    private RenderTexture rt;
    private Texture2D screenShot;
    private WaitForSecondsRealtime waitRealtime;
    private WaitForSeconds waitScaled;

    [Serializable]
    private class StreamStats
    {
        public float score;
        public float progress;
        public float safetyScore;
        public int tasks;
        public float time;
        public int errors;
        public int aiInquiries;
    }

    [Serializable]
    private class StreamPayload
    {
        public string deviceId;
        public string session_id;
        public string frame_base64;
        public string trainee_name;
        public string doctor_code;
        public StreamStats stats;
    }

    private void Awake()
    {
        deviceId = SystemInfo.deviceUniqueIdentifier;

        if (streamCamera == null)
            streamCamera = Camera.main;

        if (activityLogger == null)
            activityLogger = FindFirstObjectByType<ActivityLogger>();
    }

    private void Start()
    {
        if (streamCamera != null)
            streamCamera.cullingMask = streamingLayers;

        InitializeBuffers();
        UpdateWaitObject();

        StartCoroutine(StreamLoop());
    }

    private void InitializeBuffers()
    {
        ReleaseBuffers();

        rt = new RenderTexture(targetResolution.x, targetResolution.y, 16, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 1;
        rt.useMipMap = false;
        rt.autoGenerateMips = false;
        rt.Create();

        screenShot = new Texture2D(targetResolution.x, targetResolution.y, TextureFormat.RGB24, false);
    }

    private void UpdateWaitObject()
    {
        float interval = Mathf.Max(0.1f, 1f / Mathf.Max(1f, frameRate));

        if (useRealtimeWait)
            waitRealtime = new WaitForSecondsRealtime(interval);
        else
            waitScaled = new WaitForSeconds(interval);
    }

    private IEnumerator StreamLoop()
    {
        while (isStreaming)
        {
            if (!isUploading && CanCapture())
            {
                yield return CaptureAndUpload();
            }

            if (useRealtimeWait)
                yield return waitRealtime;
            else
                yield return waitScaled;
        }
    }

    private bool CanCapture()
    {
        return streamCamera != null &&
               streamCamera.gameObject.activeInHierarchy &&
               rt != null &&
               screenShot != null;
    }

    private IEnumerator CaptureAndUpload()
    {
        isUploading = true;

        // Capture frame
        streamCamera.targetTexture = rt;
        streamCamera.Render();

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;

        screenShot.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0, false);
        screenShot.Apply(false, false);

        streamCamera.targetTexture = null;
        RenderTexture.active = previous;

        // Encode image
        byte[] jpgBytes = screenShot.EncodeToJPG(imageQuality);
        string base64Frame = Convert.ToBase64String(jpgBytes);

        // Build stats
        StreamStats stats = BuildStats();

        // Build payload
        StreamPayload payload = new StreamPayload
        {
            deviceId = deviceId,
            session_id = PlayerPrefs.GetString("SessionID", ""),
            frame_base64 = base64Frame,
            trainee_name = PlayerPrefs.GetString("TraineeName", "Active Trainee"),
            doctor_code = PlayerPrefs.GetString("DoctorCode", "0000"),
            stats = stats
        };

        string json = JsonUtility.ToJson(payload);

        yield return UploadJson(json);

        if (extraUploadCooldown > 0f)
            yield return new WaitForSecondsRealtime(extraUploadCooldown);

        isUploading = false;
    }

    private StreamStats BuildStats()
    {
        StreamStats stats = new StreamStats
        {
            score = 0f,
            progress = 0f,
            safetyScore = 0f,
            tasks = 0,
            time = 0f,
            errors = 0,
            aiInquiries = 0
        };

        if (activityLogger == null)
            return stats;

        var data = activityLogger.GetCurrentLogData();
        if (data == null)
            return stats;

        stats.score = data.safetyScore;
        stats.progress = data.tasksCompleted * 100f / Mathf.Max(1, data.totalTasks);
        stats.safetyScore = data.safetyScore;
        stats.tasks = data.tasksCompleted;
        stats.time = data.sessionDuration;
        stats.errors = data.interfaceActionsCount + data.toolActionsCount;
        stats.aiInquiries = data.aiInquiryCount;

        return stats;
    }

    private IEnumerator UploadJson(string json)
    {
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest request = new UnityWebRequest(streamEndpoint, UnityWebRequest.kHttpVerbPOST))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = 5;
            request.certificateHandler = new BypassCertificateHandler();

            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("X-Device-Secret", deviceSecret);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[LiveStreamManager] Upload failed: {request.error} | Code: {request.responseCode}");
            }
        }
    }

    public void OnDeviceCheckedIn(string hardwareId, string atCode)
    {
        deviceId = hardwareId;
        Debug.Log($"[LiveStreamManager] Device checked in. Hardware ID: {deviceId}, AT Code: {atCode}");
    }

    public void SetStreaming(bool active)
    {
        isStreaming = active;

        if (isStreaming)
        {
            UpdateWaitObject();

            if (gameObject.activeInHierarchy)
                StartCoroutine(StreamLoop());
        }
    }

    public void ApplyQuest2LowSettings()
    {
        frameRate = 4f;
        imageQuality = 40;
        targetResolution = new Vector2Int(640, 360);

        InitializeBuffers();
        UpdateWaitObject();
    }

    private void OnDisable()
    {
        ReleaseBuffers();
    }

    private void OnDestroy()
    {
        ReleaseBuffers();
    }

    private void ReleaseBuffers()
    {
        if (rt != null)
        {
            if (rt.IsCreated())
                rt.Release();

            Destroy(rt);
            rt = null;
        }

        if (screenShot != null)
        {
            Destroy(screenShot);
            screenShot = null;
        }
    }

    public class BypassCertificateHandler : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData)
        {
            return true;
        }
    }
}