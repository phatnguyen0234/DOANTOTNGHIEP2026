using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Android;

public class FarmInputController : MonoBehaviour
{
    [Header("Tilemaps & Grid")]
    [SerializeField] private Tilemap groundTileMap;
    [SerializeField] private Tilemap highlightTileMap;
    [SerializeField] private TileBase soilTile;
    [SerializeField] private TileBase soilWetTile;
    [SerializeField] private TileBase greenTile;
    [SerializeField] private TileBase redTile;

    [Header("Entities & Camera")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Transform player;

    [Header("Crop Settings")]
    [SerializeField] private GameObject cropPrefab;
    [SerializeField] private CropData cropData;
    [SerializeField] private CropTile cropTile;

    [Header("Inventory & Hotbar")]
    [SerializeField] private Inventory inventory;
    [SerializeField] private HotbarController hotbarController;

    [SerializeField] private LayerMask cropMask;
    [SerializeField] private LayerMask treeMask;


    private readonly Dictionary<Vector3Int, GameObject> planted = new Dictionary<Vector3Int, GameObject>();
    private Vector3Int targetCellHoe;
    private Vector3Int targetCellWater;


    public FarmTool CurrentTool { get; private set; } = FarmTool.None;

    #region Unity Lifecycle

    private void Awake()
    {
        ResolveDependencies();
    }

    private void Start()
    {
        ResolveDependencies();
        UpdateCurrentTool();
    }

    private void OnEnable()
    {
        ResolveDependencies();
        if (hotbarController != null)
        {
            hotbarController.OnSelectedSlotChanged += HandleSelectedSlotChanged;
        }
        if (inventory != null)
        {
            inventory.OnInventoryChanged += HandleInventoryChanged;
        }
        UpdateCurrentTool();
    }

    private void OnDisable()
    {
        if (hotbarController != null)
        {
            hotbarController.OnSelectedSlotChanged -= HandleSelectedSlotChanged;
        }
        if (inventory != null)
        {
            inventory.OnInventoryChanged -= HandleInventoryChanged;
        }
    }

    private void Update()
    {
        SelectTool();

        bool isPointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        bool isBagOpen = inventory != null && inventory.IsBagOpen;

        if (CurrentTool == FarmTool.Seed && !isPointerOverUI && !isBagOpen)
        {
            HighLight();
        }
        else
        {
            highlightTileMap.ClearAllTiles();
        }

        Use();
    }

    #endregion

    #region Dependency Resolution & Tool Selection

    private void ResolveDependencies()
    {
        if (inventory == null)
        {
            inventory = FindAnyObjectByType<Inventory>();
        }

        if (hotbarController == null)
        {
            hotbarController = FindAnyObjectByType<HotbarController>();
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    private void HandleSelectedSlotChanged(int slotIndex)
    {
        UpdateCurrentTool();
    }

    private void HandleInventoryChanged()
    {
        UpdateCurrentTool();
    }

    // Tự động đồng bộ CurrentTool dựa theo ItemData hiện tại trong ô Hotbar đang chọn
    public void SelectTool()
    {
        UpdateCurrentTool();
    }

    public void UpdateCurrentTool()
    {
        if (hotbarController == null)
        {
            hotbarController = FindAnyObjectByType<HotbarController>();
        }

        InventorySlot activeSlot = hotbarController != null ? hotbarController.SelectedSlot : null;
        if (activeSlot == null || activeSlot.IsEmpty() || activeSlot.ItemData == null)
        {
            CurrentTool = FarmTool.None;
            return;
        }

        ItemData item = activeSlot.ItemData;

        if (item.ToolType == ToolType.Hoe)
        {
            CurrentTool = FarmTool.Hoe;
        }
        else if (item.ToolType == ToolType.WateringCan)
        {
            CurrentTool = FarmTool.Water;
        }
        else if (item.ItemType == ItemType.Seed || item.ToolType == ToolType.SeedBag)
        {
            CurrentTool = FarmTool.Seed;
        }
        else if (item.ToolType == ToolType.Harvest)
        {
            CurrentTool = FarmTool.Harvest;
        }
        else if (item.ToolType == ToolType.Axe)
        {
            CurrentTool = FarmTool.Axe;
        }
        else if (item.ToolType == ToolType.Pickaxe)
        {
            CurrentTool = FarmTool.Pickaxe;
        }
        else
        {
            CurrentTool = FarmTool.None;
        }
    }

    #endregion

    #region Actions & Execution

    public void Use()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        // Bỏ qua nếu click chuột trên UI (ví dụ ô Hotbar, Túi đồ)
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        // Bỏ qua nếu túi đồ đang mở
        if (inventory != null && inventory.IsBagOpen) return;

        switch (CurrentTool)
        {
            case FarmTool.Hoe:
                Hoe();
                break;
            case FarmTool.Seed:
                Seed();
                break;
            case FarmTool.Water:
                Water();
                break;
            case FarmTool.Harvest:
                Harvest();
                break;
            case FarmTool.Axe:
                Axe();
                break;
            case FarmTool.None:
                // Nếu tay không hoặc vật phẩm không phải công cụ canh tác, cho phép click thu hoạch cây chín
                TryHarvestAtMouse();
                break;
        }
    }

    public bool CanPlant(Vector3Int cell)
    {
        Vector3Int playerCell = groundTileMap.WorldToCell(player.transform.position);
        Vector3Int distance = cell - playerCell;
        TileBase tile = groundTileMap.GetTile(cell);
        bool isSoil = tile == soilTile || tile == soilWetTile;
        bool isInRange = CheckDistance(distance);

        // Kiểm tra cây đã trồng, dọn dẹp nếu GameObject cây đã bị xóa
        if (planted.TryGetValue(cell, out GameObject existingCrop) && existingCrop == null)
        {
            planted.Remove(cell);
        }

        bool isPlanted = planted.ContainsKey(cell);
        return isInRange && isSoil && !isPlanted;
    }

    public void HighLight()
    {
        Vector3 mouWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouWorld.z = 0f;
        Vector3Int mouCell = groundTileMap.WorldToCell(mouWorld);
        highlightTileMap.ClearAllTiles();

        if (CanPlant(mouCell))
        {
            highlightTileMap.SetTile(mouCell, greenTile);
        }
        else
        {
            highlightTileMap.SetTile(mouCell, redTile);
        }
    }

    private void Hoe()
    {
        Vector3 posWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        posWorld.z = 0f;
        Vector3Int posCell = groundTileMap.WorldToCell(posWorld);
        Vector3Int playerCell = groundTileMap.WorldToCell(player.transform.position);
        Vector3Int distance = posCell - playerCell;
        Vector2 disWorld = (Vector2)(posWorld - player.transform.position);

        if (CheckDistance(distance))
        {
            targetCellHoe = posCell;
            playerMovement.TryUseHoe(disWorld);
        }
        else
        {
            targetCellHoe = playerCell + Offset(playerMovement.FacingDirection); 
            playerMovement.TryUseHoe(playerMovement.FacingDirection);
        }
    }

    public void Seed()
    {
        InventorySlot activeSlot = hotbarController != null ? hotbarController.SelectedSlot : null;
        if (activeSlot == null || activeSlot.IsEmpty() || activeSlot.ItemData == null)
        {
            CurrentTool = FarmTool.None;
            return;
        }

        ItemData seedItem = activeSlot.ItemData;
        if (seedItem.ItemType != ItemType.Seed && seedItem.ToolType != ToolType.SeedBag)
        {
            return;
        }

        Vector3 mouWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouWorld.z = 0f;
        Vector3Int mouCell = groundTileMap.WorldToCell(mouWorld);

        if (CanPlant(mouCell))
        {
            CropData toPlant = seedItem.CropData != null ? seedItem.CropData : cropData;
            if (toPlant == null)
            {
                Debug.LogWarning($"[FarmInputController] Chưa cấu hình CropData cho hạt giống '{seedItem.ItemName}'!", this);
                return;
            }

            Vector3 mouseCenter = groundTileMap.GetCellCenterWorld(mouCell);
            GameObject crop = Instantiate(cropPrefab, mouseCenter, Quaternion.identity);

            CropTile tile = crop.GetComponent<CropTile>();
            if (tile != null)
            {
                tile.Init(toPlant);
            }
            else
            {
                SpriteRenderer sr = crop.GetComponent<SpriteRenderer>();
                if (sr != null && toPlant.stageSprites != null && toPlant.stageSprites.Length > 0)
                {
                    sr.sprite = toPlant.stageSprites[0];
                }
            }

            planted[mouCell] = crop;

            // Trừ 1 hạt giống từ ô Hotbar đang chọn
            if (inventory != null && hotbarController != null)
            {
                inventory.RemoveItemFromSlot(hotbarController.SelectedSlotIndex, 1);
            }
            else
            {
                activeSlot.RemoveAmount(1);
            }

            // Nếu đã gieo hết hạt trong slot, cập nhật lại CurrentTool và xóa ô viền sáng
            if (activeSlot.IsEmpty())
            {
                UpdateCurrentTool();
                highlightTileMap.ClearAllTiles();
            }
        }
    }

    public void Water()
    {
 
        Vector3 posWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        posWorld.z = 0f;
        Vector3Int posCell = groundTileMap.WorldToCell(posWorld);
        Vector3Int playerCell = groundTileMap.WorldToCell(player.transform.position);
        Vector3Int distance = posCell - playerCell;
        Vector2 disWorld = (Vector2)(posWorld - player.transform.position);

        Vector3 targetWorldWater;
        if (CheckDistance(distance))
        {
            targetCellWater = posCell;
            targetWorldWater = groundTileMap.GetCellCenterWorld(targetCellWater);
        }
        else
        {
            targetCellWater = playerCell + Offset(playerMovement.FacingDirection);
            targetWorldWater = groundTileMap.GetCellCenterWorld(targetCellWater);
        }
        RaycastHit2D hit = Physics2D.Raycast(targetWorldWater, Vector2.zero, cropMask);
        bool isSoil = groundTileMap.GetTile(targetCellWater) == soilTile;
        if (isSoil)
        {
            if (hit.collider != null)
            {
                CropTile crop = hit.collider.GetComponent<CropTile>();
                crop.isWatered = true;
                if (CheckDistance(distance)) playerMovement.UsingWater(disWorld);
                else playerMovement.UsingWater(playerMovement.FacingDirection);
            }
        }
    }

    public void Harvest()
    {
        TryHarvestAtMouse();
    }

    private bool TryHarvestAtMouse()
    {
        Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0;
        Vector3Int mouseCell = groundTileMap.WorldToCell(mouseWorld);
        Vector3Int playerCell = groundTileMap.WorldToCell(player.transform.position);
        Vector3Int distance = mouseCell - playerCell;
        TileBase tile = groundTileMap.GetTile(mouseCell);
        bool isSoil = tile == soilTile || tile == soilWetTile;

        if (CheckDistance(distance) && isSoil)
        {
            RaycastHit2D hit = Physics2D.Raycast(groundTileMap.GetCellCenterWorld(mouseCell), Vector2.zero);
            if (hit.collider != null)
            {
                CropTile crop = hit.collider.GetComponent<CropTile>();
                if (crop != null && crop.isHavest)
                {
                    // Thêm vật phẩm nông sản vào Inventory nếu có cấu hình
                    if (crop.CropData != null && crop.CropData.harvestItem != null)
                    {
                        int amount = Mathf.Max(1, crop.CropData.harvestAmount);
                        if (inventory != null)
                        {
                            bool added = inventory.TryAddItem(crop.CropData.harvestItem, amount);
                            if (!added)
                            {
                                Debug.LogWarning($"[FarmInputController] Túi đồ đã đầy! Không thể thu hoạch {crop.CropData.harvestItem.ItemName}.");
                                return false;
                            }
                        }
                    }

                    // Xóa khỏi từ điển theo dõi planted
                    Vector3Int cropCell = groundTileMap.WorldToCell(hit.collider.transform.position);
                    planted.Remove(cropCell);
                    planted.Remove(mouseCell);

                    crop.UpdateSprite(crop.currentGrowthStage + 1);
                    Destroy(hit.collider.gameObject, 0.1f);
                    return true;
                }
            } 
        }

        return false;
    }

    public void Axe()
    {
        Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;
        Vector3Int mouseCell = groundTileMap.WorldToCell(mousePos);
        Vector3Int playerCell = groundTileMap.WorldToCell(player.transform.position);
        Vector3Int distance = mouseCell - playerCell;
        bool isRange = CheckDistance(distance);
        Vector3 targetAxe;
        if (isRange)
        {
            targetAxe = groundTileMap.GetCellCenterWorld(mouseCell);
        }
        else
        {
            Vector3Int targetAxeCell = playerCell + Offset(playerMovement.FacingDirection);
            targetAxe = groundTileMap.GetCellCenterWorld(targetAxeCell);
        }
        RaycastHit2D hit = Physics2D.Raycast(targetAxe, Vector2.zero, treeMask);
        if(hit.collider != null)
        {
            if (isRange) playerMovement.UsingAxe((Vector2) (targetAxe - player.transform.position));
        //    else playerMovement.UsingAxe(playerMovement.FacingDirection);
        }
    }

    public void OnWaterAnimationComplete()
    {
        groundTileMap.SetTile(targetCellWater, soilWetTile);
    }

    public void OnHoeAnimationComplete()
    {
        groundTileMap.SetTile(targetCellHoe, soilTile);
    }

    private Vector3Int Offset(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y))
        {
            return direction.x > 0 ? Vector3Int.right : Vector3Int.left;
        }
        else
        {
            return direction.y > 0 ? Vector3Int.up : Vector3Int.down;
        }
    }

    private bool CheckDistance(Vector3Int distance)
    {
        return distance != Vector3Int.zero && Mathf.Abs(distance.x) <= 1 && Mathf.Abs(distance.y) <= 1;
    }

    #endregion
}