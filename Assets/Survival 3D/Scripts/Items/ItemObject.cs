using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemObject : MonoBehaviour, IInteractable
{
    public ItemDatabase item;

    public string GetInteractPrompt()
    {
        return item != null ? string.Format("Pick Up {0}", item.displayName) : "Pick Up Item";
    }

    public void OnInteract()
    {
        if (Inventory.instance == null)
            Inventory.instance = FindAnyObjectByType<Inventory>();

        //after interact add item to inventory
        if (Inventory.instance != null && item != null)
        {
            Inventory.instance.AddItem(item);
        }
        //destroy item in the world because we picked them up
        Destroy(gameObject);
    }
}
