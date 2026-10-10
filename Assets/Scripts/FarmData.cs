using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class FarmData
{
    public string farmId;
    public string playerId;

    // List để sau này dễ save/load JSON.
    public List<FarmCell> cells = new List<FarmCell>();
    public List<TreeCell> treeCells = new List<TreeCell>();
    public List<RockCell> rockCells = new List<RockCell>();

    public FarmCell GetCell(Vector3Int pos)
    {
        foreach(FarmCell cell in cells)
        {
            if(cell.position == pos)
            {
                return cell;
            }
        }
        return null;
    }

    public FarmCell SetCell(Vector3Int pos)
    {
        FarmCell cell = new FarmCell(pos);
        cells.Add(cell);
        return cell;
    }

    public TreeCell GetTreeCell(Vector3Int pos)
    {
        foreach(TreeCell cell in treeCells)
        {
            if(cell.position == pos)
            {
                return cell;
            }
        }
        return null;
    }

    public TreeCell SetTreeCell(Vector3Int pos, string treeId)
    {
        TreeCell cell = new TreeCell(pos, treeId);
        treeCells.Add(cell);
        return cell;
    }

    public RockCell GetRockCell(Vector3Int pos)
    {
        foreach (RockCell cell in rockCells)
        {
            if (cell.position == pos)
            {
                return cell;
            }
        }
        return null;
    }

    public RockCell SetRockCell(Vector3Int pos)
    {
        RockCell cell = new RockCell(pos);
        rockCells.Add(cell);
        return cell;
    }
}