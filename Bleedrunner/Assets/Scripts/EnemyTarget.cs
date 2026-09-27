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

    void OnEnable()
    {
        EnemySpawner.ActiveEnemyCount++;
    }

    void OnDisable()
    {
        EnemySpawner.ActiveEnemyCount = Mathf.Max(0, EnemySpawner.ActiveEnemyCount - 1);
    }

    void Start()
    {
        PlayerController25D target = FindFirstObjectByType<PlayerController25D>();
        if (target != null) player = target.transform;
    }

    void FixedUpdate()
    {
        if (player == null || (TimeManager.Instance != null && (TimeManager.Instance.isDead || TimeManager.Instance.isPaused)))
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        // Get smart navigation waypoint from the maze generator
        Vector3 targetPoint = player.position;
        if (ProceduralMazeGenerator.Instance != null)
        {
            targetPoint = ProceduralMazeGenerator.Instance.GetNextWaypointForEnemy(transform.position, player.position);
        }

        Vector3 direction = (targetPoint - transform.position);
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.05f)
        {
            direction.Normalize();
            rb.linearVelocity = direction * chaseSpeed;
            rb.MoveRotation(Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 12f * Time.fixedDeltaTime));
        }
        else
        {
            // Close to node, push directly toward player
            Vector3 direct = (player.position - transform.position);
            direct.y = 0f;
            rb.linearVelocity = direct.normalized * chaseSpeed;
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

        if (ChamberManager.Instance != null)
        {
            ChamberManager.Instance.RegisterKill();
        }

        Destroy(gameObject);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (TimeManager.Instance != null && TimeManager.Instance.isPaused) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            bool isProtected = ChamberManager.Instance != null && ChamberManager.Instance.isGraceBufferActive;

            if (!isProtected && TimeManager.Instance != null)
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