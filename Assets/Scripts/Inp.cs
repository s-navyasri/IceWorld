using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Works with the old Input Manager, the new Input System, or both.
public static class Inp
{
    public static bool Left, Right, Jump, Slide, Pause;
    static Vector2 start; static bool swiping;

    public static void Poll()
    {
        Left = Right = Jump = Slide = Pause = false;
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        if (k != null)
        {
            Left = k.leftArrowKey.wasPressedThisFrame || k.aKey.wasPressedThisFrame;
            Right = k.rightArrowKey.wasPressedThisFrame || k.dKey.wasPressedThisFrame;
            Jump = k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame;
            Slide = k.downArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame;
            Pause = k.escapeKey.wasPressedThisFrame || k.pKey.wasPressedThisFrame;
        }
        var p = Pointer.current;
        if (p != null)
        {
            if (p.press.wasPressedThisFrame) { start = p.position.ReadValue(); swiping = true; }
            if (p.press.wasReleasedThisFrame && swiping) { Swipe(p.position.ReadValue() - start); swiping = false; }
        }
#else
        Left = Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A);
        Right = Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D);
        Jump = Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space);
        Slide = Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S);
        Pause = Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P);
        if (Input.GetMouseButtonDown(0)) { start = Input.mousePosition; swiping = true; }
        if (Input.GetMouseButtonUp(0) && swiping) { Swipe((Vector2)Input.mousePosition - start); swiping = false; }
#endif
    }

    static void Swipe(Vector2 d)
    {
        if (d.magnitude < 40) return;
        if (Mathf.Abs(d.x) > Mathf.Abs(d.y)) { if (d.x > 0) Right = true; else Left = true; }
        else if (d.y > 0) Jump = true; else Slide = true;
    }
}
