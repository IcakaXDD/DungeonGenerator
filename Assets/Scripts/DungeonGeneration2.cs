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
    
    public int intersectLength = 1;

    private int distanceFromEdge;
    private RectInt bigRoom;

    public List<RectInt> rooms;

    private void Start()
    {
        GenerateDungeon();
    }

    private void GenerateDungeon()
    {
        bigRoom = new RectInt(0, 0, SIZE, SIZE);
        distanceFromEdge = hMin + 2 * intersectLength;
        rooms = new List<RectInt>();

        HorizontalSplit(rooms, bigRoom, 0);

        StartCoroutine(DrawDungeon(rooms));

        Debug.Log("Total Rooms Generated: " + rooms.Count);
    }
    public void HorizontalSplit(List<RectInt> rooms, RectInt room, int counter = 0)
    {

        if (counter >= numberOfRooms || NotBigEnough(room) || OutOfBounds(room))
        {
            bool isCounterFailed = counter >= numberOfRooms;
            bool isBigEnough = NotBigEnough(room);
            bool isOutOfBounds = OutOfBounds(room);
            if (isCounterFailed)
            {
                Debug.LogWarning("Counter failed "+ room);
            }
            if (isBigEnough)
            {
                Debug.LogWarning("Room is not big enough"+room);
            }
            if (isOutOfBounds)
            {
                Debug.LogWarning("Room is out of bounds" + room);
            }
            return;
        }
        if (rooms.Count >= numberOfRooms)
        {
            Debug.Log("Room limit reached");
            return;
        }
        int splitPoint = CalculateHorizontalSplitPoint(room);
        if (splitPoint == -1)
        {
            Debug.LogWarning("Invalid split point detected.");
            return;
        }
        RectInt roomA = new RectInt(room.x, room.y, room.width, splitPoint + intersectLength);
        RectInt roomB = new RectInt(room.x, room.y + splitPoint, room.width, room.height - splitPoint);
        rooms.Remove(room);
        rooms.Add(roomA);
        rooms.Add(roomB);
        

        VerticalSplit(rooms, roomA, counter + 1);
        VerticalSplit(rooms, roomB, counter + 1);
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

    public void VerticalSplit(List<RectInt> rooms, RectInt room, int counter)
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
            return;
        }
        if (rooms.Count >= numberOfRooms)
        {
            Debug.Log("Room limit reached");
            return;
        }
        int splitPoint = CalculateVerticalSplitPoint(room);
        if (splitPoint == -1)
        {
            Debug.LogWarning("Invalid split point detected.");
            return;
        }
        
        RectInt roomA = new RectInt(room.x, room.y, splitPoint + intersectLength, room.height);
        RectInt roomB = new RectInt(room.x + splitPoint, room.y, room.width - splitPoint, room.height);
        rooms.Remove(room);
        rooms.Add(roomA);
        rooms.Add(roomB);
        
        HorizontalSplit(rooms, roomA, counter + 1);
        HorizontalSplit(rooms, roomB, counter + 1);
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
            DebugDrawingBatcher.BatchCall(() => AlgorithmsUtils.DebugRectInt(room, Color.green, 100f));
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
