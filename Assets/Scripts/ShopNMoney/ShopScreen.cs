using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BlitzyUI;

// Quản lý màn hình Cửa hàng (ShopScreen) trong kiến trúc UIManager BlitzyUI.
// Tự động sinh danh sách vật phẩm bán từ ShopNPC / ShopManager.
// Tích hợp hiển thị tiền người chơi trực tiếp trên Header.
// Hỗ trợ đóng cửa hàng bằng nút X hoặc phím ESC.
[RequireComponent(typeof(Canvas))]
public class ShopScreen : BlitzyUI.Screen
{
    [Header("UI Panels")]
    [Tooltip("Panel chính chứa giao diện cửa hàng.")]
    [SerializeField] private GameObject shopPanel;

    [Tooltip("Transform chứa các ô ShopSlotUI (gắn LayoutGroup hoặc Content trong ScrollRect).")]
    [SerializeField] private Transform shopContentContainer;

    [Tooltip("Prefab ô bán hàng ShopSlotUI (tùy chọn nếu ShopManager đã có).")]
    [SerializeField] private ShopSlotUI shopSlotPrefab;

    [Header("Header Elements")]
    [Tooltip("Text tiêu đề cửa hàng / tên NPC.")]
    [SerializeField] private TextMeshProUGUI titleText;

    [Tooltip("Text hiển thị số tiền hiện tại của người chơi.")]
    [SerializeField] private TextMeshProUGUI moneyText;

    [Tooltip("Nút bấm đóng cửa hàng (X).")]
    [SerializeField] private Button closeButton;

    public static ShopScreen Instance { get; private set; }

    public Transform ContentContainer => shopContentContainer;
    public bool IsOpen => shopPanel != null ? shopPanel.activeSelf : gameObject.activeSelf;

    private PlayerMoney playerMoney;

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        ResolveReferences();
        RegisterEvents();
    }

    private void Update()
    {
        if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseScreen();
        }
    }

    private void OnDestroy()
    {
        UnregisterEvents();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    #endregion

    #region BlitzyUI.Screen Lifecycle

    public override void OnSetup()
    {
        Instance = this;
        keepCached = true;

        ResolveReferences();
        RegisterEvents();
    }

    public override void OnPush(Data data)
    {
        ResolveReferences();

        if (shopPanel != null)
        {
            shopPanel.SetActive(true);
        }

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        ShopNPC npc = null;
        List<ShopItem> items = null;

        if (data != null)
        {
            data.TryGet("npc", out npc);
            data.TryGet("items", out items);
        }

        if (titleText != null)
        {
            titleText.text = npc != null ? $"CỬA HÀNG - {npc.name}" : "CỬA HÀNG";
        }

        // 1. Tạo danh sách vật phẩm bán qua ShopManager
        if (ShopManager.instance != null)
        {
            Transform targetContainer = shopContentContainer != null ? shopContentContainer : transform;
            ShopManager.instance.GenerateShopUI(targetContainer, items);
        }

        // 2. Cập nhật và lắng nghe thay đổi tiền người chơi
        if (playerMoney == null)
        {
            playerMoney = FindAnyObjectByType<PlayerMoney>(FindObjectsInactive.Include);
        }

        if (playerMoney != null)
        {
            playerMoney.OnMoneyChanged -= UpdateMoneyDisplay;
            playerMoney.OnMoneyChanged += UpdateMoneyDisplay;
            UpdateMoneyDisplay(playerMoney.CurrentMoney);
        }

        PushFinished();
    }

    public override void OnPop()
    {
        if (playerMoney != null)
        {
            playerMoney.OnMoneyChanged -= UpdateMoneyDisplay;
        }

        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
        }

        if (ShopManager.instance != null)
        {
            ShopManager.instance.OnShopScreenClosed();
        }

        PopFinished();
    }

    public override void OnFocus()
    {
        if (playerMoney != null)
        {
            UpdateMoneyDisplay(playerMoney.CurrentMoney);
        }
    }

    public override void OnFocusLost()
    {
    }

    #endregion

    #region Internal Logic

    private void ResolveReferences()
    {
        if (shopPanel == null) shopPanel = gameObject;
        if (shopContentContainer == null) shopContentContainer = GetComponentInChildren<VerticalLayoutGroup>(true)?.transform ?? transform;
        if (closeButton == null) closeButton = GetComponentInChildren<Button>(true);
    }

    private void RegisterEvents()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseScreen);
            closeButton.onClick.AddListener(CloseScreen);
        }
    }

    private void UnregisterEvents()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseScreen);
        }

        if (playerMoney != null)
        {
            playerMoney.OnMoneyChanged -= UpdateMoneyDisplay;
        }
    }

    private void UpdateMoneyDisplay(int currentMoney)
    {
        if (moneyText != null)
        {
            moneyText.text = $"{currentMoney:N0} G";
        }
    }

    public void CloseScreen()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.QueuePopTo(ScreenIds.Shop, include: true);
        }
        else
        {
            if (shopPanel != null) shopPanel.SetActive(false);
            if (ShopManager.instance != null) ShopManager.instance.CloseShop();
        }
    }

    #endregion
}

