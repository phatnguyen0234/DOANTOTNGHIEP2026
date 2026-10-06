using UnityEngine;

public class TreeCell 
{
    public Vector3Int position;
    public string treeId;
    public int currentHits;
    public bool isFelled;

    public TreeCell(Vector3Int position, string treeId)
    {
        this.position = position;
        this.treeId = treeId;
    }
}
