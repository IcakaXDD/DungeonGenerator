using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class DungeonGenerator2 : MonoBehaviour
{
    public int SIZE = 100;
    public int numberOfRooms = 50;
    public int wMin = 5;
    public int hMin = 5;
    public bool fastTest = false;

    public int intersectLength = 1;
    private int distanceFromEdge;
    private RectInt bigRoom;
    private bool canISeeTheRooms = false;
    private bool doorCheckerStarted = false;
    private float secondsForTest = 0;
    private bool isCoroutineFinished = false;
    private int activeSplittingCoroutines = 0;


    public List<RectInt> doors;
    public List<RectInt> rooms;


    public DebugDrawingBatcher rectIntDrawingBathcer;



    private void Start()
    {
        doors = new List<RectInt>();
        if (!fastTest)
        {
            secondsForTest = 0.5f;
        }
        GenerateDungeon();
    }
    private void GenerateDungeon()
    {
        bigRoom = new RectInt(0, 0, SIZE, SIZE);
        distanceFromEdge = hMin + 2 * intersectLength;
        rooms = new List<RectInt>();
        AlgorithmsUtils.DebugRectInt(bigRoom, Color.red, 100f);
        if (Random.Range(0, 2) == 1)
        {
            StartCoroutine(HorizontalSplit(rooms, bigRoom, 0));
        }
        else
        {
            StartCoroutine(VerticalSplit(rooms, bigRoom, 0));
        }
        StartCoroutine(SplitingFinished());
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
                        RectInt door = new RectInt(intersection.x+intersection.width/2,intersection.y,2,2);
                        AlgorithmsUtils.DebugRectInt(door, Color.cyan,100f);
                        doors.Add(door);
                    }
                    else
                    {
                        RectInt door = new RectInt(intersection.x, intersection.y+ intersection.height/ 2, 2, 2);
                        AlgorithmsUtils.DebugRectInt(door, Color.cyan, 100f);
                        doors.Add(door);
                    }
                }   
            }
        }
        yield break;
    }   
    
    private IEnumerator SplitingFinished()
    {
        yield return new WaitUntil(()=> activeSplittingCoroutines == 0);
        StartCoroutine(DoorChecker());
    }

    public IEnumerator HorizontalSplit(List<RectInt> rooms, RectInt room, int counter = 0, bool trySplitingTheOpposite = true)
    {

        if (counter >= numberOfRooms || NotBigEnough(room) || OutOfBounds(room))
        {
            bool isCounterFailed = counter >= numberOfRooms;
            bool isBigEnough = NotBigEnough(room);
            bool isOutOfBounds = OutOfBounds(room);
            if (isCounterFailed)
            {
                Debug.LogWarning("Counter failed " + room);
            }
            if (isBigEnough)
            {
                Debug.LogWarning("Room is not big enough" + room);
            }
            if (isOutOfBounds)
            {
                Debug.LogWarning("Room is out of bounds" + room);
            }
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
        do
        {
            c++;
            splitPoint = Random.Range(c, c + room.height);
            if (splitPoint > room.height - distanceFromEdge)
            {
                continue;
            }
            else if (splitPoint < distanceFromEdge)
            {
                continue;
            }
            break;
        }
        while (c < room.height);
        if (c == room.height)
        {
            return -1;
        }
        else
        {
            return splitPoint;
        }
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
                Debug.LogWarning("Counter failed " + room);
            }
            if (isBigEnough)
            {
                Debug.LogWarning("Room is not big enough" + room);
            }
            if (isOutOfBounds)
            {
                Debug.LogWarning("Room is out of bounds" + room);
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
            Debug.LogWarning("Invalid split point detected.");
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

        do
        {
            c++;
            splitPoint = Random.Range(c, c + room.width);
            if (splitPoint > room.width - distanceFromEdge)
            {
                continue;
            }
            else if (splitPoint < distanceFromEdge)
            {
                continue;
            }
            break;
        }
        while (c < room.width);
        if (c == room.width)
        {
            return -1;
        }
        else
        {
            return splitPoint;
        }
    }

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
            rectIntDrawingBathcer.BatchCall(() =>AlgorithmsUtils.DebugRectInt(room, Color.green, 100f));
        }

    }
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

}