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

    //public void AttemptBuyItem(int index)
    //{
    //    if (index < 0 || index >= currentShopItems.Count)
    //        return;

    //    ShopItem itemToBuy = currentShopItems[index];

    //    if (itemToBuy.stock <= 0)
    //        return;

    //    if (playerMoney.CurrentMoney < itemToBuy.itemData.BuyPrice)
    //    {
    //        Debug.LogWarning("[Shop] You don't have enough money!");
    //        return;
    //    }

    //    if (!playerInventory.CanAddItem(itemToBuy.itemData, 1))
    //    {
    //        Debug.LogWarning("[Shop] Full bag!");
    //        return;
    //    }

    //    playerMoney.TrySpendMoney(itemToBuy.itemData.BuyPrice);

    //    playerInventory.TryAddItem(itemToBuy.itemData, 1);

    //    itemToBuy.stock--;

    //    Debug.Log(
    //        $"[Shop] Bought {itemToBuy.itemData.ItemName}. Remaining stock: {itemToBuy.stock}");

    //    activeUIElements[index].RefreshUI();
    //}


    // --2 HAM DUOI THAY THE ATTEMPTBUYITEM, DE GIUP CHO CODE DE QUAN LY HON, VA DE THEM CAC TINH NANG MULTIPLAYER SAU NAY--
    // Call this from ShopSlotUI instead of AttemptBuyItem directly
    public void RequestBuyItem(int index)
    {
        if (index < 0 || index >= currentShopItems.Count)
            return;

        ShopItem itemToBuy = currentShopItems[index];

        // Client-side pre-validation (prevents unnecessary network traffic later)
        if (itemToBuy.stock <= 0 ||
            playerMoney.CurrentMoney < itemToBuy.itemData.BuyPrice ||
            !playerInventory.CanAddItem(itemToBuy.itemData, 1))
        {
            return;
        }

        // In Singleplayer: Execute immediately.
        // In Multiplayer: Replace this call with RequestBuyServerRpc(index).
        ExecuteBuyTransaction(index);
    }

    // [SERVER AUTHORITY] In Multiplayer, this logic runs exclusively on the Server
    private void ExecuteBuyTransaction(int index)
    {
        if (index < 0 || index >= currentShopItems.Count)
            return;

        ShopItem itemToBuy = currentShopItems[index];

        // Authoritative validation
        if (itemToBuy.stock <= 0)
            return;

        if (playerMoney.CurrentMoney < itemToBuy.itemData.BuyPrice)
            return;

        if (!playerInventory.CanAddItem(itemToBuy.itemData, 1))
            return;

        // Commit state changes
        playerMoney.TrySpendMoney(itemToBuy.itemData.BuyPrice);
        playerInventory.TryAddItem(itemToBuy.itemData, 1);
        itemToBuy.stock--;

        // In Multiplayer: Broadcast updated stock to all clients via ClientRpc
        OnTransactionCompleted(index);
    }

    private void OnTransactionCompleted(int index)
    {
        if (index >= 0 && index < activeUIElements.Count)
        {
            activeUIElements[index].RefreshUI();
        }
    }
}