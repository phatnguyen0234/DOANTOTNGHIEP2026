using UnityEngine;

// Interface chuẩn hóa cho tất cả các đối tượng trong thế giới game mà người chơi có thể tương tác (Click chuột phải).
// Ví dụ: Rương đồ (Chest), Bàn chế tạo (CraftingTable), Thùng giao hàng (ShippingBin), NPC, v.v.
public interface IInteractable
{
    // Thông điệp gợi ý hành động hiển thị cho người chơi (tùy chọn UI tooltip).
    string InteractionPrompt { get; }

    // Kiểm tra xem đối tượng tương tác (ví dụ: Player) có đủ điều kiện tương tác hay không (ví dụ: cự ly, trạng thái).
    bool CanInteract(GameObject interactor);

    // Thực hiện hành vi tương tác chính (ví dụ: mở rương, mở giao diện, nói chuyện).
    void Interact(GameObject interactor);
}
