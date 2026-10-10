using System;
using UnityEngine;

[Serializable]
public class RockCell : CellData
{
    public bool isDestroyed = false;
    public RockCell(Vector3Int position) : base(position)
    {
    }
}
