using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lava : MonoBehaviour {

    private void OnTriggerEnter2D(Collider2D collision) {
        if (collision.transform.parent != null
         && collision.transform.parent.TryGetComponent(out PlayerHealth player))
            player.Death();
    }
}
