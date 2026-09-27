using System.Collections.Generic;
using UnityEngine;

public class ProceduralMazeGenerator : MonoBehaviour
{
    public static ProceduralMazeGenerator Instance;

    [Header("Drag & Drop Object References")]
    [Tooltip("Drag your ExitGate GameObject from the Hierarchy here.")]
    public GameObject exitGateObject;
    [Tooltip("Drag your Player GameObject here (optional, auto-finds if empty).")]
    public GameObject playerObject;

    [Header("Maze Size & Corridor Width")]
    [Tooltip("Corridor and room tile width in world units. 5.0 to 7.0 provides wide combat arenas.")]
    [Range(4.0f, 10.0f)] public float corridorWidth = 5.5f;
    [Tooltip("Number of cells across the horizontal axis (Odd numbers recommended)")]
    [Range(9, 35)] public int mazeColumns = 15;
    [Tooltip("Number of cells across the vertical axis (Odd numbers recommended)")]
    [Range(9, 35)] public int mazeRows = 15;
    public float wallHeight = 2.8f;

    [Header("Open Space & Arena Settings")]
    [Tooltip("Radius of the safe open room around the player at spawn (2 = 5x5 open area)")]
    [Range(1, 4)] public int spawnRoomRadius = 2;
    [Tooltip("Radius of the open room around the exit gate (2 = 5x5 open area)")]
    [Range(1, 4)] public int exitRoomRadius = 2;
    [Tooltip("How many large open arena rooms to carve in the middle of the maze")]
    [Range(0, 6)] public int bonusCombatRooms = 3;
    [Tooltip("Radius of bonus arena rooms (1 = 3x3, 2 = 5x5)")]
    [Range(1, 3)] public int bonusRoomRadius = 2;

    [Header("Exit Gate Randomization")]
    [Tooltip("If checked, the exit gate picks a random distant room each run instead of a fixed corner.")]
    public bool randomizeExitGateLocation = true;
    [Tooltip("Minimum distance in world units the exit must be from the player")]
    public float minExitDistanceFromPlayer = 20f;

    [Header("Corridor Flow (Loops vs Dead Ends)")]
    [Range(0f, 1f)]
    [Tooltip("Higher = more loops and connected flanking routes. 0.5+ ensures no dead ends.")]
    public float corridorLoopFactor = 0.55f;

    [Header("3D Modular Assets (Optional)")]
    public GameObject customWallPrefab;
    public GameObject customFloorPrefab;
    public GameObject customPillarPrefab;
    public Material fallbackWallMaterial;
    public Color fallbackWallColor = new Color(0.18f, 0.18f, 0.22f);

    public int[,] grid; // 1 = Wall, 0 = Open Floor
    private Transform mazeParent;
    private List<Vector3> openFloorPositions = new List<Vector3>();

    // Flowfield enemy navigation
    private Vector2Int[,] flowNextStep;
    private float flowfieldTimer = 0f;
    private Vector3 currentSpawnWorldPos;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (playerObject == null)
        {
            PlayerController25D p = FindFirstObjectByType<PlayerController25D>();
            if (p != null) playerObject = p.gameObject;
        }

        if (exitGateObject == null)
        {
            ChamberExit exit = FindFirstObjectByType<ChamberExit>();
            if (exit != null) exitGateObject = exit.gameObject;
        }

        GenerateNewLevel();
    }

    void Update()
    {
        flowfieldTimer += Time.deltaTime;
        if (flowfieldTimer >= 0.15f)
        {
            flowfieldTimer = 0f;
            UpdatePlayerFlowfield();
        }
    }

    public void GenerateNewLevel()
    {
        if (mazeParent != null)
        {
            Destroy(mazeParent.gameObject);
        }

        openFloorPositions.Clear();
        mazeParent = new GameObject("Procedural_Geometry_Container").transform;
        mazeParent.parent = transform;

        // Ensure odd grid dimensions for proper maze algorithms
        int w = (mazeColumns % 2 == 0) ? mazeColumns + 1 : mazeColumns;
        int h = (mazeRows % 2 == 0) ? mazeRows + 1 : mazeRows;

        grid = new int[w, h];
        flowNextStep = new Vector2Int[w, h];

        // Fill with walls initially
        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                grid[x, y] = 1;
            }
        }

        // 1. Carve corridors
        CarveMaze(w, h, 1, 1);

        // 2. Clear guaranteed wide spawn zone
        Vector2Int spawnGridPos = new Vector2Int(spawnRoomRadius + 1, spawnRoomRadius + 1);
        ClearRoom(w, h, spawnGridPos.x, spawnGridPos.y, spawnRoomRadius);

        // 3. Clear bonus arena battle rooms across the map
        CarveRandomArenaRooms(w, h);

        // 4. Determine Exit Gate Grid Position
        Vector2Int exitGridPos = DetermineExitPosition(w, h, spawnGridPos);
        ClearRoom(w, h, exitGridPos.x, exitGridPos.y, exitRoomRadius);

        // 5. Braid corridors to remove dead ends
        BraidDeadEnds(w, h);

        // 6. Build 3D walls & floor
        Build3DGeometry(w, h);

        // 7. Place player and gate in safe world coordinates
        currentSpawnWorldPos = GridToWorld(w, h, spawnGridPos.x, spawnGridPos.y);
        currentSpawnWorldPos.y = 0.5f;

        Vector3 exitWorldPos = GridToWorld(w, h, exitGridPos.x, exitGridPos.y);
        exitWorldPos.y = 0.1f;

        PositionEntities(currentSpawnWorldPos, exitWorldPos, w, h);
        UpdatePlayerFlowfield();
    }

    void CarveMaze(int w, int h, int startX, int startY)
    {
        grid[startX, startY] = 0;
        List<Vector2Int> stack = new List<Vector2Int> { new Vector2Int(startX, startY) };

        Vector2Int[] directions = {
            new Vector2Int(0, 2),
            new Vector2Int(0, -2),
            new Vector2Int(2, 0),
            new Vector2Int(-2, 0)
        };

        while (stack.Count > 0)
        {
            Vector2Int current = stack[stack.Count - 1];
            List<Vector2Int> unvisitedNeighbors = new List<Vector2Int>();

            for (int i = 0; i < directions.Length; i++)
            {
                int nx = current.x + directions[i].x;
                int ny = current.y + directions[i].y;

                if (nx > 0 && nx < w - 1 && ny > 0 && ny < h - 1 && grid[nx, ny] == 1)
                {
                    unvisitedNeighbors.Add(directions[i]);
                }
            }

            if (unvisitedNeighbors.Count > 0)
            {
                Vector2Int chosenDir = unvisitedNeighbors[Random.Range(0, unvisitedNeighbors.Count)];
                int wallX = current.x + (chosenDir.x / 2);
                int wallY = current.y + (chosenDir.y / 2);
                int nextX = current.x + chosenDir.x;
                int nextY = current.y + chosenDir.y;

                grid[wallX, wallY] = 0;
                grid[nextX, nextY] = 0;

                stack.Add(new Vector2Int(nextX, nextY));
            }
            else
            {
                stack.RemoveAt(stack.Count - 1);
            }
        }
    }

    void ClearRoom(int w, int h, int cx, int cy, int radius)
    {
        for (int x = cx - radius; x <= cx + radius; x++)
        {
            for (int y = cy - radius; y <= cy + radius; y++)
            {
                if (x > 0 && x < w - 1 && y > 0 && y < h - 1)
                {
                    grid[x, y] = 0;
                }
            }
        }
    }

    void CarveRandomArenaRooms(int w, int h)
    {
        for (int i = 0; i < bonusCombatRooms; i++)
        {
            int rx = Random.Range(3, w - 4);
            int ry = Random.Range(3, h - 4);
            ClearRoom(w, h, rx, ry, bonusRoomRadius);
        }
    }

    Vector2Int DetermineExitPosition(int w, int h, Vector2Int spawnGrid)
    {
        if (!randomizeExitGateLocation)
        {
            return new Vector2Int(w - 2 - exitRoomRadius, h - 2 - exitRoomRadius);
        }

        List<Vector2Int> candidates = new List<Vector2Int>();
        for (int x = 2; x < w - 2; x++)
        {
            for (int y = 2; y < h - 2; y++)
            {
                Vector3 worldPos = GridToWorld(w, h, x, y);
                Vector3 spawnPos = GridToWorld(w, h, spawnGrid.x, spawnGrid.y);

                if (Vector3.Distance(worldPos, spawnPos) >= minExitDistanceFromPlayer)
                {
                    candidates.Add(new Vector2Int(x, y));
                }
            }
        }

        if (candidates.Count > 0)
        {
            return candidates[Random.Range(0, candidates.Count)];
        }

        return new Vector2Int(w - 2 - exitRoomRadius, h - 2 - exitRoomRadius);
    }

    void BraidDeadEnds(int w, int h)
    {
        Vector2Int[] cardinal = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        for (int x = 1; x < w - 1; x++)
        {
            for (int y = 1; y < h - 1; y++)
            {
                if (grid[x, y] == 0)
                {
                    int wallCount = 0;
                    for (int i = 0; i < cardinal.Length; i++)
                    {
                        if (grid[x + cardinal[i].x, y + cardinal[i].y] == 1) wallCount++;
                    }

                    if (wallCount >= 3 && Random.value < corridorLoopFactor)
                    {
                        List<Vector2Int> breakable = new List<Vector2Int>();
                        for (int i = 0; i < cardinal.Length; i++)
                        {
                            int tx = x + cardinal[i].x;
                            int ty = y + cardinal[i].y;
                            if (tx > 0 && tx < w - 1 && ty > 0 && ty < h - 1)
                            {
                                breakable.Add(new Vector2Int(tx, ty));
                            }
                        }

                        if (breakable.Count > 0)
                        {
                            Vector2Int pick = breakable[Random.Range(0, breakable.Count)];
                            grid[pick.x, pick.y] = 0;
                        }
                    }
                }
            }
        }
    }

    void Build3DGeometry(int w, int h)
    {
        float xOffset = (w * corridorWidth) / 2f;
        float zOffset = (h * corridorWidth) / 2f;

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                Vector3 worldPos = new Vector3((x * corridorWidth) - xOffset, 0f, (y * corridorWidth) - zOffset);

                if (grid[x, y] == 1)
                {
                    if (customWallPrefab != null)
                    {
                        GameObject wallObj = Instantiate(customWallPrefab, worldPos, Quaternion.identity, mazeParent);
                        EnsureCollider(wallObj);
                    }
                    else
                    {
                        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        wall.name = $"Wall_{x}_{y}";
                        wall.transform.parent = mazeParent;
                        wall.transform.position = new Vector3(worldPos.x, wallHeight / 2f, worldPos.z);
                        wall.transform.localScale = new Vector3(corridorWidth, wallHeight, corridorWidth);

                        Renderer r = wall.GetComponent<Renderer>();
                        if (fallbackWallMaterial != null) r.material = fallbackWallMaterial;
                        else r.material.color = fallbackWallColor;
                    }

                    if (customPillarPrefab != null && (x % 2 == 0 && y % 2 == 0))
                    {
                        Instantiate(customPillarPrefab, worldPos, Quaternion.identity, mazeParent);
                    }
                }
                else
                {
                    openFloorPositions.Add(new Vector3(worldPos.x, 0.5f, worldPos.z));

                    if (customFloorPrefab != null)
                    {
                        Instantiate(customFloorPrefab, new Vector3(worldPos.x, 0f, worldPos.z), Quaternion.identity, mazeParent);
                    }
                }
            }
        }
    }

    void EnsureCollider(GameObject obj)
    {
        if (obj.GetComponent<Collider>() == null && obj.GetComponentInChildren<Collider>() == null)
        {
            BoxCollider box = obj.AddComponent<BoxCollider>();
            box.size = new Vector3(corridorWidth, wallHeight, corridorWidth);
            box.center = new Vector3(0f, wallHeight / 2f, 0f);
        }
    }

    void PositionEntities(Vector3 spawnPos, Vector3 exitPos, int w, int h)
    {
        // 1. Move Player
        if (playerObject != null)
        {
            playerObject.transform.position = spawnPos;
            Rigidbody rb = playerObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.position = spawnPos;
                rb.linearVelocity = Vector3.zero;
            }
        }

        // 2. Move Exit Gate
        if (exitGateObject != null)
        {
            exitGateObject.transform.position = exitPos;
        }

        // 3. Auto-fit Ground plane to cover whole maze area
        GameObject ground = GameObject.Find("Ground");
        if (ground != null && customFloorPrefab == null)
        {
            ground.transform.position = new Vector3(0f, -0.05f, 0f);
            ground.transform.localScale = new Vector3((w * corridorWidth) + 8f, 0.1f, (h * corridorWidth) + 8f);
        }
    }

    // --- ENEMY PATHFINDING & FLOWFIELD ---
    void UpdatePlayerFlowfield()
    {
        if (playerObject == null || grid == null) return;

        int w = grid.GetLength(0);
        int h = grid.GetLength(1);

        Vector2Int pGrid = WorldToGrid(w, h, playerObject.transform.position);
        if (pGrid.x < 0 || pGrid.x >= w || pGrid.y < 0 || pGrid.y >= h) return;

        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        bool[,] visited = new bool[w, h];

        queue.Enqueue(pGrid);
        visited[pGrid.x, pGrid.y] = true;
        flowNextStep[pGrid.x, pGrid.y] = pGrid;

        Vector2Int[] cardinal = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        while (queue.Count > 0)
        {
            Vector2Int curr = queue.Dequeue();

            for (int i = 0; i < cardinal.Length; i++)
            {
                int nx = curr.x + cardinal[i].x;
                int ny = curr.y + cardinal[i].y;

                if (nx >= 0 && nx < w && ny >= 0 && ny < h)
                {
                    if (!visited[nx, ny] && grid[nx, ny] == 0)
                    {
                        visited[nx, ny] = true;
                        flowNextStep[nx, ny] = curr;
                        queue.Enqueue(new Vector2Int(nx, ny));
                    }
                }
            }
        }
    }

    public Vector3 GetNextWaypointForEnemy(Vector3 enemyPos, Vector3 targetPos)
    {
        if (grid == null) return targetPos;

        int w = grid.GetLength(0);
        int h = grid.GetLength(1);

        // Line of sight check
        Vector3 rayStart = enemyPos; rayStart.y = 0.6f;
        Vector3 rayEnd = targetPos; rayEnd.y = 0.6f;
        Vector3 diff = rayEnd - rayStart;

        if (!Physics.Raycast(rayStart, diff.normalized, diff.magnitude))
        {
            return targetPos;
        }

        Vector2Int eGrid = WorldToGrid(w, h, enemyPos);
        if (eGrid.x >= 0 && eGrid.x < w && eGrid.y >= 0 && eGrid.y < h && flowNextStep != null)
        {
            Vector2Int next = flowNextStep[eGrid.x, eGrid.y];
            Vector3 target = GridToWorld(w, h, next.x, next.y);
            target.y = enemyPos.y;
            return target;
        }

        return targetPos;
    }

    public Vector2Int WorldToGrid(int w, int h, Vector3 worldPos)
    {
        float xOffset = (w * corridorWidth) / 2f;
        float zOffset = (h * corridorWidth) / 2f;

        int x = Mathf.RoundToInt((worldPos.x + xOffset) / corridorWidth);
        int y = Mathf.RoundToInt((worldPos.z + zOffset) / corridorWidth);

        x = Mathf.Clamp(x, 0, w - 1);
        y = Mathf.Clamp(y, 0, h - 1);
        return new Vector2Int(x, y);
    }

    public Vector3 GridToWorld(int w, int h, int x, int y)
    {
        float xOffset = (w * corridorWidth) / 2f;
        float zOffset = (h * corridorWidth) / 2f;
        return new Vector3((x * corridorWidth) - xOffset, 0.5f, (y * corridorWidth) - zOffset);
    }

    public Vector3 GetValidSpawnPosition(Vector3 fromPos, float minDistance, float maxDistance)
    {
        if (openFloorPositions.Count == 0) return Vector3.zero;

        List<Vector3> valid = new List<Vector3>();
        for (int i = 0; i < openFloorPositions.Count; i++)
        {
            float dist = Vector3.Distance(fromPos, openFloorPositions[i]);
            if (dist >= minDistance && dist <= maxDistance)
            {
                valid.Add(openFloorPositions[i]);
            }
        }

        if (valid.Count > 0)
        {
            return valid[Random.Range(0, valid.Count)];
        }

        return openFloorPositions[Random.Range(openFloorPositions.Count / 2, openFloorPositions.Count)];
    }
}