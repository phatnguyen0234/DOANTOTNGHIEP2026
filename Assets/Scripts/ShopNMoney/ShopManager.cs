using System.Collections.Generic;
using UnityEngine;

public class ShopManager : MonoBehaviour
{
    public static ShopManager instance { get; private set; }

    public bool IsShopOpen => shopUIPanel.activeSelf;

    public ShopNPC CurrentShopNpc { get; private set; }

    [Header("Dependencies")]
    [SerializeField] private Inventory playerInventory;
    [SerializeField] private PlayerMoney playerMoney;

    [Header("UI Generation")]
    [SerializeField] private GameObject shopUIPanel;
    [SerializeField] private ShopSlotUI shopSlotPrefab;
    [SerializeField] private Transform shopContentContainer;

    private List<ShopItem> currentShopItems = new();
    private readonly List<ShopSlotUI> activeUIElements = new();

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    private void Start()
    {
        shopUIPanel.SetActive(false);
    }

    private void Update()
    {
        if (IsShopOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseShop();
        }
    }

    public void OpenShop(ShopNPC shopNpc, List<ShopItem> shopItems)
    {
        CurrentShopNpc = shopNpc;

        currentShopItems = shopItems;

        GenerateShopUI();

        shopUIPanel.SetActive(true);

        Debug.Log("[Shop] Shop opened");
    }

    public void CloseShop()
    {
        shopUIPanel.SetActive(false);

        CurrentShopNpc = null;

        Debug.Log("[Shop] Shop closed");
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
            ShopSlotUI newSlot =
                Instantiate(shopSlotPrefab, shopContentContainer);

            newSlot.Setup(this, i, currentShopItems[i]);

            activeUIElements.Add(newSlot);
        }
    }

    public void AttemptBuyItem(int index)
    {
        if (index < 0 || index >= currentShopItems.Count)
            return;

        ShopItem itemToBuy = currentShopItems[index];

        if (itemToBuy.stock <= 0)
            return;

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

        Debug.Log(
            $"[Shop] Bought {itemToBuy.itemData.ItemName}. Remaining stock: {itemToBuy.stock}");

        activeUIElements[index].RefreshUI();
    }
}