using UnityEngine;
using System.Collections;
using TMPro;

public class PlayerManager : MonoBehaviour {

    public static Camera camera;

    internal InputManager input;
    internal PlayerMovement movement;
    internal PlayerHealth health;

    internal Rigidbody2D rb;
    internal BoxCollider2D col;
    internal SpriteRenderer rend;

    internal CameraMovement cam;

    internal LayerMask groundMask, playerMask;

    [SerializeField] private TextMeshProUGUI text;

    void Start() {
        camera = Camera.main;

        input = GetComponent<InputManager>();
        movement = GetComponent<PlayerMovement>();
        health = GetComponent<PlayerHealth>();
        //anim = GetComponent<PlayerAnimation>();

        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<BoxCollider2D>();
        rend = GetComponent<SpriteRenderer>();
        //animator = GetComponent<Animator>();

        cam = FindObjectOfType<CameraMovement>();

        groundMask = LayerMask.GetMask("Ground");
        playerMask = LayerMask.GetMask("Player");
    }

    public void FreezePlayer(bool freeze) {
        rb.isKinematic = freeze;
        if (freeze) rb.velocity = Vector2.zero;
    }

    private void LateUpdate(){
        text.text = debugText;
        debugText = "";
    }

    private string debugText;
    public void DebugText(string line) {
        debugText += line + "\n";
    }
}
