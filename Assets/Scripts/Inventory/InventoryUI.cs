using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Quản lý giao diện tổng thể của Inventory.
// Lắng nghe sự kiện OnInventoryChanged từ Inventory để cập nhật toàn bộ các ô UI.
// Điều phối tương tác Kéo Thả (Drag & Drop) và tạo hiệu ứng trực quan Drag Ghost.
public class InventoryUI : MonoBehaviour
{
    [Header("Inventory & UI References")]
    [Tooltip("Tham chiếu tới Component Inventory cần hiển thị.")]
    [SerializeField] private Inventory inventory;

    [Tooltip("Transform chứa các ô slot UI (Grid Layout Group hoặc Tương đương).")]
    [SerializeField] private Transform slotContainer;

    [Tooltip("Prefab UI của một ô InventorySlotUI.")]
    [SerializeField] private InventorySlotUI slotPrefab;

    [Header("Drag & Drop Settings")]
    [Tooltip("Canvas gốc chứa giao diện (tự động tìm kiếm nếu để trống).")]
    [SerializeField] private Canvas rootCanvas;

    private readonly List<InventorySlotUI> slotUIList = new List<InventorySlotUI>();
    private bool isInitialized = false;

    // Thành phần trực quan hiển thị icon khi đang kéo (Drag Ghost)
    private Image dragGhostImage;
    private RectTransform dragGhostRect;
    private CanvasGroup dragGhostCanvasGroup;

    public Inventory Inventory => inventory;

    #region Unity Lifecycle

    private void Awake()
    {
        if (rootCanvas == null)
        {
            rootCanvas = GetComponentInParent<Canvas>();
        }

        // Tự động kiểm tra và thêm GraphicRaycaster trên Canvas nếu thiếu
        if (rootCanvas != null && rootCanvas.GetComponent<GraphicRaycaster>() == null)
        {
            Debug.LogWarning("[InventoryUI] Root Canvas chưa có GraphicRaycaster! Đang tự động thêm GraphicRaycaster...", this);
            rootCanvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        // Kiểm tra EventSystem trong scene
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            Debug.LogError("[InventoryUI] <color=red>KHÔNG TÌM THẤY EventSystem trong Scene!</color> Hãy tạo GameObject EventSystem trong Unity Hierarchy (Chuột phải > UI > Event System) để hệ thống bắt được Raycast kéo thả.", this);
        }
    }

    private void Start()
    {
        Initialize();
    }

    private void OnEnable()
    {
        if (inventory != null)
        {
            inventory.OnInventoryChanged += RefreshAllSlots;
        }

        if (isInitialized)
        {
            RefreshAllSlots();
        }
    }

    private void OnDisable()
    {
        if (inventory != null)
        {
            inventory.OnInventoryChanged -= RefreshAllSlots;
        }
    }

    #endregion

    #region UI Setup & Generation

    // Khởi tạo và sinh động danh sách các ô Slot UI theo đúng Capacity của Inventory.
    public void Initialize()
    {
        if (isInitialized) return;

        if (inventory == null)
        {
            Debug.LogError("[InventoryUI] Chưa gán tham chiếu Inventory vào Inspector!", this);
            return;
        }

        if (slotContainer == null || slotPrefab == null)
        {
            Debug.LogError("[InventoryUI] Chưa gán Slot Container hoặc Slot Prefab vào Inspector!", this);
            return;
        }

        if (rootCanvas == null)
        {
            rootCanvas = GetComponentInParent<Canvas>();
        }

        CreateSlots();
        isInitialized = true;
        RefreshAllSlots();
    }

    // Xóa các UI slot cũ (nếu có) và khởi tạo số lượng slot mới bằng đúng capacity.
    private void CreateSlots()
    {
        // Dọn dẹp các con cũ trong container
        foreach (Transform child in slotContainer)
        {
            Destroy(child.gameObject);
        }

        slotUIList.Clear();

        // Tạo đúng số lượng slot bằng inventory.Capacity
        for (int i = 0; i < inventory.Capacity; i++)
        {
            InventorySlotUI slotUI = Instantiate(slotPrefab, slotContainer);

            // Ép buộc GameObject của Slot clone và script luôn Active
            slotUI.gameObject.SetActive(true);
            slotUI.enabled = true;
            slotUI.EnsureActive();

            slotUI.Setup(i, this);
            slotUIList.Add(slotUI);
        }
    }

    #endregion

    #region UI Refresh

    // Làm mới lại toàn bộ các ô Slot trên giao diện từ dữ liệu hiện tại của Inventory.
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

    #endregion

    #region Drag & Drop Management

    // Kiểm tra xem một slot cụ thể có đang chứa vật phẩm hay không.
    public bool HasItemInSlot(int slotIndex)
    {
        if (inventory == null || !inventory.IsValidIndex(slotIndex))
            return false;

        InventorySlot slot = inventory.GetSlot(slotIndex);
        return slot != null && !slot.IsEmpty();
    }

    // Bắt đầu thao tác kéo: Khởi tạo và hiển thị Drag Ghost icon theo con trỏ chuột.
    public void StartDragging(int slotIndex, PointerEventData eventData)
    {
        if (!HasItemInSlot(slotIndex)) return;

        InventorySlot slot = inventory.GetSlot(slotIndex);
        EnsureDragGhost();

        if (dragGhostImage != null && slot.ItemData != null)
        {
            dragGhostImage.sprite = slot.ItemData.Icon;
            dragGhostImage.raycastTarget = false;
            if (dragGhostCanvasGroup != null)
            {
                dragGhostCanvasGroup.blocksRaycasts = false;
                dragGhostCanvasGroup.interactable = false;
            }

            dragGhostImage.gameObject.SetActive(true);
            dragGhostRect.SetAsLastSibling();

            UpdateDragPosition(eventData);
        }
    }

    // Di chuyển icon bóng theo tọa độ con trỏ chuột.
    public void OnDragging(PointerEventData eventData)
    {
        UpdateDragPosition(eventData);
    }

    // Kết thúc thao tác kéo: Ẩn Drag Ghost.
    public void EndDragging()
    {
        if (dragGhostImage != null)
        {
            dragGhostImage.gameObject.SetActive(false);
        }

        RefreshAllSlots();
    }

    // Xử lý sự kiện thả vật phẩm từ fromSlotIndex vào toSlotIndex.
    public void HandleDrop(int fromSlotIndex, int toSlotIndex)
    {
        if (inventory == null || fromSlotIndex == toSlotIndex)
            return;

        inventory.MoveOrSwapSlots(fromSlotIndex, toSlotIndex);
    }

    // Đảm bảo đối tượng Drag Ghost đã được tạo sẵn trong Root Canvas.
    private void EnsureDragGhost()
    {
        if (dragGhostImage != null) return;

        Transform parentTransform = rootCanvas != null ? rootCanvas.transform : transform.root;
        GameObject ghostObj = new GameObject("Inventory_DragGhost", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
        ghostObj.transform.SetParent(parentTransform, false);

        dragGhostImage = ghostObj.GetComponent<Image>();
        dragGhostImage.raycastTarget = false;
        dragGhostImage.color = Color.white;

        dragGhostCanvasGroup = ghostObj.GetComponent<CanvasGroup>();
        dragGhostCanvasGroup.blocksRaycasts = false;
        dragGhostCanvasGroup.interactable = false;

        dragGhostRect = ghostObj.GetComponent<RectTransform>();
        dragGhostRect.pivot = new Vector2(0.5f, 0.5f);
        dragGhostRect.sizeDelta = new Vector2(50f, 50f);

        ghostObj.SetActive(false);
    }

    // Cập nhật vị trí hiển thị Drag Ghost theo vị trí PointerEventData.
    private void UpdateDragPosition(PointerEventData eventData)
    {
        if (dragGhostRect == null || rootCanvas == null) return;

        if (rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            dragGhostRect.position = eventData.position;
        }
        else
        {
            Camera eventCamera = eventData.pressEventCamera ?? rootCanvas.worldCamera ?? Camera.main;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rootCanvas.transform as RectTransform,
                eventData.position,
                eventCamera,
                out Vector2 localPos))
            {
                dragGhostRect.localPosition = localPos;
            }
        }
    }

    #endregion
}
