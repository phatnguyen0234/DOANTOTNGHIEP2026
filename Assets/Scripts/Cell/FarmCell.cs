using System;
using UnityEngine;
using UnityEngine.Tilemaps;

[Serializable]
public class FarmCell : CellData
{

    // Trạng thái hiện tại của ô.
    public FarmCellState state = FarmCellState.Empty;

    // Hôm nay player đã tưới ô này chưa?
    public bool wateredToday;

    // Số ngày cây đã phát triển thành công.
    public int growthDays;

    // Số ngày cây cần để chín.
    public int daysToMature;

    // Tạm dùng string; sau này có thể thay bằng CropDefinition.
    public string cropId;

    public bool isWithered;

    public int untillDays = 0;
    public string originTileName;
    public FertilizerType fertilizer = FertilizerType.None;
    [NonSerialized] public TileBase originTile;

    public FarmCell(Vector3Int position) : base(position)
    {
    }

    public bool Till()
    {
        if(state == FarmCellState.Empty)
        {
            state = FarmCellState.Tilled;
            return true;
        }
        return false;
    }

    public bool Plant(string cropId, int daysToMature)
    {
        if (state == FarmCellState.Tilled)
        {
            state = FarmCellState.Seeded;
            this.cropId = cropId;
            this.daysToMature = daysToMature;
            growthDays = 0;
            wateredToday = false;
            return true;
        }
        return false;
    }

    public bool Water()
    {
        if(state != FarmCellState.Empty)
        {
            wateredToday = true;
            return true;
        }
        return false;
    }
}