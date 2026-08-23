using System;
using UnityEngine;

[Serializable]
public class FarmCell
{
    // Tọa độ cell trên Grid/Tilemap.
    public Vector3Int position;

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

    public FarmCell(Vector3Int position)
    {
        this.position = position;
    }
}