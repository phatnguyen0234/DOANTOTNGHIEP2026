using System;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class StaminaUI : MonoBehaviour
{
    [SerializeField] private Image energyImage;
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private float slide = 5f;
    [SerializeField] private Gradient gradient;
    private float targetFillAmount;

    private void OnEnable()
    {
        PlayerStamina.onStaminaChanged += HandleStaminaChange;
    }

    private void OnDisable()
    {
        PlayerStamina.onStaminaChanged -= HandleStaminaChange;
    }

    private void Update()
    {
        energyImage.fillAmount = Mathf.MoveTowards(energyImage.fillAmount, targetFillAmount, slide * Time.deltaTime);
        energyImage.color = gradient.Evaluate(energyImage.fillAmount);
    }

    private void HandleStaminaChange(float current, float max)
    {
        targetFillAmount = Mathf.Clamp01(current / max);
        int currentInt = Mathf.Max(0, Mathf.RoundToInt(current));
        int maxInt = Mathf.RoundToInt(max);
        text.text = $"{currentInt}/{maxInt}";
     //   gradient.Evaluate(targetFillAmount);
    }
}
