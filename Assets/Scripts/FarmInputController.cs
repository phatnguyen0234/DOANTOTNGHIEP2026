using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.EventSystems;
using Unity.VisualScripting;


public class  FarmInputController : MonoBehaviour
{
    [SerializeField] private Tilemap groundTileMap;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Transform player;
   // [SerializeField] private Tilemap farmSoilTileMap;
    [SerializeField] private TileBase soilTile;

    private Vector3Int targetCell;

    public FarmTool CurrentTool { get; private set; }

    private void Update()
    {
        SelectTool();
        Use();
    }
    
    public void SelectTool()
    {
        if(Input.GetKeyDown(KeyCode.Alpha1)) CurrentTool = FarmTool.Hoe;
        else if(Input.GetKeyDown(KeyCode.Alpha2)) CurrentTool = FarmTool.Seed;
        else if (Input.GetKeyDown(KeyCode.Alpha3)) CurrentTool = FarmTool.Water;
        else if (Input.GetKeyDown(KeyCode.Alpha4)) CurrentTool = FarmTool.Harvest;
    }

    public void Use()
    {
        if(!Input.GetMouseButtonDown(0)) return;
        switch (CurrentTool)
        {
            case FarmTool.Hoe:
                Hoe();
                break;
            case FarmTool.Seed:
                Seed();
                break;
            //case FarmTool.Water:
            //    Water();
            //    break;
            //case FarmTool.Harvest:
            //    Harvest();
            //    break;
        }
    }
    private void Hoe()
    {
        Vector3 posWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        posWorld.z = 0f;
        Vector3Int posCell = groundTileMap.WorldToCell(posWorld);
        Vector3Int playerCell = groundTileMap.WorldToCell(player.transform.position);
        Vector3Int distance = posCell - playerCell;
        Vector2 disWorld = (Vector2)(posWorld - player.transform.position);
        if (CheckDistance(distance))
        {
            targetCell = posCell;
            playerMovement.TryUseHoe(disWorld);
        }
        else
        {
            targetCell = playerCell + Offset(playerMovement.FacingDirection);
            playerMovement.TryUseHoe(playerMovement.FacingDirection);
        }
    }
    private Vector3Int Offset(Vector2 direction)
    {
        if(Mathf.Abs(direction.x) >= Mathf.Abs(direction.y))
        {
            if(direction.x > 0)
            {
                return Vector3Int.right;
            }
            else
            {
                return Vector3Int.left;
            }
        }
        else
        {
            if(direction.y > 0)
            {
                return Vector3Int.up;
            }
            else
            {
                return Vector3Int.down;
            }
        }
    }

    public void OnHoeAnimationComplete()
    {
        groundTileMap.SetTile(targetCell, soilTile);
    }

    private bool CheckDistance(Vector3Int distance)
    {
        if(distance != Vector3Int.zero && Mathf.Abs(distance.x) <= 1 && Mathf.Abs(distance.y) <= 1)
        {
            return true;
        }
        return false;
    }
    
    public void Seed()
    {
        Debug.Log("Seed");
    }
}