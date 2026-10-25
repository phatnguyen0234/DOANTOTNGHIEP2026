using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Crop", menuName = "Farm/Crop Data")]
public class CropData : ScriptableObject
{
    public string id;
    public string cropName;

    public float growthTime;
    public int maxStage;

    public Sprite[] stageSprites;

    [Header("Drop Table Configuration")]
    [Tooltip("Bảng tỉ lệ rơi DropTable khi thu hoạch (ưu tiên sử dụng nếu được gán).")]
    [SerializeField] private DropTable dropTable;

    [Header("Harvest Reward (Fallback)")]
    [Tooltip("Vật phẩm ItemData nhận được khi thu hoạch cây này (dùng khi không có DropTable).")]
    public ItemData harvestItem;

    [Tooltip("Số lượng vật phẩm thu hoạch được.")]
    public int harvestAmount = 1;

    public List<Season> allowedSeason;
    public int maxUnwateredDays = 2;
    public bool isReharvest;
    public int regrowStage;
    public DropTable DropTable => dropTable;
}