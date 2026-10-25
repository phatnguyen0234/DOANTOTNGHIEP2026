using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Quản lý giao diện hiển thị của Rương đồ (Chest UI).
// Tự động sinh số ô slot theo dung lượng của rương đang mở.
// Tự động mở đồng thời túi đồ của người chơi bên dưới và hỗ trợ đóng bằng phím ESC hoặc nút X.
// Điều phối các thao tác nhanh: Shift-Click (Quick Transfer), Lấy tất cả (Loot All), Cất tất cả (Deposit All).
public class ChestUI : MonoBehaviour
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

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (chestPanel == null)
        {
            chestPanel = gameObject;
        }

        if (closeButton != null) closeButton.onClick.AddListener(CloseChest);
        if (lootAllButton != null) lootAllButton.onClick.AddListener(HandleLootAll);
        if (depositAllButton != null) depositAllButton.onClick.AddListener(HandleDepositAll);
    }

    private void Start()
    {
        // Mặc định ban đầu luôn đóng giao diện rương
        if (chestPanel != null && chestPanel.activeSelf)
        {
            chestPanel.SetActive(false);
        }
    }

    private void Update()
    {
        // Phím tắt ESC đóng rương
        if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseChest();
        }
    }

    // Mở giao diện rương chỉ định và đồng thời mở túi đồ của người chơi
    public void OpenChest(Chest chest)
    {
        if (chest == null || chest.Inventory == null) return;

        if (currentChest != null && currentChest != chest)
        {
            currentChest.Inventory.OnInventoryChanged -= RefreshSlots;
            currentChest.OnChestClosed();
        }

        currentChest = chest;
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

        // Tự động mở túi đồ người chơi để sẵn sàng kéo thả hoặc Shift-Click
        if (Inventory.Instance != null && !Inventory.Instance.IsBagOpen)
        {
            Inventory.Instance.OpenBag();
        }

        RefreshSlots();
    }

    // Đóng giao diện rương và trả lại trạng thái bình thường cho túi đồ
    public void CloseChest()
    {
        if (!IsOpen && currentChest == null) return;

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
        if (slotContainer == null || slotPrefab == null || currentChest == null) return;

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
            // Click slot ở túi người chơi -> Chuyển sang rương
            InventoryTransferHelper.QuickTransferItem(playerInv, clickedSlot.SlotIndex, chestInv);
        }
        else if (clickedSlot.AssignedInventory == chestInv)
        {
            // Click slot ở rương -> Chuyển sang túi người chơi
            InventoryTransferHelper.QuickTransferItem(chestInv, clickedSlot.SlotIndex, playerInv);
        }
    }

    // Nút Lấy tất cả (Loot All): Chuyển tối đa vật phẩm từ rương về túi đồ người chơi
    private void HandleLootAll()
    {
        if (currentChest == null || currentChest.Inventory == null || Inventory.Instance == null) return;
        InventoryTransferHelper.TransferAll(currentChest.Inventory, Inventory.Instance);
    }

    // Nút Cất tất cả (Deposit All): Chuyển tối đa vật phẩm từ túi đồ người chơi vào rương
    private void HandleDepositAll()
    {
        if (currentChest == null || currentChest.Inventory == null || Inventory.Instance == null) return;
        InventoryTransferHelper.TransferAll(Inventory.Instance, currentChest.Inventory);
    }
}
