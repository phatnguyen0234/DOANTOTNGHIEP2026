using System.Collections.Generic;
using UnityEngine;

public class WorldGrid : MonoBehaviour
{
    [SerializeField]
    private Grid grid;

    private readonly Dictionary<Vector3Int, GridCell> cells = new();

    public Vector3Int WorldToCell(Vector3 worldPosition)
    {
        return grid.WorldToCell(worldPosition);
    }

    public Vector3 CellToWorldCenter(Vector3Int position)
    {
        return grid.GetCellCenterWorld(position);
    }

    public GridCell GetCell(Vector3Int position)
    {
        cells.TryGetValue(position, out var cell);
        return cell;
    }

    public GridCell GetOrCreateCell(Vector3Int position)
    {
        if (cells.TryGetValue(position, out var cell))
            return cell;

        cell = new GridCell(position);
        cells.Add(position, cell);

        return cell;
    }
}