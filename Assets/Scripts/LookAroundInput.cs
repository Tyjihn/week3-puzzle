using UnityEngine;
using UnityEngine.InputSystem;

// Shared "hold to look around" input for puzzle interaction modes.
// Space is the main control (works with one-button Mac mice); right mouse is kept as an alternative.
public static class LookAroundInput
{
    public const string PromptText = "Hold Space to look";

    public static bool IsHeld()
    {
        bool spaceHeld = Keyboard.current != null && Keyboard.current.spaceKey.isPressed;
        bool rightMouseHeld = Mouse.current != null && Mouse.current.rightButton.isPressed;
        return spaceHeld || rightMouseHeld;
    }

    // Updates the lookingAround flag and cursor state when the hold state changes.
    public static void Update(ref bool lookingAround)
    {
        bool held = IsHeld();

        if (held == lookingAround)
            return;

        lookingAround = held;
        Cursor.lockState = held ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !held;
    }
}
