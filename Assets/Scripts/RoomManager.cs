using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RoomGroup {
    public string name = "newGroup";
    public Color color = Color.white;
    public List<Room> rooms;
}

[System.Serializable]
public class Room {
    public Vector2Int pos, size = Vector2Int.one;
}

public class RoomManager : MonoBehaviour {

    [SerializeField] private Vector2 roomSize, checkBuffer;
    [SerializeField] private bool alwaysShowBounds,
                                  showCheck;

    [SerializeField] private List<RoomGroup> roomGroups;

    private PlayerManager m;

    private void Start() {
        m = FindObjectOfType<PlayerManager>();
    }

    private void Update() { 
        foreach (RoomGroup g in roomGroups)
            foreach (Room r in g.rooms)
                if (Physics2D.OverlapBox(r.pos * roomSize, roomSize - checkBuffer * 2, 0, m.playerMask)) {
                    m.cam.ChangeRoom(r.pos * roomSize);
                    m.cam.TrackWithin(r.pos * roomSize, (r.pos + r.size - Vector2.one) * roomSize);
                }
    }

    private void DrawBounds() {

        foreach (RoomGroup g in roomGroups)
            foreach (Room r in g.rooms) {

                Vector2 pos = (r.pos + (r.size - Vector2.one) / 2) * roomSize,
                        size = r.size * roomSize;

                Gizmos.color = g.color;
                Gizmos.DrawWireCube(pos, size);

                if (showCheck) {
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireCube(pos, size - checkBuffer * 2);
                }
            }
    }

    private void OnDrawGizmosSelected(){
        if (!alwaysShowBounds) DrawBounds();
    }
    private void OnDrawGizmos() {
        if (alwaysShowBounds) DrawBounds();
    }
}