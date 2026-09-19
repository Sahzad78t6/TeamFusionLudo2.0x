using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NotificationUI : MonoBehaviour
{
    public static NotificationUI instance;
    public Transform notificationContainer;
    private List<GameObject> activeNotifications = new List<GameObject>();

    private void Awake()
    {
        if (instance == null) instance = this;
    }

    public void ShowNotification(string message, Color color)
    {
        Transform container = notificationContainer != null ? notificationContainer : transform;
        if (container == null) return;

        GameObject notifObj = new GameObject("NotificationItem");
        notifObj.transform.SetParent(container, false);

        TextMeshProUGUI txt = notifObj.AddComponent<TextMeshProUGUI>();
        txt.text = message;
        txt.fontSize = 22;
        txt.fontStyle = FontStyles.Bold;
        txt.color = color;
        txt.alignment = TextAlignmentOptions.Center;

        Outline outline = notifObj.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        activeNotifications.Add(notifObj);
        StartCoroutine(FadeOutAndDestroy(notifObj, txt));
    }

    private IEnumerator FadeOutAndDestroy(GameObject obj, TextMeshProUGUI txt)
    {
        float duration = 2.0f;
        float elapsed = 0f;
        Vector3 startPos = obj.transform.localPosition;
        Vector3 endPos = startPos + new Vector3(0, 40f, 0);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            if (txt != null)
            {
                txt.color = new Color(txt.color.r, txt.color.g, txt.color.b, alpha);
            }
            if (obj != null)
            {
                obj.transform.localPosition = Vector3.Lerp(startPos, endPos, elapsed / duration);
            }
            yield return null;
        }

        if (obj != null)
        {
            activeNotifications.Remove(obj);
            Destroy(obj);
        }
    }
}
