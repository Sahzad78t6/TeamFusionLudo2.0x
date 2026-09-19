using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MapWindowUI : MonoBehaviour
{
    public static MapWindowUI instance;
    public GameObject mapPanel;
    public RectTransform playerMarker;

    private void Awake()
    {
        if (instance == null) instance = this;
    }

    private void Start()
    {
        if (mapPanel != null) mapPanel.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            ToggleMap();
        }

        if (mapPanel != null && mapPanel.activeSelf && playerMarker != null && Camera.main != null)
        {
            float rotY = Camera.main.transform.eulerAngles.y;
            playerMarker.localEulerAngles = new Vector3(0, 0, -rotY);
        }
    }

    public void ToggleMap()
    {
        if (mapPanel == null) return;
        bool isActive = !mapPanel.activeSelf;
        mapPanel.SetActive(isActive);
        if (PlayerController.instance != null)
        {
            PlayerController.instance.ToggleCursor(isActive);
        }
    }
}
