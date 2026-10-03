using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.IO;

public class SoilManager : MonoBehaviour
{
    [SerializeField] private Tilemap groundTileMap;
    [SerializeField] private TileBase soilTile;
    [SerializeField] private TileBase soilWetTile;
    [SerializeField] private List<CropData> cropDataList;
    [SerializeField] private CropTile cropTilePrefab;
    public static SoilManager Instance;
    
    public FarmData currentFarmData;
    public Dictionary<Vector3Int, CropTile> farmCells = new Dictionary<Vector3Int, CropTile>();

    

    private void Awake()
    {
        Instance = this;
        LoadFarmDataJson();
    }

    private void Start()
    {
        if(TimeManager.Instance != null)
        {
            TimeManager.Instance.onNewDay += HandleNewDay;
        }
        ReBuildFarm();
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
        string filepath = Path.Combine(Application.persistentDataPath, "FarmData.json");
        File.WriteAllText(filepath, json);
    }

    public void LoadFarmDataJson()
    {
        string filepath = Path.Combine(Application.persistentDataPath, "FarmData.json");
        if(!File.Exists(filepath))
        {
            return;
        }
        string json = File.ReadAllText(filepath);
        currentFarmData = JsonUtility.FromJson<FarmData>(json);
    }

    private void ReBuildFarm()
    {
        ClearFarm();
        foreach(FarmCell cell in currentFarmData.cells)
        {
            if (cell.wateredToday)
            {
                groundTileMap.SetTile(cell.position, soilWetTile);

            }
            else if(cell.state != FarmCellState.Empty)
            {
                groundTileMap.SetTile(cell.position, soilTile);
            }

            if (!string.IsNullOrEmpty(cell.cropId))
            {
                CropData cropData = GetCropData(cell.cropId);
                Vector3 position = groundTileMap.GetCellCenterWorld(cell.position);
                CropTile cropTile = Instantiate(cropTilePrefab, position, Quaternion.identity, transform);
                cropTile.Init(cropData, cell.growthDays);
                farmCells[cell.position] = cropTile;
            }
        }
    }

    private void ClearFarm()
    {
        foreach(CropTile crop in farmCells.Values)
        {
            if (crop != null)
            {
                Destroy(crop.gameObject);
            }
        }
        farmCells.Clear();
    }

    public CropData GetCropData(string cropId)
    {
        foreach(CropData cropData in cropDataList)
        {
            if (cropData.id == cropId)
            {
                return cropData;
            }
        }
        return null;
    }

    [ContextMenu("Open json")]
    public void OpenJson()
    {
        string filepath = Path.Combine(Application.persistentDataPath, "FarmData.json");
        if (File.Exists(filepath))
        {
            Application.OpenURL(filepath);
        }
    }
}
