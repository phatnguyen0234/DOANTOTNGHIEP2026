using TMPro;
using UnityEngine;

public class MoneyUI : MonoBehaviour
{
    [SerializeField] private PlayerMoney playerMoney;
    [SerializeField] private TextMeshProUGUI moneyText;

    private void OnEnable()
    {
        if (playerMoney != null)
        {
            playerMoney.OnMoneyChanged += UpdateMoneyDisplay;
        }
    }

    private void OnDisable()
    {
        if (playerMoney != null)
        {
            playerMoney.OnMoneyChanged -= UpdateMoneyDisplay;
        }
    }

    private void UpdateMoneyDisplay(int currentMoney)
    {
        if (moneyText != null)
        {
            moneyText.text = currentMoney.ToString() + " G";
        }
    }
}