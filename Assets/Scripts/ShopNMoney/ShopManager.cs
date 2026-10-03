using System.Collections.Generic;
using UnityEngine;

public class ShopManager : MonoBehaviour
{
    public static ShopManager instance { get; private set; }

    [Header("Dependencies")]
    [SerializeField] private Inventory playerInventory;
    [SerializeField] private PlayerMoney playerMoney;

    [Header("UI Generation")]
    [SerializeField] private GameObject shopUIPanel;
    [SerializeField] private ShopSlotUI shopSlotPrefab;

    [SerializeField] private Transform shopContentContainer;

    //[Header("Shop Database")]
    //[SerializeField] private List<ShopItem> shopItems = new List<ShopItem>();

    private List<ShopItem> currentShopItems = new List<ShopItem>();
    private List<ShopSlotUI> activeUIElements = new List<ShopSlotUI>();

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            instance = this;
        }
    }

    public void OpenShop(List<ShopItem> shopItems)
    {
        currentShopItems = shopItems;
        GenerateShopUI();
        shopUIPanel.SetActive(true);
    }

    public void CloseShop()
    {
        shopUIPanel.SetActive(false);
    }

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

        for (int i = 0; i < currentShopItems.Count; i++)
        {
            ShopSlotUI newSlot = Instantiate(shopSlotPrefab, shopContentContainer);
            newSlot.Setup(this, i, currentShopItems[i]);
            activeUIElements.Add(newSlot);
        }
    }

    public void AttemptBuyItem(int index)
    {
        if (index < 0 || index >= currentShopItems.Count) return;

        ShopItem itemToBuy = currentShopItems[index];

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