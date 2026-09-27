using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController25D : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 10f;

    [Header("Aiming Polish")]
    public bool stabilizeAimDuringShake = true;
    [Tooltip("Optional: Drag your 'Visor' child object here for instant 0ms mouse aiming without affecting body physics.")]
    public Transform visualAimHolder;

    [Header("Dash Settings")]
    public bool canDash = true;
    public float dashSpeed = 26f;
    public float dashDuration = 0.14f;
    public float dashCooldown = 0.75f;
    public bool isDashing = false;

    [Header("Siphon Dash Perk Tunables")]
    public bool hasSiphonDash = false;
    public float siphonRadius = 1.2f;
    public float siphonTimeGain = 0.5f;

    private Rigidbody rb;
    private Vector3 moveInput;
    private Camera cam;
    private Plane aimPlane;
    private Quaternion targetRotation;
    private float nextDashTime;

    // Baseline stat memory
    private float baseMoveSpeed;
    private bool baseCanDash;
    private bool baseHasSiphonDash;
    private float baseSiphonTimeGain;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        cam = Camera.main;
        aimPlane = new Plane(Vector3.up, new Vector3(0f, 1f, 0f));
        targetRotation = transform.rotation;

        // Remember initial Inspector stats
        baseMoveSpeed = moveSpeed;
        baseCanDash = canDash;
        baseHasSiphonDash = hasSiphonDash;
        baseSiphonTimeGain = siphonTimeGain;
    }

    void Update()
    {
        if (TimeManager.Instance != null && (TimeManager.Instance.isDead || TimeManager.Instance.isPaused))
        {
            moveInput = Vector3.zero;
            return;
        }

        Vector2 inputDir = Vector2.zero;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) inputDir.y += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) inputDir.y -= 1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) inputDir.x -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) inputDir.x += 1f;
        }
        moveInput = new Vector3(inputDir.x, 0f, inputDir.y).normalized;

        if (canDash && Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame && Time.time >= nextDashTime && !isDashing)
        {
            StartCoroutine(PerformDash());
        }

        if (Mouse.current != null && cam != null)
        {
            Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
            Ray ray = cam.ScreenPointToRay(mouseScreenPos);

            if (stabilizeAimDuringShake && CameraFollow.Instance != null)
            {
                ray.origin -= CameraFollow.Instance.shakeOffset;
            }

            if (aimPlane.Raycast(ray, out float enter))
            {
                Vector3 targetPoint = ray.GetPoint(enter);
                Vector3 lookDirection = targetPoint - transform.position;
                lookDirection.y = 0f;

                if (lookDirection.sqrMagnitude > 0.001f)
                {
                    targetRotation = Quaternion.LookRotation(lookDirection);

                    if (visualAimHolder != null)
                    {
                        visualAimHolder.rotation = targetRotation;
                    }
                }
            }
        }
    }

    void FixedUpdate()
    {
        if (TimeManager.Instance != null && (TimeManager.Instance.isDead || TimeManager.Instance.isPaused))
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        if (!isDashing)
        {
            rb.linearVelocity = moveInput * moveSpeed;
        }

        if (visualAimHolder == null)
        {
            rb.MoveRotation(targetRotation);
        }
    }

    IEnumerator PerformDash()
    {
        isDashing = true;
        nextDashTime = Time.time + dashCooldown;

        Vector3 dashDir = moveInput.sqrMagnitude > 0.01f ? moveInput : transform.forward;
        rb.linearVelocity = dashDir * dashSpeed;

        float elapsed = 0f;
        while (elapsed < dashDuration)
        {
            if (TimeManager.Instance != null && TimeManager.Instance.isPaused) break;

            if (hasSiphonDash)
            {
                Collider[] hitColliders = Physics.OverlapSphere(transform.position, siphonRadius);
                for (int i = 0; i < hitColliders.Length; i++)
                {
                    if (hitColliders[i].CompareTag("Enemy"))
                    {
                        if (TimeManager.Instance != null)
                        {
                            TimeManager.Instance.AddTime(siphonTimeGain);
                        }
                    }
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        rb.linearVelocity = Vector3.zero;
        isDashing = false;
    }

    public void ResetPlayerStats()
    {
        moveSpeed = baseMoveSpeed;
        canDash = baseCanDash;
        hasSiphonDash = baseHasSiphonDash;
        siphonTimeGain = baseSiphonTimeGain;
        isDashing = false;
    }

    void OnDisable()
    {
        if (rb != null) rb.linearVelocity = Vector3.zero;
    }
}