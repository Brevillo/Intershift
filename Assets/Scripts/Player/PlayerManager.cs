using UnityEngine;
using System.Collections;
using TMPro;

public class PlayerManager : MonoBehaviour {

    public static PlayerManager instance;
    public static InputManager input;
    public static PlayerMovement movement;
    public static PlayerHealth health;

    public static new Transform transform;
    public static Rigidbody2D rb;
    public static BoxCollider2D col;
    public static SpriteRenderer rend;

    public static CameraMovement cam;
    public static Camera cameraObject;
    public static RoomManager rooms;

    public static LayerMask groundMask, playerMask;

    [SerializeField] LayerMask groundMaskSerialized, playerMaskSerialized;
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private bool showDebugText;

    private void OnValidate() {
        groundMask = groundMaskSerialized;
        playerMask = playerMaskSerialized;
    }

    private void Awake() {

        instance = this;

        Get(ref input);
        Get(ref movement);
        Get(ref health);

        Get(ref transform);
        Get(ref rb);
        Get(ref col);
        Get(ref rend);

        cam = FindObjectOfType<CameraMovement>();
        cameraObject = Camera.main;
        rooms = FindObjectOfType<RoomManager>();

        groundMask = LayerMask.GetMask("Ground");
        playerMask = LayerMask.GetMask("Player");
    }

    private void Get<type>(ref type get) => get = GetComponent<type>();

    public void FreezePlayer(bool freeze) {
        rb.isKinematic = freeze;
        if (freeze) rb.velocity = Vector2.zero;
    }

    private void LateUpdate(){
        text.text = showDebugText ? debugText : "";
        debugText = "";
    }

    private string debugText;
    public void DebugText(string line) {
        debugText += line + "\n";
    }
}
