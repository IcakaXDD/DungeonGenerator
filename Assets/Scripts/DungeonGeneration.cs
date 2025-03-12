using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class DungeonGenerator : MonoBehaviour
{
    public int SIZE = 100;
    public int numberOfRooms = 50;
    public int wMin = 5;
    public int hMin = 5;
    public int intersectLength = 1;

    private RectInt bigRoom;
    public List<RectInt> rooms;

    private void Start()
    {
        GenerateDungeon();
    }

    private void GenerateDungeon()
    {
        bigRoom = new RectInt(0, 0, SIZE, SIZE);
        rooms = new List<RectInt>();

        // Use a queue to control room splitting
        List<RectInt> roomList = new List<RectInt> { bigRoom };

        while (roomList.Count < numberOfRooms)
        {
            // Get the largest room to split
            roomList.Sort((a, b) => b.width * b.height - a.width * a.height);
            RectInt currentRoom = roomList[0];
            roomList.RemoveAt(0);

            bool splitHorizontally = Random.value > 0.5f;

            if (splitHorizontally)
            {
                SplitRoomHorizontally(currentRoom, roomList);
            }
            else
            {
                SplitRoomVertically(currentRoom, roomList);
            }
        }

        rooms = roomList;
        StartCoroutine(DrawDungeon(rooms));
    }

    private void SplitRoomHorizontally(RectInt room, List<RectInt> roomList)
    {
        if (room.height < hMin * 2) return;

        int splitPoint = Random.Range(hMin, room.height - hMin);
        RectInt roomA = new RectInt(room.x, room.y, room.width, splitPoint + intersectLength);
        RectInt roomB = new RectInt(room.x, room.y + splitPoint, room.width, room.height - splitPoint);

        roomList.Add(roomA);
        roomList.Add(roomB);
    }

    private void SplitRoomVertically(RectInt room, List<RectInt> roomList)
    {
        if (room.width < wMin * 2) return;

        int splitPoint = Random.Range(wMin, room.width - wMin);
        RectInt roomA = new RectInt(room.x, room.y, splitPoint + intersectLength, room.height);
        RectInt roomB = new RectInt(room.x + splitPoint, room.y, room.width - splitPoint, room.height);

        roomList.Add(roomA);
        roomList.Add(roomB);
    }

    private IEnumerator DrawDungeon(List<RectInt> rooms)
    {
        foreach (var room in rooms)
        {
            yield return new WaitForSeconds(0.05f);
            DebugDrawingBatcher.BatchCall(() => AlgorithmsUtils.DebugRectInt(room, Color.green, 100f));
        }
    }
}

