using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class RoomGroup {
    public string name = "newGroup";
    public Color color = Color.white;
    public List<Room> rooms;
}

[System.Serializable]
public class Room {
    public Vector2Int pos = Vector2Int.zero,
                      size = Vector2Int.one;
    public Vector2 Center() => pos + (size - Vector2.one) / 2f;
    public Vector2 UpperBound() => pos + size - Vector2.one;

    public Room(Vector2Int pos, Vector2Int size) {
        this.pos = pos;
        this.size = size;
    }
}

public class RoomManager : MonoBehaviour {

    internal readonly Vector2 roomSize = new Vector2(64f, 36f);
    [SerializeField] private Vector2 checkBuffer;
    [SerializeField] private bool alwaysShowBounds,
                                  showTriggers;

    [SerializeField] private List<RoomGroup> roomGroups;

    internal UnityEvent<Room> RoomChange = new UnityEvent<Room>();
    private Room currentRoom;

    private PlayerManager m;

    private void Start() {
        m = FindObjectOfType<PlayerManager>();
    }

    private void Update() {
        CheckRooms();
    }

    public Room CheckRooms() {
        foreach (RoomGroup g in roomGroups) foreach (Room r in g.rooms)
            if (currentRoom != r && Physics2D.OverlapBox(r.Center() * roomSize, r.size * roomSize - checkBuffer * 2f, 0, m.playerMask)) {
                RoomChange.Invoke(r);
                currentRoom = r;
                return r;
            }
        return null;
    }

    private void DrawBounds() {

        if (m == null) {
            m = FindObjectOfType<PlayerManager>();
            if (m.cam == null) m.GetReferences();
        }

        foreach (RoomGroup g in roomGroups)
            foreach (Room r in g.rooms) {

                Vector2 pos = r.Center() * roomSize,
                        size = r.size * roomSize;

                Gizmos.color = g.color;
                Gizmos.DrawWireCube(pos, size);

                if (showTriggers) {
                    // screen transition trigger
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireCube(pos, size - checkBuffer * 2f);

                    // camera track threshold
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireCube(pos, size - roomSize - m.cam.trackBoxExtents * 2f);
                    Gizmos.DrawWireCube(pos, size - roomSize + m.cam.trackBoxExtents * 2f);
                }
            }
    }

    private void OnDrawGizmosSelected() {
        if (!alwaysShowBounds) DrawBounds();
    }
    private void OnDrawGizmos() {
        if (alwaysShowBounds) DrawBounds();
    }
}