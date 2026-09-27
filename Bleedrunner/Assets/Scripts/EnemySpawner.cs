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
    public float spawnRadiusMin = 6f;
    public float spawnRadiusMax = 18f;

    [Header("Adaptive AI Pacing")]
    public float highTensionThreshold = 0.25f;
    public float highTensionSpeedMultiplier = 0.4f;

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
                SpawnEnemyInMaze();
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

    void SpawnEnemyInMaze()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("<color=red>[EnemySpawner]</color> Enemy Prefab slot is EMPTY in the Inspector!");
            return;
        }

        Vector3 spawnPos;

        if (ProceduralMazeGenerator.Instance != null)
        {
            spawnPos = ProceduralMazeGenerator.Instance.GetValidSpawnPosition(player.position, spawnRadiusMin, spawnRadiusMax);
        }
        else
        {
            Vector2 circle = Random.insideUnitCircle.normalized * Random.Range(spawnRadiusMin, spawnRadiusMax);
            spawnPos = new Vector3(player.position.x + circle.x, 0.5f, player.position.z + circle.y);
        }

        spawnPos.y = 0.5f;
        Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
    }
}