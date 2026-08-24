using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Quản lý hiển thị trực quan của một ô Hotbar đơn lẻ.
// Đảm bảo tất cả các GameObject/Component con luôn ở trạng thái Active khi runtime.
// Riêng ActiveImage chỉ được Active và Enabled khi ô đó đang được chọn (Selected).
public class HotbarSlotUI : MonoBehaviour
{
    [Header("UI Bindings")]
    [Tooltip("Image hiển thị Icon của Item.")]
    [SerializeField] private Image iconImage;

    [Tooltip("TextMeshPro hiển thị số lượng Stack của Item.")]
    [SerializeField] private TextMeshProUGUI amountText;

    [Tooltip("Image viền sáng hiển thị khi ô đang được chọn.")]
    [SerializeField] private Image activeImage;

    private int slotIndex = -1;
    private bool isSelected = false;

    public int SlotIndex => slotIndex;
    public bool IsSelected => isSelected;

    private void Awake()
    {
        EnsureActive();
    }

    private void OnEnable()
    {
        EnsureActive();
    }

    // Thiết lập chỉ số slot ban đầu khi Instantiate prefab.
    public void Setup(int index)
    {
        slotIndex = index;
        EnsureActive();
        SetSelected(false);
    }

    // Đảm bảo GameObject này và toàn bộ thành phần con luôn Active và Enabled khi runtime.
    // Riêng ActiveImage chỉ được active theo trạng thái isSelected.
    public void EnsureActive()
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        if (!enabled)
        {
            enabled = true;
        }

        if (iconImage != null)
        {
            if (!iconImage.gameObject.activeSelf) iconImage.gameObject.SetActive(true);
            if (!iconImage.enabled) iconImage.enabled = true;
            iconImage.color = Color.white;
        }

        if (amountText != null)
        {
            if (!amountText.gameObject.activeSelf) amountText.gameObject.SetActive(true);
            if (!amountText.enabled) amountText.enabled = true;
        }

        // ActiveImage chỉ active và enabled khi ô này đang được chọn
        if (activeImage != null)
        {
            if (activeImage.gameObject != gameObject)
            {
                activeImage.gameObject.SetActive(isSelected);
            }
            activeImage.enabled = isSelected;
        }
    }

    // Làm mới dữ liệu hiển thị (Icon, Số lượng) từ InventorySlot
    public void Refresh(InventorySlot slot)
    {
        EnsureActive();

        // ----------------------------------------------------
        // TRƯỜNG HỢP 1: Ô RỖNG (Empty Slot / Không có vật phẩm)
        // ----------------------------------------------------
        if (slot == null || slot.IsEmpty())
        {
            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.color = Color.white;
            }

            if (amountText != null)
            {
                amountText.text = string.Empty;
            }

            return;
        }

        // ----------------------------------------------------
        // TRƯỜNG HỢP 2: Ô CÓ CHỨA ITEM (Có dữ liệu)
        // ----------------------------------------------------
        if (iconImage != null)
        {
            iconImage.sprite = slot.ItemData.Icon;
            iconImage.color = Color.white;
        }

        if (amountText != null)
        {
            amountText.text = slot.Amount > 1 ? slot.Amount.ToString() : string.Empty;
        }
    }

    // Bật/tắt hiệu ứng viền sáng khi được chọn
    public void SetSelected(bool selected)
    {
        isSelected = selected;

        if (activeImage != null)
        {
            if (activeImage.gameObject != gameObject)
            {
                activeImage.gameObject.SetActive(selected);
            }
            activeImage.enabled = selected;
        }
    }
}
