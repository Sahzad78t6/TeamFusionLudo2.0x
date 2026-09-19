using System;
using System.Collections;
using System.Collections.Generic;
using PolyverseSkiesAsset;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Bed : Buildings, IInteractable
{

    public float wakupTime;
    public float startSleepTime;
    public float endSleepTime;
    public float sleepToGive;
    public RawImage FadeScreen2;

    public string GetInteractPrompt()
    {
        return CanSleep() ? "Sleep" : "It's too early to sleep";
    }
    
    private void Start()
    {
        var images = UnityEngine.Resources.FindObjectsOfTypeAll<RawImage>();
        if (images.Length > 0) FadeScreen2 = images[0];
    }

   

    public void OnInteract()    
    {
        if (CanSleep())
        {
            if (FadeScreen2 != null && FadeScreen2.GetComponent<Animation>() != null)
            {
                FadeScreen2.GetComponent<Animation>().Play("sleep_anim");
            }

            if (PolyverseSkies.instance != null)
            {
                PolyverseSkies.instance.gameObject.SetActive(false);
                PolyverseSkies.instance.gameObject.SetActive(true);
                PolyverseSkies.instance.timeOfDay = 0.0f;
            }
            
            if (DayNight.instance != null)
            {
                DayNight.instance.time = wakupTime;
            }
           
            if (PlayerNeeds.instance != null)
            {
                PlayerNeeds.instance.Sleep(sleepToGive);
            }
        }
    }

    bool CanSleep()
    {
        if (DayNight.instance == null) return true;
        return DayNight.instance.time >= startSleepTime || DayNight.instance.time < endSleepTime;
    }
    
}
    