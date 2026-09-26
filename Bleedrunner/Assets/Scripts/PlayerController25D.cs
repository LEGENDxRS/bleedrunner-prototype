using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController25D : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 10f;

    [Header("Aiming Stability")]
    [Tooltip("Stops camera shake from shifting your mouse raycast and twitching the player.")]
    public bool stabilizeAimDuringShake = true;

    private Rigidbody rb;
    private Vector3 moveInput;
    private Camera cam;
    private Plane aimPlane;
    private Quaternion targetRotation;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        cam = Camera.main;
        aimPlane = new Plane(Vector3.up, new Vector3(0f, 1f, 0f));
        targetRotation = transform.rotation;
    }

    void Update()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");
        moveInput = new Vector3(x, 0f, z).normalized;

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
        rb.linearVelocity = moveInput * moveSpeed;
        rb.MoveRotation(targetRotation);
    }

    void OnDisable()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
        }
    }
}