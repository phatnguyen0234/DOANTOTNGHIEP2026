using UnityEngine;

[CreateAssetMenu(fileName = "NewFertilizer", menuName = "Farm/Fertilizer")]
public class Fertilizer : ScriptableObject
{
    [SerializeField] private string id;
    [SerializeField] private string name;
    [SerializeField] private string description;
    [SerializeField] private Sprite icon;
    [SerializeField] private FertilizerType fertilizerType;
}
