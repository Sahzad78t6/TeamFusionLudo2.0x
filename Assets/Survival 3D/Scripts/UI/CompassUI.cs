using UnityEngine;
using TMPro;

public class CompassUI : MonoBehaviour
{
    public TextMeshProUGUI compassText;
    private Transform playerCamera;

    private void Start()
    {
        if (Camera.main != null)
        {
            playerCamera = Camera.main.transform;
        }
    }

    private void Update()
    {
        if (playerCamera == null && Camera.main != null)
        {
            playerCamera = Camera.main.transform;
        }

        if (playerCamera != null && compassText != null)
        {
            float headingAngle = playerCamera.eulerAngles.y;
            headingAngle = (headingAngle + 360f) % 360f;

            string direction = GetDirectionString(headingAngle);
            compassText.text = direction;
        }
    }

    private string GetDirectionString(float angle)
    {
        // 8-point compass strip centered around current heading
        string[] dirs = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
        int mainIndex = Mathf.RoundToInt(angle / 45f) % 8;
        int prevIndex = (mainIndex + 7) % 8;
        int nextIndex = (mainIndex + 1) % 8;

        return string.Format("{0}    <color=#FFD700><b>{1}</b></color>    {2}", dirs[prevIndex], dirs[mainIndex], dirs[nextIndex]);
    }
}
