using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CraftingTable : Buildings, IInteractable
{
    public CraftingWindow craftingWindow;
    private PlayerController player;
    
    private void Start()
    {
        var windows = UnityEngine.Resources.FindObjectsOfTypeAll<CraftingWindow>();
        if (windows.Length > 0) craftingWindow = windows[0];
        player = PlayerController.instance ?? FindAnyObjectByType<PlayerController>();
    }

    public string GetInteractPrompt()
    {
        return "Craft";
    }

    public void OnInteract()
    {
        if (craftingWindow == null)
        {
            var windows = UnityEngine.Resources.FindObjectsOfTypeAll<CraftingWindow>();
            if (windows.Length > 0) craftingWindow = windows[0];
        }
        if (player == null)
        {
            player = PlayerController.instance ?? FindAnyObjectByType<PlayerController>();
        }

        if (craftingWindow != null) craftingWindow.gameObject.SetActive(true);
        if (player != null) player.ToggleCursor(true);
    }
}
