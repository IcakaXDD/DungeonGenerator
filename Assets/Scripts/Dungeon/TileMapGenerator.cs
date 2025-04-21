using System;
using System.Text;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Buffers;

[DefaultExecutionOrder(3)]
public class TileMapGenerator : MonoBehaviour
{
    
    [SerializeField]
    private UnityEvent onGenerateTileMap;

    [SerializeField]
    DungeonGenerator2 dungeonGenerator;

    [SerializeField]
    List<WallScriptableObject> prefabs = new List<WallScriptableObject>();

    private Dictionary<int, GameObject> prefabDictionary = new Dictionary<int, GameObject>();
    
    public int [,] _tileMap;

    public static TileMapGenerator Instance { get; private set; }

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
        dungeonGenerator = GetComponent<DungeonGenerator2>();

        foreach (var prefab in prefabs)
        {
            prefabDictionary.Add(prefab.value, prefab.gameObject);
        }
    }
    
    [Button]
    public void GenerateTileMap()
    {
        int[,] tileMap = new int[dungeonGenerator.GetDungeonBounds().height, dungeonGenerator.GetDungeonBounds().width];
        int rows = tileMap.GetLength(0);
        int cols = tileMap.GetLength(1);
        List<RectInt> rooms = new List<RectInt>();
        List<RectInt> doors = new List<RectInt>();
        rooms = dungeonGenerator.GetRooms();
        doors = dungeonGenerator.GetDoors();
       
        //Fill the map with empty spaces
        GenerateTileValues(tileMap, rooms, doors);
        SpawnWalls();

        _tileMap = tileMap;

        onGenerateTileMap.Invoke();
    }

    private static void GenerateTileValues(int[,] tileMap, List<RectInt> rooms, List<RectInt> doors)
    {
        foreach (var room in rooms)
        {
            AlgorithmsUtils.FillRectangleOutline(tileMap, room, 1);
        }
        foreach (var door in doors)
        {
            AlgorithmsUtils.FillRectangleOutline(tileMap, door, 0);
        }
    }

    public string ToString(bool flip)
    {
        if (_tileMap == null) return "Tile map not generated yet.";
        
        int rows = _tileMap.GetLength(0);
        int cols = _tileMap.GetLength(1);
        
        var sb = new StringBuilder();
    
        int start = flip ? rows - 1 : 0;
        int end = flip ? -1 : rows;
        int step = flip ? -1 : 1;

        for (int i = start; i != end; i += step)
        {
            for (int j = 0; j < cols; j++)
            {
                sb.Append((_tileMap[i, j]==0?'⬜':'⬛')); //Replaces 1 with '#' making it easier to visualize
            }
            sb.AppendLine();
        }
    
        return sb.ToString();
    }
    
    public int[,] GetTileMap()
    {
        return _tileMap.Clone() as int[,];
    }
    
    [Button]
    public void PrintTileMap()
    {
        Debug.Log(ToString(true));
    }

    public void SpawnWalls()
    {
        if (_tileMap == null) return;

        int rows = _tileMap.GetLength(0);
        int cols = _tileMap.GetLength(1);

        for (int x = 0; x < rows - 1; x++)
        {
            for (int y = 0; y < cols - 1; y++)
            {
                int topLeft = _tileMap[x + 1, y];
                int topRight = _tileMap[x + 1, y + 1];
                int bottomLeft = _tileMap[x, y];
                int bottomRight = _tileMap[x, y + 1];

                int value = bottomLeft*8 + topLeft*4+topRight*2+bottomRight*1;

                prefabDictionary.TryGetValue(value,out GameObject prefab);

                Vector3 position = new Vector3(y+1, 0, x+1);

                if (prefab == null||value==0)
                {
                    //Debug.LogWarning($"No prefab found for tile value: {value} at position ({x},{y})");
                    continue;
                }
                Instantiate(prefab, position, prefab.gameObject.transform.localRotation,transform);
            }
        }
    }

}
