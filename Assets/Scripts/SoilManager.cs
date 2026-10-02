using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class SoilManager : MonoBehaviour
{
    [SerializeField] private Tilemap groundTileMap;
    [SerializeField] private TileBase soilTile;
    [SerializeField] private TileBase soilWetTile;
    public static SoilManager Instance;
    
    public FarmData currentFarmData;
    public Dictionary<Vector3Int, CropTile> farmCells = new Dictionary<Vector3Int, CropTile>();

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if(TimeManager.Instance != null)
        {
            TimeManager.Instance.onNewDay += HandleNewDay;
        }
    }

    private void OnDisable()
    {
        if(TimeManager.Instance != null)
        {
            TimeManager.Instance.onNewDay -= HandleNewDay;
        }
    }

    public bool IsTilled(Vector3Int pos)
    {
        FarmCell cell = currentFarmData.GetCell(pos);
        return cell != null && cell.state == FarmCellState.Tilled;
    } 

    public bool CanWater(Vector3Int pos)
    {
        FarmCell cell = currentFarmData.GetCell(pos);
        return cell != null && cell.state != FarmCellState.Empty && !cell.wateredToday;
    }

    public void Tilled(Vector3Int pos)
    {
        FarmCell cell = currentFarmData.GetCell(pos);
        if(cell == null)
        {
            cell = currentFarmData.SetCell(pos);
        }
        if(cell.Till())
        {
            groundTileMap.SetTile(pos, soilTile);
        }
    }


    public void PlantCrop(Vector3Int pos, CropData cropData, CropTile cropTile)
    {
        FarmCell cell = currentFarmData.GetCell(pos);
        if(cell != null && cell.Plant(cropData.id, cropData.maxStage-1))
        {
            farmCells[pos] = cropTile;
        }
    }

    public void WaterSoil(Vector3Int pos)
    {
        FarmCell cell = currentFarmData.GetCell(pos);
        if (cell != null && cell.Water())
        {
            groundTileMap.SetTile(pos, soilWetTile);
        }
    }

    public void HandleNewDay()
    {
        foreach(FarmCell cell in currentFarmData.cells)
        {
            if(farmCells.TryGetValue(cell.position, out CropTile croptile) && croptile != null)
            {
                croptile.Grow(cell.wateredToday);
                cell.growthDays = croptile.currentGrowthStage;
                if (croptile.isHavest)
                {
                    cell.state = FarmCellState.ReadyToHarvest;
                }
                else if (cell.growthDays > 0)
                {
                    cell.state = FarmCellState.Growing;
                }
            }

            if (cell.wateredToday)
            {
                groundTileMap.SetTile(cell.position, soilTile);
                cell.wateredToday = false;
            }
        }
    }

    [ContextMenu("Get Farm Data JSON")]
    public void GetFarmDataJson()
    {
        string json = JsonUtility.ToJson(currentFarmData, true);
        PlayerPrefs.SetString("FarmData", json);
        PlayerPrefs.Save();
    }

    public void LoadFarmDataJson()
    {
        string json = PlayerPrefs.GetString("FarmData");
        currentFarmData = JsonUtility.FromJson<FarmData>(json);

        ClearMap();
    }

    [ContextMenu("Clear Map")]
    private void ClearMap()
    {
        foreach(CropTile pos in farmCells.Values)
        {
            if (pos != null)
            {
                Destroy(pos.gameObject);
            }
        }
        farmCells.Clear();
        groundTileMap.ClearAllTiles();
    }


}
