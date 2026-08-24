using UnityEngine;

// Script tiện ích hỗ trợ kiểm thử và demo các tính năng của hệ thống Inventory / Hotbar trong Unity Editor / Runtime.
// Thao tác trực tiếp vào ô Slot hiện đang ACTIVE (được chọn trên Hotbar).
public class InventoryTester : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("Tham chiếu tới Inventory chính.")]
    [SerializeField] private Inventory inventory;

    [Tooltip("Tham chiếu tới HotbarController để lấy ô đang active.")]
    [SerializeField] private HotbarController hotbarController;

    [Header("Item Test Data Assets")]
    [SerializeField] private ItemData dog;
    [SerializeField] private ItemData chicken;

    private void Awake()
    {
        // Tự động tìm kiếm nếu chưa gán tham chiếu trong Inspector
        if (inventory == null)
        {
            inventory = FindFirstObjectByType<Inventory>();
        }

        if (hotbarController == null)
        {
            hotbarController = FindFirstObjectByType<HotbarController>();
        }
    }

    private void Update()
    {
        // Nhấn phím Q: Thêm 10 Dog vào ô đang Active
        if (Input.GetKeyDown(KeyCode.Q))
        {
            AddDog();
        }

        // Nhấn phím W: Thêm 5 Chicken vào ô đang Active
        if (Input.GetKeyDown(KeyCode.W))
        {
            AddChicken();
        }

        // Nhấn phím R: Xóa 5 Item từ ô đang Active
        if (Input.GetKeyDown(KeyCode.R))
        {
            RemoveFromActiveSlot(5);
        }

        // Nhấn phím T: Thêm 90 Dog vào ô đang Active (Kiểm tra Stack)
        if (Input.GetKeyDown(KeyCode.T))
        {
            AddDog();
        }

        // Nhấn phím Y: Xóa 25 Item từ ô đang Active
        if (Input.GetKeyDown(KeyCode.Y))
        {
            RemoveFromActiveSlot(25);
        }

        // Nhấn phím U: Xóa sạch ô đang Active
        if (Input.GetKeyDown(KeyCode.U))
        {
            ClearActiveSlot();
        }
    }

    #region Context Menu & Public Test Actions

    [ContextMenu("Q. Add Dog x10 to Active Slot")]
    public void AddDog()
    {
        ExecuteAddItemToActiveSlot(dog, 1);
    }

    [ContextMenu("W. Add Chicken x5 to Active Slot")]
    public void AddChicken()
    {
        ExecuteAddItemToActiveSlot(chicken, 1);
    }


    [ContextMenu("R. Remove 5 Items from Active Slot")]
    public void RemoveDog()
    {
        RemoveFromActiveSlot(5);
    }

    [ContextMenu("T. Add Dog x1 (Stack Test) to Active Slot")]
    public void AddLargeDogStack()
    {
        ExecuteAddItemToActiveSlot(dog, 1);
    }

    [ContextMenu("Y. Remove 25 Items from Active Slot")]
    public void RemoveLargeDog()
    {
        RemoveFromActiveSlot(25);
    }

    [ContextMenu("U. Clear Active Slot")]
    public void ClearActiveSlot()
    {
        if (inventory == null) return;

        int activeIndex = GetActiveSlotIndex();
        InventorySlot slot = inventory.GetSlot(activeIndex);

        if (slot == null || slot.IsEmpty())
        {
            Debug.Log($"<color=yellow>[InventoryTester] Slot Hotbar [{activeIndex + 1}] (Index {activeIndex}) vốn đang rỗng.</color>");
            return;
        }

        string removedName = slot.ItemData.ItemName;
        int removedCount = slot.Amount;

        inventory.ClearSlot(activeIndex);
        Debug.Log($"<color=yellow>[InventoryTester] Đã xóa sạch Slot Hotbar [{activeIndex + 1}] (Index {activeIndex}) - Từng chứa {removedCount}x '{removedName}'.</color>");
    }

    [ContextMenu("0. Clear Entire Inventory")]
    public void ClearAll()
    {
        if (inventory == null) return;
        inventory.ClearAll();
        Debug.Log("<color=yellow>[InventoryTester] Đã dọn sạch toàn bộ Inventory.</color>");
    }

    #endregion

    #region Execution Helpers

    public void RemoveFromActiveSlot(int amount)
    {
        ExecuteRemoveItemFromActiveSlot(amount);
    }

    private int GetActiveSlotIndex()
    {
        if (hotbarController != null)
        {
            return hotbarController.SelectedSlotIndex;
        }

        return 0;
    }

    private void ExecuteAddItemToActiveSlot(ItemData item, int amount)
    {
        if (inventory == null)
        {
            Debug.LogError("[InventoryTester] Chưa gán tham chiếu Inventory!", this);
            return;
        }

        if (item == null)
        {
            Debug.LogError("[InventoryTester] ItemData đang bị null!", this);
            return;
        }

        int activeIndex = GetActiveSlotIndex();
        InventorySlot slot = inventory.GetSlot(activeIndex);

        if (slot == null)
        {
            Debug.LogError($"[InventoryTester] Không tìm thấy Slot tại index {activeIndex}!", this);
            return;
        }

        // Kiểm tra nếu ô đang chứa item loại khác
        if (!slot.IsEmpty() && slot.ItemData.ItemID != item.ItemID)
        {
            Debug.LogWarning($"<color=orange>[InventoryTester] Slot Hotbar [{activeIndex + 1}] (Index {activeIndex}) đang chứa '{slot.ItemData.ItemName}'. Không thể thêm '{item.ItemName}' vào ô này!</color>");
            return;
        }

        int remaining = inventory.AddItemToSlot(activeIndex, item, amount);
        int added = amount - remaining;

        if (added > 0)
        {
            Debug.Log($"<color=green>[InventoryTester] Thêm thành công {added}x '{item.ItemName}' vào Slot Hotbar [{activeIndex + 1}] (Index {activeIndex}). Số lượng hiện tại trong ô: {slot.Amount}/{slot.ItemData.MaxStack}.</color>");
        }

        if (remaining > 0)
        {
            Debug.LogWarning($"<color=orange>[InventoryTester] Còn dư {remaining}x '{item.ItemName}' do Slot Hotbar [{activeIndex + 1}] đã đạt MaxStack ({slot.ItemData.MaxStack}).</color>");
        }
    }

    private void ExecuteRemoveItemFromActiveSlot(int amount)
    {
        if (inventory == null)
        {
            Debug.LogError("[InventoryTester] Chưa gán tham chiếu Inventory!", this);
            return;
        }

        int activeIndex = GetActiveSlotIndex();
        InventorySlot slot = inventory.GetSlot(activeIndex);

        if (slot == null || slot.IsEmpty())
        {
            Debug.LogWarning($"<color=red>[InventoryTester] Thao tác trừ thất bại! Slot Hotbar [{activeIndex + 1}] (Index {activeIndex}) hiện đang rỗng.</color>");
            return;
        }

        string itemName = slot.ItemData.ItemName;
        int currentAmount = slot.Amount;
        int removed = inventory.RemoveItemFromSlot(activeIndex, amount);

        if (removed > 0)
        {
            int remainingInSlot = slot.IsEmpty() ? 0 : slot.Amount;
            Debug.Log($"<color=cyan>[InventoryTester] Đã trừ {removed}x '{itemName}' khỏi Slot Hotbar [{activeIndex + 1}] (Index {activeIndex}). Còn lại trong ô: {remainingInSlot}.</color>");
        }
    }

    #endregion
}
