using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class MobileControls : MonoBehaviour
{
    public static MobileControls Instance;

    [Header("Aim Mode Selector")]
    [Tooltip("CHECKED = Dual-Stick (Virtual Right Stick).\nUNCHECKED = Tap-To-Aim (Tap anywhere on screen to shoot).")]
    public bool useAimJoystick = false;

    [Header("Input Telemetry (Read-only)")]
    public Vector2 moveInput;
    public Vector2 aimInput;
    public bool isFiring;
    public bool dashTriggered;

    [Header("Auto-Detection Settings")]
    public GameObject controlsContainer;
    public bool showInEditorForTesting = true;

    [Header("Left Move Joystick UI")]
    public RectTransform moveBase;
    public RectTransform moveHandle;
    public float moveRange = 90f;

    [Header("Right Aim Joystick UI (Optional / Toggleable)")]
    [Tooltip("Drag the AimBase GameObject here so it can be enabled/disabled dynamically.")]
    public GameObject aimJoystickRoot;
    public RectTransform aimBase;
    public RectTransform aimHandle;
    public float aimRange = 90f;
    [Range(0.05f, 0.5f)] public float fireDeadzone = 0.15f;

    private int moveFingerId = -999;
    private int aimFingerId = -999;
    private Canvas parentCanvas;
    private Camera cam;
    private Plane groundPlane;
    private Transform playerTransform;
    private bool isMobileUser = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        parentCanvas = GetComponentInParent<Canvas>();
        cam = Camera.main;
        groundPlane = new Plane(Vector3.up, new Vector3(0f, 0.6f, 0f));

        DetectDeviceAndSetVisibility();
        ApplyAimModeVisibility();
    }

    void Start()
    {
        CachePlayer();
    }

    void OnValidate()
    {
        // Updates joystick visibility live inside the Unity Editor when clicking the checkbox
        ApplyAimModeVisibility();
    }

    public void ApplyAimModeVisibility()
    {
        if (aimJoystickRoot != null)
        {
            aimJoystickRoot.SetActive(useAimJoystick);
        }
        else if (aimBase != null)
        {
            aimBase.gameObject.SetActive(useAimJoystick);
        }
    }

    void DetectDeviceAndSetVisibility()
    {
#if UNITY_EDITOR
        isMobileUser = showInEditorForTesting;
#else
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

        // If joystick mode is disabled, evaluate screen touches for tap-aiming
        if (!useAimJoystick)
        {
            HandleTapToAimAndFire();
        }
    }

    void CachePlayer()
    {
        if (playerTransform == null)
        {
            PlayerController25D p = FindFirstObjectByType<PlayerController25D>();
            if (p != null) playerTransform = p.transform;
        }
    }

    // ==========================================
    // MODE A: TAP-TO-AIM & FIRE LOGIC
    // ==========================================
    void HandleTapToAimAndFire()
    {
        isFiring = false;

        if (Touchscreen.current == null) return;
        if (playerTransform == null)
        {
            CachePlayer();
            if (playerTransform == null) return;
        }

        Camera eventCam = (parentCanvas != null && parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : parentCanvas.worldCamera;
        var touches = Touchscreen.current.touches;

        for (int i = 0; i < touches.Count; i++)
        {
            var touch = touches[i];
            if (!touch.press.isPressed) continue;

            int touchId = touch.touchId.ReadValue();

            // 1. IGNORE THE MOVEMENT FINGER (Even if it drags miles outside the joystick circle)
            if (touchId == moveFingerId) continue;

            Vector2 touchPos = touch.position.ReadValue();

            // 2. LEFT-SCREEN GUARD: The left 40% of the screen is strictly reserved for movement/kiting
            if (touchPos.x < Screen.width * 0.40f) continue;

            // 3. Ignore touches inside the moveBase rect itself
            if (moveBase != null && RectTransformUtility.RectangleContainsScreenPoint(moveBase, touchPos, eventCam))
            {
                continue;
            }

            // 4. Ignore touches on UI elements/buttons (like Dash)
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touchId))
            {
                continue;
            }

            // 5. Cast ray to floor plane and shoot
            Ray ray = cam.ScreenPointToRay(touchPos);
            if (groundPlane.Raycast(ray, out float enter))
            {
                Vector3 worldHit = ray.GetPoint(enter);
                Vector3 dir = worldHit - playerTransform.position;
                dir.y = 0f;

                if (dir.sqrMagnitude > 0.05f)
                {
                    dir.Normalize();
                    aimInput = new Vector2(dir.x, dir.z);
                    isFiring = true;
                    break; // Right-hand aim touch acquired
                }
            }
        }
    }

    // ==========================================
    // MODE B: VIRTUAL RIGHT JOYSTICK LOGIC
    // ==========================================
    public void OnAimPointerDown(BaseEventData data)
    {
        if (!useAimJoystick) return;
        PointerEventData pData = (PointerEventData)data;
        aimFingerId = pData.pointerId;
        UpdateAimStick(pData);
    }

    public void OnAimDrag(BaseEventData data)
    {
        if (!useAimJoystick) return;
        PointerEventData pData = (PointerEventData)data;
        if (pData.pointerId != aimFingerId) return;
        UpdateAimStick(pData);
    }

    void UpdateAimStick(PointerEventData pData)
    {
        if (aimBase == null) return;
        Camera eventCam = (parentCanvas != null && parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : parentCanvas.worldCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(aimBase, pData.position, eventCam, out Vector2 localPoint))
        {
            aimInput = Vector2.ClampMagnitude(localPoint / aimRange, 1f);

            if (aimHandle != null)
            {
                aimHandle.anchoredPosition = aimInput * aimRange;
            }

            isFiring = aimInput.magnitude > fireDeadzone;
        }
    }

    public void OnAimPointerUp(BaseEventData data)
    {
        if (!useAimJoystick) return;
        PointerEventData pData = (PointerEventData)data;
        if (pData.pointerId != aimFingerId) return;

        aimFingerId = -999;
        aimInput = Vector2.zero;
        isFiring = false;
        if (aimHandle != null) aimHandle.anchoredPosition = Vector2.zero;
    }

    // ==========================================
    // SHARED: LEFT MOVEMENT JOYSTICK
    // ==========================================
    public void OnMovePointerDown(BaseEventData data)
    {
        PointerEventData pData = (PointerEventData)data;
        moveFingerId = pData.pointerId;
        UpdateMoveStick(pData);
    }

    public void OnMoveDrag(BaseEventData data)
    {
        PointerEventData pData = (PointerEventData)data;
        if (pData.pointerId != moveFingerId) return;
        UpdateMoveStick(pData);
    }

    void UpdateMoveStick(PointerEventData pData)
    {
        if (moveBase == null) return;
        Camera eventCam = (parentCanvas != null && parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : parentCanvas.worldCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(moveBase, pData.position, eventCam, out Vector2 localPoint))
        {
            moveInput = Vector2.ClampMagnitude(localPoint / moveRange, 1f);
            if (moveHandle != null) moveHandle.anchoredPosition = moveInput * moveRange;
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

    // ==========================================
    // SHARED: DASH BUTTON
    // ==========================================
    public void OnDashButtonPressed() => dashTriggered = true;

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