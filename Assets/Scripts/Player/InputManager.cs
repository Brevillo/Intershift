using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
public class Button {
    public bool pressed, down, released;
    private bool pressedLast;
    public void Update() {
        down = pressed && !pressedLast;
        released = !pressed && pressedLast;
        pressedLast = pressed;
    }
}

public class InputManager : MonoBehaviour {

    private Controls controls;

    internal Vector2 Move;
    internal Button
        Jump   = new Button(),
        Action = new Button(),

        Debug1 = new Button(),
        Debug2 = new Button(),
        FPS    = new Button(),
        Godmode = new Button();

    private void Awake() {

        controls = new Controls();
        var g = controls.Gameplay;

        // separated x/y axis because the input sytem was normalizing keyboard inputs
        g.HorizontalMove.performed += c => Move.x = c.ReadValue<float>();
        g.HorizontalMove.canceled += c => Move.x = 0;

        g.VerticalMove.performed += c => Move.y = c.ReadValue<float>();
        g.VerticalMove.canceled += c => Move.y = 0;

        g.Jump.performed += c => Jump.pressed = true;
        g.Jump.canceled += c => Jump.pressed = false;

        g.Action.performed += c => Action.pressed = true;
        g.Action.canceled += c => Action.pressed = false;


        g.Debug1.performed += c => Debug1.pressed = true;
        g.Debug1.canceled += c => Debug1.pressed = false;

        g.Debug2.performed += c => Debug2.pressed = true;
        g.Debug2.canceled += c => Debug2.pressed = false;

        g.FPS.performed += c => FPS.pressed = true;
        g.FPS.canceled += c => FPS.pressed = false;

        g.Godmode.performed += c => Godmode.pressed = true;
        g.Godmode.canceled += c => Godmode.pressed = false;
    }


    private void OnEnable() => controls.Gameplay.Enable();
    private void OnDisable() => controls.Gameplay.Disable();

    private void Update() {
        Jump.Update();
        Action.Update();

        Debug1.Update();
        Debug2.Update();
        FPS.Update();
        Godmode.Update();
    }
}
