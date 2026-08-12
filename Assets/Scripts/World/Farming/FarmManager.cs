
public class FarmManager : MonoBehaviour
{
    [SerializeField]
    private WorldGrid worldGrid;

    public event System.Action<Vector3Int> CellChanged;

    public bool CanTill(Vector3Int position)
    {
        var cell = worldGrid.GetCell(position);

        if (cell == null)
            return false;

        if (!cell.Ground.IsFarmable)
            return false;

        if (cell.Surface != null)
            return false;

        if (cell.IsOccupied)
            return false;

        if (cell.Farm.IsTilled)
            return false;

        return true;
    }

    public bool Till(Vector3Int position)
    {
        if (!CanTill(position))
            return false;

        var cell = worldGrid.GetCell(position);

        cell.Farm.IsTilled = true;

        CellChanged?.Invoke(position);

        return true;
    }
}