using NaughtyAttributes;
using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using static UnityEditor.Experimental.AssetDatabaseExperimental.AssetDatabaseCounters;
using static UnityEngine.Rendering.DebugUI.Table;

[DefaultExecutionOrder(2)]
public class FloorFillSpawner : MonoBehaviour
{
    List<RectInt> rooms = new List<RectInt>();
    List<RectInt> doors = new List<RectInt>();
    public int[,] tileMap; 
    private int rows;
    private int cols;
    public bool[,] visited;
    public GameObject objectToDisappear;
    public GameObject floor;
    public bool floorPlaced = false;

    public static FloorFillSpawner Instance { get; private set; }

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

    [Button]
    public void FloorFill()
    {
        rooms = DungeonGenerator2.Instance.GetRooms();
        tileMap = TileMapGenerator.Instance.GetTileMap();
        doors = DungeonGenerator2.Instance.GetDoors();
        InitVisited();
        StartCoroutine(StartFlooring());
        
    }
    ///<summary>
    ///Gets the first room of the graph and starts the floor filling
    ///</summary>
    private IEnumerator StartFlooring()
    {
        Graph<RectInt> graph = DungeonGenerator2.Instance.originalGraph.Clone();

        List<RectInt> rectInts = graph.GetNodes();
        RectInt startRoom = rectInts[0];

        int startC = startRoom.xMin + startRoom.width / 2;
        int startR = startRoom.yMin + startRoom.height / 2;

        yield return StartCoroutine(FloorFillBFS(startR, startC, startRoom));

        Debug.Log("Floor placed");
        floorPlaced = true;
    }
    ///<summary>
    ///Spawning the floor using a BFS approach
    ///</summary>
    private IEnumerator FloorFillBFS(int startR, int startC, RectInt room)
    {
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        queue.Enqueue(new Vector2Int(startC, startR));
        visited[startR, startC] = true;
        Instantiate(floor, new Vector3(startC + 0.5f, 0, startR + 0.5f), Quaternion.identity, transform);

        int processedPerFrame =200; 
        int processed = 0;

        while (queue.Count > 0)
        {
            Vector2Int tile = queue.Dequeue();
            int r = tile.y;
            int c = tile.x;

            TrySpawnTile(r + 1, c, queue, room); // Up
            TrySpawnTile(r - 1, c, queue, room); // Down
            TrySpawnTile(r, c + 1, queue, room); // Right
            TrySpawnTile(r, c - 1, queue, room); // Left

            processed++;
            if (processed >= processedPerFrame&& DungeonGenerator2.Instance.CheckExecutionMode()&&!DungeonGenerator2.Instance.skipThisStep)
            {
                processed = 0; // this is equal to yield return
                yield return null;
                
                 //it is like saying to unity after 4*processedPerFrame tiles to take a break to avoid stack overflow
            }
        }
    }
    ///<summary>
    ///Spawning of the tiles
    ///</summary>
    private void TrySpawnTile(int r, int c, Queue<Vector2Int> queue, RectInt room)
    {
        if (r < 0 || r >= rows || c < 0 || c >= cols) return;
        if (tileMap[r, c] != 0 || visited[r, c]) return;

        visited[r, c] = true;
        Instantiate(floor, new Vector3(c + 0.5f, 0, r + 0.5f), Quaternion.identity, transform);
        queue.Enqueue(new Vector2Int(c, r));
    }

    void InitVisited()
    {
        rows = tileMap.GetLength(0);
        cols = tileMap.GetLength(1);
        visited = new bool[rows, cols];
    }
    
}
