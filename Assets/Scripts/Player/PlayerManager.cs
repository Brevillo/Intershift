using UnityEngine;
using System.Collections;
using TMPro;

public class PlayerManager : MonoBehaviour {

    internal InputManager input;
    internal PlayerMovement movement;
    internal PlayerHealth health;

    internal Rigidbody2D rb;
    internal BoxCollider2D col;
    internal SpriteRenderer rend;

    internal CameraMovement cam;
    internal RoomManager rooms;

    internal LayerMask groundMask, playerMask;

    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private bool showDebugText;

    void Start() {
        GetReferences();
    }

    public void GetReferences() {

        GetComp(ref input);
        GetComp(ref movement);
        GetComp(ref health);

        GetComp(ref rb);
        GetComp(ref col);
        GetComp(ref rend);

        cam = FindObjectOfType<CameraMovement>();
        rooms = FindObjectOfType<RoomManager>();

        groundMask = LayerMask.GetMask("Ground");
        playerMask = LayerMask.GetMask("Player");
    }

    private void GetComp<type>(ref type get) => get = GetComponent<type>();

    public void FreezePlayer(bool freeze) {
        rb.isKinematic = freeze;
        if (freeze) rb.velocity = Vector2.zero;
    }

    public void ScreenFreeze(float dur) => StartCoroutine(ScreenFreezeCoroutine(dur));

    private IEnumerator ScreenFreezeCoroutine(float dur) {
        float ogTimeScale = Time.timeScale;
        Time.timeScale = 0;
        yield return new WaitForSecondsRealtime(dur);
        Time.timeScale = ogTimeScale;
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
