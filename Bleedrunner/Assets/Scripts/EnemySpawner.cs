using UnityEngine;
using UnityEngine.Serialization;

public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner Instance;

    [Header("Prefabs")]
    [FormerlySerializedAs("fodderEnemyPrefab")]
    public GameObject enemyPrefab;

    [Header("Spawn Balances")]
    public float baseSpawnInterval = 1.0f;
    public int maxEnemiesAlive = 10;
    public float spawnRadiusMin = 7f;
    public float spawnRadiusMax = 13f;
    public float arenaLimit = 22f;

    [Header("Adaptive AI Pacing")]
    [Tooltip("Tension value below which buzzer-beater front spawns trigger")]
    public float highTensionThreshold = 0.25f;
    [Tooltip("Spawning interval multiplier when in high tension panic state")]
    public float highTensionSpeedMultiplier = 0.4f;
    [Tooltip("Forward distance to place clutch enemies")]
    public float clutchSpawnDistance = 8.0f;
    [Tooltip("Left/Right spread variance for clutch spawns")]
    public float clutchSpread = 2.5f;

    [Header("Telemetry Monitor")]
    [SerializeField] private float currentTension = 0.5f;

    private float nextSpawnTime;
    private Transform player;
    public static int ActiveEnemyCount = 0;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        ActiveEnemyCount = 0;
    }

    void Start()
    {
        FindPlayer();
    }

    void Update()
    {
        if (TimeManager.Instance != null && (TimeManager.Instance.isDead || TimeManager.Instance.isPaused)) return;

        if (player == null)
        {
            FindPlayer();
            if (player == null) return;
        }

        if (TimeManager.Instance != null)
        {
            float timerRatio = TimeManager.Instance.currentTime / TimeManager.Instance.maxTime;
            currentTension = timerRatio * Mathf.Max(TimeManager.Instance.killVelocity, 0.5f);
        }

        if (Time.time >= nextSpawnTime)
        {
            if (ActiveEnemyCount < maxEnemiesAlive)
            {
                EvaluateAndSpawn(currentTension);
            }

            float dynamicInterval = (currentTension < highTensionThreshold)
                ? baseSpawnInterval * highTensionSpeedMultiplier
                : baseSpawnInterval;

            nextSpawnTime = Time.time + dynamicInterval;
        }
    }

    void FindPlayer()
    {
        PlayerController25D p = FindFirstObjectByType<PlayerController25D>();
        if (p != null) player = p.transform;
    }

    void EvaluateAndSpawn(float tension)
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("<color=red>[EnemySpawner]</color> Enemy Prefab slot is EMPTY in the Inspector!");
            return;
        }

        Vector3 spawnPos;

        if (tension < highTensionThreshold)
        {
            Vector3 forwardOffset = player.forward * clutchSpawnDistance;
            Vector3 lateralJitter = player.right * Random.Range(-clutchSpread, clutchSpread);
            spawnPos = player.position + forwardOffset + lateralJitter;
        }
        else
        {
            Vector2 circle = Random.insideUnitCircle.normalized * Random.Range(spawnRadiusMin, spawnRadiusMax);
            spawnPos = new Vector3(player.position.x + circle.x, 1f, player.position.z + circle.y);
        }

        spawnPos.y = 1f;
        spawnPos.x = Mathf.Clamp(spawnPos.x, -arenaLimit, arenaLimit);
        spawnPos.z = Mathf.Clamp(spawnPos.z, -arenaLimit, arenaLimit);

        Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
    }
}