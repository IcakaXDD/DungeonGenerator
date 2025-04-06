using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using Unity.VisualScripting;
using UnityEditor.Overlays;
using UnityEditor;
using System;
using NaughtyAttributes.Test;

public class DungeonGenerator2 : MonoBehaviour
{
    public int SIZE = 100;
    public int numberOfRooms = 50;
    public int wMin = 5;
    public int hMin = 5;
    public int intersectLength = 1;

    public int deletePercent;
    
    public bool fastTest = false;

    [Header("Seed Settings")]
    public bool UseSeed = false;
    public string seed;

    private int distanceFromEdge;
    private RectInt bigRoom;
    private float secondsForTest = 0;
    private int activeSplittingCoroutines = 0;

    [SerializeField] Graph<RectInt> originalGraph;
    [SerializeField] Graph<RectInt> backUpGraph;
    public List<RectInt> doors;
    public List<RectInt> rooms;


    public DebugDrawingBatcher rectIntDrawingBatcher;
    private System.Random seededRandom;

    private void Start()
    {
        rectIntDrawingBatcher = DebugDrawingBatcher.GetInstance();
        doors = new List<RectInt>();
        originalGraph = new Graph<RectInt>();

        InitializeRandom();
        if (!fastTest)
        {
            secondsForTest = 0.2f;
        }
        if (seed != null && seed.Length == 11 && UseSeed)
        {
            SIZE = int.Parse(seed.Substring(0, 3));
            numberOfRooms = int.Parse(seed.Substring(3, 2));
            wMin = int.Parse(seed.Substring(5, 2));
            hMin = int.Parse(seed.Substring(7, 2));
            intersectLength = int.Parse(seed.Substring(9, 1));
            
            
        }   
        ValidateParameters();
        GenerateDungeon();
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
        SIZE = Mathf.Clamp(SIZE, 50, 800);
        numberOfRooms = Mathf.Clamp(numberOfRooms, 10, 300);
        wMin = Mathf.Clamp(wMin, 3, 40);
        hMin = Mathf.Clamp(hMin, 3, 40);
        intersectLength = Mathf.Clamp(intersectLength, 1, 3);
    }
    #endregion
    private void GenerateDungeon()
    {
        bigRoom = new RectInt(0, 0, SIZE, SIZE);
        distanceFromEdge = hMin + 2 * intersectLength;
        rooms = new List<RectInt>();
        AlgorithmsUtils.DebugRectInt(bigRoom, Color.red, 100f);
        if (seededRandom.Next(0,2)==1)
        {
            StartCoroutine(HorizontalSplit(rooms, bigRoom, 0));
        }
        else
        {
            StartCoroutine(VerticalSplit(rooms, bigRoom, 0));
        }
        StartCoroutine(SplitingFinished());
    }
    

    #region Spliting Methods
    public IEnumerator HorizontalSplit(List<RectInt> rooms, RectInt room, int counter = 0, bool trySplitingTheOpposite = true)
    {

        if (counter >= numberOfRooms || NotBigEnough(room) || OutOfBounds(room))
        {
            yield break;
        }
        if (rooms.Count >= numberOfRooms)
        {
            yield break;
        }
        int splitPoint = CalculateHorizontalSplitPoint(room);
        if (splitPoint == -1)
        {
            if (trySplitingTheOpposite)
            {
                yield return StartCoroutine(VerticalSplit(rooms, room, counter,false));
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
        yield return new WaitForSeconds(secondsForTest);
        yield return StartCoroutine(VerticalSplit(rooms, roomA, counter + 1));
        yield return StartCoroutine(VerticalSplit(rooms, roomB, counter + 1));
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

    public IEnumerator VerticalSplit(List<RectInt> rooms, RectInt room, int counter, bool trySplitingTheOpposite = true)
    {

        if (counter == numberOfRooms || NotBigEnough(room) || OutOfBounds(room))
        {
            yield break;
        }
        if (rooms.Count >= numberOfRooms)
        {
            Debug.Log("Room limit reached");
            yield break;
        }
        int splitPoint = CalculateVerticalSplitPoint(room);
        if (splitPoint == -1)
        {
            if (trySplitingTheOpposite)
            {
                yield return StartCoroutine(HorizontalSplit(rooms, room, counter,false));
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


        rectIntDrawingBatcher.BatchCall(()=> AlgorithmsUtils.DebugRectInt(roomA, Color.yellow, 1f));
        rectIntDrawingBatcher.BatchCall(() => AlgorithmsUtils.DebugRectInt(roomB, Color.yellow, 1f));
        yield return new WaitForSeconds(secondsForTest);
        yield return StartCoroutine(HorizontalSplit(rooms, roomA, counter + 1));

        yield return StartCoroutine(HorizontalSplit(rooms, roomB, counter + 1));
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

    private IEnumerator SplitingFinished()
    {
        yield return new WaitUntil(() => activeSplittingCoroutines == 0);

        Debug.Log("Splitting finished");

        yield return StartCoroutine(DoorChecker());
        
    }


    

    #endregion

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
                        RectInt door = new RectInt(intersection.x + seededRandom.Next(intersectLength + 5, intersection.width - (5 + intersectLength)), intersection.y, intersectLength, intersectLength);
                        yield return new WaitForSeconds(secondsForTest);
                        rectIntDrawingBatcher.BatchCall(()=> AlgorithmsUtils.DebugRectInt(door, Color.cyan, 1));
                        doors.Add(door);
                    }
                    else
                    {
                        RectInt door = new RectInt(intersection.x, intersection.y + seededRandom.Next(intersectLength + 5, intersection.height - (5 + intersectLength)), intersectLength, intersectLength);
                        yield return new WaitForSeconds(secondsForTest);
                        rectIntDrawingBatcher.BatchCall(() => AlgorithmsUtils.DebugRectInt(door, Color.cyan, 1));
                        doors.Add(door);
                    }
                }
            }
        }

        
        yield return StartCoroutine(DeleteSmallRooms());
        yield return StartCoroutine(DrawDungeon());
        yield return StartCoroutine(GraphCreator(true));
        yield break;
    }


    private IEnumerator DeleteSmallRooms()
    {
        Debug.Log("Starts deleting "+deletePercent+" of the rooms");

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

                yield return new WaitForSeconds(secondsForTest);
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
                DebugExtension.DebugWireSphere(roomPosition, Color.red, 3, 100);
                yield return new WaitForSeconds(secondsForTest);
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
                            yield return new WaitForSeconds(secondsForTest);
                        }
                    }

                    originalGraph.AddEdge(rooms[i], doors[j]);
                    if (stepByStep)
                    {
                        Debug.DrawLine(roomPosition, doorPosition, Color.red, 100f);
                        yield return new WaitForSeconds(secondsForTest);
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
 

    private void GraphCreatorWithoutDoors()
    {
        originalGraph.Clear();
        List<RectInt> visited = new List<RectInt>();
        for (int i = 0; i < rooms.Count; i++)
        {
            CheckContainsList(visited, i);
            for (int j = i + 1; j < rooms.Count; j++)
            {
                if (AlgorithmsUtils.Intersects(rooms[i], rooms[j]))
                {
                    CheckContainsList(visited, j);
                    originalGraph.AddEdge(rooms[i], rooms[j]);
                }
            }
        }
    }

    #endregion

    private IEnumerator DrawDungeon()
    {
        Debug.Log("Redrawing the dungeon");
        rectIntDrawingBatcher.ClearAllBatches();
        foreach (var room in rooms)
        {
            yield return new WaitForSeconds(secondsForTest);
            rectIntDrawingBatcher.BatchCall(() => AlgorithmsUtils.DebugRectInt(room, Color.green, 100f));
        }
        foreach (var door in doors)
        {
            yield return new WaitForSeconds(secondsForTest);
            rectIntDrawingBatcher.BatchCall(() => AlgorithmsUtils.DebugRectInt(door, Color.cyan, 1));
        }

    }

    #region Room Checks
    private bool NotBigEnough(RectInt room)
    {
        return room.width <= distanceFromEdge || room.height <= distanceFromEdge;
    }

    private bool OutOfBounds(RectInt room)
    {
        return room.x < 0 || room.y < 0 || (room.x + room.width) > SIZE || (room.y + room.height) > SIZE;
    }
    #endregion
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
