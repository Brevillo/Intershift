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
    [HideInInspector] public string elementName;
    public string name;
    public Vector2 pos = Vector2.zero,
                   size = Vector2.one;
    public float cameraSize = 1f;
    public RespawnPoint[] respawnPoints;

    public Vector2 center => pos + (size - Vector2.one) / 2f;
}

[System.Serializable]
public class RespawnPoint {
    public Vector2 pos;
    public float gravDir;
    public Vector2 worldPos => Physics2D.Raycast(pos + room.pos * RoomManager.roomSize, gravDir.DegToVector(), Mathf.Infinity, PlayerManager.groundMask).point;
    internal Room room;
}

public class RoomManager : MonoBehaviour {

    public static RoomManager instance;

    [SerializeField] Vector3Int startAt;

    public static readonly Vector2 roomSize = new Vector2(64f, 36f);
    [SerializeField] private Vector2 checkBuffer;
    [SerializeField] private bool alwaysShowBounds,
                                  showTriggers;

    [SerializeField] private List<RoomGroup> roomGroups;

    internal event System.Action<Room> RoomChange;
    private Room currentRoom;

    private void OnValidate() {

        if (Application.isPlaying) return;

        // name room elements
        foreach (RoomGroup g in roomGroups)
            for (int i = 0; i < g.rooms.Count; i++) {
                Room r = g.rooms[i];
                r.elementName = i + "   " + r.name;

                foreach (RespawnPoint p in r.respawnPoints)
                    p.room = r;
            }

        // move camera and player according to startAt

        Room room = roomGroups[startAt.x].rooms[startAt.y];
        if (room != null) {

            RespawnPoint respawn = room.respawnPoints[startAt.z];
            PlayerMovement.respawnInfo.XY(respawn.worldPos);
            PlayerMovement.respawnInfo.Z(respawn.gravDir);

            PlayerMovement player = FindObjectOfType<PlayerMovement>();
            Vector2 pos = PlayerMovement.respawnInfo;
            float dir = PlayerMovement.respawnInfo.z,
                  height = player.GetComponent<BoxCollider2D>().bounds.extents.y;

            player.transform.SetPositionAndRotation(pos + -dir.DegToVector() * height, Quaternion.Euler(0, 0, dir + 90f));

            FindObjectOfType<CameraMovement>().Snap(room, startAt.z);
        }
    }

    private void UpdateRespawnRooms() {
        foreach (RoomGroup g in roomGroups)
            foreach (Room r in g.rooms)
                foreach (RespawnPoint p in r.respawnPoints)
                    p.room = r;
    }

    private void Awake() {
        instance = this;

        UpdateRespawnRooms();
    }

    private void Update() {
        CheckRooms(PlayerManager.transform.position);
    }

    public void CheckRooms(Vector2 player) {
        foreach (RoomGroup g in roomGroups)
            foreach (Room r in g.rooms) {
                if (r == currentRoom) continue;

                Vector2 center = r.center * roomSize,
                        size   = r.size * roomSize / 2f;

                // check if player is in the room
                if (Mathf.Abs(player.x - center.x) < size.x && Mathf.Abs(player.y - center.y) < size.y) {

                    RoomChange.Invoke(r);
                    currentRoom = r;

                    // get checkpoint
                    RespawnPoint close = null;
                    float dist = Mathf.Infinity;

                    foreach (RespawnPoint p in r.respawnPoints) {
                        float newDist = (p.worldPos - player).sqrMagnitude;
                        if (newDist < dist) {
                            dist = newDist;
                            close = p;
                        }
                    }

                    if (close != null) {
                        Vector2 respawn = close.worldPos;
                        PlayerMovement.respawnInfo = new Vector3(respawn.x, respawn.y, close.gravDir);
                    }

                    return;
                }
            }
    }

    private void DrawBounds() {

        CameraMovement cam = FindObjectOfType<CameraMovement>();

        foreach (RoomGroup g in roomGroups)
            foreach (Room r in g.rooms) {

                Vector2 pos = r.center * roomSize,
                        size = r.size * roomSize;

                // room bounds
                Gizmos.color = g.color;
                Gizmos.DrawWireCube(pos, size);

                // respawn points
                foreach (RespawnPoint p in r.respawnPoints) {
                    Vector2 respawnPivot = r.pos * roomSize;
                    Gizmos.DrawWireSphere(p.pos + respawnPivot, 1f);
                    Gizmos.DrawLine(p.pos + respawnPivot, p.worldPos);
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