using UnityEngine;

// Lớp tiện ích tĩnh hỗ trợ di chuyển và trao đổi vật phẩm an toàn (Atomic Transfers) giữa 2 Inventory bất kỳ.
// Đảm bảo không bao giờ bị mất hoặc nhân bản vật phẩm khi di chuyển qua lại giữa Túi đồ và Rương đồ.
public static class InventoryTransferHelper
{
    // Chuyển hoặc hoán đổi vật phẩm giữa 2 slot cụ thể của 2 kho khác nhau (hoặc cùng kho).
    // fromInv: Kho đồ nguồn
    // fromIndex: Vị trí slot nguồn
    // toInv: Kho đồ đích
    // toIndex: Vị trí slot đích
    // True nếu thực hiện thành công ít nhất một phần thao tác.
    public static bool TransferBetweenSlots(Inventory fromInv, int fromIndex, Inventory toInv, int toIndex)
    {
        if (fromInv == null || toInv == null) return false;
        if (!fromInv.IsValidIndex(fromIndex) || !toInv.IsValidIndex(toIndex)) return false;

        // Nếu cùng 1 kho và cùng 1 index thì bỏ qua
        if (fromInv == toInv && fromIndex == toIndex) return false;

        // Nếu cùng 1 kho, tận dụng logic MoveOrSwapSlots có sẵn
        if (fromInv == toInv)
        {
            return fromInv.MoveOrSwapSlots(fromIndex, toIndex);
        }

        InventorySlot fromSlot = fromInv.GetSlot(fromIndex);
        InventorySlot toSlot = toInv.GetSlot(toIndex);

        if (fromSlot == null || fromSlot.IsEmpty()) return false;

        ItemData itemA = fromSlot.ItemData;
        int amountA = fromSlot.Amount;

        // Trường hợp 1: Slot đích rỗng -> Chuyển toàn bộ sang slot đích
        if (toSlot == null || toSlot.IsEmpty())
        {
            fromInv.ClearSlot(fromIndex);
            toInv.AddItemToSlot(toIndex, itemA, amountA);
            return true;
        }

        // Trường hợp 2: Slot đích cùng loại Item và còn chỗ stack
        if (toSlot.CanStack(itemA))
        {
            int remaining = toInv.AddItemToSlot(toIndex, itemA, amountA);
            int moved = amountA - remaining;

            if (moved > 0)
            {
                fromInv.RemoveItemFromSlot(fromIndex, moved);
                return true;
            }
            return false;
        }

        // Trường hợp 3: Khác loại Item hoặc slot đích đã max stack -> Hoán đổi vị trí (Swap)
        ItemData itemB = toSlot.ItemData;
        int amountB = toSlot.Amount;

        fromInv.ClearSlot(fromIndex);
        toInv.ClearSlot(toIndex);

        toInv.AddItemToSlot(toIndex, itemA, amountA);
        fromInv.AddItemToSlot(fromIndex, itemB, amountB);

        return true;
    }

    // Chuyển nhanh 1 vật phẩm từ một slot nguồn sang kho đích (dùng cho Shift + Click).
    // Tự động tìm slot cùng loại để stack trước, sau đó tìm slot trống mới.
    // Trả về số lượng vật phẩm thực tế đã chuyển thành công.
    public static int QuickTransferItem(Inventory fromInv, int fromIndex, Inventory toInv)
    {
        if (fromInv == null || toInv == null || !fromInv.IsValidIndex(fromIndex)) return 0;

        InventorySlot fromSlot = fromInv.GetSlot(fromIndex);
        if (fromSlot == null || fromSlot.IsEmpty()) return 0;

        ItemData item = fromSlot.ItemData;
        int amount = fromSlot.Amount;

        // Thêm vào kho đích và nhận lại số lượng còn dư không thể chứa
        int remaining = toInv.AddItem(item, amount);
        int moved = amount - remaining;

        if (moved > 0)
        {
            fromInv.RemoveItemFromSlot(fromIndex, moved);
        }

        return moved;
    }

    // Lấy toàn bộ vật phẩm từ kho nguồn chuyển sang kho đích (Loot All).
    // Dừng lại khi kho đích đã đầy toàn bộ.
    // Trả về tổng số lượt chuyển thành công.
    public static int TransferAll(Inventory fromInv, Inventory toInv)
    {
        if (fromInv == null || toInv == null) return 0;

        int totalTransferred = 0;
        for (int i = 0; i < fromInv.Capacity; i++)
        {
            InventorySlot slot = fromInv.GetSlot(i);
            if (slot != null && !slot.IsEmpty())
            {
                int moved = QuickTransferItem(fromInv, i, toInv);
                if (moved > 0)
                {
                    totalTransferred += moved;
                }
            }
        }

        return totalTransferred;
    }
}
