
using UnityEngine;

[CreateAssetMenu(menuName = "_Game/Placement/Placeable Data")]
public class PlaceableData : ScriptableObject
{
    public string Id;
    public string DisplayName;

    public GameObject Prefab;

    public Vector2Int Size = Vector2Int.one;

    public bool BlocksMovement = true;
}