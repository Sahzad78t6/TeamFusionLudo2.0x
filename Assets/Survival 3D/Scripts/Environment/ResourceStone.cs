using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using Unity.Mathematics;
using UnityEngine;

public class ResourceStone : MonoBehaviour, IInteractable
{
    public ItemDatabase itemToGive;
    public int capacity;
    public int quantityPerHit = 1;
    public GameObject hitParticle;

    public string GetInteractPrompt()
    {
        return itemToGive != null ? string.Format("Mine {0}", itemToGive.displayName) : "Mine Stone";
    }

    public void OnInteract()
    {
        Gather(transform.position, Vector3.up);
    }

    public void Gather(Vector3 hitpoint, Vector3 hitNormal)
    {
        for (int i = 0; i < quantityPerHit; i++)
        {
            if (capacity <= 0)
                break;
            capacity -= 1;
            
            if (Inventory.instance == null)
                Inventory.instance = FindAnyObjectByType<Inventory>();

            if (Inventory.instance != null && itemToGive != null)
            {
                Inventory.instance.AddItem(itemToGive);
            }

            if (NotificationUI.instance != null && itemToGive != null)
            {
                NotificationUI.instance.ShowNotification(string.Format("+{0} {1}", quantityPerHit, itemToGive.displayName.ToUpper()), new Color(0.3f, 1f, 0.4f));
            }
        }
        if (hitParticle != null)
        {
            Destroy(Instantiate(hitParticle, hitpoint, quaternion.LookRotation(hitNormal, Vector3.up)), 1.0f);
        }
        
        if (capacity <= 0)
            Destroy(gameObject);
    }
}