using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

public class ResourceFruitTree : MonoBehaviour, IInteractable
{
    public ItemDatabase itemToGive;
    public ItemDatabase itemtoGive2;
    public int capacityitem1;
    public int capacityitem2;
    public int quantityPerHit = 1;
    public GameObject hitParticle;

    public string GetInteractPrompt()
    {
        return itemToGive != null ? string.Format("Harvest {0}", itemToGive.displayName) : "Harvest Fruit";
    }

    public void OnInteract()
    {
        Gather(transform.position, Vector3.up);
    }

    public void Gather(Vector3 hitpoint, Vector3 hitNormal)
    {
        if (Inventory.instance == null)
            Inventory.instance = FindAnyObjectByType<Inventory>();

        for (int i = 0; i < quantityPerHit; i++)
        {
            if (capacityitem1 <= 0)
                break;
            capacityitem1 -= 1;

            if (Inventory.instance != null && itemToGive != null)
            {
                Inventory.instance.AddItem(itemToGive);
            }

            if (NotificationUI.instance != null && itemToGive != null)
            {
                NotificationUI.instance.ShowNotification(string.Format("+{0} {1}", quantityPerHit, itemToGive.displayName.ToUpper()), new Color(0.3f, 1f, 0.4f));
            }
        }
        for (int x = 0; x < quantityPerHit; x++)
        {
            if (capacityitem2 <= 0)
                break;
            capacityitem2 -= 1;
            
            if (Inventory.instance != null && itemtoGive2 != null)
            {
                Inventory.instance.AddItem(itemtoGive2);
            }

            if (NotificationUI.instance != null && itemtoGive2 != null)
            {
                NotificationUI.instance.ShowNotification(string.Format("+{0} {1}", quantityPerHit, itemtoGive2.displayName.ToUpper()), new Color(0.3f, 1f, 0.4f));
            }
        }
        if (hitParticle != null)
        {
            Destroy(Instantiate(hitParticle, hitpoint, quaternion.LookRotation(hitNormal, Vector3.up)), 1.0f);
        }
        
        if (capacityitem1 <= 0)
            Destroy(gameObject);
    }
}