using System;
using System.Collections.Generic;
using UnityEngine;

// Quản lý dữ liệu và logic nghiệp vụ của Inventory (thêm, bớt, stack, kiểm tra vật phẩm).
// Phát ra sự kiện OnInventoryChanged khi có bất kỳ thay đổi nào trong kho đồ.
public class Inventory : MonoBehaviour
{
    [Header("Inventory Settings")]
    [Tooltip("Số lượng ô chứa tối đa của Inventory.")]
    [SerializeField, Min(1)] private int capacity = 20;

    [Tooltip("Danh sách các ô chứa trong Inventory.")]
    [SerializeField] private List<InventorySlot> slots = new List<InventorySlot>();

    // Sự kiện được kích hoạt mỗi khi trạng thái kho đồ thay đổi (thêm, xóa, stack, clear item).
    // UI hoặc các hệ thống khác sẽ đăng ký lắng nghe sự kiện này để cập nhật tương ứng.
    public event Action OnInventoryChanged;

    #region Properties

    public int Capacity => capacity;
    public IReadOnlyList<InventorySlot> Slots => slots;

    #endregion

    #region Unity Lifecycle & Initialization

    private void Awake()
    {
        InitializeSlots();
    }

    private void OnValidate()
    {
        if (capacity < 1) capacity = 1;
        InitializeSlots();
    }

    // Khởi tạo và đảm bảo số lượng ô trong danh sách luôn khớp chính xác với capacity.
    private void InitializeSlots()
    {
        if (slots == null)
        {
            slots = new List<InventorySlot>();
        }

        while (slots.Count < capacity)
        {
            slots.Add(new InventorySlot());
        }

        while (slots.Count > capacity)
        {
            slots.RemoveAt(slots.Count - 1);
        }
    }

    #endregion

    #region Public Query APIs

    // Kiểm tra xem chỉ số index có nằm trong phạm vi ô hợp lệ của Inventory hay không.
    public bool IsValidIndex(int index)
    {
        return slots != null && index >= 0 && index < slots.Count;
    }

    // Lấy tham chiếu đến InventorySlot tại vị trí index chỉ định.
    // Chỉ số của slot (0 -> capacity - 1).
    // Đối tượng InventorySlot hoặc null nếu chỉ số không hợp lệ.
    public InventorySlot GetSlot(int index)
    {
        if (index < 0 || index >= slots.Count)
        {
            Debug.LogWarning($"[Inventory] Index {index} nằm ngoài phạm vi dung lượng ({slots.Count}).", this);
            return null;
        }

        return slots[index];
    }

    // Đếm tổng số lượng của một Item cụ thể hiện có trong toàn bộ Inventory.
    // ItemData cần đếm.
    // Tổng số lượng item tìm thấy.
    public int GetItemCount(ItemData item)
    {
        if (item == null) return 0;

        int total = 0;
        for (int i = 0; i < slots.Count; i++)
        {
            InventorySlot slot = slots[i];
            if (!slot.IsEmpty() && slot.ItemData.ItemID == item.ItemID)
            {
                total += slot.Amount;
            }
        }

        return total;
    }

    // Kiểm tra xem Inventory có chứa đủ số lượng của Item yêu cầu hay không.
    // ItemData cần kiểm tra.
    // Số lượng yêu cầu.
    // True nếu có đủ hoặc nhiều hơn số lượng yêu cầu.
    public bool HasItem(ItemData item, int amount)
    {
        if (item == null || amount <= 0) return false;
        return GetItemCount(item) >= amount;
    }

    // Kiểm tra xem Inventory có đủ chỗ trống để chứa toàn bộ số lượng Item này hay không.
    // ItemData cần kiểm tra.
    // Số lượng muốn thêm.
    // True nếu còn đủ chỗ chứa toàn bộ.
    public bool CanAddItem(ItemData item, int amount)
    {
        if (item == null || amount <= 0) return false;

        int remainingNeeded = amount;

        // 1. Kiểm tra không gian còn lại trên các ô chứa cùng loại
        for (int i = 0; i < slots.Count; i++)
        {
            InventorySlot slot = slots[i];
            if (slot.CanStack(item))
            {
                remainingNeeded -= slot.RemainingSpace();
                if (remainingNeeded <= 0) return true;
            }
        }

        // 2. Kiểm tra các ô trống
        for (int i = 0; i < slots.Count; i++)
        {
            InventorySlot slot = slots[i];
            if (slot.IsEmpty())
            {
                remainingNeeded -= item.MaxStack;
                if (remainingNeeded <= 0) return true;
            }
        }

        return remainingNeeded <= 0;
    }

    #endregion

    #region Add / Remove Item Business Logic

    // Thêm Item vào Inventory theo đúng thứ tự nghiệp vụ:
    // 1. Tìm các slot đang chứa cùng Item để stack trước.
    // 2. Nếu vẫn còn dư, tìm các slot rỗng (Empty Slot) để tạo stack mới.
    // 3. Nếu hết slot, dừng lại và trả về kết quả.
    // Kích hoạt sự kiện OnInventoryChanged nếu có bất kỳ vật phẩm nào được thêm thành công.
    // ItemData cần thêm.
    // Số lượng cần thêm.
    // True nếu đã thêm thành công toàn bộ số lượng yêu cầu mà không bị dư.
    public bool TryAddItem(ItemData item, int amount)
    {
        if (item == null || amount <= 0)
        {
            Debug.LogWarning("[Inventory] Thao tác TryAddItem thất bại do ItemData null hoặc amount <= 0.", this);
            return false;
        }

        int remainingAmount = amount;

        // Bước 1: Ưu tiên xếp chồng (stack) vào các slot cũ đang chứa cùng loại Item
        for (int i = 0; i < slots.Count; i++)
        {
            InventorySlot slot = slots[i];
            if (slot.CanStack(item))
            {
                remainingAmount = slot.AddAmount(remainingAmount);
                if (remainingAmount <= 0)
                    break;
            }
        }

        // Bước 2: Nếu vẫn còn dư, tìm các slot rỗng (Empty Slot) để tạo stack mới
        if (remainingAmount > 0)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                InventorySlot slot = slots[i];
                if (slot.IsEmpty())
                {
                    int amountToAddInNewSlot = Mathf.Min(remainingAmount, item.MaxStack);
                    slot.SetItem(item, amountToAddInNewSlot);
                    remainingAmount -= amountToAddInNewSlot;

                    if (remainingAmount <= 0)
                        break;
                }
            }
        }

        // Bước 3: Phát sự kiện nếu có ít nhất 1 item được thêm vào thành công
        int addedCount = amount - remainingAmount;
        if (addedCount > 0)
        {
            OnInventoryChanged?.Invoke();
        }

        // Trả về true nếu toàn bộ số lượng đã được thêm hết vào kho
        return remainingAmount == 0;
    }

    // Thêm Item và trả về số lượng còn dư không thể chứa hết trong kho đồ (hữu ích cho việc drop item ra đất khi đầy kho).
    // ItemData cần thêm.
    // Số lượng cần thêm.
    // Số lượng còn dư (0 nếu thêm thành công toàn bộ).
    public int AddItem(ItemData item, int amount)
    {
        if (item == null || amount <= 0) return amount;

        int remainingAmount = amount;

        // 1. Stack vào ô cũ
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].CanStack(item))
            {
                remainingAmount = slots[i].AddAmount(remainingAmount);
                if (remainingAmount <= 0) break;
            }
        }

        // 2. Tạo ô mới
        if (remainingAmount > 0)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].IsEmpty())
                {
                    int addCount = Mathf.Min(remainingAmount, item.MaxStack);
                    slots[i].SetItem(item, addCount);
                    remainingAmount -= addCount;
                    if (remainingAmount <= 0) break;
                }
            }
        }

        if (amount - remainingAmount > 0)
        {
            OnInventoryChanged?.Invoke();
        }

        return remainingAmount;
    }

    // Xóa một số lượng Item khỏi Inventory theo nguyên tắc Atomic:
    // - Kiểm tra HasItem trước. Nếu không đủ tổng số lượng, không trừ bất kỳ slot nào và trả về false.
    // - Nếu đủ, duyệt trừ dần qua các ô chứa item và kích hoạt OnInventoryChanged.
    // ItemData cần xóa.
    // Số lượng cần xóa.
    // True nếu xóa thành công, False nếu không đủ số lượng hoặc dữ liệu không hợp lệ.
    public bool TryRemoveItem(ItemData item, int amount)
    {
        if (item == null || amount <= 0)
        {
            Debug.LogWarning("[Inventory] Thao tác TryRemoveItem thất bại do ItemData null hoặc amount <= 0.", this);
            return false;
        }

        // Kiểm tra điều kiện tiên quyết: phải có đủ tổng số lượng
        if (!HasItem(item, amount))
        {
            return false;
        }

        int remainingToRemove = amount;

        // Duyệt qua các slot chứa item đó và trừ dần
        for (int i = 0; i < slots.Count; i++)
        {
            InventorySlot slot = slots[i];
            if (!slot.IsEmpty() && slot.ItemData.ItemID == item.ItemID)
            {
                int removed = slot.RemoveAmount(remainingToRemove);
                remainingToRemove -= removed;

                if (remainingToRemove <= 0)
                {
                    break;
                }
            }
        }

        // Kích hoạt sự kiện cập nhật UI
        OnInventoryChanged?.Invoke();
        return true;
    }

    // Thêm Item trực tiếp vào một ô chỉ định.
    // Trả về số lượng còn dư không thể thêm (0 nếu đã thêm hết).
    public int AddItemToSlot(int slotIndex, ItemData item, int amount)
    {
        if (item == null || amount <= 0 || !IsValidIndex(slotIndex))
            return amount;

        InventorySlot slot = slots[slotIndex];
        int remaining = amount;

        if (slot.IsEmpty())
        {
            int addCount = Mathf.Min(amount, item.MaxStack);
            slot.SetItem(item, addCount);
            remaining -= addCount;
            OnInventoryChanged?.Invoke();
        }
        else if (slot.CanStack(item))
        {
            remaining = slot.AddAmount(amount);
            if (remaining < amount)
            {
                OnInventoryChanged?.Invoke();
            }
        }
        else
        {
            Debug.LogWarning($"[Inventory] Slot {slotIndex} đang chứa vật phẩm khác ({slot.ItemData.ItemName}) hoặc đã đầy stack ({slot.Amount}/{slot.ItemData.MaxStack}).", this);
        }

        return remaining;
    }

    // Xóa một số lượng Item trực tiếp từ một ô chỉ định.
    // Trả về số lượng thực tế đã trừ.
    public int RemoveItemFromSlot(int slotIndex, int amount)
    {
        if (amount <= 0 || !IsValidIndex(slotIndex))
            return 0;

        InventorySlot slot = slots[slotIndex];
        if (slot.IsEmpty())
            return 0;

        int removed = slot.RemoveAmount(amount);
        if (removed > 0)
        {
            OnInventoryChanged?.Invoke();
        }

        return removed;
    }

    // Xóa sạch vật phẩm tại một ô chỉ định.
    public void ClearSlot(int slotIndex)
    {
        if (!IsValidIndex(slotIndex)) return;

        if (!slots[slotIndex].IsEmpty())
        {
            slots[slotIndex].Clear();
            OnInventoryChanged?.Invoke();
        }
    }

    // Xóa toàn bộ vật phẩm trong tất cả các ô của Inventory.
    public void ClearAll()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            slots[i].Clear();
        }

        OnInventoryChanged?.Invoke();
    }

    // Di chuyển hoặc hoán đổi vị trí (Swap) giữa hai ô trong kho đồ:
    // - Nếu ô đích rỗng: Di chuyển vật phẩm từ ô nguồn sang ô đích.
    // - Nếu ô đích chứa cùng loại vật phẩm và có thể stack: Xếp chồng vào ô đích, phần dư (nếu có) giữ lại ở ô nguồn.
    // - Nếu ô đích chứa vật phẩm khác (hoặc cùng loại nhưng đã đầy stack): Đổi chỗ (Swap) 2 vật phẩm cho nhau.
    public bool MoveOrSwapSlots(int fromIndex, int toIndex)
    {
        if (!IsValidIndex(fromIndex) || !IsValidIndex(toIndex) || fromIndex == toIndex)
            return false;

        InventorySlot fromSlot = slots[fromIndex];
        InventorySlot toSlot = slots[toIndex];

        if (fromSlot.IsEmpty())
            return false;

        // Trường hợp 1: Ô đích rỗng -> Di chuyển toàn bộ sang ô đích
        if (toSlot.IsEmpty())
        {
            toSlot.SetItem(fromSlot.ItemData, fromSlot.Amount);
            fromSlot.Clear();
            OnInventoryChanged?.Invoke();
            return true;
        }

        // Trường hợp 2: Cùng loại item và còn khoảng trống để stack
        if (toSlot.CanStack(fromSlot.ItemData))
        {
            int remaining = toSlot.AddAmount(fromSlot.Amount);
            if (remaining <= 0)
            {
                fromSlot.Clear();
            }
            else
            {
                fromSlot.SetItem(fromSlot.ItemData, remaining);
            }

            OnInventoryChanged?.Invoke();
            return true;
        }

        // Trường hợp 3: Khác loại item hoặc ô đích đã max stack -> Đổi chỗ 2 vật phẩm (Swap)
        fromSlot.SwapWith(toSlot);
        OnInventoryChanged?.Invoke();
        return true;
    }

    // Hoán đổi trực tiếp vị trí vật phẩm giữa hai ô bất kỳ.
    public bool SwapSlots(int indexA, int indexB)
    {
        if (!IsValidIndex(indexA) || !IsValidIndex(indexB) || indexA == indexB)
            return false;

        slots[indexA].SwapWith(slots[indexB]);
        OnInventoryChanged?.Invoke();
        return true;
    }

    #endregion
}
