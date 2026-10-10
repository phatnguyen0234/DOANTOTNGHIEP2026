using System;
using UnityEngine;

[Serializable]
public class CellData
{
    public Vector3Int position;

    public CellData(Vector3Int position)
    {
        this.position = position;
    }
}
