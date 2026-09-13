using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.EventSystems;

// Quản lý tương tác giữa người chơi và thế giới nông trại (Farm & World Grid).
// Lắng nghe thao tác Click chuột phải (RMB) để kích hoạt hành vi của Item đang chọn trên Hotbar (1-9).
public class FarmInputController : MonoBehaviour
{
    [Header("Tilemap References")]
    [Tooltip("Tilemap nền đất chính.")]
    [SerializeField] private Tilemap groundTileMap;

    [Tooltip("Tilemap hiển thị đất đã cuốc/nông trại.")]
    [SerializeField] private Tilemap farmSoildTileMap;

    [Header("Core Dependencies")]
    [Tooltip("Camera chính dùng để tính toán tọa độ chuột sang World.")]
    [SerializeField] private Camera mainCamera;

    [Tooltip("Transform của nhân vật người chơi.")]
    [SerializeField] private Transform Player;

    [Tooltip("Tham chiếu tới hệ thống Inventory.")]
    [SerializeField] private Inventory inventory;

    [Tooltip("Tham chiếu tới HotbarController để lấy ô đang active.")]
    [SerializeField] private HotbarController hotbarController;

    [Tooltip("Tham chiếu tới FarmManager để xử lý logic đất/cây.")]
    [SerializeField] private FarmManager farmManager;

    [Header("Interaction Settings")]
    [Tooltip("Khoảng cách tối đa (đơn vị world) mà người chơi có thể tương tác với ô đất.")]
    [SerializeField] private float maxInteractDistance = 2.5f;

    [Tooltip("Layer cản trở (nếu cần kiểm tra va chạm).")]
    [SerializeField] private LayerMask obtacleLayer;

    // Backward-compatibility properties
    public Vector3Int standCell { get; private set; }
    public Vector3 standPos { get; private set; }
    public Vector3Int TargetCell { get; private set; }
    public bool HasTarget { get; private set; }
    public FarmTool CurrentTool { get; private set; }

    public static readonly Vector3Int[] adjacentCells = new Vector3Int[]
    {
        Vector3Int.up,
        Vector3Int.down,
        Vector3Int.right,
        Vector3Int.left
    };

    private void Awake()
    {
        ResolveDependencies();
    }

    private void Start()
    {
        ResolveDependencies();
    }

    private void ResolveDependencies()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (Player == null)
        {
            PlayerMovement pm = FindAnyObjectByType<PlayerMovement>();
            if (pm != null)
            {
                Player = pm.transform;
            }
        }

        if (inventory == null)
        {
            inventory = FindAnyObjectByType<Inventory>();
        }

        if (hotbarController == null)
        {
            hotbarController = FindAnyObjectByType<HotbarController>();
        }

        if (farmManager == null)
        {
            farmManager = FindAnyObjectByType<FarmManager>();
        }

        if (groundTileMap == null)
        {
            groundTileMap = FindAnyObjectByType<Tilemap>();
        }
    }

    private void Update()
    {
        // Kiểm tra phím Click chuột phải (RMB - Right Mouse Button)
        if (Input.GetMouseButtonDown(1))
        {
            HandleRightClickInteraction();
        }
    }

    // Xử lý tương tác khi người chơi nhấp chuột phải vào thế giới game
    private void HandleRightClickInteraction()
    {
        // 1. Kiểm tra nếu con trỏ đang nằm trên UI (Inventory, Hotbar, Menu...) thì bỏ qua
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        if (groundTileMap == null)
        {
            Debug.LogError("[FarmInputController] Chưa gán groundTileMap!", this);
            return;
        }

        // 2. Chuyển đổi vị trí con trỏ chuột sang tọa độ thế giới (World Position)
        Camera cam = mainCamera != null ? mainCamera : Camera.main;
        if (cam == null) return;

        Vector3 mouseWorldPos = cam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;

        Vector3Int targetCell = groundTileMap.WorldToCell(mouseWorldPos);
        Vector3 cellCenterWorld = groundTileMap.GetCellCenterWorld(targetCell);

        // 3. Kiểm tra cự ly từ Player tới ô đất mục tiêu
        Vector3 playerPos = Player != null ? Player.position : transform.position;
        float distance = Vector3.Distance(playerPos, cellCenterWorld);
        if (distance > maxInteractDistance)
        {
            Debug.LogWarning($"<color=orange>[FarmInputController] Quá xa để tương tác! Khoảng cách: {distance:F1}m (Tối đa: {maxInteractDistance}m)</color>");
            return;
        }

        // Cập nhật thông tin target cho debug/hệ thống khác
        TargetCell = targetCell;
        HasTarget = true;

        // 4. Lấy ô đang được chọn trên Hotbar
        InventorySlot activeSlot = hotbarController != null ? hotbarController.SelectedSlot : null;
        ItemData item = (activeSlot != null && !activeSlot.IsEmpty()) ? activeSlot.ItemData : null;

        // 5. Thực thi hành vi tương ứng với Item đang cầm
        ExecuteInteraction(targetCell, cellCenterWorld, item);
    }

    // Điều phối và thực thi hành vi tương ứng theo ToolType của item
    private void ExecuteInteraction(Vector3Int targetCell, Vector3 cellCenterWorld, ItemData item)
    {
        if (farmManager == null)
        {
            Debug.LogError("[FarmInputController] Chưa gán tham chiếu FarmManager!", this);
            return;
        }

        // TRƯỜNG HỢP 1: CẦM ITEM CÓ HÀNH VI CÔNG CỤ
        if (item != null)
        {
            // A. Cuốc đất (Hoe)
            if (item.ToolType == ToolType.Hoe)
            {
                ExecuteHoeAction(targetCell);
                return;
            }

            // B. Tưới nước (Watering Can)
            if (item.ToolType == ToolType.WateringCan)
            {
                ExecuteWaterAction(targetCell);
                return;
            }

            // C. Búa hoặc Xẻng (Hammer / Shovel) - Phá dỡ/san phẳng ô đất
            if (item.ToolType == ToolType.Hammer || item.ToolType == ToolType.Shovel)
            {
                ExecuteRemoveSoilAction(targetCell);
                return;
            }

            // D. Thu hoạch (Harvest)
            if (item.ToolType == ToolType.Harvest)
            {
                ExecuteHarvestAction(targetCell);
                return;
            }
        }

        // TRƯỜNG HỢP 2: TAY TRỐNG HOẶC ITEM KHÔNG PHẢI TOOL NÔNG TRẠI
        // Tự động kiểm tra nếu ô đất có cây chín thì cho phép thu hoạch
        if (farmManager.CanHarvest(targetCell, out _))
        {
            ExecuteHarvestAction(targetCell);
            return;
        }

        string itemName = item != null ? item.ItemName : "Tay không";
        Debug.Log($"<color=grey>[FarmInputController] Không có hành vi tương tác cho '{itemName}' tại ô {targetCell}.</color>");
    }

    #region Action Implementations

    // Hành vi cuốc đất
    private void ExecuteHoeAction(Vector3Int targetCell)
    {
        if (farmManager.CanHoe(targetCell, out string message))
        {
            if (farmManager.Hoe(targetCell, out message))
            {
                Debug.Log($"<color=green>[FarmInputController] Thành công: {message} tại ô {targetCell}</color>");
            }
        }
        else
        {
            Debug.LogWarning($"<color=yellow>[FarmInputController] {message}</color>");
        }
    }

    // Hành vi tưới nước
    private void ExecuteWaterAction(Vector3Int targetCell)
    {
        if (farmManager.CanWater(targetCell, out string message))
        {
            if (farmManager.Water(targetCell, out message))
            {
                Debug.Log($"<color=green>[FarmInputController] Thành công: {message} tại ô {targetCell}</color>");
            }
        }
        else
        {
            Debug.LogWarning($"<color=yellow>[FarmInputController] {message}</color>");
        }
    }

    // Hành vi san phẳng / dọn đất (Búa / Xẻng)
    private void ExecuteRemoveSoilAction(Vector3Int targetCell)
    {
        if (farmManager.CanRemoveSoil(targetCell, out string message))
        {
            if (farmManager.RemoveSoil(targetCell, out message))
            {
                Debug.Log($"<color=green>[FarmInputController] Thành công: {message} tại ô {targetCell}</color>");
            }
        }
        else
        {
            Debug.LogWarning($"<color=yellow>[FarmInputController] {message}</color>");
        }
    }

    // Hành vi thu hoạch cây trồng
    private void ExecuteHarvestAction(Vector3Int targetCell)
    {
        if (farmManager.CanHarvest(targetCell, out string message))
        {
            FarmCell cell = farmManager.GetCell(targetCell);
            string cropId = cell != null ? cell.cropId : "Cây trồng";

            if (farmManager.Harvest(targetCell, out message))
            {
                Debug.Log($"<color=green>[FarmInputController] Thành công: {message} ({cropId}) tại ô {targetCell}</color>");
            }
        }
        else
        {
            Debug.LogWarning($"<color=yellow>[FarmInputController] {message}</color>");
        }
    }

    #endregion

    #region Backward Compatibility Helpers

    public bool FindAdjacentCell(Vector3Int targetCell, out Vector3Int adjacentCell)
    {
        adjacentCell = default;
        bool found = false;

        float nearestDistance = float.MaxValue;
        Vector3Int playerCell = groundTileMap != null && Player != null ? groundTileMap.WorldToCell(Player.position) : targetCell;

        foreach (Vector3Int offset in adjacentCells)
        {
            Vector3Int cell = targetCell + offset;

            if (cell == playerCell)
            {
                adjacentCell = cell;
                return true;
            }

            if (groundTileMap != null && Player != null)
            {
                float d = Vector3.Distance(groundTileMap.GetCellCenterWorld(cell), Player.position);
                if (d < nearestDistance)
                {
                    adjacentCell = cell;
                    nearestDistance = d;
                    found = true;
                }
            }
        }
        return found;
    }

    public bool CanDig(Vector3Int cell)
    {
        if (groundTileMap == null || !groundTileMap.HasTile(cell)) return false;
        if (farmSoildTileMap != null && farmSoildTileMap.HasTile(cell)) return false;
        return true;
    }

    public void SelectTool()
    {
        CurrentTool = FarmTool.Hoe;
    }

    public void ClearTool()
    {
        CurrentTool = FarmTool.None;
    }

    #endregion
}