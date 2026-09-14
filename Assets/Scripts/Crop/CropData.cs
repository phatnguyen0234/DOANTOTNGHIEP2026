using UnityEngine;

[CreateAssetMenu(fileName = "New Crop", menuName = "Farm/Crop Data")]
public class CropData : ScriptableObject
{
    public string id;
    public string cropName;

    public float growthTime;
    public int maxStage;

    public Sprite[] stageSprites;

    [Header("Harvest Reward")]
    [Tooltip("Vật phẩm ItemData nhận được khi thu hoạch cây này.")]
    public ItemData harvestItem;

    [Tooltip("Số lượng vật phẩm thu hoạch được.")]
    public int harvestAmount = 1;
}