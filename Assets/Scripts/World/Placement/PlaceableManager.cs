
public class PlaceableManager : MonoBehaviour
{
    [SerializeField]
    private WorldGrid worldGrid;

    private readonly Dictionary<string, PlaceableInstance> instances = new();

    public bool CanPlace(
        PlaceableData data,
        Vector3Int origin)
    {
        foreach (var position in GetOccupiedCells(data, origin))
        {
            var cell = worldGrid.GetCell(position);

            if (cell == null)
                return false;

            if (!cell.Ground.CanBuild)
                return false;

            if (cell.Surface != null)
                return false;

            if (cell.IsOccupied)
                return false;

            if (cell.Farm.HasCrop)
                return false;
        }

        return true;
    }
}