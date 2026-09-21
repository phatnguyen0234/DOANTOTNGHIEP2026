using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopSlotUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private TextMeshProUGUI stockText;
    [SerializeField] private Button buyButton; 

    private ShopManager shopManager;
    private int slotIndex;
    private ShopItem currentShopItem;

    public void Setup(ShopManager manager, int index, ShopItem shopItem)
    {
        shopManager = manager;
        slotIndex = index;
        currentShopItem = shopItem;

        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(OnBuyClicked);

        RefreshUI();
    }

    public void RefreshUI()
    {
        if (currentShopItem.itemData == null) return;

        iconImage.sprite = currentShopItem.itemData.Icon;
        nameText.text = currentShopItem.itemData.ItemName;
        priceText.text = currentShopItem.itemData.BuyPrice.ToString() + " G";

        stockText.text = currentShopItem.stock;

        if (currentShopItem.stock <= 0)
        {
            iconImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);
            stockText.text = "<color=red>Sold Out</color>";
        }
        else
        {
            iconImage.color = Color.white; 
        }
    }

    private void OnBuyClicked()
    {
        if (currentShopItem.stock <= 0)
        {
            Debug.LogWarning($"[Shop]'{currentShopItem.itemData.ItemName}' is out of stock !");
            return;
        }

        shopManager.AttemptBuyItem(slotIndex);
    }
}