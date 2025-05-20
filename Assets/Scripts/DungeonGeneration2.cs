using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using UnityEngine;

public class DungeonGenerator2 : MonoBehaviour
{
    [Header("Dungeon Settings")]
    public int SIZE = 100;
    //public int numberOfRooms = 50;
    public int wMin = 5;
    public int hMin = 5;
    public int intersectLength = 1;
    public int deletePercent = 0;
    public float secondsForTest = 0;

    [Space]
    public bool graphTree = true;

    public bool placeAssetsCubes = false;

    public bool placeAssetsMarchingSquares = false;

    public bool showAssetsStepByStep = false;

    [Header("Required Assets")]

    public NavMeshSurface navMesh;

    public GameObject wallPrefab;

    public GameObject floor;

    private DebugDrawingBatcher rectIntDrawingBatcher;

    public enum ExecutionMode { Coroutine, Instant }
    [Header("Execution Settings")]
    public ExecutionMode executionMode = ExecutionMode.Coroutine;
    public KeyCode skipKey = KeyCode.Space;    // Key to skip to instant finish during coroutine


    [Header("Seed Settings")]
    public bool UseSeed = false;
    public string seed;  

    [Header("Lists")]

    private Graph<RectInt> originalGraph;
    private Graph<RectInt> backUpGraph;
    public List<RectInt> doors;
    public List<RectInt> rooms;

    private System.Random seededRandom;

    private int distanceFromEdge;
    private RectInt bigRoom;

    private int activeSplittingCoroutines = 0;

    public static DungeonGenerator2 Instance { get; private set; }

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

    private void Update()
    {
        // Skip to instant finish if in Coroutine mode
        if (CheckExecutionMode() && Input.GetKeyDown(skipKey))
        {
            executionMode = ExecutionMode.Instant;                 
        }
    }

    #region Seed and Random set
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

    private void GenerateDungeon()
    {
        bigRoom = new RectInt(0, 0, SIZE, SIZE);
        distanceFromEdge = hMin + 2 * intersectLength;
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
                if(CheckExecutionMode())
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
        if (CheckExecutionMode())
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

    private int CalculateHorizontalSplitPoint(RectInt room)
    {
        int splitPoint = -1;
        int c = 0;
        int minSplit = distanceFromEdge;
        int maxSplit = room.height - distanceFromEdge;

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
                if(CheckExecutionMode())
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
        if(CheckExecutionMode())
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

    private int CalculateVerticalSplitPoint(RectInt room)
    {
        int splitPoint = -1;
        int c = 0;

        int minSplit = distanceFromEdge;
        int maxSplit = room.width - distanceFromEdge;

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
    private bool NotBigEnough(RectInt room)
    {
        return room.width <= distanceFromEdge && room.height <= distanceFromEdge;
    }

    private bool OutOfBounds(RectInt room)
    {
        return room.x < 0 || room.y < 0 || (room.x + room.width) > SIZE || (room.y + room.height) > SIZE;
    }
    #endregion


    #endregion

    private IEnumerator StepsToFinish()
    {
        yield return new WaitUntil(() => activeSplittingCoroutines == 0);

        Debug.Log("Splitting finished");


        yield return StartCoroutine(DoorChecker());

        yield return StartCoroutine(DeleteSmallRooms());


        yield return StartCoroutine(DrawDungeon());
        yield return StartCoroutine(GraphCreator(true));
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
            TileMapGenerator.Instance.GenerateTileMap();
            Debug.Log("Floor fill");
            FloorFillSpawner.Instance.FloorFill();
            yield return new WaitUntil(() => FloorFillSpawner.Instance.floorPlaced == true);
            BakeNavMesh();
        }

        Debug.Log("The dungeon is finished for " + Time.time + " second.");

        yield break;

    }

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
                            if(CheckExecutionMode())
                            {
                                yield return new WaitForSeconds(secondsForTest);
                            }
                            
                            rectIntDrawingBatcher.BatchCall(() => AlgorithmsUtils.DebugRectInt(door1, Color.blue, 1f));
                            doors.Add(door1);
                            continue;
                        }

                        RectInt door = new RectInt(intersection.x + seededRandom.Next(intersectLength + 2, intersection.width - (2 + intersectLength)), intersection.y, intersectLength, intersectLength);
                        if (CheckExecutionMode())
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
                            if(CheckExecutionMode())
                            {
                                yield return new WaitForSeconds(secondsForTest);
                            }
                            rectIntDrawingBatcher.BatchCall(() => AlgorithmsUtils.DebugRectInt(door1, Color.blue, 1f));
                            doors.Add(door1);
                            continue;
                        }

                        RectInt door = new RectInt(intersection.x, intersection.y + seededRandom.Next(intersectLength + 2, intersection.height - (2 + intersectLength)), intersectLength, intersectLength);
                        if (CheckExecutionMode())
                            yield return new WaitForSeconds(secondsForTest);
                        rectIntDrawingBatcher.BatchCall(() => AlgorithmsUtils.DebugRectInt(door, Color.cyan, 1f));


                        doors.Add(door);
                    }
                }
            }
        }



    }

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

                if (CheckExecutionMode())
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
                if(CheckExecutionMode())
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
                            if (CheckExecutionMode()) { yield return new WaitForSeconds(secondsForTest); }
                         
                        }
                    }

                    originalGraph.AddEdge(rooms[i], doors[j]);
                    if (stepByStep)
                    {
                        Debug.DrawLine(roomPosition, doorPosition, Color.red, 100f);
                        if (CheckExecutionMode())
                        {
                            yield return new WaitForSeconds(secondsForTest);
                        }
                    }

                }
            }
        }
    }


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

        //PrintAndVisualizeGraph();
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

    #region Draw Methods
    private IEnumerator DrawDungeon()
    {
        Debug.Log("Redrawing the dungeon");
        if (graphTree)
        {
            //originalGraph.BFSTree2();
            yield return StartCoroutine(MakeTheGraphAsTree());
        }

        rectIntDrawingBatcher.ClearCalls();
        Draw();
        yield return null;

    }

    private void Draw()
    {
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
    }

    #endregion

    #region SpawningAssets
    public IEnumerator SpawnDungeonAssets()
    {
        int processedPerFrame = 20;
        int processed = 0;
        GameObject dungeon = new GameObject("Dungeon");

        foreach (RectInt room in rooms)
        {
            GameObject roomParent = new GameObject("Room_" + room.x + "_" + room.y);
            roomParent.transform.SetParent(dungeon.transform);
            roomParent.transform.position = new Vector3(room.center.x, 0, room.center.y);
            if (CheckExecutionMode())
            {
                yield return StartCoroutine(SpawnWalls(room, roomParent, processed, processedPerFrame));
            }
            StartCoroutine(SpawnWalls(room, roomParent, processed, processedPerFrame));
            SpawnFloor(room, roomParent);
        }
    }

    private void SpawnFloor(RectInt room, GameObject parent)
    {
        Vector3 floorScale = new Vector3(room.width, room.height, 1);
        GameObject floorInstance = Instantiate(floor, new Vector3(room.center.x, -0.5f, room.center.y), floor.transform.rotation, parent.transform);
        floorInstance.transform.localScale = floorScale;
    }

    public IEnumerator SpawnWalls(RectInt room, GameObject parent,int processed,int processedPerFrame)
    {
        GameObject wallParent = new GameObject("Walls");
        wallParent.transform.SetParent(parent.transform);
        
        for (int x = room.x; x < room.x + room.width; x++)
        {
            // Bottom wall
            Vector3 bottomPos = new Vector3(x, 0, room.y);
            TrySpawnWall(wallParent, bottomPos);

            // Top wall
            Vector3 topPos = new Vector3(x, 0, room.y + room.height - 1);
            TrySpawnWall(wallParent, topPos);

            processed+=2;
            if( CheckExecutionMode())
            {
                processed = 0;
                yield return null;
            }

        }

        for (int y = room.y; y < room.y + room.height; y++)
        {
            // Left wall
            Vector3 leftPos = new Vector3(room.x, 0, y);
            TrySpawnWall(wallParent, leftPos);

            // Right wall
            Vector3 rightPos = new Vector3(room.x + room.width - 1, 0, y);
            TrySpawnWall(wallParent, rightPos);

            processed+=2;
            if (processedPerFrame < processed||CheckExecutionMode())
            {
                processed = 0;
                yield return null;
            }
        }
    }

    private void TrySpawnWall(GameObject wallParent, Vector3 Pos)
    {
        if (CheckForOverlapping(Pos))
        {
            if (!IsDoorPosition(Pos))
            {
                Instantiate(wallPrefab, Pos, Quaternion.identity, wallParent.transform);
            }
        }
    }

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

    private bool CheckForOverlapping(Vector3 position)
    {
        Collider[] coliider = Physics.OverlapBox(position, new Vector3(0.45f, 0.45f, 0.45f));
        return coliider.Length == 0;
    }
    #endregion

    #region Get Methods
    public List<RectInt> GetRooms()
    {
        return rooms;
    }

    public List<RectInt> GetDoors()
    {
        return doors;
    }

    public RectInt GetDungeonBounds()
    {
        return bigRoom;
    }

    #endregion

    private void BakeNavMesh()
    {
        navMesh.BuildNavMesh();
    }

    public bool CheckExecutionMode()
    {
        if (executionMode == ExecutionMode.Coroutine)
        {
            return true;
        }
        return false;
    }

}
//[CustomEditor(typeof(DungeonGenerator2))]
//public class DungeonGeneration2Editor : Editor 
//{
//    public override void OnInspectorGUI()
//    {
//        var script = (DungeonGenerator2)target;
//        script.UseSeed = EditorGUILayout.Toggle("Use seed",script.UseSeed);
//        if (!script.UseSeed)
//        {
//            return;
//        }
//        script.seed = EditorGUILayout.TextArea("Seed: ", script.seed);
//    }
//}
