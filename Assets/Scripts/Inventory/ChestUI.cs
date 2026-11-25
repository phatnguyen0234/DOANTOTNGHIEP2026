using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BlitzyUI;

// Quản lý giao diện hiển thị của Rương đồ (Chest UI).
// Kế thừa BlitzyUI.Screen để quản lý vòng đời và điều hướng tập trung qua Screen Stack.
// Tự động sinh số ô slot theo dung lượng của rương đang mở.
// Tự động mở đồng thời túi đồ của người chơi bên dưới và hỗ trợ đóng bằng phím ESC hoặc nút X.
// Điều phối các thao tác nhanh: Shift-Click (Quick Transfer), Lấy tất cả (Loot All), Cất tất cả (Deposit All).
[RequireComponent(typeof(Canvas))]
public class ChestUI : BlitzyUI.Screen
{
    [Header("UI Panels")]
    [Tooltip("Panel UI của rương đồ cần bật/tắt (nếu để trống sẽ dùng chính GameObject này).")]
    [SerializeField] private GameObject chestPanel;

    [Tooltip("TextMeshProUGUI hiển thị tên tiêu đề của rương.")]
    [SerializeField] private TextMeshProUGUI titleText;

    [Tooltip("Transform chứa các ô slot của rương (gắn GridLayoutGroup).")]
    [SerializeField] private Transform slotContainer;

    [Tooltip("Prefab InventorySlotUI dùng để sinh các ô chứa đồ trong rương.")]
    [SerializeField] private InventorySlotUI slotPrefab;

    [Header("Action Buttons")]
    [Tooltip("Nút bấm đóng rương (dấu X).")]
    [SerializeField] private Button closeButton;

    [Tooltip("Nút bấm Lấy tất cả vật phẩm từ rương về túi đồ.")]
    [SerializeField] private Button lootAllButton;

    [Tooltip("Nút bấm Cất tất cả vật phẩm từ túi đồ vào rương.")]
    [SerializeField] private Button depositAllButton;

    private Chest currentChest;
    private readonly List<InventorySlotUI> chestSlotUIList = new List<InventorySlotUI>();

    public static ChestUI Instance { get; private set; }

    public Chest CurrentChest => currentChest;
    public bool IsOpen => chestPanel != null ? chestPanel.activeSelf : gameObject.activeSelf;

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        if (chestPanel == null)
        {
            chestPanel = gameObject;
        }

        RegisterButtonEvents();
    }

    private void OnDestroy()
    {
        if (currentChest != null && currentChest.Inventory != null)
        {
            currentChest.Inventory.OnInventoryChanged -= RefreshSlots;
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

        if (chestPanel == null)
        {
            chestPanel = gameObject;
        }

        RegisterButtonEvents();
    }

    public override void OnPush(Data data)
    {
        if (data != null && data.TryGet<Chest>("chest", out var chest))
        {
            currentChest = chest;
        }

        OpenChestInternal(currentChest);

        // BẮT BUỘC: Thông báo hoàn tất mở màn hình cho UIManager
        PushFinished();
    }

    public override void OnPop()
    {
        CloseChestInternal();

        // BẮT BUỘC: Thông báo hoàn tất đóng màn hình cho UIManager
        PopFinished();
    }

    public override void OnFocus()
    {
        RefreshSlots();
    }

    public override void OnFocusLost()
    {
    }

    #endregion

    private void RegisterButtonEvents()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseChest);
            closeButton.onClick.AddListener(CloseChest);
        }

        if (lootAllButton != null)
        {
            lootAllButton.onClick.RemoveListener(HandleLootAll);
            lootAllButton.onClick.AddListener(HandleLootAll);
        }

        if (depositAllButton != null)
        {
            depositAllButton.onClick.RemoveListener(HandleDepositAll);
            depositAllButton.onClick.AddListener(HandleDepositAll);
        }
    }

    // Mở giao diện rương chỉ định (ưu tiên qua UIManager Queue)
    public void OpenChest(Chest chest)
    {
        if (chest == null || chest.Inventory == null) return;

        if (UIManager.Instance != null)
        {
            var data = new Data();
            data.Add("chest", chest);
            UIManager.Instance.QueuePush(ScreenIds.Chest, data);
        }
        else
        {
            currentChest = chest;
            OpenChestInternal(currentChest);
        }
    }

    // Đóng giao diện rương (ưu tiên qua UIManager Queue)
    public void CloseChest()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.QueuePopTo(ScreenIds.Chest, include: true);
        }
        else
        {
            CloseChestInternal();
        }
    }

    private void OpenChestInternal(Chest chest)
    {
        if (chest == null || chest.Inventory == null) return;

        currentChest = chest;
        currentChest.Inventory.OnInventoryChanged -= RefreshSlots;
        currentChest.Inventory.OnInventoryChanged += RefreshSlots;

        if (titleText != null)
        {
            titleText.text = currentChest.ChestName;
        }

        SetupSlots(currentChest.Inventory.Capacity);

        if (chestPanel != null)
        {
            chestPanel.SetActive(true);
        }

        if (transform.parent != null && !transform.parent.gameObject.activeSelf)
        {
            transform.parent.gameObject.SetActive(true);
        }

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        // Tự động mở túi đồ người chơi để sẵn sàng kéo thả hoặc Shift-Click
        if (Inventory.Instance != null && !Inventory.Instance.IsBagOpen)
        {
            Inventory.Instance.OpenBag();
        }

        RefreshSlots();
    }

    private void CloseChestInternal()
    {
        if (currentChest != null)
        {
            currentChest.Inventory.OnInventoryChanged -= RefreshSlots;
            currentChest.OnChestClosed();
            currentChest = null;
        }

        if (chestPanel != null && chestPanel.activeSelf)
        {
            chestPanel.SetActive(false);
        }

        // Đóng túi đồ người chơi khi đóng rương
        if (Inventory.Instance != null && Inventory.Instance.IsBagOpen)
        {
            Inventory.Instance.CloseBag();
        }
    }

    // Khởi tạo các ô slot UI theo đúng số lượng Capacity của rương
    private void SetupSlots(int capacity)
    {
        if (slotContainer == null || currentChest == null) return;

        if (slotPrefab == null)
        {
            slotPrefab = Resources.Load<InventorySlotUI>("Prefabs/SlotPrefab");
        }

        if (slotPrefab == null) return;

        // Sinh thêm nếu thiếu slot
        while (chestSlotUIList.Count < capacity)
        {
            int index = chestSlotUIList.Count;
            InventorySlotUI slotUI = Instantiate(slotPrefab, slotContainer);
            slotUI.gameObject.SetActive(true);
            slotUI.enabled = true;
            slotUI.EnsureActive();
            slotUI.Setup(index, currentChest.Inventory);
            chestSlotUIList.Add(slotUI);
        }

        // Cập nhật lại setup cho các slot
        for (int i = 0; i < chestSlotUIList.Count; i++)
        {
            if (i < capacity)
            {
                chestSlotUIList[i].gameObject.SetActive(true);
                chestSlotUIList[i].Setup(i, currentChest.Inventory);
            }
            else
            {
                chestSlotUIList[i].gameObject.SetActive(false);
            }
        }
    }

    // Làm mới lại toàn bộ các ô Slot trên giao diện rương từ dữ liệu hiện tại
    public void RefreshSlots()
    {
        if (currentChest == null || currentChest.Inventory == null) return;

        for (int i = 0; i < chestSlotUIList.Count; i++)
        {
            if (i < currentChest.Inventory.Capacity)
            {
                InventorySlot slotData = currentChest.Inventory.GetSlot(i);
                chestSlotUIList[i].Refresh(slotData);
            }
        }
    }

    // Xử lý chuyển nhanh (Shift + Click) giữa Rương và Túi đồ người chơi
    public void HandleQuickTransfer(InventorySlotUI clickedSlot)
    {
        if (clickedSlot == null || currentChest == null || currentChest.Inventory == null) return;

        Inventory playerInv = Inventory.Instance;
        Inventory chestInv = currentChest.Inventory;

        if (clickedSlot.AssignedInventory == playerInv)
        {
            InventoryTransferHelper.QuickTransferItem(playerInv, clickedSlot.SlotIndex, chestInv);
        }
        else if (clickedSlot.AssignedInventory == chestInv)
        {
            InventoryTransferHelper.QuickTransferItem(chestInv, clickedSlot.SlotIndex, playerInv);
        }
    }

    private void HandleLootAll()
    {
        if (currentChest == null || currentChest.Inventory == null || Inventory.Instance == null) return;
        InventoryTransferHelper.TransferAll(currentChest.Inventory, Inventory.Instance);
    }

    private void HandleDepositAll()
    {
        if (currentChest == null || currentChest.Inventory == null || Inventory.Instance == null) return;
        InventoryTransferHelper.TransferAll(Inventory.Instance, currentChest.Inventory);
    }
}
