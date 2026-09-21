using System.Collections.Generic;
using UnityEngine;

public class ShopManager : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private Inventory playerInventory;
    [SerializeField] private PlayerMoney playerMoney;

    [Header("Shop Database")]
    [SerializeField] private List<ShopItem> shopItems = new List<ShopItem>();

    [Header("UI Generation")]
    [SerializeField] private ShopSlotUI shopSlotPrefab;

    [SerializeField] private Transform shopContentContainer;

    private List<ShopSlotUI> activeUIElements = new List<ShopSlotUI>();

    private void Start()
    {
        GenerateShopUI();
    }

    private void GenerateShopUI()
    {
        foreach (Transform child in shopContentContainer)
        {
            Destroy(child.gameObject);
        }
        activeUIElements.Clear();

        for (int i = 0; i < shopItems.Count; i++)
        {
            ShopSlotUI newSlot = Instantiate(shopSlotPrefab, shopContentContainer);
            newSlot.Setup(this, i, shopItems[i]);
            activeUIElements.Add(newSlot);
        }
    }

    public void AttemptBuyItem(int index)
    {
        if (index < 0 || index >= shopItems.Count) return;

        ShopItem itemToBuy = shopItems[index];

        if (itemToBuy.stock <= 0) return;

        if (playerMoney.CurrentMoney < itemToBuy.itemData.BuyPrice)
        {
            Debug.LogWarning("[Shop] You don't have enough money!");
            return;
        }

        if (!playerInventory.CanAddItem(itemToBuy.itemData, 1))
        {
            Debug.LogWarning("[Shop] Full bag!");
            return;
        }


        playerMoney.TrySpendMoney(itemToBuy.itemData.BuyPrice); 
        playerInventory.TryAddItem(itemToBuy.itemData, 1);      
        itemToBuy.stock--;                                     

        Debug.Log($"[Shop] Buy succesful {itemToBuy.itemData.ItemName}. Shop has {itemToBuy.stock}");

        activeUIElements[index].RefreshUI();
    }
}