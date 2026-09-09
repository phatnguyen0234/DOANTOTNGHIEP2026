using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.EventSystems;
using Unity.VisualScripting;

public class  FarmInputController : MonoBehaviour
{
    [SerializeField] private Tilemap groundTileMap;
    [SerializeField] private Tilemap farmSoildTileMap;
    [SerializeField] private Camera mainCamera;

    [SerializeField] private Transform Player;
    [SerializeField] private LayerMask obtacleLayer;

    public Vector3Int standCell { get; private set; }
    public Vector3 standPos { get; private set; }
    private Vector2 size = new Vector2(0.6f,0.6f);
    public static readonly Vector3Int[] adjacentCells = new Vector3Int[]
    {
        Vector3Int.up,
        Vector3Int.down,
        Vector3Int.right,
        Vector3Int.left
    };

    public Vector3Int TargetCell { get; private set;  }
    public bool HasTarget { get; private set;  }

    public FarmTool CurrentTool { get; private set; }

    private void Update()
    {
        if (CurrentTool != FarmTool.Hoe) 
            return;
        if (!Input.GetMouseButtonDown(0)) 
            return;
        if(EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) 
            return;
        Vector3 posWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        posWorld.z = 0;
        Vector3Int cell = groundTileMap.WorldToCell(posWorld);
        if(!CanDig(cell)) 
            return;
        if(!FindAdjacentCell(cell, out Vector3Int adjacentCell))
        {
            Debug.Log("No adjacent cell found");
            return;
        }
        standCell = adjacentCell;
        standPos = groundTileMap.GetCellCenterWorld(standCell);
        TargetCell = cell;
        HasTarget = true;
        Debug.Log($"Target cell: {TargetCell}, Stand cell: {standCell}");
    }

    public bool FindAdjacentCell(Vector3Int targetCell, out Vector3Int adjacentCell)
    {
        adjacentCell = default;
        bool found = false;

        float nearestDistance = float.MaxValue;
        Vector3Int playerCell = groundTileMap.WorldToCell(Player.position);

        foreach(Vector3Int offset in adjacentCells)
        {
            Vector3Int cell = targetCell + offset;

            Debug.Log($"Checking adjacent cell: {cell}");
            //if (!CanStand(cell)) 
            //    return false;

            if (cell == playerCell)
            {
                adjacentCell = cell;
                return true;
            }

            float distance = Vector3.Distance(groundTileMap.GetCellCenterWorld(cell), Player.position);
            if(distance < nearestDistance)
            {
                adjacentCell = cell;
                nearestDistance = distance;
                found = true;
            }    
        }
        return found;
    }

    //public bool CanStand(Vector3Int cell)
    //{
    //    if(!groundTileMap.HasTile(cell)) return false;
    //    Vector3 posWorld = groundTileMap.GetCellCenterWorld(cell);
    //    bool hit = Physics2D.OverlapBox(posWorld, size, 0f, obtacleLayer) != null;
    //    Debug.Log($"CanStand check for cell {cell}: {hit}");
    //    return hit;
    //}

    public bool CanDig(Vector3Int cell)
    {
        if(!groundTileMap.HasTile(cell)) return false;
        if(farmSoildTileMap.HasTile(cell)) return false;
        return true;
    }

    public void SelectTool()
    {
        CurrentTool = FarmTool.Hoe;
    }

    public void ClearTool()
    {
        CurrentTool = FarmTool.None;
    }
}