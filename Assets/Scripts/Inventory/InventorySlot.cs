using System;
using UnityEngine;

// Đại diện cho dữ liệu runtime của một ô chứa trong Inventory.
// Quản lý dữ liệu ItemData hiện tại và số lượng amount trong ô.
[Serializable]
public class InventorySlot
{
    [SerializeField] private ItemData itemData;
    [SerializeField] private int amount;

    #region Constructors

    public InventorySlot()
    {
        Clear();
    }

    public InventorySlot(ItemData itemData, int amount)
    {
        if (itemData != null && amount > 0)
        {
            this.itemData = itemData;
            this.amount = Mathf.Min(amount, itemData.MaxStack);
        }
        else
        {
            Clear();
        }
    }

    #endregion

    #region Properties

    public ItemData ItemData => itemData;
    public int Amount => amount;

    #endregion

    #region Core Slot Business Logic

    // Kiểm tra xem slot có đang rỗng không.
    public bool IsEmpty()
    {
        return itemData == null || amount <= 0;
    }

    // Kiểm tra xem slot hiện tại có thể xếp chồng (stack) thêm Item này hay không.
    // Điều kiện: Slot không rỗng, đúng loại Item, và chưa đạt maxStack.
    public bool CanStack(ItemData item)
    {
        if (IsEmpty() || item == null || itemData == null)
            return false;

        return itemData.ItemID == item.ItemID && amount < itemData.MaxStack;
    }

    // Trả về khoảng trống còn lại có thể chứa thêm trong slot hiện tại.
    // Nếu slot rỗng, trả về 0 (chưa có ItemData để xác định maxStack).
    public int RemainingSpace()
    {
        if (IsEmpty() || itemData == null)
            return 0;

        return Mathf.Max(0, itemData.MaxStack - amount);
    }

    // Thêm một số lượng vào slot hiện tại (tối đa tới maxStack).
    // Số lượng muốn thêm.
    // Số lượng còn dư KHÔNG THỂ thêm vào slot (0 nếu đã thêm hết).
    public int AddAmount(int amountToAdd)
    {
        if (amountToAdd <= 0)
            return 0;

        if (IsEmpty() || itemData == null)
            return amountToAdd;

        int space = RemainingSpace();
        int added = Mathf.Min(space, amountToAdd);

        amount += added;

        return amountToAdd - added;
    }

    // Xóa/trừ một số lượng item từ slot này.
    // Nếu số lượng về 0 hoặc nhỏ hơn, slot sẽ tự động Clear.
    // Số lượng cần trừ.
    // Số lượng item thực tế đã được trừ từ slot.
    public int RemoveAmount(int amountToRemove)
    {
        if (IsEmpty() || amountToRemove <= 0)
            return 0;

        int removed = Mathf.Min(amount, amountToRemove);
        amount -= removed;

        if (amount <= 0)
        {
            Clear();
        }

        return removed;
    }

    // Đặt mới dữ liệu cho một ô (thường dùng khi gán item vào slot rỗng).
    public void SetItem(ItemData newItem, int newAmount)
    {
        if (newItem == null || newAmount <= 0)
        {
            Clear();
            return;
        }

        itemData = newItem;
        amount = Mathf.Min(newAmount, newItem.MaxStack);
    }

    // Đổi chỗ toàn bộ dữ liệu vật phẩm với một ô khác.
    public void SwapWith(InventorySlot other)
    {
        if (other == null) return;

        ItemData tempItem = this.itemData;
        int tempAmount = this.amount;

        this.itemData = other.itemData;
        this.amount = other.amount;

        other.itemData = tempItem;
        other.amount = tempAmount;
    }

    // Xóa toàn bộ dữ liệu trong ô, đưa về trạng thái rỗng.
    public void Clear()
    {
        itemData = null;
        amount = 0;
    }

    #endregion
}
