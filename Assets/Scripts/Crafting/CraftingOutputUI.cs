using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CraftingOutputUI : MonoBehaviour
{
    [Header("UI References")]
    public Image iconImage;
    public TextMeshProUGUI amountText;
    public Button outputButton;

    private void Start()
    {
        outputButton.onClick.AddListener(OnOutputClicked);
        ClearResult();
    }

    public void DisplayResult(ItemData item, int displayAmount)
    {
        iconImage.sprite = item.Icon;
        iconImage.color = Color.white;
        amountText.text = displayAmount > 1 ? displayAmount.ToString() : "";
        outputButton.interactable = true;
    }

    public void ClearResult()
    {
        iconImage.sprite = null;
        iconImage.color = Color.clear;
        amountText.text = "";
        outputButton.interactable = false;
    }

    public void OnOutputClicked()
    {
        if (CraftingSystem.Instance != null)
        {
            bool isShiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            CraftingSystem.Instance.ExecuteCrafting(isShiftHeld);
        }
        else
        {
            Debug.LogError("Cant find Crafting System");
        }
    }
}