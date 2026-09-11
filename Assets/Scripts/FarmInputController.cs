using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using Unity.VisualScripting;


public class  FarmInputController : MonoBehaviour
{
    [SerializeField] private Tilemap groundTileMap;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Transform player;
    [SerializeField] private Tilemap highlightTileMap;
    [SerializeField] private TileBase soilTile;
    [SerializeField] private TileBase soilWetTile;
    [SerializeField] private TileBase greenTile;
    [SerializeField] private TileBase redTile;
    [SerializeField] private GameObject cropPrefab;
    [SerializeField] private CropData cropData;

    Dictionary<Vector3Int, GameObject> planted = new Dictionary<Vector3Int, GameObject>();

    private Vector3Int targetCellHoe;
    private Vector3Int targetCellWater;

    public FarmTool CurrentTool { get; private set; }

    private void Update()
    {
        SelectTool();
        if(CurrentTool == FarmTool.Seed) HighLight(); 
        else highlightTileMap.ClearAllTiles();
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
            case FarmTool.Water:
                Water();
                break;
                //case FarmTool.Harvest:
                //    Harvest();
                //    break;
        }
    }

    
    public bool CanPlant(Vector3Int cell)
    {
        Vector3Int playerCell = groundTileMap.WorldToCell(player.transform.position);
        Vector3Int distance = cell - playerCell;
        bool isSoil = groundTileMap.GetTile(cell) == soilTile;
        bool isInRange = CheckDistance(distance);
        bool isPlanted = planted.ContainsKey(cell);
        if(isInRange && isSoil && !isPlanted)
        {
            return true;
        }
        return false;
    }

    public void HighLight()
    {
        Vector3 mouWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouWorld.z = 0f;
        Vector3Int mouCell = groundTileMap.WorldToCell(mouWorld);
        highlightTileMap.ClearAllTiles();
        if(CanPlant(mouCell))
        {
            highlightTileMap.SetTile(mouCell, greenTile);
        }
        else highlightTileMap.SetTile(mouCell, redTile);
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
            targetCellHoe = posCell;
            playerMovement.TryUseHoe(disWorld);
        }
        else
        {
            targetCellHoe = playerCell + Offset(playerMovement.FacingDirection); 
            playerMovement.TryUseHoe(playerMovement.FacingDirection);
        }
    }

    public void Seed()
    {
        Vector3 mouWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouWorld.z = 0f;
        Vector3Int mouCell = groundTileMap.WorldToCell(mouWorld);
        if (CanPlant(mouCell))
        {
            Vector3 mouseCenter = groundTileMap.GetCellCenterWorld(mouCell);
            GameObject crop = Instantiate(cropPrefab, mouseCenter, Quaternion.identity);
            crop.GetComponent<SpriteRenderer>().sprite = cropData.stageSprites[0];
            planted.Add(mouCell, crop);
        }
    }

    public void Water()
    {
        Vector3 posWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        posWorld.z = 0f;
        Vector3Int posCell = groundTileMap.WorldToCell(posWorld);
        Vector3Int playerCell = groundTileMap.WorldToCell(player.transform.position);
        Vector3Int distance = posCell - playerCell;
        Vector2 disWorld = (Vector2)(posWorld - player.transform.position);
        bool isSoil = groundTileMap.GetTile(posCell) == soilTile;
        if (CheckDistance(distance))
        { 
            if (isSoil)
            {
                targetCellWater = posCell;
               // Debug.Log($"Water: mouse target | cell: {targetCellWater} | direction: {direction.normalized}");
                playerMovement.UsingWater(disWorld.normalized);
            }
        }
        else
        {
            if (isSoil)
            {
                targetCellWater = playerCell + Offset(playerMovement.FacingDirection);
             //   Debug.Log($"Water: facing target | cell: {targetCellWater} | facing direction: {playerMovement.FacingDirection}");
                playerMovement.UsingWater(playerMovement.FacingDirection);
            }
        }
    }

    public void OnWaterAnimationComplete()
    {
        groundTileMap.SetTile(targetCellWater, soilWetTile);
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
        groundTileMap.SetTile(targetCellHoe, soilTile);
    }

    private bool CheckDistance(Vector3Int distance)
    {
        if(distance != Vector3Int.zero && Mathf.Abs(distance.x) <= 1 && Mathf.Abs(distance.y) <= 1)
        {
            return true;
        }
        return false;
    }
    
    
}