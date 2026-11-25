using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShopManager : MonoBehaviour
{
    public static ShopManager instance { get; private set; }

    public bool IsShopOpen
    {
        get
        {
            if (BlitzyUI.UIManager.Instance != null)
            {
                var screen = BlitzyUI.UIManager.Instance.GetScreen(BlitzyUI.ScreenIds.Shop);
                return screen != null && screen.gameObject.activeSelf;
            }
            return shopUIPanel != null && shopUIPanel.activeSelf;
        }
    }

    public ShopNPC CurrentShopNpc { get; private set; }

    [Header("Dependencies")]
    [SerializeField] private Inventory playerInventory;
    [SerializeField] private PlayerMoney playerMoney;

    [Header("UI Generation")]
    [SerializeField] private GameObject shopUIPanel;
    [SerializeField] private ScrollRect shopScrollRect; // Drag ShopUI here in Inspector
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
        ResolveDependencies();
    }

    private void ResolveDependencies()
    {
        if (playerInventory == null) playerInventory = Inventory.Instance ?? FindAnyObjectByType<Inventory>(FindObjectsInactive.Include);
        if (playerMoney == null) playerMoney = FindAnyObjectByType<PlayerMoney>(FindObjectsInactive.Include);
    }

    private void Start()
    {
        if (shopUIPanel != null)
        {
            shopUIPanel.SetActive(false);
        }
    }

    private void Update()
    {
        if (BlitzyUI.UIManager.Instance != null && BlitzyUI.UIManager.Instance.handleEscapeKey)
            return;

        if (IsShopOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseShop();
        }
    }

    //public void OpenShop(ShopNPC shopNpc, List<ShopItem> shopItems)
    //{
    //    CurrentShopNpc = shopNpc;

    //    currentShopItems = shopItems;

    //    GenerateShopUI();

    //    shopUIPanel.SetActive(true);

    //    Debug.Log("[Shop] Shop opened");
    //}

    public void OpenShop(ShopNPC shopNpc, List<ShopItem> shopItems)
    {
        CurrentShopNpc = shopNpc;
        currentShopItems = shopItems != null ? shopItems : new List<ShopItem>();

        if (BlitzyUI.UIManager.Instance != null)
        {
            if (IsShopOpen && ShopScreen.Instance != null)
            {
                ShopScreen.Instance.RefreshShop(shopNpc, currentShopItems);
                return;
            }

            var data = new BlitzyUI.Screen.Data();
            data.Add("npc", shopNpc);
            data.Add("items", currentShopItems);
            BlitzyUI.UIManager.Instance.QueuePush(BlitzyUI.ScreenIds.Shop, data);
            return;
        }

        GenerateShopUI(shopContentContainer);

        if (shopUIPanel != null)
        {
            shopUIPanel.SetActive(true);
        }

        // Reset scroll position to the top whenever the shop opens
        if (shopScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            shopScrollRect.verticalNormalizedPosition = 1f;
        }

        Debug.Log("[Shop] Shop opened");
    }

    public void CloseShop()
    {
        if (BlitzyUI.UIManager.Instance != null)
        {
            BlitzyUI.UIManager.Instance.QueuePopTo(BlitzyUI.ScreenIds.Shop, include: true);
            CurrentShopNpc = null;
            return;
        }

        if (shopUIPanel != null)
        {
            shopUIPanel.SetActive(false);
        }

        CurrentShopNpc = null;

        Debug.Log("[Shop] Shop closed");
    }

    public void OnShopScreenClosed()
    {
        CurrentShopNpc = null;
        Debug.Log("[Shop] Shop screen closed");
    }

    public void GenerateShopUI(Transform container = null, List<ShopItem> overrideItems = null)
    {
        Transform targetContainer = container != null ? container : shopContentContainer;
        if (targetContainer == null) return;

        if (overrideItems != null)
        {
            currentShopItems = overrideItems;
        }

        if (shopSlotPrefab == null)
        {
            shopSlotPrefab = Resources.Load<ShopSlotUI>("Prefabs/Shop/ShopSlot");
        }

        foreach (Transform child in targetContainer)
        {
            Destroy(child.gameObject);
        }

        activeUIElements.Clear();

        if (shopSlotPrefab == null)
        {
            Debug.LogError("[ShopManager] Không tìm thấy shopSlotPrefab!");
            return;
        }

        for (int i = 0; i < currentShopItems.Count; i++)
        {
            ShopSlotUI newSlot = Instantiate(shopSlotPrefab, targetContainer);
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
        ResolveDependencies();

        if (index < 0 || index >= currentShopItems.Count)
            return;

        ShopItem itemToBuy = currentShopItems[index];

        // Client-side pre-validation (prevents unnecessary network traffic later)
        if (itemToBuy.IsSoldOut ||
    playerMoney == null || playerMoney.CurrentMoney < itemToBuy.itemData.BuyPrice ||
    playerInventory == null || !playerInventory.CanAddItem(itemToBuy.itemData, 1))
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
        ResolveDependencies();

        if (index < 0 || index >= currentShopItems.Count)
            return;

        ShopItem itemToBuy = currentShopItems[index];

        // Authoritative validation
        if (itemToBuy.IsSoldOut)
            return;

        if (playerMoney == null || playerMoney.CurrentMoney < itemToBuy.itemData.BuyPrice)
        {
            Debug.LogWarning("[Shop] You don't have enough money!");
            return;
        }

        if (playerInventory == null || !playerInventory.CanAddItem(itemToBuy.itemData, 1))
        {
            Debug.LogWarning("[Shop] Full bag!");
            return;
        }

        // Commit state changes
        playerMoney.TrySpendMoney(itemToBuy.itemData.BuyPrice);
        playerInventory.TryAddItem(itemToBuy.itemData, 1);
        if (!itemToBuy.IsUnlimited)
        {
            itemToBuy.stock--;
        }

        // In Multiplayer: Broadcast updated stock to all clients via ClientRpc
        OnTransactionCompleted(index);

        // Re-evaluate material-gated items in case buying this item unlocked new stock
        if (CurrentShopNpc != null)
        {
            CurrentShopNpc.SyncObtainedMaterialsFromInventory();
        }
    }

    private void OnTransactionCompleted(int index)
    {
        if (index >= 0 && index < activeUIElements.Count && activeUIElements[index] != null)
        {
            activeUIElements[index].RefreshUI();
        }
    }
}