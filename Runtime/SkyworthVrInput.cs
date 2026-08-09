using System;
using UnityEngine;

public static class SkyworthVrInput
{
    private static bool legacyInputUnavailable;

    public static bool GetButton(SkyworthVrButton button)
    {
        return Any(button, Input.GetKey);
    }

    public static bool GetButtonDown(SkyworthVrButton button)
    {
        return Any(button, Input.GetKeyDown);
    }

    public static bool GetButtonUp(SkyworthVrButton button)
    {
        return Any(button, Input.GetKeyUp);
    }

    private static bool Any(SkyworthVrButton button, Func<KeyCode, bool> read)
    {
        if (legacyInputUnavailable)
        {
            return false;
        }

        try
        {
            var keys = GetKeys(button);
            for (var i = 0; i < keys.Length; i++)
            {
                if (read(keys[i]))
                {
                    return true;
                }
            }
        }
        catch (InvalidOperationException exception)
        {
            legacyInputUnavailable = true;
            Debug.LogWarning("SKYWORTH_INPUT UnityEngine.Input is unavailable. Set Active Input Handling to Both or Input Manager (Old). " + exception.Message);
        }

        return false;
    }

    private static KeyCode[] GetKeys(SkyworthVrButton button)
    {
        switch (button)
        {
            case SkyworthVrButton.Confirm:
                return new[] { KeyCode.Return, KeyCode.JoystickButton0, KeyCode.Joystick1Button0 };
            case SkyworthVrButton.Back:
                return new[] { KeyCode.Escape };
            case SkyworthVrButton.Home:
                return new[] { KeyCode.Home };
            case SkyworthVrButton.Right:
                return new[] { KeyCode.RightArrow };
            case SkyworthVrButton.Down:
                return new[] { KeyCode.DownArrow };
            case SkyworthVrButton.VolumeUp:
            case SkyworthVrButton.VolumeDown:
                return Array.Empty<KeyCode>();
            default:
                return Array.Empty<KeyCode>();
        }
    }
}
