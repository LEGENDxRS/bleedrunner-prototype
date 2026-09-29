using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner Instance;
    public static int ActiveEnemyCount = 0;

    [System.Serializable]
    public class EnemyTypeConfig
    {
        public string typeName = "Standard Swarmer";
        [Tooltip("Drag and drop the NPC prefab here.")]
        public GameObject enemyPrefab;

        [Header("Time Economy Dials")]
        public float timeGainedOnKill = 1.5f;
        public float timeLostOnCollision = 2.0f;

        [Header("Combat & Locomotion")]
        public int health = 1;
        public float chaseSpeed = 4.5f;

        [Header("Spawn Odds")]
        [Range(0.1f, 10f)]
        public float spawnWeight = 1.0f;
    }

    [Header("Enemy Roster")]
    public List<EnemyTypeConfig> enemyRoster = new List<EnemyTypeConfig>();

    [Header("Base Spawn Settings (Chamber 1)")]
    [Tooltip("Starting interval between enemy spawns in Chamber 1.")]
    public float baseSpawnInterval = 0.8f;
    [Tooltip("Max enemies allowed on screen simultaneously in Chamber 1.")]
    public int baseMaxEnemies = 12;
    public float minSpawnDistance = 4.5f;
    public float maxSpawnDistance = 18.0f;

    [Header("Per-Chamber Escalation Dials")]
    [Tooltip("How much faster enemies spawn with each chamber cleared (in seconds).")]
    public float spawnIntervalReductionPerChamber = 0.08f;
    [Tooltip("Absolute fastest spawn rate ceiling (so the game never spawns 60 enemies/sec).")]
    public float minimumIntervalFloor = 0.25f;
    [Tooltip("How many extra enemies can exist at once per chamber cleared.")]
    public int additionalEnemiesPerChamber = 2;

    [Header("Telemetry (Read-only)")]
    public int currentChamber = 1;
    public float currentSpawnInterval;
    public int currentMaxSimultaneousEnemies;

    private float spawnTimer = 0f;
    private Transform playerTransform;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        ActiveEnemyCount = 0;
        RecalculateChamberStats();
    }

    void Start()
    {
        CachePlayer();
    }

    void Update()
    {
        if (TimeManager.Instance != null && (TimeManager.Instance.isDead || TimeManager.Instance.isPaused))
            return;

        if (enemyRoster.Count == 0) return;

        spawnTimer += Time.deltaTime;
        if (spawnTimer >= currentSpawnInterval)
        {
            spawnTimer = 0f;
            if (ActiveEnemyCount < currentMaxSimultaneousEnemies)
            {
                SpawnRandomEnemyFromRoster();
            }
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

    /// <summary>
    /// Recalculates difficulty based on current chamber number.
    /// Formula: BaseInterval - ((Chamber - 1) * Decay)
    /// </summary>
    public void RecalculateChamberStats()
    {
        // Faster trickle with every chamber
        float intervalReduction = (currentChamber - 1) * spawnIntervalReductionPerChamber;
        currentSpawnInterval = Mathf.Max(minimumIntervalFloor, baseSpawnInterval - intervalReduction);

        // Larger swarm cap with every chamber
        currentMaxSimultaneousEnemies = baseMaxEnemies + ((currentChamber - 1) * additionalEnemiesPerChamber);
    }

    /// <summary>
    /// Call this whenever the player clears a room / selects a perk
    /// </summary>
    public void AdvanceChamber(int newChamberNumber)
    {
        currentChamber = newChamberNumber;
        RecalculateChamberStats();
    }

    public void SpawnRandomEnemyFromRoster()
    {
        if (playerTransform == null) CachePlayer();
        if (playerTransform == null || enemyRoster.Count == 0) return;

        EnemyTypeConfig selectedType = PickWeightedEnemyConfig();
        if (selectedType == null || selectedType.enemyPrefab == null) return;

        Vector3 spawnPos = Vector3.zero;

        if (ProceduralMazeGenerator.Instance != null)
        {
            spawnPos = ProceduralMazeGenerator.Instance.GetValidSpawnPosition(
                playerTransform.position,
                minSpawnDistance,
                maxSpawnDistance
            );
        }
        else
        {
            Vector2 randomCircle = Random.insideUnitCircle.normalized * Random.Range(minSpawnDistance, maxSpawnDistance);
            spawnPos = new Vector3(playerTransform.position.x + randomCircle.x, 0.5f, playerTransform.position.z + randomCircle.y);
        }

        GameObject spawnedNPC = Instantiate(selectedType.enemyPrefab, spawnPos, Quaternion.identity);

        EnemyTarget targetComp = spawnedNPC.GetComponent<EnemyTarget>();
        if (targetComp != null)
        {
            targetComp.health = selectedType.health;
            targetComp.chaseSpeed = selectedType.chaseSpeed;
            targetComp.timeReward = selectedType.timeGainedOnKill;
            targetComp.timePenaltyOnHit = selectedType.timeLostOnCollision;
        }
    }

    EnemyTypeConfig PickWeightedEnemyConfig()
    {
        float totalWeight = 0f;
        for (int i = 0; i < enemyRoster.Count; i++)
        {
            if (enemyRoster[i].enemyPrefab != null)
                totalWeight += enemyRoster[i].spawnWeight;
        }

        if (totalWeight <= 0f) return null;

        float randomVal = Random.Range(0f, totalWeight);
        float runningSum = 0f;

        for (int i = 0; i < enemyRoster.Count; i++)
        {
            if (enemyRoster[i].enemyPrefab == null) continue;
            runningSum += enemyRoster[i].spawnWeight;
            if (randomVal <= runningSum)
                return enemyRoster[i];
        }

        return enemyRoster[0];
    }
}