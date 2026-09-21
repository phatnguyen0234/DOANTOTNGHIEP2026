using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Quản lý hiển thị trực quan của một ô Hotbar đơn lẻ.
// Đảm bảo tất cả các GameObject/Component con luôn ở trạng thái Active và Enabled khi runtime.
// Riêng SelectedFrame (activeImage) giữ GameObject luôn Active, chỉ bật/tắt component Image.enabled theo isSelected.
public class HotbarSlotUI : MonoBehaviour, IPointerClickHandler
{
    [Header("UI Bindings")]
    [Tooltip("Image nền của ô Hotbar (Background) - LUÔN LUÔN Active và Enabled.")]
    [SerializeField] private Image backgroundImage;

    [Tooltip("Image hiển thị Icon của Item - LUÔN LUÔN Active và Enabled.")]
    [SerializeField] private Image iconImage;

    [Tooltip("TextMeshPro hiển thị số lượng Stack của Item - LUÔN LUÔN Active và Enabled.")]
    [SerializeField] private TextMeshProUGUI amountText;

    [Tooltip("Image viền sáng hiển thị khi ô đang được chọn (SelectedFrame) - GameObject LUÔN Active, chỉ Image.enabled bật/tắt.")]
    [SerializeField] private Image activeImage;

    private int slotIndex = -1;
    private bool isSelected = false;

    public int SlotIndex => slotIndex;
    public bool IsSelected => isSelected;
    public Image BackgroundImage => backgroundImage;
    public Image IconImage => iconImage;
    public TextMeshProUGUI AmountText => amountText;
    public Image ActiveImage => activeImage;

    private void Awake()
    {
        ValidateAndResolveReferences();
        EnsureActive();
    }

    private void OnEnable()
    {
        EnsureActive();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ValidateAndResolveReferences();
    }
#endif

    // Xác thực và tự động liên kết (Auto-resolve) các component con theo Hierarchy
    public void ValidateAndResolveReferences()
    {
        // 1. Tự động tìm kiếm nếu thiếu tham chiếu
        if (backgroundImage == null)
        {
            Transform bgTrans = transform.Find("Background");
            if (bgTrans != null) backgroundImage = bgTrans.GetComponent<Image>();
        }

        if (iconImage == null)
        {
            Transform iconTrans = transform.Find("Icon");
            if (iconTrans != null) iconImage = iconTrans.GetComponent<Image>();
        }

        if (amountText == null)
        {
            Transform countTrans = transform.Find("AmountText") ?? transform.Find("Count");
            if (countTrans != null) amountText = countTrans.GetComponent<TextMeshProUGUI>();
        }

        if (activeImage == null)
        {
            Transform selTrans = transform.Find("SelectedFrame") ?? transform.Find("ActiveFrame");
            if (selTrans != null) activeImage = selTrans.GetComponent<Image>();
        }

        // 2. Bảo vệ đặc biệt: Tránh trường hợp activeImage trỏ nhầm vào Background
        if (backgroundImage == null && activeImage != null && activeImage.gameObject.name.ToLower().Contains("background"))
        {
            Debug.LogWarning($"[HotbarSlotUI] '{gameObject.name}': activeImage đang trỏ vào GameObject '{activeImage.gameObject.name}'. Đang tự động chuyển sang backgroundImage!", this);
            backgroundImage = activeImage;
            Transform selTrans = transform.Find("SelectedFrame") ?? transform.Find("ActiveFrame");
            activeImage = selTrans != null ? selTrans.GetComponent<Image>() : null;
        }

        // 3. Kiểm tra trùng lặp reference (Không cho phép dùng chung Image)
        if (activeImage != null && backgroundImage != null && activeImage == backgroundImage)
        {
            Debug.LogError($"[HotbarSlotUI] CẢNH BÁO BINDING: '{gameObject.name}' có activeImage trùng với backgroundImage! Hãy gán activeImage vào SelectedFrame.", this);
            Transform selTrans = transform.Find("SelectedFrame") ?? transform.Find("ActiveFrame");
            if (selTrans != null && selTrans.TryGetComponent<Image>(out var selImg))
            {
                activeImage = selImg;
                Debug.LogWarning($"[HotbarSlotUI] '{gameObject.name}': Đã tự động tách activeImage sang child SelectedFrame.", this);
            }
            else
            {
                // Ngắt gán activeImage để ngăn chặn việc SetSelected(false) làm tắt Background Image
                activeImage = null;
            }
        }

        if (iconImage != null && backgroundImage != null && iconImage == backgroundImage)
        {
            Debug.LogError($"[HotbarSlotUI] CẢNH BÁO BINDING: '{gameObject.name}' có iconImage trùng với backgroundImage!", this);
        }

        if (activeImage != null && iconImage != null && activeImage == iconImage)
        {
            Debug.LogError($"[HotbarSlotUI] CẢNH BÁO BINDING: '{gameObject.name}' có activeImage trùng với iconImage!", this);
        }
    }

    // Thiết lập chỉ số slot ban đầu khi Instantiate prefab.
    public void Setup(int index)
    {
        slotIndex = index;
        ValidateAndResolveReferences();
        EnsureActive();
        SetSelected(false);
    }

    // Đảm bảo GameObject này và toàn bộ thành phần con luôn Active và Enabled khi runtime.
    // Riêng ActiveImage (SelectedFrame): GameObject LUÔN Active, chỉ Image.enabled bật/tắt theo isSelected.
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

        // 1. Background: GameObject LUÔN Active, Image.enabled LUÔN = true
        if (backgroundImage != null)
        {
            if (!backgroundImage.gameObject.activeSelf) backgroundImage.gameObject.SetActive(true);
            if (!backgroundImage.enabled) backgroundImage.enabled = true;
        }

        // 2. Icon: GameObject LUÔN Active, Image.enabled LUÔN = true
        if (iconImage != null)
        {
            if (!iconImage.gameObject.activeSelf) iconImage.gameObject.SetActive(true);
            if (!iconImage.enabled) iconImage.enabled = true;
            iconImage.color = Color.white;
        }

        // 3. AmountText / Count: GameObject LUÔN Active, TextMeshProUGUI LUÔN Enabled
        if (amountText != null)
        {
            if (!amountText.gameObject.activeSelf) amountText.gameObject.SetActive(true);
            if (!amountText.enabled) amountText.enabled = true;
        }

        // 4. SelectedFrame: GameObject LUÔN Active, Image.enabled phụ thuộc isSelected
        if (activeImage != null)
        {
            if (!activeImage.gameObject.activeSelf)
            {
                activeImage.gameObject.SetActive(true);
            }
            activeImage.enabled = isSelected;
        }
    }

    // Làm mới dữ liệu hiển thị (Icon, Số lượng) từ InventorySlot.
    // Tuyệt đối không tắt (enabled = false hoặc SetActive false) Background, Icon hay AmountText.
    public void Refresh(InventorySlot slot)
    {
        EnsureActive();

        // TRƯỜNG HỢP 1: Ô RỖNG (Empty Slot / Không có vật phẩm)
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

        // TRƯỜNG HỢP 2: Ô CÓ CHỨA ITEM (Có dữ liệu)
        if (iconImage != null)
        {
            iconImage.sprite = slot.ItemData != null ? slot.ItemData.Icon : null;
            iconImage.color = Color.white;
        }

        if (amountText != null)
        {
            amountText.text = slot.Amount > 1 ? slot.Amount.ToString() : string.Empty;
        }
    }

    // Bật/tắt trạng thái lựa chọn ô.
    // SelectedFrame GameObject LUÔN Active, chỉ bật/tắt component Image.
    public void SetSelected(bool selected)
    {
        isSelected = selected;

        if (activeImage != null)
        {
            if (!activeImage.gameObject.activeSelf)
            {
                activeImage.gameObject.SetActive(true);
            }
            activeImage.enabled = selected;
        }
    }

    // Xử lý khi người chơi click chuột vào ô Hotbar này để chọn
    public void OnPointerClick(PointerEventData eventData)
    {
        if (slotIndex >= 0)
        {
            HotbarController controller = FindAnyObjectByType<HotbarController>();
            if (controller != null)
            {
                controller.SelectSlot(slotIndex);
            }
        }
    }
}
