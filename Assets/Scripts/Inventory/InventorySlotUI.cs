using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Đại diện cho thành phần hiển thị giao diện UI của một ô Inventory đơn lẻ.
// Hỗ trợ sự kiện Kéo Thả (Drag & Drop) và bắt Raycast con trỏ chuột.
// Tự động kiểm tra raycastTarget, CanvasGroup, EventSystem để gỡ lỗi khi không tương tác được.
public class InventorySlotUI : MonoBehaviour, IPointerDownHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [Header("UI Component Bindings")]
    [Tooltip("Image hiển thị Icon của Item.")]
    [SerializeField] private Image iconImage;

    [Tooltip("TextMeshProUGUI hiển thị số lượng Stack của Item.")]
    [SerializeField] private TextMeshProUGUI amountText;

    [Tooltip("Đối tượng trực quan hiển thị khi ô đang rỗng (tùy chọn).")]
    [SerializeField] private GameObject emptyStateVisual;

    [Header("Drag & Drop Settings")]
    [Tooltip("CanvasGroup điều khiển độ mờ khi đang kéo (tự động thêm nếu chưa có).")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Debug Settings")]
    [Tooltip("Bật log chi tiết cho các sự kiện con trỏ và kéo thả của ô này.")]
    [SerializeField] private bool enableDebugLogs = true;

    private int slotIndex = -1;
    private InventoryUI parentUI;
    private bool isDragging = false;
    private Image slotBackgroundImage;

    public int SlotIndex => slotIndex;

    private void Awake()
    {
        EnsureActive();
    }

    private void OnEnable()
    {
        EnsureActive();
    }

    // Thiết lập chỉ số slot và tham chiếu tới InventoryUI cha.
    public void Setup(int index, InventoryUI ui = null)
    {
        slotIndex = index;
        parentUI = ui != null ? ui : GetComponentInParent<InventoryUI>();
        EnsureActive();
    }

    // Đảm bảo GameObject này và các thành phần con luôn luôn Active, Enabled và không bị ẩn alpha.
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

        // Đảm bảo có Image trên chính ô Slot để bắt được Raycast (tự bổ sung Image trong suốt nếu thiếu)
        slotBackgroundImage = GetComponent<Image>();
        if (slotBackgroundImage == null)
        {
            slotBackgroundImage = gameObject.AddComponent<Image>();
            slotBackgroundImage.color = Color.clear;
        }
        if (!slotBackgroundImage.enabled) slotBackgroundImage.enabled = true;
        slotBackgroundImage.raycastTarget = true;

        // BẮT BUỘC: canvasGroup phải thuộc chính GameObject của ô Slot này, không trỏ nhầm sang Parent/Panel
        if (canvasGroup == null || canvasGroup.gameObject != gameObject)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (canvasGroup != null && !isDragging)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }

        // Tự động kiểm tra CanvasGroup cha để bỏ chặn Raycast nếu bị tắt nhầm trên Panel
        CanvasGroup parentCG = GetComponentInParent<CanvasGroup>();
        if (parentCG != null && parentCG.gameObject != gameObject && !parentCG.blocksRaycasts && !isDragging)
        {
            Debug.LogWarning($"[InventorySlotUI] CanvasGroup cha trên '{parentCG.gameObject.name}' đang bị tắt blocksRaycasts! Tự động kích hoạt lại raycast.", this);
            parentCG.blocksRaycasts = true;
            parentCG.interactable = true;
        }

        // Tự động kiểm tra GraphicRaycaster trên Canvas chứa ô slot
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null && parentCanvas.GetComponent<GraphicRaycaster>() == null)
        {
            Debug.LogWarning($"[InventorySlotUI] Canvas chứa ô slot ({parentCanvas.gameObject.name}) thiếu GraphicRaycaster! Tự động thêm...", this);
            parentCanvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        if (iconImage != null)
        {
            if (!iconImage.gameObject.activeSelf) iconImage.gameObject.SetActive(true);
            if (!iconImage.enabled) iconImage.enabled = true;
            // Bật raycastTarget trên iconImage để chuột chạm vào icon luôn phát hiện được sự kiện Raycast & Kéo thả
            iconImage.raycastTarget = true;
            iconImage.color = Color.white;
        }

        if (amountText != null)
        {
            if (!amountText.gameObject.activeSelf) amountText.gameObject.SetActive(true);
            if (!amountText.enabled) amountText.enabled = true;
            amountText.raycastTarget = false;
        }

        if (emptyStateVisual != null && !emptyStateVisual.activeSelf)
        {
            emptyStateVisual.SetActive(true);
        }
    }

    // Cập nhật hiển thị giao diện dựa trên dữ liệu hiện tại của InventorySlot.
    // Tuyệt đối không tắt (SetActive false hoặc enabled false) bất kỳ GameObject/Component nào.
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

    #region Pointer & Raycast Event Handlers

    public void OnPointerDown(PointerEventData eventData)
    {
        if (EventSystem.current == null)
        {
            Debug.LogError($"[InventorySlotUI] <color=red>KHÔNG TÌM THẤY EventSystem trong Scene!</color> Hãy tạo GameObject EventSystem trong Hierarchy (GameObject > UI > Event System).", this);
            return;
        }

        if (enableDebugLogs)
        {
            string hitObjName = eventData.pointerCurrentRaycast.gameObject != null ? eventData.pointerCurrentRaycast.gameObject.name : "None";
            Debug.Log($"<color=cyan>[InventorySlotUI] [OnPointerDown] Slot Index: {slotIndex} | Hit Object: {hitObjName} | Position: {eventData.position}</color>", this);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"<color=lime>[InventorySlotUI] [OnPointerClick] Slot Index: {slotIndex}</color>", this);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"<color=grey>[InventorySlotUI] [OnPointerEnter] Chuột đi vào Slot Index: {slotIndex}</color>", this);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"<color=grey>[InventorySlotUI] [OnPointerExit] Chuột rời khỏi Slot Index: {slotIndex}</color>", this);
        }
    }

    #endregion

    #region Drag & Drop Handlers

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (parentUI == null)
        {
            parentUI = GetComponentInParent<InventoryUI>();
        }

        bool hasItem = parentUI != null && parentUI.HasItemInSlot(slotIndex);
        if (enableDebugLogs)
        {
            Debug.Log($"<color=yellow>[InventorySlotUI] [OnBeginDrag] Bắt đầu kéo Slot Index: {slotIndex} | HasItem: {hasItem} | ParentUI: {(parentUI != null ? "OK" : "NULL")}</color>", this);
        }

        // Không cho phép kéo nếu ô đang rỗng hoặc không có parentUI
        if (!hasItem || parentUI == null)
        {
            eventData.pointerDrag = null;
            return;
        }

        isDragging = true;

        EnsureActive();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0.5f;
            canvasGroup.blocksRaycasts = false;
        }

        parentUI.StartDragging(slotIndex, eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || parentUI == null) return;

        parentUI.OnDragging(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (enableDebugLogs)
        {
            string dropTarget = eventData.pointerCurrentRaycast.gameObject != null ? eventData.pointerCurrentRaycast.gameObject.name : "None (ra ngoài UI)";
            Debug.Log($"<color=yellow>[InventorySlotUI] [OnEndDrag] Kết thúc kéo Slot Index: {slotIndex} | Thả tại: {dropTarget}</color>", this);
        }

        isDragging = false;

        EnsureActive();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }

        if (parentUI != null)
        {
            parentUI.EndDragging();
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null)
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning($"[InventorySlotUI] [OnDrop] Slot {slotIndex} nhận OnDrop nhưng pointerDrag bị null!", this);
            }
            return;
        }

        InventorySlotUI fromSlotUI = eventData.pointerDrag.GetComponentInParent<InventorySlotUI>();
        if (fromSlotUI != null)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"<color=green>[InventorySlotUI] [OnDrop] Thả từ Slot {fromSlotUI.SlotIndex} -> Slot {this.SlotIndex}</color>", this);
            }

            if (fromSlotUI != this)
            {
                if (parentUI == null)
                {
                    parentUI = GetComponentInParent<InventoryUI>();
                }

                if (parentUI != null)
                {
                    parentUI.HandleDrop(fromSlotUI.SlotIndex, this.SlotIndex);
                }
            }
        }
        else if (enableDebugLogs)
        {
            Debug.LogWarning($"[InventorySlotUI] [OnDrop] Object kéo '{eventData.pointerDrag.name}' không chứa InventorySlotUI!", this);
        }
    }

    #endregion
}
