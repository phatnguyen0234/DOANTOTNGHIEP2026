using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Quản lý giao diện tổng thể của Inventory.
// Lắng nghe sự kiện OnInventoryChanged từ Inventory để cập nhật toàn bộ các ô UI.
// Điều phối tương tác Kéo Thả (Drag & Drop) và tạo hiệu ứng trực quan Drag Ghost.
// Hỗ trợ phím tắt ESC bật/tắt (Toggle) hiển thị túi đồ.
public class InventoryUI : MonoBehaviour
{
    [Header("Inventory & UI References")]
    [Tooltip("Tham chiếu tới Component Inventory cần hiển thị.")]
    [SerializeField] private Inventory inventory;

    [Header("Slot Containers")]
    [Tooltip("Transform chứa các ô slot thuộc Hotbar trong túi đồ (Slot index từ 0 đến hotbarSize - 1).")]
    [SerializeField] private Transform hotbarSlotContainer;

    [Tooltip("Transform chứa các ô slot còn lại của túi đồ (Slot index từ hotbarSize đến Capacity - 1).")]
    [SerializeField] private Transform mainSlotContainer;

    [Tooltip("Số lượng ô Hotbar (mặc định: 10).")]
    [SerializeField, Min(1)] private int hotbarSize = 10;

    [Tooltip("Prefab UI của một ô InventorySlotUI.")]
    [SerializeField] private InventorySlotUI slotPrefab;

    [Header("Bag Toggle Settings")]
    [Tooltip("Phím tắt để bật/tắt hiển thị túi đồ.")]
    [SerializeField] private KeyCode toggleKey = KeyCode.Escape;

    [Tooltip("Panel UI của túi đồ cần bật/tắt (nếu để trống sẽ tự động điều khiển GameObject này).")]
    [SerializeField] private GameObject bagPanel;

    [Tooltip("Trạng thái hiển thị ban đầu khi bắt đầu game (mặc định: false - đóng).")]
    [SerializeField] private bool startOpen = false;

    [Header("Hotbar UI Reference")]
    [Tooltip("Tham chiếu tới GameObject của Hotbar UI (sẽ ẩn đi khi mở túi đồ và hiện lại khi đóng). Tự động tìm kiếm nếu để trống.")]
    [SerializeField] private GameObject hotbarUI;

    [Header("Drag & Drop Settings")]
    [Tooltip("Canvas gốc chứa giao diện (tự động tìm kiếm nếu để trống).")]
    [SerializeField] private Canvas rootCanvas;

    private readonly List<InventorySlotUI> slotUIList = new List<InventorySlotUI>();
    private bool isInitialized = false;
    private int lastToggleFrame = -1;

    // Thành phần trực quan hiển thị icon khi đang kéo (Drag Ghost)
    private Image dragGhostImage;
    private RectTransform dragGhostRect;
    private CanvasGroup dragGhostCanvasGroup;

    public static InventoryUI Instance { get; private set; }

    // Sự kiện phát ra khi trạng thái túi đồ thay đổi (true: Mở, false: Đóng)
    public static event Action<bool> OnBagToggled;

    public Inventory Inventory => inventory;
    public bool IsOpen => bagPanel != null ? bagPanel.activeSelf : gameObject.activeSelf;
    public Transform HotbarSlotContainer => hotbarSlotContainer;
    public Transform MainSlotContainer => mainSlotContainer;
    public int HotbarSize => hotbarSize;

    #region Unity Lifecycle

    private void Awake()
    {
        Instance = this;

        if (bagPanel == null)
        {
            bagPanel = gameObject;
        }

        ResolveHotbarUI();

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

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        Initialize();

        // Khởi tạo trạng thái ban đầu: Nếu không cấu hình mở sẵn thì đóng túi đồ
        if (!startOpen)
        {
            CloseBag();
        }
        else
        {
            OpenBag();
        }
    }

    private void Update()
    {
        // Lắng nghe phím bấm bật/tắt túi đồ
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleBag();
        }
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

    #region Bag Visibility Control

    // Tự động tìm kiếm GameObject của HotbarUI trong Scene nếu chưa được gán
    private void ResolveHotbarUI()
    {
        if (hotbarUI == null)
        {
            HotbarUI hb = FindAnyObjectByType<HotbarUI>(FindObjectsInactive.Include);
            if (hb != null)
            {
                hotbarUI = hb.gameObject;
            }
        }
    }

    // Bật/tắt trạng thái hiển thị của túi đồ
    public void ToggleBag()
    {
        // Chống kích hoạt lặp lại nhiều lần trong cùng một frame
        if (Time.frameCount == lastToggleFrame) return;
        lastToggleFrame = Time.frameCount;

        if (IsOpen)
        {
            CloseBag();
        }
        else
        {
            OpenBag();
        }
    }

    // Mở túi đồ (Đồng thời tắt UI Hotbar)
    public void OpenBag()
    {
        GameObject target = bagPanel != null ? bagPanel : gameObject;
        if (!target.activeSelf)
        {
            target.SetActive(true);
        }

        // Tắt UI Hotbar khi mở túi đồ
        ResolveHotbarUI();
        if (hotbarUI != null && hotbarUI.activeSelf)
        {
            hotbarUI.SetActive(false);
        }

        RefreshAllSlots();
        OnBagToggled?.Invoke(true);
    }

    // Đóng túi đồ (Đồng thời bật lại UI Hotbar)
    public void CloseBag()
    {
        EndDragging();

        GameObject target = bagPanel != null ? bagPanel : gameObject;
        if (target.activeSelf)
        {
            target.SetActive(false);
        }

        // Bật lại UI Hotbar khi đóng túi đồ
        ResolveHotbarUI();
        if (hotbarUI != null && !hotbarUI.activeSelf)
        {
            hotbarUI.SetActive(true);
        }

        OnBagToggled?.Invoke(false);
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

        if (hotbarSlotContainer == null && mainSlotContainer == null)
        {
            Debug.LogError("[InventoryUI] Cần gán ít nhất một Container (hotbarSlotContainer hoặc mainSlotContainer) vào Inspector!", this);
            return;
        }

        if (slotPrefab == null)
        {
            Debug.LogError("[InventoryUI] Chưa gán Slot Prefab vào Inspector!", this);
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

    // Xóa các UI slot cũ (nếu có) và khởi tạo các ô slot phân bổ vào hotbarSlotContainer và mainSlotContainer.
    private void CreateSlots()
    {
        // Dọn dẹp các con cũ trong hotbarSlotContainer
        if (hotbarSlotContainer != null)
        {
            foreach (Transform child in hotbarSlotContainer)
            {
                Destroy(child.gameObject);
            }
        }

        // Dọn dẹp các con cũ trong mainSlotContainer (tránh dọn 2 lần nếu trỏ cùng 1 transform)
        if (mainSlotContainer != null && mainSlotContainer != hotbarSlotContainer)
        {
            foreach (Transform child in mainSlotContainer)
            {
                Destroy(child.gameObject);
            }
        }

        slotUIList.Clear();

        int effectiveHotbarCount = Mathf.Clamp(hotbarSize, 0, inventory.Capacity);

        // Tạo đúng số lượng slot bằng inventory.Capacity
        for (int i = 0; i < inventory.Capacity; i++)
        {
            // Xác định container phù hợp cho slot index này
            Transform targetContainer;
            if (i < effectiveHotbarCount && hotbarSlotContainer != null)
            {
                targetContainer = hotbarSlotContainer;
            }
            else
            {
                targetContainer = mainSlotContainer != null ? mainSlotContainer : hotbarSlotContainer;
            }

            if (targetContainer == null)
            {
                Debug.LogError($"[InventoryUI] Không tìm thấy Container hợp lệ cho Slot Index {i}!", this);
                continue;
            }

            InventorySlotUI slotUI = Instantiate(slotPrefab, targetContainer);

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
