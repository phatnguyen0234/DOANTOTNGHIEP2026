using UnityEngine;
using UnityEngine.UI;
using BlitzyUI;

// Quản lý giao diện Bàn chế tạo (CraftingScreen) trong hệ thống UIManager.
// Kế thừa BlitzyUI.Screen để đồng bộ điều hướng tập trung qua Screen Stack.
// Tự động mở túi đồ người chơi ở nửa dưới màn hình để kéo thả nguyên liệu.
// Tự động hoàn trả nguyên liệu trên lưới 3x3 về túi đồ khi đóng màn hình.
[RequireComponent(typeof(Canvas))]
public class CraftingScreen : BlitzyUI.Screen
{
    [Header("UI Panels")]
    [Tooltip("Panel chứa toàn bộ khung bàn chế tạo.")]
    [SerializeField] private GameObject craftingPanel;

    [Tooltip("Nút bấm đóng bàn chế tạo (X).")]
    [SerializeField] private Button closeButton;

    [Header("Crafting Components")]
    [Tooltip("Inventory 9 ô đại diện cho lưới 3x3.")]
    [SerializeField] private Inventory gridInventory;

    [Tooltip("InventoryUI hiển thị các ô lưới 3x3.")]
    [SerializeField] private InventoryUI gridInventoryUI;

    [Tooltip("Thành phần hiển thị và nhận sản phẩm đầu ra.")]
    [SerializeField] private CraftingOutputUI outputUI;

    public static CraftingScreen Instance { get; private set; }

    public Inventory GridInventory => gridInventory;
    public bool IsOpen => craftingPanel != null ? craftingPanel.activeSelf : gameObject.activeSelf;

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
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseScreen);
        }

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

        if (craftingPanel != null)
        {
            craftingPanel.SetActive(true);
        }

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        // 1. Kết nối với hệ thống CraftingSystem
        if (CraftingSystem.Instance != null && gridInventory != null && outputUI != null)
        {
            CraftingSystem.Instance.BindCraftingUI(gridInventory, outputUI);
        }

        // 2. Tự động mở túi đồ của người chơi bên dưới để kéo thả nguyên liệu
        if (Inventory.Instance != null && !Inventory.Instance.IsBagOpen)
        {
            Inventory.Instance.OpenBag();
        }

        PushFinished();
    }

    public override void OnPop()
    {
        // 1. Hoàn trả toàn bộ nguyên liệu dở dang trên lưới 3x3 về túi đồ người chơi
        ReturnItemsToPlayer();

        // 2. Xóa kết quả trên ô output
        if (outputUI != null)
        {
            outputUI.ClearResult();
        }

        // 3. Ngắt kết nối với CraftingSystem
        if (CraftingSystem.Instance != null && gridInventory != null)
        {
            CraftingSystem.Instance.UnbindCraftingUI(gridInventory);
        }

        if (craftingPanel != null)
        {
            craftingPanel.SetActive(false);
        }

        // 4. Đóng túi đồ người chơi nếu đang mở
        if (Inventory.Instance != null && Inventory.Instance.IsBagOpen)
        {
            Inventory.Instance.CloseBag();
        }

        PopFinished();
    }

    public override void OnFocus()
    {
    }

    public override void OnFocusLost()
    {
    }

    #endregion

    #region Internal Logic & Item Handling

    private void ResolveReferences()
    {
        if (craftingPanel == null) craftingPanel = gameObject;
        if (gridInventory == null) gridInventory = GetComponentInChildren<Inventory>(true);
        if (gridInventoryUI == null) gridInventoryUI = GetComponentInChildren<InventoryUI>(true);
        if (outputUI == null) outputUI = GetComponentInChildren<CraftingOutputUI>(true);
    }

    private void RegisterEvents()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseScreen);
            closeButton.onClick.AddListener(CloseScreen);
        }
    }

    public void CloseScreen()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.QueuePopTo(ScreenIds.Crafting, include: true);
        }
        else
        {
            if (craftingPanel != null) craftingPanel.SetActive(false);
            ReturnItemsToPlayer();
            if (Inventory.Instance != null && Inventory.Instance.IsBagOpen)
            {
                Inventory.Instance.CloseBag();
            }
        }
    }

    public void ReturnItemsToPlayer()
    {
        if (gridInventory == null) return;
        Inventory playerInv = Inventory.Instance ?? FindAnyObjectByType<Inventory>(FindObjectsInactive.Include);
        if (playerInv == null) return;

        ItemDropSpawner dropSpawner = FindAnyObjectByType<ItemDropSpawner>(FindObjectsInactive.Include);
        PlayerMovement playerMovement = FindAnyObjectByType<PlayerMovement>(FindObjectsInactive.Include);
        Vector3 dropPos = playerMovement != null ? playerMovement.transform.position : Vector3.zero;

        for (int i = 0; i < gridInventory.Capacity; i++)
        {
            InventorySlot slot = gridInventory.GetSlot(i);
            if (!slot.IsEmpty())
            {
                int leftover = playerInv.AddItem(slot.ItemData, slot.Amount);
                if (leftover == 0)
                {
                    gridInventory.ClearSlot(i);
                }
                else
                {
                    Debug.LogWarning($"[CraftingScreen] Túi đồ người chơi đã đầy! Đang rơi {slot.ItemData.ItemName} ra đất.");
                    gridInventory.RemoveItemFromSlot(i, slot.Amount - leftover);
                    if (dropSpawner != null)
                    {
                        dropSpawner.SpawnDrop(slot.ItemData, leftover, dropPos);
                    }
                }
            }
        }
    }

    #endregion
}

