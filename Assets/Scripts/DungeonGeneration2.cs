using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.UIElements;

public class DungeonGenerator2 : MonoBehaviour
{
    [Header("Dungeon Settings")]
    public int SIZE = 100;
    //public int numberOfRooms = 50;
    public int wMin = 5; 
    public int hMin = 5;
    private int intersectLength = 2;//the length of the intersection between the rooms
    public int deletePercent = 0;
    public float secondsForTest = 0;

    [Space]
    public bool noRoomLooping = true;

    public bool placeAssetsCubes = false;

    public bool placeAssetsMarchingSquares = false;

    [Header("Required Assets")]

    public NavMeshSurface navMesh;

    public GameObject wallPrefab;

    public GameObject floor;

    public PlayerController player;

    private DebugDrawingBatcher rectIntDrawingBatcher;

    public enum ExecutionMode { Coroutine, Instant, Step_by_step }
    [Header("Execution Settings")]
    public ExecutionMode executionMode = ExecutionMode.Coroutine;
    public KeyCode instantKey = KeyCode.S;    
    public KeyCode skipKey = KeyCode.Space;


    [Header("Seed Settings")]
    public bool UseSeed = false;
    public string seed;  

    [Header("Lists")]

    public Graph<RectInt> originalGraph;
    private Graph<RectInt> backUpGraph;
    public List<RectInt> doors;
    public List<RectInt> rooms;

    private System.Random seededRandom;

    private int distanceFromEdgeH;
    private int distanceFromEdgeW;
    private RectInt bigRoom;

    private int activeSplittingCoroutines = 0;

    [HideInInspector]
    public bool skipThisStep = false;

    public static DungeonGenerator2 Instance { get; private set; }

    /// <summary>
    /// Initializes the singleton instance of the dungeon generator.
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Starts dungeon generation and applies initial seed logic.
    /// </summary>
    private void Start()
    {
        rectIntDrawingBatcher = DebugDrawingBatcher.GetInstance();
        doors = new List<RectInt>();
        originalGraph = new Graph<RectInt>();

        InitializeRandom();
        if (UseSeed)
        {
            ValidateParameters();
        }
        GenerateDungeon();
    }
    /// <summary>
    /// Handles key inputs to switch generation modes during runtime.
    /// </summary>
    private void Update()
    {
        if (CheckExecutionMode() && Input.GetKeyDown(instantKey))
        {
            executionMode = ExecutionMode.Instant;                 
        }
        if (CheckExecutionMode() && Input.GetKeyDown(skipKey))
        {
            executionMode = ExecutionMode.Step_by_step;
            skipThisStep = true;
        }
        
    }

    #region Seed and Random set
    /// <summary>
    /// Initializes the random number generator using a seed if enabled.
    /// </summary>
    private void InitializeRandom()
    {
        if (UseSeed)
        {
            int numericSeed;

            if (string.IsNullOrEmpty(seed))
            {
                numericSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            }
            else
            {
                numericSeed = seed.GetHashCode();
            }

            seededRandom = new System.Random(numericSeed);
        }
        else
        {
            seededRandom = new System.Random();
        }
    }
    /// <summary>
    /// Adjusts dungeon generation parameters based on seed.
    /// </summary>
    private void ValidateParameters()
    {
        SIZE = seededRandom.Next(50, 200);
        SIZE = Mathf.Clamp(SIZE, 50, 200);
        //numberOfRooms = seededRandom.Next(10, 300);
        //numberOfRooms = Mathf.Clamp(numberOfRooms, 10, 300);
        wMin = seededRandom.Next(5, 30);
        wMin = Mathf.Clamp(wMin, 3, 40);
        hMin = seededRandom.Next(5, 30);
        hMin = Mathf.Clamp(hMin, 3, 40);
        if (intersectLength < 2)
        {
            intersectLength = 2;
        }
    }
    #endregion
    /// <summary>
    /// Entry point to begin dungeon generation via recursive room splitting.
    /// </summary>
    private void GenerateDungeon()
    {
        bigRoom = new RectInt(0, 0, SIZE, SIZE);
        distanceFromEdgeH = hMin + 2 * intersectLength;
        distanceFromEdgeW = wMin + 2 * intersectLength;
        rooms = new List<RectInt>();
        AlgorithmsUtils.DebugRectInt(bigRoom, Color.red, 100f);
        if (seededRandom.Next(0, 2) == 1)
        {
            StartCoroutine(HorizontalSplit(rooms, bigRoom));
        }
        else
        {
            StartCoroutine(VerticalSplit(rooms, bigRoom));
        }
        StartCoroutine(StepsToFinish());
    }

    #region Spliting Methods
    /// <summary>
    /// Attempts to split a room horizontally and recursively split the resulting subrooms.
    /// </summary>
    public IEnumerator HorizontalSplit(List<RectInt> rooms, RectInt room, bool trySplitingTheOpposite = true)
    {
        if (NotBigEnough(room) || OutOfBounds(room)/*|| rooms.Count >= numberOfRooms*/)
        {
            yield break;
        }
        int splitPoint = CalculateHorizontalSplitPoint(room);
        if (splitPoint == -1)
        {
            if (trySplitingTheOpposite)
            {
                if(CheckExecutionMode() && !skipThisStep)
                {
                    yield return StartCoroutine(VerticalSplit(rooms, room, false));
                }
                else
                {
                    StartCoroutine(VerticalSplit(rooms,room, false));
                }
            }
            yield break;
        }
        activeSplittingCoroutines++;
        RectInt roomA = new RectInt(room.x, room.y, room.width, splitPoint + intersectLength);
        RectInt roomB = new RectInt(room.x, room.y + splitPoint, room.width, room.height - splitPoint);

        rooms.Remove(room);
        rooms.Add(roomA);
        rooms.Add(roomB);

        rectIntDrawingBatcher.BatchCall(() => AlgorithmsUtils.DebugRectInt(roomA, Color.yellow, 1f));
        rectIntDrawingBatcher.BatchCall(() => AlgorithmsUtils.DebugRectInt(roomB, Color.yellow, 1f));
        if (CheckExecutionMode() && !skipThisStep)
        {
            yield return new WaitForSeconds(secondsForTest);
            yield return StartCoroutine(VerticalSplit(rooms, roomA));
            yield return StartCoroutine(VerticalSplit(rooms, roomB));
        }
        else
        {
            StartCoroutine(VerticalSplit(rooms, roomA));
            StartCoroutine(VerticalSplit(rooms, roomB));
        }
        activeSplittingCoroutines--;
    }

    /// <summary>
    /// Tries to find a valid horizontal split point within a room.
    /// </summary>
    private int CalculateHorizontalSplitPoint(RectInt room)
    {
        int splitPoint = -1;
        int c = 0;
        int minSplit = distanceFromEdgeW;
        int maxSplit = room.height - distanceFromEdgeW;

        do
        {
            c++;
            if (minSplit > maxSplit)
            {
                continue;
            }
            splitPoint = seededRandom.Next(minSplit, maxSplit);

            // Ensure valid split point
            if (splitPoint + intersectLength <= room.height - intersectLength &&
                splitPoint - intersectLength >= intersectLength)
            {
                break;
            }
        }
        while (c < 100); // Max 100 attempts

        return splitPoint;
    }
    /// <summary>
    /// Attempts to split a room vertically and recursively split the resulting subrooms.
    /// </summary>
    public IEnumerator VerticalSplit(List<RectInt> rooms, RectInt room, bool trySplitingTheOpposite = true)
    {
        if (NotBigEnough(room) || OutOfBounds(room)/*|| rooms.Count >= numberOfRooms*/)
        {
            yield break;
        }
        int splitPoint = CalculateVerticalSplitPoint(room);
        if (splitPoint == -1)
        {
            if (trySplitingTheOpposite)
            {
                if(CheckExecutionMode() && !skipThisStep)
                {
                    yield return StartCoroutine(HorizontalSplit(rooms, room, false));
                }
                else
                {
                    StartCoroutine(HorizontalSplit(rooms, room, false));
                }
                
            }
            //Debug.LogWarning("Invalid split point detected.");
            yield break;
        }
        activeSplittingCoroutines++;
        RectInt roomA = new RectInt(room.x, room.y, splitPoint + intersectLength, room.height);

        RectInt roomB = new RectInt(room.x + splitPoint, room.y, room.width - splitPoint, room.height);
        rooms.Remove(room);
        rooms.Add(roomA);
        rooms.Add(roomB);


        rectIntDrawingBatcher.BatchCall(() => AlgorithmsUtils.DebugRectInt(roomA, Color.yellow, 1f));
        rectIntDrawingBatcher.BatchCall(() => AlgorithmsUtils.DebugRectInt(roomB, Color.yellow, 1f));
        if(CheckExecutionMode() && !skipThisStep)
        {
            yield return new WaitForSeconds(secondsForTest);
            yield return StartCoroutine(HorizontalSplit(rooms, roomA));
            yield return StartCoroutine(HorizontalSplit(rooms, roomB));
        }
        else
        {
            StartCoroutine(HorizontalSplit(rooms, roomA));
            StartCoroutine(HorizontalSplit(rooms, roomB));
        }


        activeSplittingCoroutines--;
    }
    /// <summary>
    /// Tries to find a valid vertical split point within a room.
    /// </summary>
    private int CalculateVerticalSplitPoint(RectInt room)
    {
        int splitPoint = -1;
        int c = 0;

        int minSplit = distanceFromEdgeH;
        int maxSplit = room.width - distanceFromEdgeH;

        do
        {
            c++;
            if (minSplit > maxSplit)
            {
                continue;
            }
            splitPoint = seededRandom.Next(minSplit, maxSplit);

            if (splitPoint + intersectLength <= room.width - intersectLength &&
                splitPoint - intersectLength >= intersectLength)
            {
                break;
            }
        }
        while (c < 100);

        return splitPoint;

    }

    #region Room Checks
    /// <summary>
    /// Checks if a room is too small to split further.
    /// </summary>
    private bool NotBigEnough(RectInt room)
    {
        return room.width <= distanceFromEdgeW && room.height <= distanceFromEdgeH;
    }
    /// <summary>
    /// Checks if a room is going ot of the dungeon bounds.
    /// </summary>
    private bool OutOfBounds(RectInt room)
    {
        return room.x < 0 || room.y < 0 || (room.x + room.width) > SIZE || (room.y + room.height) > SIZE;
    }
    #endregion


    #endregion
    /// <summary>
    /// Coroutine that executes the main dungeon generation steps in sequence.
    /// </summary>
    private IEnumerator StepsToFinish()
    {
        yield return new WaitUntil(() => activeSplittingCoroutines == 0);
        skipThisStep = false;

        Debug.Log("Splitting finished");

        yield return StartCoroutine(DoorChecker());
        skipThisStep = false;
        yield return StartCoroutine(DeleteSmallRooms());
        skipThisStep = false;
        Debug.Log("Redrawing the dungeon");
        if (noRoomLooping)
        {
            yield return StartCoroutine(MakeTheGraphAsTree());
        }
        yield return StartCoroutine(DrawDungeon());
        skipThisStep = false;
        TileMapGenerator.Instance.GenerateTileMap();
        yield return StartCoroutine(GraphCreator(true));
        skipThisStep = false;
        Debug.Log("Dungeon Finished");
        if (placeAssetsCubes)
        {
            yield return StartCoroutine(SpawnDungeonAssets());
            BakeNavMesh();
        }
        if (placeAssetsMarchingSquares && !placeAssetsCubes)
        {
            Debug.Log("Generating Tile Map");
            TileMapGenerator.Instance.GenerateTileMap();
            Debug.Log("Placing wall assets");
            StartCoroutine(TileMapGenerator.Instance.SpawnWalls());
            Debug.Log("Floor fill");
            FloorFillSpawner.Instance.FloorFill();
            yield return new WaitUntil(() => FloorFillSpawner.Instance.floorPlaced == true);
            BakeNavMesh();
        }
        FixPlayerPosition();
        Debug.Log("The dungeon is finished for " + Time.time + " second.");

        yield break;

    }

    /// <summary>
    /// Finds and stores doors between rooms based on intersections.
    /// </summary>
    private IEnumerator DoorChecker()
    {
        Debug.Log("Entered DoorChecker()");
        for (int i = 0; i < rooms.Count; i++)
        {
            RectInt roomA = rooms[i];
            for (int j = i + 1; j < rooms.Count; j++)
            {
                RectInt roomB = rooms[j];
                if (roomA.Equals(roomB)) continue;

                if (AlgorithmsUtils.Intersects(roomA, roomB))
                {
                    RectInt intersection = AlgorithmsUtils.Intersect(roomA, roomB);

                    if (intersection.width < wMin && intersection.height < hMin) continue;
                    if (intersection.width > intersection.height)
                    {
                        if (intersectLength + 2 > intersection.width - (2 + intersectLength))
                        {
                            RectInt door1 = new RectInt(intersection.x + intersection.width / 2, intersection.y, intersectLength, intersectLength);
                            if(CheckExecutionMode() && !skipThisStep)
                            {
                                yield return new WaitForSeconds(secondsForTest);
                            }
                            
                            rectIntDrawingBatcher.BatchCall(() => AlgorithmsUtils.DebugRectInt(door1, Color.blue, 1f));
                            doors.Add(door1);
                            continue;
                        }

                        RectInt door = new RectInt(intersection.x + seededRandom.Next(intersectLength + 2, intersection.width - (2 + intersectLength)), intersection.y, intersectLength, intersectLength);
                        if (CheckExecutionMode() && !skipThisStep)
                        {
                            yield return new WaitForSeconds(secondsForTest);
                        }
                      
                        rectIntDrawingBatcher.BatchCall(() => AlgorithmsUtils.DebugRectInt(door, Color.cyan, 1f));
                        doors.Add(door);
                    }
                    else
                    {
                        if (intersectLength + 2 > intersection.height - (2 + intersectLength))
                        {
                            RectInt door1 = new RectInt(intersection.x, intersection.y + intersection.height / 2, intersectLength, intersectLength);
                            if(CheckExecutionMode() && !skipThisStep)
                            {
                                yield return new WaitForSeconds(secondsForTest);
                            }
                            rectIntDrawingBatcher.BatchCall(() => AlgorithmsUtils.DebugRectInt(door1, Color.blue, 1f));
                            doors.Add(door1);
                            continue;
                        }

                        RectInt door = new RectInt(intersection.x, intersection.y + seededRandom.Next(intersectLength + 2, intersection.height - (2 + intersectLength)), intersectLength, intersectLength);
                        if (CheckExecutionMode() && !skipThisStep)
                            yield return new WaitForSeconds(secondsForTest);
                        rectIntDrawingBatcher.BatchCall(() => AlgorithmsUtils.DebugRectInt(door, Color.cyan, 1f));


                        doors.Add(door);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Deletes a percentage of small rooms while preserving graph connectivity.
    /// </summary>
    private IEnumerator DeleteSmallRooms()
    {
        Debug.Log("Starts deleting " + deletePercent + "% of the rooms");

        List<RectInt> orderRooms = rooms.OrderBy(room => room.width * room.height).ToList();
        int numberOfRoomsToDelete = (int)(orderRooms.Count * deletePercent / 100);
        int removedCount = 0;
        yield return StartCoroutine(GraphCreator(false));
        backUpGraph = originalGraph.Clone();
        foreach (var roomToDelete in orderRooms)
        {
            if (removedCount >= numberOfRoomsToDelete) break;
            // All doors in tthe room to delete
            List<RectInt> doorInRoom = new List<RectInt>();

            foreach (var door in doors)
            {
                if (AlgorithmsUtils.Intersects(roomToDelete, door))
                {
                    doorInRoom.Add(door);
                }
            }

            foreach (var door in doorInRoom)
            {
                originalGraph.RemoveNode(door);
            }

            originalGraph.RemoveNode(roomToDelete);

            foreach (var door in doorInRoom)
            {
                if (!doors.Any(d => AlgorithmsUtils.Intersects(d, door)))
                    originalGraph.RemoveNode(door);
            }

            if (originalGraph.IsFullyConnected())
            {
                rooms.Remove(roomToDelete);
                foreach (var door in doorInRoom)
                {
                    doors.Remove(door);
                }
                removedCount++;
                AlgorithmsUtils.DebugRectInt(roomToDelete, Color.red, 3f);
                backUpGraph = originalGraph.Clone();

                if (CheckExecutionMode() && !skipThisStep)
                {
                    yield return new WaitForSeconds(secondsForTest);
                }
                
            }
            else
            {
                originalGraph = backUpGraph.Clone();
            }
        }
        Debug.Log(removedCount + " out of " + numberOfRoomsToDelete + " are deleted");
    }

    #region Graph Methods

    /// <summary>
    /// Builds a graph from rooms and doors with visual debug output.
    /// </summary>
    private IEnumerator GraphCreator(bool stepByStep = false)
    {
        originalGraph.Clear();
        List<RectInt> visited = new List<RectInt>();
        for (int i = 0; i < rooms.Count; i++)
        {
            originalGraph.AddNode(rooms[i]);
            visited.Add(rooms[i]);
            Vector3 roomPosition = new Vector3(rooms[i].center.x, 0, rooms[i].center.y);
            if (stepByStep)
            {
                DebugExtension.DebugWireSphere(roomPosition, Color.red, 1, 100);
                if(CheckExecutionMode() && !skipThisStep)
                {
                    yield return new WaitForSeconds(secondsForTest);
                }
                
            }
            for (int j = 0; j < doors.Count; j++)
            {
                if (AlgorithmsUtils.Intersects(rooms[i], doors[j]))
                {
                    Vector3 doorPosition = new Vector3(doors[j].center.x, 0, doors[j].center.y);

                    if (!visited.Contains(rooms[i]))
                    {
                        originalGraph.AddNode(doors[j]);
                        visited.Add(doors[j]);
                        if (stepByStep)
                        {
                            DebugExtension.DebugWireSphere(doorPosition, Color.red, 1, 100);
                            if (CheckExecutionMode() && !skipThisStep) { yield return new WaitForSeconds(secondsForTest); }
                         
                        }
                    }

                    originalGraph.AddEdge(rooms[i], doors[j]);
                    if (stepByStep)
                    {
                        Debug.DrawLine(roomPosition, doorPosition, Color.red, 100f);
                        if (CheckExecutionMode() && !skipThisStep)
                        {
                            yield return new WaitForSeconds(secondsForTest);
                        }
                    }

                }
            }
        }
    }

    /// <summary>
    /// Converts the graph into a tree to remove loops.
    /// </summary>
    private IEnumerator MakeTheGraphAsTree()
    {
        yield return StartCoroutine(GraphCreator());
        originalGraph = originalGraph.BFSTree();
        HashSet<RectInt> validDoors = new HashSet<RectInt>();
        List<RectInt> doorsToRemove = new List<RectInt>();
        foreach (var door in doors)
        {
            List<RectInt> connectedRooms = new List<RectInt>();
            connectedRooms = originalGraph.GetNeighbors(door);
            if (connectedRooms.Count == 2)
            {
                continue;
            }
            else
            {
                foreach (var room in connectedRooms)
                {
                    originalGraph.RemoveEdge(room, door);
                }
                doorsToRemove.Add(door);
            }

        }
        foreach (var door in doorsToRemove)
        {
            doors.Remove(door);
        }
    }

    #region GraphMethodsWithoutDoors
    private void GraphCreatorWithoutDoors()
    {
        originalGraph.Clear();
        // First add ALL rooms as nodes
        foreach (var room in rooms)
        {
            originalGraph.AddNode(room);
        }
        List<RectInt> visited = new List<RectInt>();
        for (int i = 0; i < rooms.Count; i++)
        {
            CheckContainsList(visited, i);
            for (int j = i + 1; j < rooms.Count; j++)
            {
                if (AlgorithmsUtils.Intersects(rooms[i], rooms[j]))
                {
                    RectInt intersection = AlgorithmsUtils.Intersect(rooms[i], rooms[j]);
                    if (intersection.width >= intersectLength + 2 || intersection.height >= intersectLength + 2)
                    {
                        CheckContainsList(visited, j);
                        originalGraph.AddEdge(rooms[i], rooms[j]);
                    }

                }
            }
        }
    }

    private void CheckContainsList(List<RectInt> visited, int i)
    {
        if (!visited.Contains(rooms[i]))
        {
            originalGraph.AddNode(rooms[i]);
            visited.Add(rooms[i]);
        }
    }

    #endregion

    #endregion
    /// <summary>
    /// Drawing the dungeon
    /// </summary>
    private IEnumerator DrawDungeon()
    {
        rectIntDrawingBatcher.ClearCalls();
        foreach (var room in rooms)
        {
            //yield return new WaitForSeconds(secondsForTest);
            rectIntDrawingBatcher.BatchCall(() => AlgorithmsUtils.DebugRectInt(room, Color.green, 1f));
        }
        foreach (var door in doors)
        {
            //yield return new WaitForSeconds(secondsForTest);
            rectIntDrawingBatcher.BatchCall(() => AlgorithmsUtils.DebugRectInt(door, Color.cyan, 1f));
        }
        yield return null;
    }

    #region SpawningAssets
    /// <summary>
    /// Spawns dungeon assets such as floors and walls in all rooms.
    /// </summary>
    public IEnumerator SpawnDungeonAssets()
    {
        int processedPerFrame = 20;
        int processed = 0;
        GameObject dungeon = new GameObject("Dungeon");
        bool[,] hasBlock;
        int rows = TileMapGenerator.Instance._tileMap.GetLength(0);
        int cols = TileMapGenerator.Instance._tileMap.GetLength(1);
        hasBlock = new bool[rows, cols];
        foreach (RectInt room in rooms)
        {
            GameObject roomParent = new GameObject("Room_" + room.x + "_" + room.y);
            roomParent.transform.SetParent(dungeon.transform);
            roomParent.transform.position = new Vector3(room.center.x, 0, room.center.y);
            yield return StartCoroutine(SpawnWalls(room, roomParent, processed, processedPerFrame,hasBlock));  
            SpawnFloor(room, roomParent);
        }
    }
    /// <summary>
    /// Spawns a floor GameObject in a given room.
    /// </summary>
    private void SpawnFloor(RectInt room, GameObject parent)
    {
        Vector3 floorScale = new Vector3(room.width, room.height, 1);
        GameObject floorInstance = Instantiate(floor, new Vector3(room.center.x, -0.5f, room.center.y), floor.transform.rotation, parent.transform);
        floorInstance.transform.localScale = floorScale;
    }
    /// <summary>
    /// Spawns walls along the perimeter of a room.
    /// </summary>
    public IEnumerator SpawnWalls(RectInt room, GameObject parent,int processed,int processedPerFrame, bool[,]tilemap)
    {
        GameObject wallParent = new GameObject("Walls");
        wallParent.transform.SetParent(parent.transform);
        
        for (int x = room.x; x < room.x + room.width; x++)
        {
            // Bottom wall
            Vector3 bottomPos = new Vector3(x, 0, room.y);
            TrySpawnWall(wallParent, bottomPos,tilemap);

            // Top wall
            Vector3 topPos = new Vector3(x, 0, room.y + room.height - 1);
            TrySpawnWall(wallParent, topPos, tilemap);

            processed+=2;
            if(CheckExecutionMode()&& !skipThisStep && processedPerFrame < processed)
            {
                processed = 0;
                yield return null;
            }

        }

        for (int y = room.y; y < room.y + room.height; y++)
        {
            // Left wall
            Vector3 leftPos = new Vector3(room.x, 0, y);
            TrySpawnWall(wallParent, leftPos, tilemap);

            // Right wall
            Vector3 rightPos = new Vector3(room.x + room.width - 1, 0, y);
            TrySpawnWall(wallParent, rightPos, tilemap);

            processed+=2;
            if (processedPerFrame < processed && CheckExecutionMode() && !skipThisStep)
            {
                processed = 0;
                yield return null;
            }
        }
    }
    /// <summary>
    /// Attempts to spawn a wall at a specific world position if it's not a door or already occupied.
    /// </summary>
    private void TrySpawnWall(GameObject wallParent, Vector3 pos, bool[,] hasBlock)
    {
        int gridX = (int)pos.x; 
        int gridY = (int)pos.z;

        if (CanPlaceBlockAt(gridX, gridY,hasBlock) && !IsDoorPosition(pos))
        {
            Instantiate(wallPrefab, pos, Quaternion.identity, wallParent.transform);
            hasBlock[gridY, gridX] = true;
        }
    }
    /// <summary>
    /// Checks if the given position overlaps with any door area.
    /// </summary>
    private bool IsDoorPosition(Vector3 position)
    {
        foreach (var door in doors)
        {
            bool isInDoorX = position.x >= door.x && position.x < door.x + door.width;
            bool isInDoorZ = position.z >= door.y && position.z < door.y + door.height;

            if (isInDoorX && isInDoorZ)
            {
                return true;
            }
        }
        return false;

    }
    /// <summary>
    /// Checks if a tile is within bounds and not already marked as occupied.
    /// </summary>
    private bool CanPlaceBlockAt(int x, int y,bool[,] hasBlock)
    {
        if (x < 0 || x >= hasBlock.GetLength(1) ||
            y < 0 || y >= hasBlock.GetLength(0))
            return false;

        return !hasBlock[y, x];
    }

    private bool CheckForOverlapping(Vector3 position)
    {
        Collider[] coliider = Physics.OverlapBox(position, new Vector3(0.45f, 0.45f, 0.45f));
        return coliider.Length == 0;
    }
    #endregion

    #region Get Methods
    /// <summary>
    /// Returns the list of rooms.
    /// </summary>
    public List<RectInt> GetRooms()
    {
        return rooms;
    }
    // <summary>
    /// Returns the list of doors.
    /// </summary>
    public List<RectInt> GetDoors()
    {
        return doors;
    }
    /// <summary>
    /// Returns the bounds of the dungeon.
    /// </summary>
    public RectInt GetDungeonBounds()
    {
        return bigRoom;
    }

    #endregion

    /// <summary>
    /// Rebuilds the navigation mesh using Unity's NavMeshSurface.
    /// </summary>
    private void BakeNavMesh()
    {
        navMesh.BuildNavMesh();
    }
    /// <summary>
    /// Checks if the current execution mode is coroutine or step-by-step.
    /// </summary>
    public bool CheckExecutionMode()
    {
        if (executionMode == ExecutionMode.Coroutine||executionMode==ExecutionMode.Step_by_step)
        {
            return true;
        }
        return false;
    }
    /// <summary>
    /// Randomly positions the player inside one of the rooms.
    /// </summary>
    public void FixPlayerPosition()
    {
        RectInt room = rooms[seededRandom.Next(0, rooms.Count - 1)];
        player.transform.position = new Vector3(room.center.x,0,room.center.y);
    }

}
