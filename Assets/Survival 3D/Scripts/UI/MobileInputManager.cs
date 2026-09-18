using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MobileInputManager : MonoBehaviour
{
    public static MobileInputManager instance;

    [Header("UI Canvas & Controls")]
    public Canvas mobileCanvas;
    public RectTransform joystickBackground;
    public RectTransform joystickHandle;
    public RectTransform touchLookZone;

    [Header("Joystick Settings")]
    public float joystickRange = 100f;
    private Vector2 moveInputVector;

    [Header("Touch Look Settings")]
    public float touchLookSensitivity = 0.5f;
    private Vector2 touchLookDelta;
    private int lookTouchId = -1;
    private Vector2 lastLookTouchPos;

    private bool isJoystickActive = false;

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

        // Auto-create Mobile UI Canvas if not assigned in Inspector
        GameObject canvasObj = new GameObject("MobileControlsCanvas");
        mobileCanvas = canvasObj.AddComponent<Canvas>();
        mobileCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        mobileCanvas.sortingOrder = 50;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        // Create Joystick Background
        GameObject bgObj = CreateUIElement("JoystickBG", canvasObj.transform, new Vector2(160, 160), new Vector2(220, 220));
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(1f, 1f, 1f, 0.25f);
        joystickBackground = bgObj.GetComponent<RectTransform>();
        SetAnchor(joystickBackground, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(220, 220));

        // Create Joystick Handle
        GameObject handleObj = CreateUIElement("JoystickHandle", bgObj.transform, new Vector2(0, 0), new Vector2(80, 80));
        Image handleImg = handleObj.AddComponent<Image>();
        handleImg.color = new Color(0.2f, 0.8f, 1f, 0.6f);
        joystickHandle = handleObj.GetComponent<RectTransform>();

        // Add Joystick Touch Handler
        JoystickTouchHandler joystickHandler = bgObj.AddComponent<JoystickTouchHandler>();
        joystickHandler.manager = this;

        // Create Touch Look Zone (Right half of screen)
        GameObject lookObj = CreateUIElement("TouchLookZone", canvasObj.transform, Vector2.zero, Vector2.zero);
        touchLookZone = lookObj.GetComponent<RectTransform>();
        SetAnchor(touchLookZone, new Vector2(0.35f, 0f), new Vector2(1f, 0.85f), Vector2.zero);
        Image lookImg = lookObj.AddComponent<Image>();
        lookImg.color = new Color(0f, 0f, 0f, 0.001f); // Invisible touch zone

        TouchLookZoneHandler lookHandler = lookObj.AddComponent<TouchLookZoneHandler>();
        lookHandler.manager = this;

        // Create Action Buttons Container (Right Side)
        CreateActionButton("JumpButton", canvasObj.transform, new Vector2(1700, 180), new Vector2(110, 110), "JUMP", Color.cyan, OnJumpPressed);
        CreateActionButton("AttackButton", canvasObj.transform, new Vector2(1720, 360), new Vector2(130, 130), "ATTACK", new Color(1f, 0.3f, 0.3f), OnAttackPressed);
        CreateActionButton("AltActionButton", canvasObj.transform, new Vector2(1560, 220), new Vector2(110, 110), "ALT/BUILD", new Color(1f, 0.8f, 0.2f), OnAltActionPressed);
        CreateActionButton("InteractButton", canvasObj.transform, new Vector2(1540, 400), new Vector2(100, 100), "INTERACT [E]", new Color(0.3f, 1f, 0.4f), OnInteractPressed);
        CreateActionButton("InventoryButton", canvasObj.transform, new Vector2(1780, 960), new Vector2(100, 100), "INV", new Color(0.8f, 0.5f, 1f), OnInventoryPressed);
        CreateActionButton("RotateButton", canvasObj.transform, new Vector2(1400, 220), new Vector2(90, 90), "ROTATE [R]", new Color(1f, 0.5f, 0.2f), OnRotatePressedHold, OnRotateReleased);
    }

    private GameObject CreateUIElement(string name, Transform parent, Vector2 anchoredPos, Vector2 size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
        return go;
    }

    private void SetAnchor(RectTransform rt, Vector2 min, Vector2 max, Vector2 position)
    {
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.anchoredPosition = position;
    }

    private void CreateActionButton(string name, Transform parent, Vector2 pos, Vector2 size, string text, Color color, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = CreateUIElement(name, parent, pos, size);
        Image img = btnObj.AddComponent<Image>();
        img.color = new Color(color.r, color.g, color.b, 0.4f);

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.pressedColor = color;
        btn.colors = colors;
        btn.onClick.AddListener(onClick);

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        Text t = textObj.AddComponent<Text>();
        t.text = text;
        t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        t.fontSize = 20;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;
        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector1.one;
        textRt.sizeDelta = Vector2.zero;
    }

    private void CreateActionButton(string name, Transform parent, Vector2 pos, Vector2 size, string text, Color color, UnityEngine.Events.UnityAction onDown, UnityEngine.Events.UnityAction onUp)
    {
        GameObject btnObj = CreateUIElement(name, parent, pos, size);
        Image img = btnObj.AddComponent<Image>();
        img.color = new Color(color.r, color.g, color.b, 0.4f);

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
        Text t = textObj.AddComponent<Text>();
        t.text = text;
        t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        t.fontSize = 18;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;
        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector1.one;
        textRt.sizeDelta = Vector2.zero;
    }

    private void Update()
    {
        // Pass movement input to PlayerController
        if (PlayerController.instance != null && moveInputVector.magnitude > 0.05f)
        {
            PlayerController.instance.SetMobileMoveInput(moveInputVector);
        }

        // Pass touch look delta to PlayerController
        if (PlayerController.instance != null && touchLookDelta.magnitude > 0.001f)
        {
            PlayerController.instance.SetMobileLookInput(touchLookDelta * touchLookSensitivity);
            touchLookDelta = Vector2.zero;
        }
    }

    // Joystick callbacks
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

    // Touch look callbacks
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

    // Action button events
    private void OnJumpPressed()
    {
        if (PlayerController.instance != null)
            PlayerController.instance.TriggerJump();
    }

    private void OnAttackPressed()
    {
        if (EquipManager.instance != null && EquipManager.instance.currentEquip != null)
        {
            EquipManager.instance.currentEquip.OnAttackInput();
        }
    }

    private void OnAltActionPressed()
    {
        if (EquipManager.instance != null && EquipManager.instance.currentEquip != null)
        {
            EquipManager.instance.currentEquip.OnAltAttackInput();
        }
    }

    private void OnInteractPressed()
    {
        if (PlayerController.instance != null)
        {
            // Simulate interact
            var interactManager = FindObjectOfType<InteractionManager>();
            if (interactManager != null)
            {
                var inputContext = new UnityEngine.InputSystem.InputAction.CallbackContext();
                // Call interact directly via interface if present
                var prop = interactManager.GetType().GetField("currentInteractable", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (prop != null)
                {
                    var interactable = prop.GetValue(interactManager) as IInteractable;
                    if (interactable != null)
                    {
                        interactable.OnInteract();
                    }
                }
            }
        }
    }

    private void OnInventoryPressed()
    {
        if (Inventory.instance != null)
        {
            Inventory.instance.Toggle();
        }
    }

    private void OnRotatePressedHold()
    {
        if (EquipBuildingKit.instance != null)
            EquipBuildingKit.instance.isRotatingMobile = true;
    }

    private void OnRotateReleased()
    {
        if (EquipBuildingKit.instance != null)
            EquipBuildingKit.instance.isRotatingMobile = false;
    }
}

public class JoystickTouchHandler : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public MobileInputManager manager;

    public void OnPointerDown(PointerEventData eventData)
    {
        manager.UpdateJoystick(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        manager.UpdateJoystick(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        manager.ResetJoystick();
    }
}

public class TouchLookZoneHandler : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public MobileInputManager manager;

    public void OnPointerDown(PointerEventData eventData)
    {
        manager.OnLookZoneDown(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        manager.OnLookZoneDrag(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        manager.OnLookZoneUp(eventData);
    }
}
