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
    public RespawnPoint[] respawnPoints;

    public Vector2 Center() => pos + (size - Vector2.one) / 2f;
    public Vector2 UpperBound() => pos + size - Vector2.one;
}

[System.Serializable]
public class RespawnPoint {
    public Vector2 pos;
    public float gravDir;
    public Vector2 RealPos(Vector2 roomPos) => Physics2D.Raycast(pos + roomPos, gravDir.DegToVector(), Mathf.Infinity, PlayerManager.groundMask).point;
}

public class RoomManager : MonoBehaviour {

    public static RoomManager instance;

    internal readonly Vector2 roomSize = new Vector2(64f, 36f);
    [SerializeField] private Vector2 checkBuffer;
    [SerializeField] private bool alwaysShowBounds,
                                  showTriggers;

    [SerializeField] private List<RoomGroup> roomGroups;

    internal UnityEvent<Room> RoomChange = new UnityEvent<Room>();
    private Room currentRoom;

    private void Awake() {
        instance = this;
    }

    private void Update() {
        CheckRooms(PlayerManager.instance.transform.position);
    }

    public void CheckRooms(Vector2 player) {
        foreach (RoomGroup g in roomGroups)
            foreach (Room r in g.rooms) {
                if (r == currentRoom) continue;

                Vector2 center = r.Center() * roomSize,
                        size   = r.size * roomSize / 2f;

                // check if player is in the room
                if (Mathf.Abs(player.x - center.x) < size.x && Mathf.Abs(player.y - center.y) < size.y) {

                    RoomChange.Invoke(r);
                    currentRoom = r;

                    // get checkpoint
                    RespawnPoint close = null;
                    float dist = Mathf.Infinity;

                    foreach (RespawnPoint p in r.respawnPoints) {
                        float newDist = (p.pos - player).sqrMagnitude;
                        if (newDist < dist) {
                            dist = newDist;
                            close = p;
                        }
                    }

                    if (close != null) {
                        Vector2 respawn = close.RealPos(r.Center() * roomSize);
                        PlayerMovement.respawmInfo = new Vector3(respawn.x, respawn.y, close.gravDir);
                    }

                    return;
                }
            }
    }

    private void DrawBounds() {

        CameraMovement cam = FindObjectOfType<CameraMovement>();

        foreach (RoomGroup g in roomGroups)
            foreach (Room r in g.rooms) {

                Vector2 pos = r.Center() * roomSize,
                        size = r.size * roomSize;

                // room bounds
                Gizmos.color = g.color;
                Gizmos.DrawWireCube(pos, size);

                // respawn points
                foreach (RespawnPoint p in r.respawnPoints) {
                    Gizmos.DrawWireSphere(p.pos + pos, 1f);
                    Gizmos.DrawLine(p.pos + pos, p.RealPos(pos));
                }

                if (showTriggers) {
                    Gizmos.color = Color.green;

                    // screen transition trigger
                    Gizmos.DrawWireCube(pos, size - checkBuffer * 2f);

                    // camera track threshold
                    Gizmos.DrawWireCube(pos, size - roomSize - cam.trackBoxExtents * 2f);
                    Gizmos.DrawWireCube(pos, size - roomSize + cam.trackBoxExtents * 2f);
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