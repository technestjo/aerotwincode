using UnityEngine;

[ExecuteInEditMode]
public class RobotController : MonoBehaviour
{
    [Header("=== تحكم الحركة ===")]
    [Tooltip("سرعة الحركة للأمام/الخلف")]
    public float moveSpeed = 2f;

    [Tooltip("سرعة الدوران")]
    public float turnSpeed = 100f;

    [Header("=== العجلات - اسحبهم من الـ Hierarchy ===")]
    public Transform wheel1;  // weel.1
    public Transform wheel2;  // weel.2
    public Transform wheel3;  // weel.3
    public Transform wheel4;  // weel.4

    [Header("=== إعدادات العجلات ===")]
    [Tooltip("سرعة دوران العجلات")]
    public float wheelRotationSpeed = 360f;

    [Tooltip("محور دوران العجلة (جرب X أو Z)")]
    public RotationAxis wheelAxis = RotationAxis.X;

    public enum RotationAxis { X, Y, Z }

    [Header("=== الصوت ===")]
    [Tooltip("صوت الموتور/الحركة")]
    public AudioSource motorSound;

    [Tooltip("ارتفاع الصوت عند الحركة")]
    [Range(0f, 1f)]
    public float maxVolume = 0.8f;

    [Tooltip("سرعة تغير الصوت")]
    public float volumeFadeSpeed = 5f;

    [Header("=== مسار الـ AI ===")]
    [Tooltip("إذا كنت تستخدم أداة Unity Splines لرسم خط أزرق ناعم، اسحبه هنا!")]
    public UnityEngine.Splines.SplineContainer splinePath;

    [Tooltip("لو لم تستخدم Splines، يمكنك وضع النقاط العادية (GameObjects) هنا بالترتيب")]
    public Transform[] pathPoints;
    
    [Tooltip("المسافة التي يعتبر عندها الروبوت أنه وصل للنقطة")]
    public float waypointThreshold = 0.2f; // مسافة قريبة جداً لضمان الدقة
    
    [Header("=== تحكم للتصوير (Inspector) ===")]
    [Tooltip("اضغط (صح) هنا عشان تخلي الروبوت يروح أو يرجع من الانسبكتر")]
    public bool triggerMoveFromInspector = false;

    // حالة لتتبع هل الروبوت في رحلة ذهاب أم عودة
    [HideInInspector]
    public bool isReturning = false;

    // متغيرات داخلية
    private float currentMove = 0f;
    private float currentTurn = 0f;
    private float wheelRotation = 0f;
    private float targetVolume = 0f;
    public bool isAIActive = false;

    void Start()
    {
        // جهّز الصوت
        if (motorSound == null)
            motorSound = GetComponent<AudioSource>();

        if (motorSound != null)
        {
            motorSound.volume = 0f;
            motorSound.loop = true;
        }
    }

    void Update()
    {
        if (triggerMoveFromInspector)
        {
            triggerMoveFromInspector = false;
            TriggerRobotAIMovement();
        }

        if (Application.isPlaying)
        {
            // تم إزالة أزرار الكيبورد لكي لا يتداخل مع حركة اللاعب في الـ VR.
            // الروبوت سيتحرك الآن فقط عند استدعاء TriggerRobotAIMovement عبر زر الـ AI.

            // حرّك الروبوت
            MoveRobot(currentMove, currentTurn, Time.deltaTime);

            // تحكم بالصوت
            HandleSound(currentMove, currentTurn);
        }
    }

    void HandleSound(float move, float turn)
    {
        if (motorSound == null) return;

        bool isMoving = (Mathf.Abs(move) > 0.1f || Mathf.Abs(turn) > 0.1f);

        if (isMoving)
        {
            if (!motorSound.isPlaying) motorSound.Play();
            targetVolume = maxVolume;
        }
        else
        {
            targetVolume = 0f;
        }

        motorSound.volume = Mathf.Lerp(motorSound.volume, targetVolume, Time.deltaTime * volumeFadeSpeed);

        if (motorSound.volume < 0.01f && !isMoving)
        {
            motorSound.Stop();
        }
    }

    void MoveRobot(float move, float turn, float deltaTime)
    {
        // === حركة الروبوت ===
        if (move != 0f)
        {
            transform.Translate(Vector3.forward * move * moveSpeed * deltaTime);
        }

        // Only apply manual turn if AI is NOT active, to prevent conflicting with RotateTowards
        if (!isAIActive && turn != 0f)
        {
            transform.Rotate(Vector3.up * turn * turnSpeed * deltaTime);
        }

        // === دوران العجلات ===
        if (move != 0f)
        {
            wheelRotation += move * wheelRotationSpeed * deltaTime;
            RotateWheels(wheelRotation);
        }
    }

    void RotateWheels(float angle)
    {
        Vector3 rotation = Vector3.zero;

        switch (wheelAxis)
        {
            case RotationAxis.X: rotation = new Vector3(angle, 0, 0); break;
            case RotationAxis.Y: rotation = new Vector3(0, angle, 0); break;
            case RotationAxis.Z: rotation = new Vector3(0, 0, angle); break;
        }

        if (wheel1 != null) wheel1.localRotation = Quaternion.Euler(rotation);
        if (wheel2 != null) wheel2.localRotation = Quaternion.Euler(rotation);
        if (wheel3 != null) wheel3.localRotation = Quaternion.Euler(rotation);
        if (wheel4 != null) wheel4.localRotation = Quaternion.Euler(rotation);
    }
    
    [ContextMenu("Trigger Robot Movement")]
    public void TriggerRobotAIMovement()
    {
        if (!isAIActive) StartCoroutine(AIMoveRoutine());
    }

    private System.Collections.IEnumerator AIMoveRoutine()
    {
        isAIActive = true;
        Vector3[] waypoints = null;

        // 1. استخراج النقاط من مسار Unity Spline بأمان
        if (splinePath != null)
        {
            try 
            {
                if (splinePath.Splines != null && splinePath.Splines.Count > 0)
                {
                    UnityEngine.Splines.Spline activeSpline = splinePath.Splines[0];
                    if (activeSpline != null && activeSpline.Count > 1) 
                    {
                        int resolution = Mathf.Max(10, Mathf.CeilToInt(activeSpline.GetLength() * 2f)); 
                        waypoints = new Vector3[resolution];

                        for (int i = 0; i < resolution; i++)
                        {
                            float t = i / (float)(resolution - 1);
                            Vector3 localPos = UnityEngine.Splines.SplineUtility.EvaluatePosition(activeSpline, t);
                            waypoints[i] = splinePath.transform.TransformPoint(localPos);
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[AI Spline] خطأ أثناء استخراج نقاط الخط: " + ex.Message);
            }
        }

        // 2. إذا لم يكن هناك Spline صالح، نستخدم النقاط العادية (Transform Array)
        if ((waypoints == null || waypoints.Length == 0) && pathPoints != null && pathPoints.Length > 0)
        {
            waypoints = new Vector3[pathPoints.Length];
            for (int i = 0; i < pathPoints.Length; i++)
            {
                if (pathPoints[i] != null) 
                    waypoints[i] = pathPoints[i].position;
            }
        }

        // 3. منع الروبوت من التعليق في حال عدم وجود مسار
        if (waypoints == null || waypoints.Length == 0)
        {
            Debug.LogWarning("[AI Spline] لا يوجد مسار Spline صالح ولا يوجد نقاط عادية!");
            isAIActive = false;
            yield break;
        }

        if (!isReturning)
        {
            // رحلة الذهاب
            for (int i = 0; i < waypoints.Length; i++)
            {
                yield return StartCoroutine(NavigateToPoint(waypoints[i]));
            }
            isReturning = true; 
        }
        else
        {
            // رحلة العودة
            int startIndex = Mathf.Max(0, waypoints.Length - 2);
            for (int i = startIndex; i >= 0; i--)
            {
                yield return StartCoroutine(NavigateToPoint(waypoints[i]));
            }
            isReturning = false; 
        }

        // التوقف التام عند الانتهاء
        currentMove = 0f;
        currentTurn = 0f;
        isAIActive = false;
    }

    private System.Collections.IEnumerator NavigateToPoint(Vector3 targetPosition)
    {
        while (true)
        {
            Vector3 directionToTarget = targetPosition - transform.position;
            directionToTarget.y = 0; // تجاهل الارتفاع تماماً

            // التحقق من الوصول
            if (directionToTarget.magnitude <= waypointThreshold) 
            {
                break; 
            }

            if (directionToTarget != Vector3.zero)
            {
                // توجيه صارم ومباشر باستخدام الرياضيات (يمنع أي اهتزاز أو دوران عشوائي)
                Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                float angle = Quaternion.Angle(transform.rotation, targetRotation);

                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);

                // إذا كان يحتاج للتدوير بشكل كبير يلتف في مكانه أولاً
                if (angle > 5f)
                {
                    currentMove = 0f; 
                    currentTurn = 1f; // فقط لنرسل إشارة دوران لدالة الصوت
                }
                else
                {
                    currentMove = 1f; // يتجه للأمام
                    currentTurn = 0f; // نصفّر الدوران لأن RotateTowards تقوم بالواجب
                }
            }

            if (!Application.isPlaying) yield break;

            yield return null; 
        }
    }

    // رسم المسار في المحرر (Editor) ليظهر كـ Spline
    void OnDrawGizmos()
    {
        if (pathPoints != null && pathPoints.Length > 0)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < pathPoints.Length; i++)
            {
                if (pathPoints[i] != null)
                {
                    Gizmos.DrawSphere(pathPoints[i].position, 0.2f);
                    if (i < pathPoints.Length - 1 && pathPoints[i + 1] != null)
                    {
                        Gizmos.DrawLine(pathPoints[i].position, pathPoints[i + 1].position);
                    }
                }
            }
        }
    }
}
