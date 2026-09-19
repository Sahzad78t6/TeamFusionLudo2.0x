using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DayNight : MonoBehaviour
{
    //23:59 , 12:00
    [Range(0.0f,1.0f)]
    public float time ;
    public float fulldayLength;
    public float startTime = 0.5f;
    private float timeRate;
    public Vector3 noon;

    [Header("Sun Setting")] 
    public Light sun;
    public Gradient sunColor;
    public AnimationCurve sunIntensity;
    
    [Header("Moon Setting")] 
    public Light moon;
    public Gradient moonColor;
    public AnimationCurve moonIntensity;

    [Header("Lightning Setting")] 
    public AnimationCurve lightningIntensityMultiplier;
    public AnimationCurve reflectionIntensityMultiplier;

    public static DayNight instance;

    void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        timeRate = 1.0f / fulldayLength;
        
        time = startTime;

    }

    private void Update()
    {
        //zaman akışı
        time += timeRate * Time.deltaTime;
        if (time >= 1.0f)
        {
            time = 0.0f;
        }
        //ışık rotasyonu
        sun.transform.eulerAngles = (time ) * noon * 4.0f;
        moon.transform.eulerAngles = (time -0.5f) * noon * 4.0f;
        
        //light intensity
        sun.intensity = sunIntensity.Evaluate(time);
        moon.intensity = moonIntensity.Evaluate(time);
        
        //change colors
        sun.color = sunColor.Evaluate(time);
        moon.color = moonColor.Evaluate(time);
        
        //enable and disable the sun
        if (sun.intensity == 0 && sun.gameObject.activeInHierarchy)
        {
            sun.gameObject.SetActive(false);
        }
        else if (sun.intensity > 0 && !sun.gameObject.activeInHierarchy)
        {
            sun.gameObject.SetActive(true);
        }
        
        //enable and disable the moon
        if (moon.intensity == 0 && moon.gameObject.activeInHierarchy)
        {
            moon.gameObject.SetActive(false);
        }
        else if (moon.intensity > 0 && !moon.gameObject.activeInHierarchy)
        {
            moon.gameObject.SetActive(true);
        }
        
        //lightning and reflection intensity
        RenderSettings.ambientIntensity = lightningIntensityMultiplier.Evaluate(time);
        RenderSettings.reflectionIntensity = reflectionIntensityMultiplier.Evaluate(time);

        // Atmospheric Fog & Ambient Polish
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        if (sun != null && sun.gameObject.activeInHierarchy)
        {
            RenderSettings.fogColor = Color.Lerp(new Color(0.75f, 0.65f, 0.5f), new Color(0.6f, 0.75f, 0.9f), sun.intensity / 2f);
            RenderSettings.fogDensity = Mathf.Lerp(0.008f, 0.003f, sun.intensity / 2f);
        }
        else
        {
            RenderSettings.fogColor = new Color(0.05f, 0.08f, 0.15f);
            RenderSettings.fogDensity = 0.012f;
        }
    }
}
