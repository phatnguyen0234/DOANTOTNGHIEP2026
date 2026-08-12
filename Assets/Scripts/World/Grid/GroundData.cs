using UnityEngine;

[CreateAssetMenu(menuName = "_Game/World/Ground Data")]
public class GroundData : ScriptableObject
{
    public string Id;
    public string DisplayName;

    public bool IsWalkable;
    public bool IsFarmable;
    public bool CanBuild;
}