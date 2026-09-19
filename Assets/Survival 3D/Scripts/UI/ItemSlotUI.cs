using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemSlotUI : MonoBehaviour
{
   public Button button;
   public Image icon;
   public TextMeshProUGUI quantityText;
   private ItemSlot currentslot;
   private Outline outline;

   public int index;
   public bool equipped;
   
   
   private void Awake()
   {
      outline = GetComponent<Outline>();
   }

   private void OnEnable()
   {
      if (outline != null)
      {
         outline.enabled = equipped;
      }
   }

   public void Set(ItemSlot slot)
   {
      //set the slot
      currentslot = slot;
      
      if (slot == null || slot.item == null)
      {
         Clear();
         return;
      }

      //enable the icon
      if (icon != null)
      {
         icon.gameObject.SetActive(true);
         //set the sprite of item
         icon.sprite = slot.item.icon;
      }
      
      //if slots.quantity greater than 1 then run slot.quantity.tostring() but if its not greater than 1 them string is empty
      if (quantityText != null)
      {
         quantityText.text = slot.quantity > 1 ? slot.quantity.ToString() : string.Empty;
      }

      if (outline != null)
      {
         outline.enabled = equipped;
      }
   }
   
   // function when we remove item
   public void Clear()
   {
      currentslot = null;
      //disable the icon
      if (icon != null)
      {
         icon.gameObject.SetActive(false);
      }
      //set quantity text empty we dont have item inside slot
      if (quantityText != null)
      {
         quantityText.text = string.Empty;
      }
   }

   public void OnClickButton()
   {
      if (Inventory.instance == null)
         Inventory.instance = FindAnyObjectByType<Inventory>();

      if (Inventory.instance != null)
      {
         Inventory.instance.SelectItem(index);
      }
   }
   
   
   
}
