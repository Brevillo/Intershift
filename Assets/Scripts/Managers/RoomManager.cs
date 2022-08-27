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
    public string name;
    public Vector2Int pos = Vector2Int.zero,
                      size = Vector2Int.one;
    public RespawnZone[] respawnZones;

    public Vector2 Center() => pos + (size - Vector2.one) / 2f;
    public Vector2 UpperBound() => pos + size - Vector2.one;
}

[System.Serializable]
public class RespawnZone {
    public Vector2 pos, size, gravDir;
}

public class RoomManager : MonoBehaviour {

    internal readonly Vector2 roomSize = new Vector2(64f, 36f);
    [SerializeField] private Vector2 checkBuffer;
    [SerializeField] private bool alwaysShowBounds,
                                  showTriggers;

    [SerializeField] private List<RoomGroup> roomGroups;

    internal UnityEvent<Room> RoomChange = new UnityEvent<Room>();
    internal UnityEvent<Vector2> ExitRespawnZone = new UnityEvent<Vector2>();
    private Room currentRoom;
    private RespawnZone currentRespawnZone;

    private PlayerManager m;

    private void Start() {
        m = FindObjectOfType<PlayerManager>();
    }

    private void Update() {
        CheckRooms();
        CheckRespawn();

        currentRespawnZone = null;
        print(currentRespawnZone != null);
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

    private void CheckRespawn() {
        Vector2 pos = currentRoom.Center() * roomSize;

        // exit respawn zone
        if (currentRespawnZone != null && !Physics2D.OverlapBox(currentRespawnZone.pos + pos, currentRespawnZone.size, 0, m.playerMask)) {
            currentRespawnZone = null;
            print("exited respawn zone");
            ExitRespawnZone.Invoke(currentRespawnZone.gravDir);
        }

        // enter respawn zone
        foreach (RespawnZone z in currentRoom.respawnZones)
            if (currentRespawnZone != z && Physics2D.OverlapBox(z.pos + pos, z.size, 0, m.playerMask)) {
                currentRespawnZone = z;
                print("new respawn zone");
                return;
            }
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

                // respawn zones
                foreach (RespawnZone z in r.respawnZones) Gizmos.DrawWireCube(z.pos + pos, z.size);

                if (showTriggers) {
                    Gizmos.color = Color.green;

                    // screen transition trigger
                    Gizmos.DrawWireCube(pos, size - checkBuffer * 2f);

                    // camera track threshold
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