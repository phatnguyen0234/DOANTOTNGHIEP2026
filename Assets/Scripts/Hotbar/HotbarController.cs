using System;
using UnityEngine;

// Quản lý trạng thái lựa chọn ô Hotbar và xử lý input bàn phím (phím 1 đến 0).
// Cung cấp dữ liệu ô đang chọn (SelectedSlot) từ Inventory cho các hệ thống Gameplay khác sử dụng.
public class HotbarController : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("Tham chiếu tới Inventory chính của Player.")]
    [SerializeField] private Inventory inventory;

    [Header("Hotbar Settings")]
    [Tooltip("Số lượng ô Hotbar hiển thị.")]
    [SerializeField, Min(1)] private int hotbarSize = 10;

    // Danh sách phím tắt tương ứng cho từng slot (Index 0 -> Phím 1, Index 9 -> Phím 0)
    private static readonly KeyCode[] HotbarKeys = new KeyCode[]
    {
        KeyCode.Alpha1, // Slot 0
        KeyCode.Alpha2, // Slot 1
        KeyCode.Alpha3, // Slot 2
        KeyCode.Alpha4, // Slot 3
        KeyCode.Alpha5, // Slot 4
        KeyCode.Alpha6, // Slot 5
        KeyCode.Alpha7, // Slot 6
        KeyCode.Alpha8, // Slot 7
        KeyCode.Alpha9, // Slot 8
        KeyCode.Alpha0  // Slot 9
    };

    public int SelectedSlotIndex { get; private set; } = 0;

    // Lấy trực tiếp ô InventorySlot hiện đang được chọn từ Inventory.
    public InventorySlot SelectedSlot
    {
        get
        {
            if (inventory == null) return null;
            return inventory.GetSlot(SelectedSlotIndex);
        }
    }

    // Sự kiện phát ra khi chỉ số slot được chọn thay đổi.
    public event Action<int> OnSelectedSlotChanged;

    private void Start()
    {
        if (inventory == null)
        {
            Debug.LogError("[HotbarController] Chưa gán tham chiếu Inventory vào Inspector!", this);
            return;
        }

        // Khởi tạo trạng thái mặc định chọn ô đầu tiên (Slot 0)
        SelectSlot(0);
    }

    private void Update()
    {
        HandleKeyboardInput();
    }

    // Duyệt qua danh sách phím số để chuyển đổi slot
    private void HandleKeyboardInput()
    {
        int effectiveSize = GetEffectiveHotbarSize();
        int maxKeysToCheck = Mathf.Min(HotbarKeys.Length, effectiveSize);

        for (int i = 0; i < maxKeysToCheck; i++)
        {
            if (Input.GetKeyDown(HotbarKeys[i]))
            {
                SelectSlot(i);
                break;
            }
        }
    }

    // Chọn một slot theo index chỉ định.
    public void SelectSlot(int index)
    {
        int effectiveSize = GetEffectiveHotbarSize();

        // Không cho phép index âm hoặc vượt quá số slot thực tế
        if (index < 0 || index >= effectiveSize)
        {
            return;
        }

        // Nếu chọn đúng ô đang active thì không cần phát lại event
        if (index == SelectedSlotIndex && SelectedSlotIndex != -1)
        {
            return;
        }

        SelectedSlotIndex = index;
        OnSelectedSlotChanged?.Invoke(SelectedSlotIndex);
    }

    // Tính toán số lượng slot thực tế khả dụng của Hotbar
    public int GetEffectiveHotbarSize()
    {
        if (inventory == null) return hotbarSize;
        return Mathf.Min(hotbarSize, inventory.Capacity);
    }
}
