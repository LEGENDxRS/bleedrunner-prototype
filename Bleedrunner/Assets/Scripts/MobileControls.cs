using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MobileControls : MonoBehaviour
{
    public static MobileControls Instance;

    [Header("Input Telemetry (Read-only)")]
    public Vector2 moveInput;
    public Vector2 aimInput;
    public bool isFiring;
    public bool dashTriggered;

    [Header("Auto-Detection Settings")]
    [Tooltip("The parent GameObject holding the joysticks & buttons (MobileControlsPanel).")]
    public GameObject controlsContainer;
    [Tooltip("If checked, keeps mobile controls visible inside the Unity Editor so you can test with mouse clicks.")]
    public bool showInEditorForTesting = true;

    [Header("Left Move Joystick UI")]
    public RectTransform moveBase;
    public RectTransform moveHandle;
    public float moveRange = 60f;

    [Header("Right Aim / Fire Joystick UI")]
    public RectTransform aimBase;
    public RectTransform aimHandle;
    public float aimRange = 60f;
    [Range(0.1f, 0.9f)] public float fireDeadzone = 0.25f;

    private int moveFingerId = -999;
    private int aimFingerId = -999;
    private Vector2 moveCenter;
    private Vector2 aimCenter;
    private bool isMobileUser = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        DetectDeviceAndSetVisibility();
    }

    void Start()
    {
        if (moveBase != null) moveCenter = moveBase.position;
        if (aimBase != null) aimCenter = aimBase.position;
    }

    void DetectDeviceAndSetVisibility()
    {
#if UNITY_EDITOR
        isMobileUser = showInEditorForTesting;
#else
        // Unity WebGL detects mobile browsers (iOS / Android) via browser user agent
        isMobileUser = Application.isMobilePlatform || SystemInfo.deviceType == DeviceType.Handheld;
#endif

        if (controlsContainer != null)
        {
            controlsContainer.SetActive(isMobileUser);
        }
    }

    void Update()
    {
#if !UNITY_EDITOR
        // Fail-safe for iPads / tablets: If hidden but screen receives a touch, activate controls
        if (controlsContainer != null && !controlsContainer.activeSelf)
        {
            if (Touchscreen.current != null && Touchscreen.current.touches.Count > 0)
            {
                isMobileUser = true;
                controlsContainer.SetActive(true);
            }
        }
#endif

        if (!isMobileUser) return;

        // Keep anchor positions aligned if phone orientation flips
        if (moveBase != null) moveCenter = moveBase.position;
        if (aimBase != null) aimCenter = aimBase.position;
    }

    // --- LEFT JOYSTICK (MOVEMENT) ---
    public void OnMovePointerDown(BaseEventData data)
    {
        PointerEventData pData = (PointerEventData)data;
        moveFingerId = pData.pointerId;
        OnMoveDrag(data);
    }

    public void OnMoveDrag(BaseEventData data)
    {
        PointerEventData pData = (PointerEventData)data;
        if (pData.pointerId != moveFingerId) return;

        Vector2 direction = pData.position - moveCenter;
        moveInput = Vector2.ClampMagnitude(direction / moveRange, 1f);

        if (moveHandle != null)
        {
            moveHandle.anchoredPosition = moveInput * moveRange;
        }
    }

    public void OnMovePointerUp(BaseEventData data)
    {
        PointerEventData pData = (PointerEventData)data;
        if (pData.pointerId != moveFingerId) return;

        moveFingerId = -999;
        moveInput = Vector2.zero;
        if (moveHandle != null) moveHandle.anchoredPosition = Vector2.zero;
    }

    // --- RIGHT JOYSTICK (AIM & FIRE) ---
    public void OnAimPointerDown(BaseEventData data)
    {
        PointerEventData pData = (PointerEventData)data;
        aimFingerId = pData.pointerId;
        OnAimDrag(data);
    }

    public void OnAimDrag(BaseEventData data)
    {
        PointerEventData pData = (PointerEventData)data;
        if (pData.pointerId != aimFingerId) return;

        Vector2 direction = pData.position - aimCenter;
        aimInput = Vector2.ClampMagnitude(direction / aimRange, 1f);

        if (aimHandle != null)
        {
            aimHandle.anchoredPosition = aimInput * aimRange;
        }

        isFiring = aimInput.magnitude > fireDeadzone;
    }

    public void OnAimPointerUp(BaseEventData data)
    {
        PointerEventData pData = (PointerEventData)data;
        if (pData.pointerId != aimFingerId) return;

        aimFingerId = -999;
        aimInput = Vector2.zero;
        isFiring = false;
        if (aimHandle != null) aimHandle.anchoredPosition = Vector2.zero;
    }

    // --- DASH BUTTON ---
    public void OnDashButtonPressed()
    {
        dashTriggered = true;
    }

    public bool ConsumeDash()
    {
        if (dashTriggered)
        {
            dashTriggered = false;
            return true;
        }
        return false;
    }
}