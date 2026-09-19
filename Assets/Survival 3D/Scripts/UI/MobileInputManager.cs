using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class MobileInputManager : MonoBehaviour
{
    public static MobileInputManager instance;

    [Header("UI Canvas & Controls")]
    public Canvas mobileCanvas;
    public RectTransform joystickBackground;
    public RectTransform joystickHandle;
    public RectTransform touchLookZone;

    [Header("Joystick Settings")]
    public float joystickRange = 90f;
    private Vector2 moveInputVector;

    [Header("Touch Look Settings")]
    public float touchLookSensitivity = 0.5f;
    private Vector2 touchLookDelta;
    private int lookTouchId = -1;
    private Vector2 lastLookTouchPos;

    [Header("Hotbar UI Slots")]
    public List<Image> hotbarIcons = new List<Image>();
    public List<TextMeshProUGUI> hotbarQuantities = new List<TextMeshProUGUI>();
    public List<Outline> hotbarOutlines = new List<Outline>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void AutoInitialize()
    {
        if (instance == null && Object.FindAnyObjectByType<MobileInputManager>() == null)
        {
            GameObject managerObj = new GameObject("MobileInputManager");
            managerObj.AddComponent<MobileInputManager>();
        }
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        EnsureMobileUI();
    }

    public void EnsureMobileUI()
    {
        if (mobileCanvas != null) return;

        // Ensure EventSystem exists
        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<StandaloneInputModule>();
        }

        // Neutralize any invisible full-screen overlays that block raycasts
        foreach (var g in UnityEngine.Resources.FindObjectsOfTypeAll<Graphic>())
        {
            if (g != null && (g.gameObject.name == "bloodimage" || g.gameObject.name == "diescreen" || g.gameObject.name == "raw" || g.gameObject.name == "FadeScreen" || g.gameObject.name.Contains("Blood") || g.gameObject.name.Contains("Damage")))
            {
                g.raycastTarget = false;
            }
        }


        // Auto-create Mobile UI Canvas
        GameObject canvasObj = new GameObject("MobileControlsCanvas");
        mobileCanvas = canvasObj.AddComponent<Canvas>();
        mobileCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        mobileCanvas.sortingOrder = 50;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        // TOUCH LOOK ZONE (Created first, sits behind all action buttons and minimap)
        GameObject lookObj = CreateUIElement("TouchLookZone", canvasObj.transform, Vector2.zero, Vector2.zero, new Vector2(0.35f, 0f), new Vector2(1f, 0.70f), new Vector2(0.5f, 0.5f));
        touchLookZone = lookObj.GetComponent<RectTransform>();
        Image lookImg = lookObj.AddComponent<Image>();
        lookImg.color = new Color(0f, 0f, 0f, 0.001f);
        lookObj.transform.SetAsFirstSibling();

        TouchLookZoneHandler lookHandler = lookObj.AddComponent<TouchLookZoneHandler>();
        lookHandler.manager = this;

        // 1. TOP-CENTER COMPASS BAR
        BuildTopCenterCompass(canvasObj.transform);

        // 2. TOP-RIGHT MINIMAP & BUTTONS
        BuildTopRightMinimap(canvasObj.transform);

        // 3. BOTTOM-CENTER HOTBAR
        BuildBottomHotbar(canvasObj.transform);

        // 4. CENTER CROSSHAIR RETICLE
        BuildCrosshair(canvasObj.transform);

        // 5. NOTIFICATION CONTAINER
        BuildNotificationContainer(canvasObj.transform);

        // 6. BOTTOM-LEFT VIRTUAL JOYSTICK (Anchored to Bottom-Left Corner (0,0), Pivot Center (0.5, 0.5))
        GameObject bgObj = CreateUIElement("JoystickBG", canvasObj.transform, new Vector2(180, 180), new Vector2(220, 220), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0.5f, 0.5f));
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.2f, 0.2f, 0.25f, 0.6f);
        Outline bgOutline = bgObj.AddComponent<Outline>();
        bgOutline.effectColor = new Color(1f, 1f, 1f, 0.5f);
        bgOutline.effectDistance = new Vector2(3, -3);
        joystickBackground = bgObj.GetComponent<RectTransform>();

        GameObject handleObj = CreateUIElement("JoystickHandle", bgObj.transform, Vector2.zero, new Vector2(90, 90), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        Image handleImg = handleObj.AddComponent<Image>();
        handleImg.color = new Color(0.2f, 0.8f, 1f, 0.9f);
        Outline handleOutline = handleObj.AddComponent<Outline>();
        handleOutline.effectColor = Color.white;
        handleOutline.effectDistance = new Vector2(2, -2);
        joystickHandle = handleObj.GetComponent<RectTransform>();

        JoystickTouchHandler joystickHandler = bgObj.AddComponent<JoystickTouchHandler>();
        joystickHandler.manager = this;

        // Crouch Button next to joystick (Anchored to Bottom-Left)
        CreateCircularActionButton("CrouchButton", canvasObj.transform, new Vector2(400, 100), new Vector2(90, 90), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0.5f, 0.5f), "CROUCH", new Color(0.2f, 0.2f, 0.25f, 0.8f), OnCrouchPressed);

        // 7. BOTTOM-RIGHT ACTION BUTTONS (Anchored to Bottom-Right Corner (1,0), Pivot Center (0.5, 0.5))
        CreateCircularActionButton("AttackButton", canvasObj.transform, new Vector2(-120, 260), new Vector2(130, 130), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0.5f), "ATTACK", new Color(0.85f, 0.2f, 0.2f, 0.85f), OnAttackPressed);
        CreateCircularActionButton("InteractButton", canvasObj.transform, new Vector2(-270, 280), new Vector2(110, 110), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0.5f), "INTERACT", new Color(0.2f, 0.8f, 0.3f, 0.85f), OnInteractPressed);
        CreateCircularActionButton("SprintButton", canvasObj.transform, new Vector2(-270, 130), new Vector2(110, 110), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0.5f), "SPRINT", new Color(0.9f, 0.6f, 0.1f, 0.85f), OnSprintPressedHold, OnSprintReleased);
        CreateCircularActionButton("JumpButton", canvasObj.transform, new Vector2(-120, 100), new Vector2(110, 110), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0.5f), "JUMP", new Color(0.2f, 0.6f, 0.9f, 0.85f), OnJumpPressed);
    }

    private void BuildTopCenterCompass(Transform parent)
    {
        // Compass Container (Anchored to Top-Center (0.5, 1))
        GameObject compassPanel = CreateUIElement("CompassPanel", parent, new Vector2(0, -30), new Vector2(500, 50), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        Image bg = compassPanel.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.08f, 0.1f, 0.65f);
        Outline outline = compassPanel.AddComponent<Outline>();
        outline.effectColor = new Color(0.4f, 0.4f, 0.45f, 0.5f);

        GameObject textObj = CreateUIElement("CompassText", compassPanel.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
        TextMeshProUGUI compassTxt = textObj.AddComponent<TextMeshProUGUI>();
        compassTxt.fontSize = 22;
        compassTxt.fontStyle = FontStyles.Bold;
        compassTxt.alignment = TextAlignmentOptions.Center;
        compassTxt.color = Color.white;

        CompassUI compassUI = compassPanel.AddComponent<CompassUI>();
        compassUI.compassText = compassTxt;
    }

    private void BuildTopRightMinimap(Transform parent)
    {
        // Minimap Container (Anchored to Top-Right (1, 1))
        GameObject minimapObj = CreateUIElement("MinimapCard", parent, new Vector2(-20, -20), new Vector2(180, 180), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f));
        Image mapBg = minimapObj.AddComponent<Image>();
        mapBg.color = new Color(0.1f, 0.25f, 0.12f, 0.8f);
        Outline mapOutline = minimapObj.AddComponent<Outline>();
        mapOutline.effectColor = new Color(0.8f, 0.8f, 0.9f, 0.6f);

        // Player Marker Arrow
        GameObject arrowObj = CreateUIElement("PlayerMarker", minimapObj.transform, Vector2.zero, new Vector2(24, 24), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        Image arrowImg = arrowObj.AddComponent<Image>();
        arrowImg.color = Color.cyan;

        // Day/Time/Temp Box below Minimap
        GameObject infoObj = CreateUIElement("MinimapInfo", parent, new Vector2(-20, -205), new Vector2(180, 40), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f));
        Image infoBg = infoObj.AddComponent<Image>();
        infoBg.color = new Color(0.08f, 0.08f, 0.1f, 0.7f);

        GameObject timeTxtObj = CreateUIElement("TimeText", infoObj.transform, new Vector2(10, 0), new Vector2(110, 35), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));
        TextMeshProUGUI timeTxt = timeTxtObj.AddComponent<TextMeshProUGUI>();
        timeTxt.fontSize = 14;
        timeTxt.fontStyle = FontStyles.Bold;
        timeTxt.color = Color.white;
        timeTxt.alignment = TextAlignmentOptions.Left;

        GameObject tempTxtObj = CreateUIElement("TempText", infoObj.transform, new Vector2(-10, 0), new Vector2(50, 35), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f));
        TextMeshProUGUI tempTxt = tempTxtObj.AddComponent<TextMeshProUGUI>();
        tempTxt.fontSize = 14;
        tempTxt.fontStyle = FontStyles.Bold;
        tempTxt.color = new Color(1f, 0.85f, 0.4f);
        tempTxt.alignment = TextAlignmentOptions.Right;

        MinimapUI miniUI = minimapObj.AddComponent<MinimapUI>();
        miniUI.playerArrow = arrowObj.GetComponent<RectTransform>();
        miniUI.dayTimeText = timeTxt;
        miniUI.tempText = tempTxt;

        // Inventory & Map Buttons
        GameObject bagBtn = CreateCircularActionButton("InventoryBtn", parent, new Vector2(-115, -260), new Vector2(85, 85), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), "BAG", new Color(0.45f, 0.35f, 0.65f, 0.95f), OnInventoryPressed);
        GameObject mapBtn = CreateCircularActionButton("MapBtn", parent, new Vector2(-25, -260), new Vector2(85, 85), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), "MAP", new Color(0.3f, 0.55f, 0.45f, 0.95f), OnMapPressed);
        bagBtn.transform.SetAsLastSibling();
        mapBtn.transform.SetAsLastSibling();
    }

    private void BuildBottomHotbar(Transform parent)
    {
        // Hotbar Container (Anchored to Bottom-Center (0.5, 0))
        GameObject hotbarCard = CreateUIElement("HotbarCard", parent, new Vector2(0, 30), new Vector2(720, 80), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
        Image hBg = hotbarCard.AddComponent<Image>();
        hBg.color = new Color(0.08f, 0.08f, 0.1f, 0.8f);
        Outline hOutline = hotbarCard.AddComponent<Outline>();
        hOutline.effectColor = new Color(0.4f, 0.4f, 0.45f, 0.5f);

        hotbarIcons.Clear();
        hotbarQuantities.Clear();
        hotbarOutlines.Clear();

        for (int i = 0; i < 8; i++)
        {
            float xPos = -315f + (i * 90f);
            int slotIndex = i;

            GameObject slotObj = CreateUIElement("HotbarSlot_" + i, hotbarCard.transform, new Vector2(xPos, 0), new Vector2(72, 72), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            Image slotBg = slotObj.AddComponent<Image>();
            slotBg.color = new Color(0.15f, 0.15f, 0.18f, 0.9f);

            Outline sOutline = slotObj.AddComponent<Outline>();
            sOutline.effectColor = new Color(1f, 0.85f, 0.2f);
            sOutline.enabled = false;
            hotbarOutlines.Add(sOutline);

            Button slotBtn = slotObj.AddComponent<Button>();
            slotBtn.onClick.AddListener(() => {
                if (Inventory.instance != null) Inventory.instance.QuickEquipSlot(slotIndex);
            });

            GameObject iconObj = CreateUIElement("Icon", slotObj.transform, Vector2.zero, new Vector2(56, 56), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            Image iconImg = iconObj.AddComponent<Image>();
            iconImg.raycastTarget = false;
            iconImg.gameObject.SetActive(false);
            hotbarIcons.Add(iconImg);

            GameObject qtyObj = CreateUIElement("QtyText", slotObj.transform, new Vector2(18, -18), new Vector2(28, 24), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            TextMeshProUGUI qtyTxt = qtyObj.AddComponent<TextMeshProUGUI>();
            qtyTxt.raycastTarget = false;
            qtyTxt.fontSize = 16;
            qtyTxt.fontStyle = FontStyles.Bold;
            qtyTxt.color = Color.white;
            qtyTxt.alignment = TextAlignmentOptions.Right;
            hotbarQuantities.Add(qtyTxt);
        }
    }

    private void BuildCrosshair(Transform parent)
    {
        GameObject crosshairObj = CreateUIElement("CrosshairDot", parent, Vector2.zero, new Vector2(10, 10), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        Image img = crosshairObj.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.6f);
        img.raycastTarget = false;
    }

    private void BuildNotificationContainer(Transform parent)
    {
        GameObject notifObj = CreateUIElement("NotificationContainer", parent, new Vector2(0, 50), new Vector2(600, 200), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        if (NotificationUI.instance != null) NotificationUI.instance.notificationContainer = notifObj.transform;
    }

    private GameObject CreateUIElement(string name, Transform parent, Vector2 anchoredPos, Vector2 size, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
        return go;
    }

    private GameObject CreateCircularActionButton(string name, Transform parent, Vector2 pos, Vector2 size, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, string text, Color color, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = CreateUIElement(name, parent, pos, size, anchorMin, anchorMax, pivot);
        Image img = btnObj.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = true;
        Outline outline = btnObj.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 1f, 1f, 0.4f);

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);

        EventTrigger trigger = btnObj.AddComponent<EventTrigger>();
        EventTrigger.Entry downEntry = new EventTrigger.Entry();
        downEntry.eventID = EventTriggerType.PointerDown;
        downEntry.callback.AddListener((data) => { onClick?.Invoke(); });
        trigger.triggers.Add(downEntry);

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI t = textObj.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = 17;
        t.fontStyle = FontStyles.Bold;
        t.alignment = TextAlignmentOptions.Center;
        t.color = Color.white;
        t.raycastTarget = false; // Disable raycast so parent button receives pointer events
        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.sizeDelta = Vector2.zero;

        btnObj.transform.SetAsLastSibling();
        return btnObj;
    }

    private GameObject CreateCircularActionButton(string name, Transform parent, Vector2 pos, Vector2 size, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, string text, Color color, UnityEngine.Events.UnityAction onDown, UnityEngine.Events.UnityAction onUp)
    {
        GameObject btnObj = CreateUIElement(name, parent, pos, size, anchorMin, anchorMax, pivot);
        Image img = btnObj.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = true;
        Outline outline = btnObj.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 1f, 1f, 0.4f);

        EventTrigger trigger = btnObj.AddComponent<EventTrigger>();

        EventTrigger.Entry entryDown = new EventTrigger.Entry();
        entryDown.eventID = EventTriggerType.PointerDown;
        entryDown.callback.AddListener((data) => { onDown(); });
        trigger.triggers.Add(entryDown);

        EventTrigger.Entry entryUp = new EventTrigger.Entry();
        entryUp.eventID = EventTriggerType.PointerUp;
        entryUp.callback.AddListener((data) => { onUp(); });
        trigger.triggers.Add(entryUp);

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI t = textObj.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = 17;
        t.fontStyle = FontStyles.Bold;
        t.alignment = TextAlignmentOptions.Center;
        t.color = Color.white;
        t.raycastTarget = false; // Disable raycast so parent event trigger receives pointer events
        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.sizeDelta = Vector2.zero;

        btnObj.transform.SetAsLastSibling();
        return btnObj;
    }

    private void Update()
    {
        if (Inventory.instance != null && Inventory.instance.isOpen())
        {
            return;
        }

        if (PlayerController.instance != null && moveInputVector.magnitude > 0.05f)
        {
            PlayerController.instance.SetMobileMoveInput(moveInputVector);
        }

        if (PlayerController.instance != null && touchLookDelta.magnitude > 0.001f)
        {
            PlayerController.instance.SetMobileLookInput(touchLookDelta * touchLookSensitivity);
            touchLookDelta = Vector2.zero;
        }

        UpdateHotbarUI();
    }

    private void UpdateHotbarUI()
    {
        if (Inventory.instance == null || Inventory.instance.uiSlots == null) return;

        for (int i = 0; i < 8 && i < hotbarIcons.Count; i++)
        {
            if (i < Inventory.instance.uiSlots.Length)
            {
                ItemSlotUI slotUI = Inventory.instance.uiSlots[i];
                if (slotUI != null && slotUI.icon != null && slotUI.icon.gameObject.activeSelf && slotUI.icon.sprite != null)
                {
                    hotbarIcons[i].gameObject.SetActive(true);
                    hotbarIcons[i].sprite = slotUI.icon.sprite;
                    hotbarQuantities[i].text = slotUI.quantityText != null ? slotUI.quantityText.text : "";
                    hotbarOutlines[i].enabled = slotUI.equipped;
                }
                else
                {
                    hotbarIcons[i].gameObject.SetActive(false);
                    hotbarQuantities[i].text = "";
                    hotbarOutlines[i].enabled = false;
                }
            }
        }
    }

    public void UpdateJoystick(PointerEventData eventData)
    {
        Vector2 position = RectTransformUtility.WorldToScreenPoint(eventData.pressEventCamera, joystickBackground.position);
        Vector2 radius = joystickBackground.sizeDelta / 2f;
        moveInputVector = (eventData.position - position) / (radius.magnitude);

        if (moveInputVector.magnitude > 1f)
            moveInputVector.Normalize();

        joystickHandle.anchoredPosition = moveInputVector * joystickRange;
    }

    public void ResetJoystick()
    {
        moveInputVector = Vector2.zero;
        joystickHandle.anchoredPosition = Vector2.zero;
        if (PlayerController.instance != null)
        {
            PlayerController.instance.SetMobileMoveInput(Vector2.zero);
        }
    }

    public void OnLookZoneDown(PointerEventData eventData)
    {
        lookTouchId = eventData.pointerId;
        lastLookTouchPos = eventData.position;
    }

    public void OnLookZoneDrag(PointerEventData eventData)
    {
        if (eventData.pointerId == lookTouchId)
        {
            Vector2 delta = eventData.position - lastLookTouchPos;
            touchLookDelta = delta;
            lastLookTouchPos = eventData.position;
        }
    }

    public void OnLookZoneUp(PointerEventData eventData)
    {
        if (eventData.pointerId == lookTouchId)
        {
            lookTouchId = -1;
            touchLookDelta = Vector2.zero;
        }
    }

    private void OnJumpPressed()
    {
        if (SurvivalAudioPlayer.instance != null) SurvivalAudioPlayer.instance.PlayUIClick();
        if (PlayerController.instance != null) PlayerController.instance.TriggerJump();
    }

    private void OnAttackPressed()
    {
        if (SurvivalAudioPlayer.instance != null) SurvivalAudioPlayer.instance.PlayUIClick();
        var equipManager = EquipManager.instance ?? Object.FindAnyObjectByType<EquipManager>();
        if (equipManager != null)
        {
            if (equipManager.currentEquip != null)
            {
                equipManager.currentEquip.OnAttackInput();
            }
            else
            {
                equipManager.UnarmedAttack();
            }
        }
    }

    private void OnInteractPressed()
    {
        if (SurvivalAudioPlayer.instance != null) SurvivalAudioPlayer.instance.PlayUIClick();
        var interactManager = Object.FindAnyObjectByType<InteractionManager>();
        if (interactManager != null)
        {
            interactManager.TriggerInteractFromMobile();
        }
    }

    private void OnSprintPressedHold()
    {
        if (SurvivalAudioPlayer.instance != null) SurvivalAudioPlayer.instance.PlayUIClick();
        var player = PlayerController.instance ?? Object.FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            player.SetSprint(true);
        }
    }

    private void OnSprintReleased()
    {
        var player = PlayerController.instance ?? Object.FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            player.SetSprint(false);
        }
    }

    private void OnCrouchPressed()
    {
        if (SurvivalAudioPlayer.instance != null) SurvivalAudioPlayer.instance.PlayUIClick();
        var player = PlayerController.instance ?? Object.FindAnyObjectByType<PlayerController>();
        if (player != null) player.ToggleCrouch();
    }

    private void OnInventoryPressed()
    {
        if (SurvivalAudioPlayer.instance != null) SurvivalAudioPlayer.instance.PlayUIClick();
        var inv = Inventory.instance ?? Object.FindAnyObjectByType<Inventory>();
        if (inv != null)
        {
            inv.Toggle();
        }
        else
        {
            var canvases = UnityEngine.Resources.FindObjectsOfTypeAll<Canvas>();
            foreach (var c in canvases)
            {
                if (c != null && (c.gameObject.name == "Inventory Canvas" || c.gameObject.name.Contains("Inventory")))
                {
                    bool active = !c.gameObject.activeSelf;
                    c.gameObject.SetActive(active);
                    c.overrideSorting = true;
                    c.sortingOrder = 500;
                    if (PlayerController.instance != null) PlayerController.instance.ToggleCursor(active);
                    Cursor.visible = active;
                    Cursor.lockState = active ? CursorLockMode.None : CursorLockMode.Locked;
                    break;
                }
            }
        }
    }

    private void OnMapPressed()
    {
        if (SurvivalAudioPlayer.instance != null) SurvivalAudioPlayer.instance.PlayUIClick();
        if (MapWindowUI.instance != null) MapWindowUI.instance.ToggleMap();
    }
}

public class JoystickTouchHandler : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public MobileInputManager manager;
    public void OnPointerDown(PointerEventData eventData) { manager.UpdateJoystick(eventData); }
    public void OnDrag(PointerEventData eventData) { manager.UpdateJoystick(eventData); }
    public void OnPointerUp(PointerEventData eventData) { manager.ResetJoystick(); }
}

public class TouchLookZoneHandler : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public MobileInputManager manager;
    public void OnPointerDown(PointerEventData eventData) { manager.OnLookZoneDown(eventData); }
    public void OnDrag(PointerEventData eventData) { manager.OnLookZoneDrag(eventData); }
    public void OnPointerUp(PointerEventData eventData) { manager.OnLookZoneUp(eventData); }
}
