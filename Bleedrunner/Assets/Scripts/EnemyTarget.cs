using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyTarget : MonoBehaviour
{
    [Header("Combat Stats")]
    public int health = 1;
    public float timeReward = 1.5f;
    public float timePenaltyOnHit = 2.0f;

    [Header("Movement")]
    public float chaseSpeed = 4.5f;

    [Header("Impact Camera Shake")]
    public bool enableHitShake = true;
    [Range(0.05f, 0.6f)] public float hitShakeDuration = 0.25f;
    [Range(0.1f, 2.0f)] public float hitShakeMagnitude = 0.8f;

    private Transform player;
    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
    }

    void Start()
    {
        PlayerController25D target = FindFirstObjectByType<PlayerController25D>();
        if (target != null) player = target.transform;
    }

    void FixedUpdate()
    {
        if (player == null || (TimeManager.Instance != null && TimeManager.Instance.isDead))
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0f;
        rb.linearVelocity = direction * chaseSpeed;

        if (direction.sqrMagnitude > 0.001f)
        {
            rb.MoveRotation(Quaternion.LookRotation(direction));
        }
    }

    public void TakeDamage(int damage)
    {
        health -= damage;
        if (health <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.AddTime(timeReward);
        }
        Destroy(gameObject);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.DeductTime(timePenaltyOnHit);
            }

            if (enableHitShake && CameraFollow.Instance != null)
            {
                CameraFollow.Instance.TriggerShake(hitShakeDuration, hitShakeMagnitude);
            }

            Destroy(gameObject);
        }
    }
}