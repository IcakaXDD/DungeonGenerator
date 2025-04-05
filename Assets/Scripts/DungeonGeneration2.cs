using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using Unity.VisualScripting;
using UnityEditor.Overlays;
using UnityEditor;

public class DungeonGenerator2 : MonoBehaviour
{
    public int SIZE = 100;
    public int numberOfRooms = 50;
    public int wMin = 5;
    public int hMin = 5;
    public int intersectLength = 1;
    public bool fastTest = false;

    [Header("Seed Settings")]
    public bool UseSeed = false;
    public string seed;

    private int distanceFromEdge;
    private RectInt bigRoom;
    private float secondsForTest = 0;
    private int activeSplittingCoroutines = 0;

    [SerializeField] Graph<RectInt> graph;
    public List<RectInt> doors;
    public List<RectInt> rooms;


    public DebugDrawingBatcher rectIntDrawingBatcher;
    private System.Random seededRandom;

    private void Start()
    {
        rectIntDrawingBatcher = DebugDrawingBatcher.GetInstance();
        doors = new List<RectInt>();
        graph = new Graph<RectInt>();

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
        SIZE = Mathf.Clamp(SIZE, 50, 500);
        numberOfRooms = Mathf.Clamp(numberOfRooms, 10, 300);
        wMin = Mathf.Clamp(wMin, 3, 40);
        hMin = Mathf.Clamp(hMin, 3, 40);
        intersectLength = Mathf.Clamp(intersectLength, 1, 3);
    }

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

    private IEnumerator GraphCreator()
    {
        List<RectInt> visited = new List<RectInt>();
        for (int i = 0; i < rooms.Count; i++)
        {
            graph.AddNode(rooms[i]);
            visited.Add(rooms[i]);
            Vector3 roomPosition = new Vector3(rooms[i].center.x, 0, rooms[i].center.y);
            DebugExtension.DebugWireSphere(roomPosition,Color.red, 3, 100);
            yield return new WaitForSeconds(secondsForTest);

            for (int j = 0; j < doors.Count; j++)
            {
                if (AlgorithmsUtils.Intersects(rooms[i], doors[j]))
                {
                    Vector3 doorPosition = new Vector3(doors[j].center.x, 0, doors[j].center.y);

                    if (!visited.Contains(rooms[i]))
                    {
                        graph.AddNode(doors[j]);
                        visited.Add(doors[j]);
                        DebugExtension.DebugWireSphere(doorPosition, Color.red, 3, 100);
                        yield return new WaitForSeconds(secondsForTest);
                    }

                    graph.AddEdge(rooms[i], doors[j]);
                    Debug.DrawLine(roomPosition,doorPosition,Color.red,100f);
                    yield return new WaitForSeconds(secondsForTest);
                }
            }
        }
    }
    

    private IEnumerator SplitingFinished()
    {
        yield return new WaitUntil(() => activeSplittingCoroutines == 0);

        Debug.Log("Splitting finished");
        foreach (var room in rooms)
        {
            rectIntDrawingBatcher.BatchCall(() => { AlgorithmsUtils.DebugRectInt(room, Color.magenta); });
            
        }

        StartCoroutine(DoorChecker());
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

                    if(intersection.width>intersection.height)
                    {
                        RectInt door = new RectInt(intersection.x+ seededRandom.Next(intersectLength+5,intersection.width-(5+intersectLength)),intersection.y,intersectLength,intersectLength);
                        yield return new WaitForSeconds(secondsForTest);
                        AlgorithmsUtils.DebugRectInt(door, Color.cyan,100f);
                        doors.Add(door);
                    }
                    else
                    {
                        RectInt door = new RectInt(intersection.x, intersection.y+ seededRandom.Next(intersectLength+5,intersection.height-(5+intersectLength)), intersectLength, intersectLength);
                        yield return new WaitForSeconds(secondsForTest);
                        AlgorithmsUtils.DebugRectInt(door, Color.cyan, 100f);
                        doors.Add(door);
                    }
                }   
            }
        }
       
        yield return StartCoroutine(GraphCreator());
        yield break;
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

        AlgorithmsUtils.DebugRectInt(roomA, Color.yellow, 100f);
        AlgorithmsUtils.DebugRectInt(roomB, Color.yellow, 100f);
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
        //int splitPoint = -1;
        //int c = 0;
        //if (UseSeed)
        //{
        //    do
        //    {
        //        if (c == 20)
        //        {
        //            return -1;
        //        }
        //        predictRandomnes +=10;
        //        if (predictRandomnes > room.height - distanceFromEdge)
        //        {
        //            predictRandomnes = 5;
        //            c++;
        //            continue;
        //        }
        //        else if (predictRandomnes < distanceFromEdge)
        //        {
        //            predictRandomnes = distanceFromEdge;
        //            c++;
        //            continue;
        //        }
        //        break;

        //    }while (true);
        //    return predictRandomnes;
        //}
        //else
        //{
        //    do
        //    {
        //        c++;
        //        splitPoint = Random.Range(c, c + room.height);
        //        if (splitPoint > room.height - distanceFromEdge)
        //        {
        //            continue;
        //        }
        //        else if (splitPoint < distanceFromEdge)
        //        {
        //            continue;
        //        }
        //        break;
        //    }
        //    while (c < room.height);
        //}


        //if (c == room.height)
        //{
        //    return -1;
        //}
        //else
        //{
        //    return splitPoint;
        //}
    }

    public IEnumerator VerticalSplit(List<RectInt> rooms, RectInt room, int counter, bool trySplitingTheOpposite = true)
    {

        if (counter == numberOfRooms || NotBigEnough(room) || OutOfBounds(room))
        {
            bool isCounterFailed = counter >= numberOfRooms;
            bool isBigEnough = NotBigEnough(room);
            bool isOutOfBounds = OutOfBounds(room);
            if (isCounterFailed)
            {
                //Debug.LogWarning("Counter failed " + room);
            }
            if (isBigEnough)
            {
                //Debug.LogWarning("Room is not big enough" + room);
            }
            if (isOutOfBounds)
            {
                //Debug.LogWarning("Room is out of bounds" + room);
            }
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


        AlgorithmsUtils.DebugRectInt(roomA, Color.yellow, 100f);
        AlgorithmsUtils.DebugRectInt(roomB, Color.yellow, 100f);
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
        //int splitPoint = -1;
        //int c = 0;
        //if (UseSeed)
        //{
        //    do
        //    {
        //        if (c == 20)
        //        {
        //            return -1;
        //        }
        //        predictRandomnes += 10;
        //        if (predictRandomnes > room.height - distanceFromEdge)
        //        {
        //            predictRandomnes = 5;
        //            c++;
        //            continue;
        //        }
        //        else if (predictRandomnes < distanceFromEdge)
        //        {
        //            predictRandomnes = distanceFromEdge;
        //            c++;
        //            continue;
        //        }

        //        break;

        //    } while (true);
        //    return predictRandomnes;
        //}
        //do
        //{
        //    c++;
        //    splitPoint = Random.Range(c, c + room.width);
        //    if (splitPoint > room.width - distanceFromEdge)
        //    {
        //        continue;
        //    }
        //    else if (splitPoint < distanceFromEdge)
        //    {
        //        continue;
        //    }
        //    break;
        //}
        //while (c < room.width);
        //if (c == room.width)
        //{
        //    return -1;
        //}
        //else
        //{
        //    return splitPoint;
        //}
    }
    #endregion

    #region DrawingMethods
    private IEnumerator DrawRooms(RectInt roomA, RectInt roomB)
    {
        yield return new WaitForSeconds(5f);
        AlgorithmsUtils.DebugRectInt(roomA, Color.green, 50f);
        yield return new WaitForSeconds(5f);
        AlgorithmsUtils.DebugRectInt(roomB, Color.green, 50f);
    }

    private IEnumerator DrawDungeon(List<RectInt> rooms)
    {
        foreach (var room in rooms)
        {
            yield return new WaitForSeconds(0.1f);
            rectIntDrawingBatcher.BatchCall(() =>AlgorithmsUtils.DebugRectInt(room, Color.green, 100f));
        }

    }
    #endregion

    #region Room Checks
    private void RoomsCounterCheck(List<RectInt> rooms, RectInt roomA, RectInt roomB)
    {
        if (rooms.Count + 2 <= numberOfRooms)
        {
            rooms.Add(roomA);
            rooms.Add(roomB);
        }
        else if (rooms.Count + 1 <= numberOfRooms)
        {
            rooms.Add(roomA);
        }
    }
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
