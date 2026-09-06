using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class FarmManager : MonoBehaviour
{
    [Header("Map")]
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private Tilemap farmSoilTilemap;
    [SerializeField] private TileBase tilledSoilTile;

    [SerializeField] private Tilemap cropsTilemap;

    [SerializeField] private TileBase carrotSeedTile;
    [SerializeField] private TileBase carrotGrowingTile;
    [SerializeField] private TileBase carrotReadyTile;
    [SerializeField] private TileBase wetSoilTile;

    [Header("Farm Data")]
    [SerializeField] private FarmData farm = new FarmData();

    [Header("Temporary Crop Settings")]
    [SerializeField] private string defaultCropId = "Carrot";
    [SerializeField] private int defaultDaysToMature = 3;

    public FarmData Farm => farm;

    private void Awake()
    {
        // Tạm set ID cứng; sau này lấy từ save game / player profile.
        farm.farmId = "farm_01";
        farm.playerId = "player_01";
    }

    // Tìm FarmCell đã tồn tại ở ô này.
    public FarmCell GetCell(Vector3Int position)
    {
        return farm.cells.Find(cell => cell.position == position);
    }

    // Chỉ tạo cell khi người chơi lần đầu tương tác với ô đất.
    private FarmCell GetOrCreateCell(Vector3Int position)
    {
        FarmCell cell = GetCell(position);

        if (cell == null)
        {
            cell = new FarmCell(position);
            farm.cells.Add(cell);
        }

        return cell;
    }

    // Check ô đó phải thực sự nằm trên Ground Tilemap.
    private bool IsFarmableGround(Vector3Int position)
    {
        return groundTilemap != null && groundTilemap.HasTile(position);
    }

    public bool CanHoe(Vector3Int position, out string message)
    {
        if (!IsFarmableGround(position))
        {
            message = "Không thể cuốc ở đây.";
            return false;
        }

        FarmCell cell = GetCell(position);

        // Cell chưa có trong list nghĩa là nó đang Empty.
        if (cell == null || cell.state == FarmCellState.Empty)
        {
            message = "Có thể cuốc đất.";
            return true;
        }

        message = "Ô này đã được cuốc hoặc đang có cây.";
        return false;
    }

    public bool Hoe(Vector3Int position, out string message)
    {
        if (!CanHoe(position, out message))
            return false;

        FarmCell cell = GetOrCreateCell(position);

        cell.state = FarmCellState.Tilled;
        cell.wateredToday = false;
        cell.growthDays = 0;
        cell.daysToMature = 0;
        cell.cropId = string.Empty;

        message = "Đã cuốc đất.";
        RefreshCellVisual(cell);
        return true;
    }

    public bool CanPlant(Vector3Int position, out string message)
    {
        if (!IsFarmableGround(position))
        {
            message = "Không thể gieo hạt ở đây.";
            return false;
        }

        FarmCell cell = GetCell(position);

        if (cell == null || cell.state != FarmCellState.Tilled)
        {
            message = "Cần cuốc đất trước khi gieo hạt.";
            return false;
        }

        message = "Có thể gieo hạt.";
        return true;
    }

    public bool Plant(Vector3Int position, out string message)
    {
        if (!CanPlant(position, out message))
            return false;

        FarmCell cell = GetCell(position);

        cell.state = FarmCellState.Seeded;
        cell.cropId = defaultCropId;
        cell.growthDays = 0;
        cell.daysToMature = defaultDaysToMature;
        cell.wateredToday = false;
        RefreshCellVisual(cell);

        message = "Đã gieo " + defaultCropId + ".";
        return true;
    }

    public bool CanWater(Vector3Int position, out string message)
    {
        if (!IsFarmableGround(position))
        {
            message = "Không thể tưới ở đây.";
            return false;
        }

        FarmCell cell = GetCell(position);

        if (cell == null)
        {
            message = "Chưa có hạt giống ở ô này.";
            return false;
        }

        if (cell.state != FarmCellState.Seeded &&
            cell.state != FarmCellState.Growing)
        {
            message = "Chỉ có thể tưới cây đang phát triển.";
            return false;
        }

        if (cell.wateredToday)
        {
            message = "Cây này đã được tưới hôm nay.";
            return false;
        }

        message = "Có thể tưới cây.";
        return true;
    }

    public bool Water(Vector3Int position, out string message)
    {
        if (!CanWater(position, out message))
            return false;

        FarmCell cell = GetCell(position);

        // Sau lần tưới đầu, hạt chính thức ở trạng thái phát triển.
        cell.state = FarmCellState.Growing;
        cell.wateredToday = true;
        RefreshCellVisual(cell);
        message = "Đã tưới cây.";
        return true;
    }

    public bool CanHarvest(Vector3Int position, out string message)
    {
        if (!IsFarmableGround(position))
        {
            message = "Không thể thu hoạch ở đây.";
            return false;
        }

        FarmCell cell = GetCell(position);

        if (cell == null || cell.state != FarmCellState.ReadyToHarvest)
        {
            message = "Cây chưa chín để thu hoạch.";
            return false;
        }

        message = "Có thể thu hoạch " + cell.cropId + ".";
        return true;
    }

    public bool Harvest(Vector3Int position, out string message)
    {
        if (!CanHarvest(position, out message))
            return false;

        FarmCell cell = GetCell(position);

        // TODO: Ở phase Inventory sẽ cộng cropId vào túi đồ tại đây.

        cell.state = FarmCellState.Empty;
        cell.wateredToday = false;
        cell.growthDays = 0;
        cell.daysToMature = 0;
        cell.cropId = string.Empty;

        message = "Đã thu hoạch.";
        return true;
    }

    public void HandleNewDay()
    {
        foreach (FarmCell cell in farm.cells)
        {
            // Chỉ cây đã gieo hoặc đang lớn mới cần xử lý ngày mới.
            if (cell.state != FarmCellState.Seeded &&
                cell.state != FarmCellState.Growing)
            {
                continue;
            }

            // Không tưới hôm nay: cây không lớn, nhưng cũng không chết.
            if (!cell.wateredToday)
            {
                continue;
            }

            cell.growthDays++;
            cell.wateredToday = false;

            if (cell.growthDays >= cell.daysToMature)
            {
                cell.state = FarmCellState.ReadyToHarvest;
            }
            else
            {
                cell.state = FarmCellState.Growing;
            }
            RefreshCellVisual(cell);
        }
    }

    private void RefreshCellVisual(FarmCell cell)
    {
        if (farmSoilTilemap != null)
        {
            if (cell.state == FarmCellState.Empty)
            {
                farmSoilTilemap.SetTile(cell.position, null);
            }
            else
            {
                TileBase soilTile = cell.wateredToday
                    ? wetSoilTile
                    : tilledSoilTile;

                farmSoilTilemap.SetTile(cell.position, soilTile);
            }
        }

        if (cropsTilemap == null)
            return;

        TileBase cropTile = null;

        switch (cell.state)
        {
            case FarmCellState.Seeded:
                cropTile = carrotSeedTile;
                break;

            case FarmCellState.Growing:
                cropTile = carrotGrowingTile;
                break;

            case FarmCellState.ReadyToHarvest:
                cropTile = carrotReadyTile;
                break;
        }

        cropsTilemap.SetTile(cell.position, cropTile);
    }
}