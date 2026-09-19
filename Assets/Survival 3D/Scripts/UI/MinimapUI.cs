using UnityEngine;
using TMPro;

public class MinimapUI : MonoBehaviour
{
    public RectTransform playerArrow;
    public TextMeshProUGUI dayTimeText;
    public TextMeshProUGUI tempText;

    private Transform playerTransform;
    private Camera mainCam;

    private void Start()
    {
        if (PlayerController.instance != null)
        {
            playerTransform = PlayerController.instance.transform;
        }
        mainCam = Camera.main;
    }

    private void Update()
    {
        if (playerTransform == null && PlayerController.instance != null)
        {
            playerTransform = PlayerController.instance.transform;
        }
        if (mainCam == null)
        {
            mainCam = Camera.main;
        }

        // Update player direction arrow in minimap
        if (playerArrow != null && mainCam != null)
        {
            playerArrow.localEulerAngles = new Vector3(0, 0, -mainCam.transform.eulerAngles.y);
        }

        // Update day / time readout from DayNight system
        if (dayTimeText != null)
        {
            float dayProgress = DayNight.instance != null ? DayNight.instance.time : 0.25f;
            int totalMinutes = Mathf.FloorToInt(dayProgress * 24f * 60f);
            int hours = (totalMinutes / 60) % 24;
            int minutes = totalMinutes % 60;
            string ampm = hours >= 12 ? "PM" : "AM";
            int displayHours = hours % 12;
            if (displayHours == 0) displayHours = 12;

            dayTimeText.text = string.Format("Day 1   {0:D2}:{1:D2} {2}", displayHours, minutes, ampm);
        }

        if (tempText != null)
        {
            float dayProgress = DayNight.instance != null ? DayNight.instance.time : 0.5f;
            float temp = Mathf.Lerp(12f, 24f, Mathf.Sin(dayProgress * Mathf.PI));
            tempText.text = string.Format("{0:F0}°C", temp);
        }
    }
}
