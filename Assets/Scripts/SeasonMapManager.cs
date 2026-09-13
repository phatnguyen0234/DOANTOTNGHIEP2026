using UnityEngine;
using UnityEngine.Tilemaps;

public class SeasonMapManager : MonoBehaviour
{
    public Tilemap groundTilemap;

    [Header("Tileset")]
    public TileBase[] springTiles;
    public TileBase[] summerTiles;
    public TileBase[] autumnTiles;
    public TileBase[] winterTiles;

    private Season currentMapSeason = Season.Spring;

    private void OnEnable()
    {
        TimeManager.OnSeasonChanged += ChangeSeasonMap;
    }

    private void OnDisable()
    {
        TimeManager.OnSeasonChanged -= ChangeSeasonMap;
    }

    private void ChangeSeasonMap(Season newSeason)
    {
        if (currentMapSeason == newSeason) return;

        TileBase[] oldTiles = GetTilesForSeason(currentMapSeason);
        TileBase[] newTiles = GetTilesForSeason(newSeason);

        if (oldTiles.Length == newTiles.Length)
        {
            for (int i = 0; i < oldTiles.Length; i++)
            {
                if (oldTiles[i] != null && newTiles[i] != null)
                {
                    groundTilemap.SwapTile(oldTiles[i], newTiles[i]);
                }
            }
        }
        else
        {
            Debug.LogWarning("So luong kh bang nhau");
        }

        currentMapSeason = newSeason;
    }

    private TileBase[] GetTilesForSeason(Season season)
    {
        switch (season)
        {
            case Season.Summer: return summerTiles;
            case Season.Autumn: return autumnTiles;
            case Season.Winter: return winterTiles;
            default: return springTiles;
        }
    }
}