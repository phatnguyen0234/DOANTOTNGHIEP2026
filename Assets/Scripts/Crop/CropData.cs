using UnityEngine;

[CreateAssetMenu(fileName = "New Crop", menuName = "Farm/Crop Data")]
public class CropData : ScriptableObject
{
    public string id;
    public string cropName;

    public float growthTime;
    public int maxStage;

    public Sprite[] stageSprites;
}