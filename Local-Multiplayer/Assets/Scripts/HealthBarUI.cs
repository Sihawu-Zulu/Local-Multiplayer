using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class HealthBarUI : MonoBehaviour
{

    [System.Serializable]
    public class PlayerHUD
    {
        public Slider healthSlider;  
        public Image  fillImage;     
        public Image avatarImage;     
        public TextMeshProUGUI nameText;          
        public TextMeshProUGUI currentHealthText;   
        public Image[] roundPips;           
    }

    [Header("Player HUDs")]
    [SerializeField] private PlayerHUD p1HUD;
    [SerializeField] private PlayerHUD p2HUD;

    [Header("Health Colours")]
    [SerializeField] private Color healthHigh = new Color(0.9f, 0.75f, 0.1f); 
    [SerializeField] private Color healthMid  = new Color(0.95f, 0.5f, 0.05f); 
    [SerializeField] private Color healthLow  = new Color(0.85f, 0.1f, 0.1f);  

    [Header("Round Pip Colours")]
    [SerializeField] private Color pipWon   = new Color(1f, 0.8f, 0f); 
    [SerializeField] private Color pipEmpty = new Color(0.25f, 0.25f, 0.25f); 

    [Header("Round Counter")]
    [SerializeField] private TextMeshProUGUI roundCounterText;

    [Header("Damage Flash")]
    [SerializeField] private float flashDuration = 0.12f;
    [SerializeField] private Color flashColor    = Color.white;

    // -------------------------------------------------------
    // called by   once both players have spawned
    // -------------------------------------------------------

    public void InitialiseHUD(PlayerHealth p1Health, PlayerHealth p2Health,
                               Sprite p1Avatar, Sprite p2Avatar,
                               string p1Name,  string p2Name)
    {
        SetupHUDDisplay(p1HUD, p1Health, p1Avatar, p1Name);
        SetupHUDDisplay(p2HUD, p2Health, p2Avatar, p2Name);


        ResetPips(p1HUD);
        ResetPips(p2HUD);


        p1Health.OnHealthChanged.AddListener((cur, max) => OnHealthChanged(p1HUD, cur, max));
        p2Health.OnHealthChanged.AddListener((cur, max) => OnHealthChanged(p2HUD, cur, max));
    }

    private void SetupHUDDisplay(PlayerHUD hud, PlayerHealth health, Sprite avatar, string charName)
    {
        if (hud.avatarImage      != null) hud.avatarImage.sprite = avatar;
        if (hud.nameText         != null) hud.nameText.text      = charName;

        SetSlider(hud, health.CurrentHealth, health.MaxHealth);
    }

    
    // health bar update
   

    private void OnHealthChanged(PlayerHUD hud, float current, float max)
    {
        SetSlider(hud, current, max);
        StartCoroutine(FlashBar(hud));
    }

    private void SetSlider(PlayerHUD hud, float current, float max)
    {
        if (hud.healthSlider == null) return;

        float t = Mathf.Clamp01(current / max);
        hud.healthSlider.value = t;

      
        if (hud.currentHealthText != null)
            hud.currentHealthText.text = Mathf.CeilToInt(current).ToString();

      
        if (hud.fillImage != null)
        {
            Color c = t > 0.5f
                ? Color.Lerp(healthMid, healthHigh, (t - 0.5f) * 2f)
                : Color.Lerp(healthLow, healthMid,  t * 2f);

            hud.fillImage.color = c;
        }
    }

    private IEnumerator FlashBar(PlayerHUD hud)
    {
        if (hud.fillImage == null) yield break;

        Color original      = hud.fillImage.color;
        hud.fillImage.color = flashColor;
        yield return new WaitForSeconds(flashDuration);
        hud.fillImage.color = original;
    }


    public void UpdateScore(int p1Wins, int p2Wins)
    {
        UpdatePips(p1HUD, p1Wins);
        UpdatePips(p2HUD, p2Wins);
    }

    private void UpdatePips(PlayerHUD hud, int winsCount)
    {
        if (hud.roundPips == null) return;

        for (int i = 0; i < hud.roundPips.Length; i++)
        {
            if (hud.roundPips[i] == null) continue;
            hud.roundPips[i].color = i < winsCount ? pipWon : pipEmpty;
        }
    }

    private void ResetPips(PlayerHUD hud)
    {
        UpdatePips(hud, 0);
    }


    public void UpdateRoundCounter(int roundNumber)
    {
        if (roundCounterText != null)
            roundCounterText.text = $"Round {roundNumber}";
    }
}