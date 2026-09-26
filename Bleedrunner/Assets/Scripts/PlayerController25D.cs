using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController25D : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 10f;
    public bool stabilizeAimDuringShake = true;

    [Header("Dash Settings")]
    public bool canDash = true;
    public float dashSpeed = 26f;
    public float dashDuration = 0.14f;
    public float dashCooldown = 0.75f;
    public bool isDashing = false;

    [Header("Siphon Dash Perk Tunables")]
    public bool hasSiphonDash = false;
    [Tooltip("Radius around the player checked for enemies when dashing")]
    public float siphonRadius = 1.2f;
    [Tooltip("Seconds granted per enemy phased through")]
    public float siphonTimeGain = 0.5f;

    private Rigidbody rb;
    private Vector3 moveInput;
    private Camera cam;
    private Plane aimPlane;
    private Quaternion targetRotation;
    private float nextDashTime;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        cam = Camera.main;
        aimPlane = new Plane(Vector3.up, new Vector3(0f, 1f, 0f));
        targetRotation = transform.rotation;
    }

    void Update()
    {
        if (TimeManager.Instance != null && (TimeManager.Instance.isDead || TimeManager.Instance.isPaused))
        {
            moveInput = Vector3.zero;
            return;
        }

        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");
        moveInput = new Vector3(x, 0f, z).normalized;

        if (canDash && Input.GetKeyDown(KeyCode.Space) && Time.time >= nextDashTime && !isDashing)
        {
            StartCoroutine(PerformDash());
        }

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
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

        if (isDashing) return;

        rb.linearVelocity = moveInput * moveSpeed;
        rb.MoveRotation(targetRotation);
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

    void OnDisable()
    {
        if (rb != null) rb.linearVelocity = Vector3.zero;
    }
}