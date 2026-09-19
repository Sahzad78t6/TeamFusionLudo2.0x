using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;



public class PlayerNeeds : MonoBehaviour,IDamagable

{
    public Image FadeScreen4;
    
    public Need health;
    public Need hunger;
    public Need thirst;
    public Need sleep; // Used for Stamina in HUD
    public Need stamina; // Alias/Dedicated Stamina

    public float staminaDrainRate = 15f;
    public float staminaRegenRate = 10f;
    public float hungerHealthdecay = 1f;
    public float thirstHealthdecay = 1.5f;

    public UnityEvent onTakeDamage;

    public static PlayerNeeds instance;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        if (health.maxValue <= 0) health.maxValue = 100f;
        if (hunger.maxValue <= 0) hunger.maxValue = 100f;
        if (thirst.maxValue <= 0) thirst.maxValue = 100f;
        if (sleep.maxValue <= 0) sleep.maxValue = 100f;

        if (health.startValue <= 0) health.startValue = 100f;
        if (hunger.startValue <= 0) hunger.startValue = 100f;
        if (thirst.startValue <= 0) thirst.startValue = 100f;
        if (sleep.startValue <= 0) sleep.startValue = 100f;

        health.currentValue = health.startValue;
        hunger.currentValue = hunger.startValue;
        thirst.currentValue = thirst.startValue;
        sleep.currentValue = sleep.startValue;
        stamina = sleep;
    }

    void Update()
    {
        hunger.Subtrack(hunger.decayRate * Time.deltaTime);
        thirst.Subtrack(thirst.decayRate * Time.deltaTime);

        // Stamina logic: if sprinting, drain stamina; else regen
        bool sprinting = PlayerController.instance != null && PlayerController.instance.isSprinting;
        if (sprinting && stamina.currentValue > 0)
        {
            stamina.Subtrack(staminaDrainRate * Time.deltaTime);
            if (stamina.currentValue <= 0 && PlayerController.instance != null)
            {
                PlayerController.instance.isSprinting = false;
            }
        }
        else
        {
            stamina.Add(staminaRegenRate * Time.deltaTime);
        }

        if (hunger.currentValue == 0.0f)
        {
            health.Subtrack(hungerHealthdecay * Time.deltaTime);
        }
        
        if (thirst.currentValue == 0.0f)
        {
            health.Subtrack(thirstHealthdecay * Time.deltaTime);
        }
        
        if (health.currentValue == 0.0f)
        {
            Die();
        }
        
        // UI bars update
        if (health.uiBar != null) health.uiBar.fillAmount = Mathf.Lerp(health.uiBar.fillAmount, health.GetPercentage(), Time.deltaTime * 10f);
        if (hunger.uiBar != null) hunger.uiBar.fillAmount = Mathf.Lerp(hunger.uiBar.fillAmount, hunger.GetPercentage(), Time.deltaTime * 10f);
        if (thirst.uiBar != null) thirst.uiBar.fillAmount = Mathf.Lerp(thirst.uiBar.fillAmount, thirst.GetPercentage(), Time.deltaTime * 10f);
        if (sleep.uiBar != null) sleep.uiBar.fillAmount = Mathf.Lerp(sleep.uiBar.fillAmount, sleep.GetPercentage(), Time.deltaTime * 10f);

        if (health.uiValueText != null) health.uiValueText.text = Mathf.CeilToInt(health.currentValue).ToString();
        if (hunger.uiValueText != null) hunger.uiValueText.text = Mathf.CeilToInt(hunger.currentValue).ToString();
        if (thirst.uiValueText != null) thirst.uiValueText.text = Mathf.CeilToInt(thirst.currentValue).ToString();
        if (sleep.uiValueText != null) sleep.uiValueText.text = Mathf.CeilToInt(sleep.currentValue).ToString();
    }

    public void Heal(float amount) { health.Add(amount); }
    public void Eat(float amount) { hunger.Add(amount); }
    public void Drink(float amount) { thirst.Add(amount); }
    public void Sleep(float amount) { sleep.Subtrack(amount); }

    public void TakePhysicDamage(int amount)
    {
        health.Subtrack(amount);
        onTakeDamage?.Invoke();
    }
    
    public void Die()
    {
        if (FadeScreen4 != null && FadeScreen4.GetComponent<Animation>() != null)
        {
            FadeScreen4.GetComponent<Animation>().Play("you_died");
        }
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        SceneManager.LoadScene("Menu");
    }
}

[System.Serializable]
public class Need
{   
    [HideInInspector]
    public float currentValue;
    public float maxValue = 100f;
    public float startValue = 100f;
    public float regenrate = 1f;
    public float decayRate = 0.5f;
    public Image uiBar;
    public TMPro.TextMeshProUGUI uiValueText;

    public void Add(float amount)
    {
        currentValue = Mathf.Min(currentValue + amount, maxValue);
    }

    public void Subtrack(float amount)
    {
        currentValue = Mathf.Max(currentValue - amount, 0);
    }

    public float GetPercentage()
    {
        return maxValue > 0 ? currentValue / maxValue : 0;
    }
}

public interface IDamagable
{
    void TakePhysicDamage(int damageAmount);
}