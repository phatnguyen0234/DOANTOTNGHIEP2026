using System.Collections.Generic;
using UnityEngine;

// Quản lý toàn bộ giao diện thanh Hotbar, tự động đồng bộ theo dữ liệu từ Inventory và HotbarController.
public class HotbarUI : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("Tham chiếu tới Component Inventory chính.")]
    [SerializeField] private Inventory inventory;

    [Tooltip("Tham chiếu tới Component HotbarController.")]
    [SerializeField] private HotbarController hotbarController;

    [Header("UI Configuration")]
    [Tooltip("Transform chứa các ô slot của Hotbar (Horizontal Layout Group).")]
    [SerializeField] private Transform slotContainer;

    [Tooltip("Prefab của ô HotbarSlotUI.")]
    [SerializeField] private HotbarSlotUI slotPrefab;

    [Tooltip("Số lượng ô Hotbar.")]
    [SerializeField, Min(1)] private int hotbarSize = 10;

    private readonly List<HotbarSlotUI> slotUIList = new List<HotbarSlotUI>();
    private bool isInitialized = false;

    #region Unity Lifecycle

    private void Start()
    {
        Initialize();
    }

    private void OnEnable()
    {
        SubscribeEvents();

        if (isInitialized)
        {
            RefreshAllSlots();
            if (hotbarController != null)
            {
                RefreshSelection(hotbarController.SelectedSlotIndex);
            }
        }
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    #endregion

    #region Event Subscription

    private void SubscribeEvents()
    {
        if (inventory != null)
        {
            inventory.OnInventoryChanged += RefreshAllSlots;
        }

        if (hotbarController != null)
        {
            hotbarController.OnSelectedSlotChanged += RefreshSelection;
        }
    }

    private void UnsubscribeEvents()
    {
        if (inventory != null)
        {
            inventory.OnInventoryChanged -= RefreshAllSlots;
        }

        if (hotbarController != null)
        {
            hotbarController.OnSelectedSlotChanged -= RefreshSelection;
        }
    }

    #endregion

    #region Initialization & Creation

    // Khởi tạo danh sách các ô Slot UI theo đúng số lượng hiệu dụng
    public void Initialize()
    {
        if (isInitialized) return;

        if (inventory == null)
        {
            Debug.LogError("[HotbarUI] Chưa gán tham chiếu Inventory vào Inspector!", this);
            return;
        }

        if (hotbarController == null)
        {
            Debug.LogError("[HotbarUI] Chưa gán tham chiếu HotbarController vào Inspector!", this);
            return;
        }

        if (slotContainer == null || slotPrefab == null)
        {
            Debug.LogError("[HotbarUI] Chưa gán Slot Container hoặc Slot Prefab vào Inspector!", this);
            return;
        }

        CreateSlots();
        isInitialized = true;

        RefreshAllSlots();
        RefreshSelection(hotbarController.SelectedSlotIndex);
    }

    // Xóa slot cũ (nếu có) và sinh các ô HotbarSlotUI mới
    private void CreateSlots()
    {
        foreach (Transform child in slotContainer)
        {
            Destroy(child.gameObject);
        }

        slotUIList.Clear();

        int count = Mathf.Min(hotbarSize, inventory.Capacity);

        for (int i = 0; i < count; i++)
        {
            HotbarSlotUI slotUI = Instantiate(slotPrefab, slotContainer);
            slotUI.gameObject.SetActive(true);
            slotUI.enabled = true;
            slotUI.EnsureActive();

            slotUI.Setup(i);
            slotUIList.Add(slotUI);
        }
    }

    #endregion

    #region UI Refresh

    // Cập nhật lại Icon và số lượng của tất cả các ô Hotbar từ 10 slot đầu của Inventory
    public void RefreshAllSlots()
    {
        if (inventory == null || slotUIList == null || slotUIList.Count == 0)
            return;

        for (int i = 0; i < slotUIList.Count; i++)
        {
            if (i < inventory.Capacity)
            {
                InventorySlot slotData = inventory.GetSlot(i);
                slotUIList[i].Refresh(slotData);
            }
        }
    }

    // Cập nhật trạng thái viền sáng ActiveImage cho ô đang được chọn
    public void RefreshSelection(int selectedIndex)
    {
        if (slotUIList == null) return;

        for (int i = 0; i < slotUIList.Count; i++)
        {
            slotUIList[i].SetSelected(i == selectedIndex);
        }
    }

    #endregion
}
