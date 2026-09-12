using UnityEngine;
using UnityEngine.InputSystem;

public static class InputRef
{
    public static InputAction Find(string path, Object owner)
    {
        InputActionAsset actions = InputSystem.actions;
        if (actions == null)
        {
            Debug.LogError("no project wide input actions asset assigned in Project Settings", owner);
            return null;
        }

        InputAction action = actions.FindAction(path);
        if (action == null)
        {
            Debug.LogError($"input action '{path}' is missing from InputSystem_Actions", owner);
        }
        return action;
    }
}
