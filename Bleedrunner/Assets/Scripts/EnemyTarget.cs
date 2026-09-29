using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyTarget : MonoBehaviour
{
    [Header("Active Dials (Injected by EnemySpawner)")]
    public int health = 1;
    public float chaseSpeed = 4.5f;
    public float timeReward = 1.5f;
    public float timePenaltyOnHit = 2.0f;

    [Header("Impact Camera Shake")]
    public bool enableHitShake = true;
    [Range(0.05f, 0.6f)] public float hitShakeDuration = 0.25f;
    [Range(0.1f, 2.0f)] public float hitShakeMagnitude = 0.8f;

    private Transform player;
    private Rigidbody rb;
    private float losCheckTimer;
    private bool hasLineOfSight = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
        losCheckTimer = Random.Range(0f, 0.15f);
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

    void Update()
    {
        if (player == null) return;

        losCheckTimer += Time.deltaTime;
        if (losCheckTimer >= 0.15f)
        {
            losCheckTimer = 0f;
            Vector3 rayStart = transform.position; rayStart.y = 0.6f;
            Vector3 rayEnd = player.position; rayEnd.y = 0.6f;
            Vector3 diff = rayEnd - rayStart;

            // Check if a physical wall is blocking the view
            if (Physics.Raycast(rayStart, diff.normalized, out RaycastHit hit, diff.magnitude))
            {
                // If it hits the player or another enemy, line of sight is clear
                if (hit.transform == player || hit.transform.CompareTag("Player") || hit.transform.CompareTag("Enemy"))
                {
                    hasLineOfSight = true;
                }
                else
                {
                    // Blocked by maze wall or obstacle
                    hasLineOfSight = false;
                }
            }
            else
            {
                hasLineOfSight = true;
            }
        }
    }

    void FixedUpdate()
    {
        if (player == null || (TimeManager.Instance != null && (TimeManager.Instance.isDead || TimeManager.Instance.isPaused)))
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        float distToPlayer = Vector3.Distance(transform.position, player.position);
        Vector3 targetPoint = player.position;

        // Only query the flowfield if far away AND obstructed by a wall
        if (!hasLineOfSight && distToPlayer > 3.5f && ProceduralMazeGenerator.Instance != null)
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
            Vector3 direct = (player.position - transform.position);
            direct.y = 0f;
            rb.linearVelocity = direct.normalized * chaseSpeed;
        }
    }

    public void TakeDamage(int damage)
    {
        health -= damage;
        if (health <= 0) Die();
    }

    void Die()
    {
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.AddTime(timeReward);
        }

        if (ChamberManager.Instance != null) ChamberManager.Instance.RegisterKill();
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