using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using UnityEngine.PlayerLoop;

public class Inventory : MonoBehaviour
{
    public ItemSlotUI[] uiSlots;
    public ItemSlot[] slots;

    public GameObject inventoryWindow;
    public Transform dropPosition;

    [Header("selected item")] 
    private ItemSlot selectedItem;
    private int selectedItemIndex;
    public TextMeshProUGUI selectedItemName;
    public TextMeshProUGUI selectedItemDescription;
    public TextMeshProUGUI selectedItemStatName;
    public TextMeshProUGUI selectedItemStatValue;
    public GameObject useButton;
    public GameObject equipButton;
    public GameObject unequipButton;
    public GameObject dropButton;

    private int currentEquipIndex;
    
    //components
    private PlayerController controller;
    private PlayerNeeds needs;
    [Header("Events")]
    public UnityEvent onOpenInventory;
    public UnityEvent onCloseInventory;
    
    //Singleton
    public static Inventory instance;

    private void Awake()
    {
        instance = this;
        controller = GetComponent<PlayerController>();
        needs = GetComponent<PlayerNeeds>();
        EnsureInventoryWindow();
    }

    public void EnsureInventoryWindow()
    {
        if (inventoryWindow != null) return;

        var canvases = UnityEngine.Resources.FindObjectsOfTypeAll<Canvas>();
        foreach (var c in canvases)
        {
            if (c != null && (c.gameObject.name == "Inventory Canvas" || c.gameObject.name.Contains("Inventory")))
            {
                inventoryWindow = c.gameObject;
                break;
            }
        }
        if (inventoryWindow == null)
        {
            GameObject invObj = GameObject.Find("Inventory Canvas") ?? GameObject.Find("InventoryWindow") ?? GameObject.Find("InventoryCard") ?? GameObject.Find("Inventory") ?? GameObject.Find("InventoryPanel");
            if (invObj != null) inventoryWindow = invObj;
        }

        EnsureCloseButton();
    }

    private void EnsureCloseButton()
    {
        if (inventoryWindow == null) return;
        Transform existing = inventoryWindow.transform.Find("CloseButton");
        if (existing == null)
        {
            GameObject closeObj = new GameObject("CloseButton");
            closeObj.transform.SetParent(inventoryWindow.transform, false);
            RectTransform rt = closeObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-40, -40);
            rt.sizeDelta = new Vector2(64, 64);

            Image img = closeObj.AddComponent<Image>();
            img.color = new Color(0.85f, 0.2f, 0.2f, 0.9f);
            img.raycastTarget = true;

            Outline outline = closeObj.AddComponent<Outline>();
            outline.effectColor = Color.white;

            Button btn = closeObj.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => Toggle());

            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(closeObj.transform, false);
            TextMeshProUGUI txt = txtObj.AddComponent<TextMeshProUGUI>();
            txt.text = "X";
            txt.fontSize = 32;
            txt.fontStyle = FontStyles.Bold;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = Color.white;
            txt.raycastTarget = false;
            RectTransform textRt = txtObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = Vector2.zero;

            closeObj.transform.SetAsLastSibling();
        }
    }

    private void Start()
    {
        EnsureInventoryWindow();
        if (inventoryWindow != null) inventoryWindow.SetActive(false);
        slots = new ItemSlot[uiSlots.Length];
        
        //initialize the slots
        for (int x = 0; x < slots.Length; x++)
        {
            slots[x] = new ItemSlot();
            uiSlots[x].index = x;
            //all the slots gonna be set clear so we are holding no item
            uiSlots[x].Clear();
        }
        
        ClearSelectedItemWindow();
    }

    private void Update()
    {
        // PC keyboard Tab / I keys to toggle inventory
        if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.I))
        {
            Toggle();
        }

        for (int i = 0; i < 8; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                QuickEquipSlot(i);
            }
        }
    }

    public void QuickEquipSlot(int index)
    {
        if (index < 0 || index >= slots.Length || slots[index] == null || slots[index].item == null) return;
        
        ItemDatabase item = slots[index].item;

        if (item.type == ItemType.Equipable)
        {
            if (uiSlots != null && index < uiSlots.Length)
            {
                if (uiSlots[index].equipped)
                {
                    UnEquip(index);
                }
                else
                {
                    if (currentEquipIndex < uiSlots.Length && uiSlots[currentEquipIndex].equipped)
                        UnEquip(currentEquipIndex);
                    
                    uiSlots[index].equipped = true;
                    currentEquipIndex = index;
                    if (EquipManager.instance != null)
                    {
                        EquipManager.instance.EquipNewItem(item);
                    }
                    UpdateUI();
                }
            }
            else if (EquipManager.instance != null)
            {
                EquipManager.instance.EquipNewItem(item);
            }
        }
        else if (item.type == ItemType.Consumable)
        {
            if (needs != null && item.consumables != null)
            {
                for (int x = 0; x < item.consumables.Length; x++)
                {
                    switch (item.consumables[x].type)
                    {
                        case ConsumableType.Health: needs.Heal(item.consumables[x].value); break;
                        case ConsumableType.Hunger: needs.Eat(item.consumables[x].value); break;
                        case ConsumableType.Thirst: needs.Drink(item.consumables[x].value); break;
                        case ConsumableType.Sleep: needs.Sleep(item.consumables[x].value); break;
                    }
                }
            }
            slots[index].quantity--;
            if (slots[index].quantity <= 0)
            {
                slots[index].item = null;
            }
            UpdateUI();
        }
    }

    public void OnInventoryButton(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started)
        {
            Toggle();
        }
    }

    public void Toggle()
    {
        EnsureInventoryWindow();

        if (inventoryWindow == null)
        {
            Debug.LogError("[Inventory] Failed to find inventoryWindow!");
            return;
        }

        if (controller == null)
        {
            controller = PlayerController.instance ?? FindAnyObjectByType<PlayerController>();
        }

        bool willBeActive = !inventoryWindow.activeSelf;
        if (inventoryWindow.transform.parent != null)
        {
            inventoryWindow.transform.parent.gameObject.SetActive(true);
        }
        inventoryWindow.SetActive(willBeActive);

        // Ensure inventory canvas sorting order is high so it renders above HUD
        Canvas invCanvas = inventoryWindow.GetComponent<Canvas>() ?? inventoryWindow.GetComponentInParent<Canvas>();
        if (invCanvas != null)
        {
            invCanvas.overrideSorting = true;
            invCanvas.sortingOrder = 500;
        }

        if (willBeActive)
        {
            onOpenInventory?.Invoke();
            if (controller != null) controller.ToggleCursor(true);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            onCloseInventory?.Invoke();
            if (controller != null) controller.ToggleCursor(false);
        }
    }

    public bool isOpen()
    {
        return inventoryWindow != null && inventoryWindow.activeInHierarchy;
    }
    
    //check if we can stack the item of we can stack then look for stack to add it to it if it cant add it to empty slot.
    public void AddItem(ItemDatabase item)
    {
        //check if we can stack the item
        if (item.canStackItem)
        {
            // get item slot to stack it to and send over the item that we want to stack
            ItemSlot slotToStackTo = GetItemstack(item);
            //if it find any item to stack
            if (slotToStackTo != null)
            {
                //add one to that quantity
                slotToStackTo.quantity++;
                UpdateUI();
                return;
            }
        }
        
        //we dont have item to stack fo and find empty slot
        ItemSlot emptySlot = GetEmptySlot();
        //if we find empty slot
        if (emptySlot != null)
        {   
            //set the item to be the item slot
            emptySlot.item = item;
            // set the quantity to 1
            emptySlot.quantity = 1;
            UpdateUI();
            return;
        }
        // if we dont have any slot available item cant stack and then just throw item away
        ThrowItem(item);
    }
    
    //spawns the objects
    void ThrowItem(ItemDatabase item)
    {
        //instantiate an item in front of player with random rotation
        Instantiate(item.dropPrefab, dropPosition.position, Quaternion.Euler(Vector3.one * Random.value * 360.0f));
    }

    void UpdateUI()
    {   
        //loop through each of ui slots
        for (int x = 0; x < slots.Length; x++)
        {   
            // does this slot contain an item
            if (slots[x].item != null)
            {
                //set the ui slots if its true
                uiSlots[x].Set(slots[x]);
            }

            //if its not true
            else
            {
                // clear that ui slot
                uiSlots[x].Clear();
            }
                
        }
    }
    
    //look for inventory and try to find an item slot which we can stack this new item to it
    ItemSlot GetItemstack(ItemDatabase item)
    {
        for (int x = 0; x < slots.Length; x++)
        {   
            //check if the item contain the slots is the item that we want to add and its quantity is not above the max stack amount
            if (slots[x].item == item && slots[x].quantity < item.maxStackamount)
            {   
                // return the item slot that the requested item can be stacked on
                return slots[x];
            }
        }
        // if there is no stack available return null
        return null;
    }

    ItemSlot GetEmptySlot()
    {
        for (int x = 0; x < slots.Length; x++)
        {   
            //return an empty slot in the inventory
            if (slots[x].item == null)
            {
                return slots[x];
            }
        }
        // if they are no empty slots return null
        return null;
    }

    public void SelectItem(int index)
    {
        //if there is no item inside of slot just dont do anything
        if (slots[index].item == null)
            return;
        
        //setting the selected item slot
        selectedItem = slots[index];
        
        //setting selected item index
        selectedItemIndex = index;
        
        //set the name of item
        selectedItemName.text = selectedItem.item.displayName;
        
        //set the description of item
        selectedItemDescription.text = selectedItem.item.description;
        
        //set the stat value and stat names of item
        selectedItemStatName.text = string.Empty;
        selectedItemStatValue.text = string.Empty;

        for (int x = 0; x < selectedItem.item.consumables.Length; x++)
        {
            selectedItemStatName.text += selectedItem.item.consumables[x].type.ToString() + "\n";
            selectedItemStatValue.text += selectedItem.item.consumables[x].value.ToString() + "\n";
        }
        
        //set the use button
        useButton.SetActive(selectedItem.item.type == ItemType.Consumable);
        
        //set the equip button                                           //&& item is not currently equipped
        equipButton.SetActive(selectedItem.item.type == ItemType.Equipable && !uiSlots[index].equipped);

        //set the unequip button                                           //&& item is currently equipped
        unequipButton.SetActive(selectedItem.item.type == ItemType.Equipable && uiSlots[index].equipped);
        
        //set the drop button
        dropButton.SetActive(true);
    }

    void ClearSelectedItemWindow()
    {
        //clear the text elements
        selectedItem = null;
        selectedItemName.text = string.Empty;
        selectedItemDescription.text = string.Empty;
        selectedItemStatName.text = string.Empty;
        selectedItemStatValue.text = string.Empty;
        
        //disable buttons
        useButton.SetActive(false);
        equipButton.SetActive(false);
        unequipButton.SetActive(false);
        dropButton.SetActive(false);
    }

    public void OnUseButton()
    {
        if (selectedItem.item.type == ItemType.Consumable)
        {
            for (int x = 0; x < selectedItem.item.consumables.Length; x++)
            {
                switch (selectedItem.item.consumables[x].type)
                {
                    case ConsumableType.Health: needs.Heal(selectedItem.item.consumables[x].value); break;
                    case ConsumableType.Hunger: needs.Eat(selectedItem.item.consumables[x].value); break;
                    case ConsumableType.Thirst: needs.Drink(selectedItem.item.consumables[x].value); break;
                    case ConsumableType.Sleep: needs.Sleep(selectedItem.item.consumables[x].value); break;
                }
            }
        }
        RemoveSelectedItem();
    }

    public void OnEquipButton()
    {
        //currentEquipedIndex is the index of the item slot that is currently equipped
        //check if any item is equipped if so the unequiped if so the unequip that current item
        if (uiSlots[currentEquipIndex].equipped)
            UnEquip(currentEquipIndex);
        
        uiSlots[selectedItemIndex].equipped = true;
        currentEquipIndex = selectedItemIndex;
        //call the funciton EquipNewItem from equi Manager
        EquipManager.instance.EquipNewItem(selectedItem.item);
        //update UI Inventory
        UpdateUI();
        //update the buttons on the selected item preview window from equip to unequip
        SelectItem(selectedItemIndex);
    }
    
    //item Slot that we want Unequip
    void UnEquip(int index)
    {
        uiSlots[index].equipped = false;
        EquipManager.instance.UnEquipItem();
        UpdateUI();
        //if is this the currently selected item, if so then selected
        if (selectedItemIndex == index)
            SelectItem(index);
    }

    public void OnUnequipButton()
    {
        UnEquip(selectedItemIndex);
    }

    public void OnDropButton()
    {
        ThrowItem(selectedItem.item);
        RemoveSelectedItem();
    }
    
    //remove the current item that we selected from inventory
    void RemoveSelectedItem()
    {
        selectedItem.quantity--;
        //if we have 1 item left or removed it so we need to check
        if (selectedItem.quantity == 0)
        {
            // if items is equipped
            if(uiSlots[selectedItemIndex].equipped == true)
                // unequip that item
                UnEquip(selectedItemIndex);
            //set item to null ( item is no longer available in inventory )
            selectedItem.item = null;
            ClearSelectedItemWindow();
        }
        //update quantity and icon
        UpdateUI();
    }
    
    // if we crafting or building something, we need to remove set of this items
    public void RemoveItem(ItemDatabase item)
    {
        //loop through item slots
        for (int i = 0; i < slots.Length; i++)
        {
            //does this slot contain item?
            if (slots[i].item == item)
            {
                //subtrack1 from quantity
                slots[i].quantity--;
                
                //but if the quantity is 0 reset the slot
                if (slots[i].quantity == 0)
                {
                    // if item equiped unequip it first
                    if(uiSlots[i].equipped ==true)
                        UnEquip(i);
                    //reset the slot
                    slots[i].item = null;
                    ClearSelectedItemWindow();
                }
                UpdateUI();
                return;
            }
        }
    }

    public bool HasItems(ItemDatabase item, int quantity)
    {
        int amount = 0;
        //loop through item slots
        for (int i = 0; i < slots.Length; i++)
        {
            //if this item is the item we request if so add 1 to it
            if (slots[i].item == item)
                amount += slots[i].quantity;
            //if this item amount is bigger than quantity return true
            if (amount >= quantity)
                return true;
        }
        //we dont have enough items
        return false;
    }

}

public class ItemSlot
{
    public ItemDatabase item;
    public int quantity;
}