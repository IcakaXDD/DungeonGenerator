using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DungeonGeneration : MonoBehaviour
{
    public int intersectRooms = 5;
    public bool splitHorizontally;
    public int numberOfRooms = 5;
    public float height;
    public int minWidth;
    public int minHeight;
    public bool firstTime = true;
    public RectInt bigRoom;
    public List<RectInt> rooms;

    void Start()
    {
        bigRoom = new RectInt(0, 0, 200, 200);
        rooms.Add(bigRoom);
        AlgorithmsUtils.DebugRectInt(bigRoom, Color.green, 5, true, height);
        StartCoroutine(GenerateRooms(bigRoom));
    }

    IEnumerator GenerateRooms(RectInt bigRoom)
    {
        yield return new WaitForSeconds(1.5f);
        while (numberOfRooms > rooms.Count)
        {
            int randomRoom = Random.Range(0, rooms.Count);
            yield return new WaitForSeconds(0.1f);

            RectInt roomToSplit = rooms[randomRoom];

            if (roomToSplit.width < minWidth || roomToSplit.height < minHeight)
            {
                continue; 
            }

            int isItHorizontal = Random.Range(0, 2);
            if (isItHorizontal == 0)
            {
                splitHorizontally = true;
            }
            else
            {
                splitHorizontally = false;
            }

            if (splitHorizontally)
            {
                SplitRoomsHorizontally(randomRoom);
            }
            else
            {
                SplitVertically(randomRoom);
            }
        }
    }

    private (RectInt, RectInt) SplitRoomsHorizontally(int indexRoom)
    {
        RectInt roomToDivide = rooms[indexRoom];
        int halfHeight = roomToDivide.height / 2;

        RectInt roomA = new RectInt(roomToDivide.x, roomToDivide.y, roomToDivide.width, halfHeight);
        RectInt roomB = new RectInt(roomToDivide.x, roomToDivide.y + halfHeight, roomToDivide.width, halfHeight);

        DebugDrawingBatcher.BatchCall(() => AlgorithmsUtils.DebugRectInt(roomA, Color.red, 100, true, height));
        DebugDrawingBatcher.BatchCall(() => AlgorithmsUtils.DebugRectInt(roomB, Color.blue, 100, true, height));

        rooms[indexRoom] = roomA;
        rooms.Add(roomB);

        return (roomA, roomB);
    }

    private (RectInt, RectInt) SplitVertically(int roomIndex)
    {
        RectInt roomToDivide = rooms[roomIndex];
        int halfWidth = roomToDivide.width / 2;

        RectInt roomA = new RectInt(roomToDivide.x, roomToDivide.y, halfWidth, roomToDivide.height);
        RectInt roomB = new RectInt(roomToDivide.x + halfWidth, roomToDivide.y, halfWidth, roomToDivide.height);

        DebugDrawingBatcher.BatchCall(() => AlgorithmsUtils.DebugRectInt(roomA, Color.magenta, 1, false, height));
        DebugDrawingBatcher.BatchCall(() => AlgorithmsUtils.DebugRectInt(roomB, Color.blue, 1, true, height));

        rooms[roomIndex] = roomA;
        rooms.Add(roomB);

        return (roomA, roomB);
    }
}