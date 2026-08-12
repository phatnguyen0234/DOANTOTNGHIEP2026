
public class FarmVisualManager : MonoBehaviour
{
    [SerializeField]
    private WorldGrid worldGrid;

    [SerializeField]
    private FarmManager farmManager;

    private void OnEnable()
    {
        farmManager.CellChanged += Refresh;
    }

    private void OnDisable()
    {
        farmManager.CellChanged -= Refresh;
    }

    private void Refresh(Vector3Int position)
    {
        GridCell cell =
            worldGrid.GetCell(position);

        // đọc FarmData
        // rồi render Tilemap
    }
}