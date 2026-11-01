using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.IO;
using Unity.VisualScripting;

public class SoilManager : MonoBehaviour
{
    [SerializeField] private Tilemap groundTileMap;
    [SerializeField] private Tilemap fertilizerTileMap;
    [SerializeField] private TileBase soilTile;
    [SerializeField] private TileBase soilWetTile;
    [SerializeField] private TileBase speedGroTile;
    [SerializeField] private TileBase retainingSoilTile;
    [SerializeField] private TileBase qualityFertilizer;
    [SerializeField] private List<CropData> cropDataList;
    [SerializeField] private CropTile cropTilePrefab;
    public static SoilManager Instance;
    
    public FarmData currentFarmData;
    public Dictionary<Vector3Int, CropTile> farmCells = new Dictionary<Vector3Int, CropTile>();
    public Dictionary<Vector3Int, Tree> treeCells = new Dictionary<Vector3Int, Tree>();
    public Dictionary<Vector3Int, Rock> rockCells = new Dictionary<Vector3Int, Rock>();
    public Dictionary<string, TileBase> dict = new Dictionary<string, TileBase>();
    public int maxunTillDays = 3;
    public SeasonTileSet seasonTileSet;


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
        ReBuildFarm();
        ReBuildTree();
        ReBuildRock();
        BuildTile();
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
        TileBase tile = groundTileMap.GetTile(pos);
        if(cell == null)
        {
            cell = currentFarmData.SetCell(pos);
        }
        if(cell.Till())
        { 
            groundTileMap.SetTile(pos, soilTile);
        }
        if(tile != soilTile)
        {
            cell.originTile = tile;
            cell.originTileName = tile.name;
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

    public void OnHit(Vector3Int pos, Tree tree)
    {
        TreeCell cell = currentFarmData.GetTreeCell(pos);
        if(cell == null)
        {
            cell = currentFarmData.SetTreeCell(pos, tree.treeId);
        }
        cell.currentHits = tree.currentHits;
        cell.isFelled = tree.IsFelled;
    }

    public void OnHitRock(Vector3Int pos)
    {
        RockCell cell = currentFarmData.GetRockCell(pos);
        if (cell == null)
        {
            cell = currentFarmData.SetRockCell(pos);
        }
        cell.isDestroyed = true;
    }

    public void BuildTile()
    {
        BoundsInt bound = groundTileMap.cellBounds;
        TileBase[] tiles = groundTileMap.GetTilesBlock(bound);
        foreach(TileBase tile in tiles)
        {
            if (tile != null && !dict.ContainsKey(tile.name))
            {
                dict[tile.name] = tile;
            }
        }

        if(seasonTileSet != null)
        {
            foreach (SeasonTileSet.Entry entry in seasonTileSet.entries)
            {
                if (entry.spring != null && !dict.ContainsKey(entry.spring.name)) dict[entry.spring.name] = entry.spring;
                if (entry.summer != null && !dict.ContainsKey(entry.summer.name)) dict[entry.summer.name] = entry.summer;
                if (entry.autumn != null && !dict.ContainsKey(entry.autumn.name)) dict[entry.autumn.name] = entry.autumn;
                if (entry.winter != null && !dict.ContainsKey(entry.winter.name)) dict[entry.winter.name] = entry.winter;
            }
        }
    }

    public TileBase GetTile(string name)
    {
        if(dict.TryGetValue(name, out TileBase tile) && tile != null)
        {
            return tile;
        }
        return null;
    }

    public bool ApplyFertilizer(Vector3Int cellPos, FertilizerType fertilizerType)
    {
        FarmCell cell = currentFarmData.GetCell(cellPos);
        if(cell == null)
        {
            cell = currentFarmData.SetCell(cellPos);
        }
        if(cell.state == FarmCellState.Tilled && cell.fertilizer == FertilizerType.None)
        {
            cell.fertilizer = fertilizerType;
            TileBase tile = GetFertilizeTile(fertilizerType);
            fertilizerTileMap.SetTile(cellPos, tile);
            return true;
        }
        return false;
    }

    private TileBase GetFertilizeTile(FertilizerType type) => type switch
    {
        FertilizerType.SpeedGro or FertilizerType.DeluxeSpeedGro or FertilizerType.HyperSpeedGro => speedGroTile,
        FertilizerType.BasicFertilizer or FertilizerType.QualityFertilizer or FertilizerType.DeluxeFertilizer => qualityFertilizer,
        FertilizerType.BasicRetainingSoil or FertilizerType.QualityRetainingSoil or FertilizerType.DeluxeRetainingSoil => retainingSoilTile,
        _ => null
    };

    public void HandleNewDay()
    {
        foreach(FarmCell cell in currentFarmData.cells)
        {
            if(cell.state == FarmCellState.Tilled && string.IsNullOrEmpty(cell.cropId))
            {
                cell.untillDays++;
                if(cell.untillDays >= maxunTillDays)
                {
                    TileBase tile = cell.originTile;
                    if(tile == null)
                    {
                        tile = GetTile(cell.originTileName);
                    }

                    TileBase tileSeason = tile;
                    if(tile != null)
                    {
                        tile = seasonTileSet.GetSeasonTile(tile, TimeManager.Instance.currentSeason);
                    }
                    groundTileMap.SetTile(cell.position, tile);
                    fertilizerTileMap.SetTile(cell.position, null);
                    cell.fertilizer = FertilizerType.None;
                    cell.state = FarmCellState.Empty;
                    cell.wateredToday = false;
                    cell.originTile = null;
                    cell.originTileName = null;
                    cell.untillDays = 0;
                    continue;
                }
            }
            else
            {
                cell.untillDays = 0;
            }
            if (cell.isWithered)
            {
                groundTileMap.SetTile(cell.position, soilTile);
                fertilizerTileMap.SetTile(cell.position, null);
                cell.fertilizer = FertilizerType.None;
                cell.wateredToday = false;
                continue;
            }
            if(farmCells.TryGetValue(cell.position, out CropTile croptile) && croptile != null)
            {
                Debug.Log("Trang thai " + cell.state);
                croptile.Check(cell.wateredToday, cell.state);
                if (croptile.isWithered)
                {
                    groundTileMap.SetTile(cell.position, soilTile);
                    fertilizerTileMap.SetTile(cell.position, null);
                    cell.fertilizer = FertilizerType.None;
                    cell.isWithered = croptile.isWithered;
                    cell.wateredToday = false;
                    continue;
                }
                croptile.Grow(cell.wateredToday, cell.fertilizer);
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
                bool retainWater = FertilizerManager.ShouldRetainWater(cell.fertilizer);
                if (!retainWater)
                {
                    groundTileMap.SetTile(cell.position, soilTile);
                    cell.wateredToday = false;
                }
            }
        }
    }


    public void ReBuildFarm()
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
                cropTile.Init(cropData, cell.growthDays, cell.isWithered);
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

    public void ReBuildTree()
    {
        RegisterTree();
        foreach (TreeCell cell in currentFarmData.treeCells)
        {
            if(treeCells.TryGetValue(cell.position, out Tree tree) && tree != null)
            {
                tree.Init(cell.currentHits, cell.isFelled);
                if(cell.isFelled)
                {
                    treeCells.Remove(cell.position);
                }
            }
        }
    }

    public void RegisterTree()
    {
        treeCells.Clear();
        Tree[] trees = UnityEngine.Object.FindObjectsByType<Tree>();
        foreach(Tree t in trees)
        {
            Vector3Int pos = groundTileMap.WorldToCell(t.transform.position);
            treeCells[pos] = t;
        }
    }

    public void ReBuildRock()
    {
        RegisterRock();
        foreach (RockCell cell in currentFarmData.rockCells)
        {
            if (rockCells.TryGetValue(cell.position, out Rock rock) && rock != null)
            {
                if (cell.isDestroyed)
                {
                    rock.Hit();
                    rockCells.Remove(cell.position);
                }
            }
        }
    }

    private void RegisterRock()
    {
        rockCells.Clear();
        Rock[] rocks = UnityEngine.Object.FindObjectsByType<Rock>();
        foreach (Rock r in rocks)
        {
            Vector3Int pos = groundTileMap.WorldToCell(r.transform.position);
            rockCells[pos] = r;
        }
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


}
