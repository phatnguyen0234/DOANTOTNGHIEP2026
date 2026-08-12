using UnityEngine;

[CreateAssetMenu(menuName = "_Game/World/Surface Data")]
public class SurfaceData : ScriptableObject
{
    public string Id;
    public string DisplayName;

    public bool BlocksMovement;

    public ToolType RequiredTool;

    public int MaxHealth = 1;
}