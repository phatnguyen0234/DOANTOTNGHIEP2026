using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CraftingOutputUI : MonoBehaviour
{
    [Header("UI References")]
    public Image iconImage;
    public TextMeshProUGUI amountText;
    public Button outputButton;
    public CraftingSystem craftingSystem;

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

    private void OnOutputClicked()
    {
        // Check if player is holding Shift for bulk crafting
        bool craftAll = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        craftingSystem.ExecuteCrafting(craftAll);
    }
}