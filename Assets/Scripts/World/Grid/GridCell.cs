[System.Serializable]
public class GridCell
{
    public Vector3Int Position { get; private set; }

    public GroundData Ground { get; set; }       // nền cơ bản của ô

    public SurfaceInstance Surface { get; set; } // vật thể nằm trên nền

    public FarmData Farm { get; private set; }

    public string OccupiedById { get; set; }

    public GridCell(Vector3Int position)
    {
        Position = position;
        Farm = new FarmData();
    }

    public bool IsOccupied =>
        !string.IsNullOrEmpty(OccupiedById);
}