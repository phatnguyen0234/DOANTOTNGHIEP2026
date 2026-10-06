using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// Bảng tile 4 mùa: mỗi dòng là cùng một tile địa hình (cỏ, đất, viền nước, vách...) ở Xuân / Hạ / Thu / Đông.
// Được tạo tự động bằng menu Tools > Map > Build Season Tiles, SeasonMapManager dùng để đổi map theo mùa.
[CreateAssetMenu(fileName = "SeasonTileSet", menuName = "Farm/Season Tile Set")]
public class SeasonTileSet : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public TileBase spring;
        public TileBase summer;
        public TileBase autumn;
        public TileBase winter;

        public TileBase Get(Season season)
        {
            switch (season)
            {
                case Season.Summer: return summer;
                case Season.Autumn: return autumn;
                case Season.Winter: return winter;
                default: return spring;
            }
        }
    }

    public List<Entry> entries = new List<Entry>();

    // Bảng tra: tile của bất kỳ mùa nào -> tile tương ứng của mùa đích.
    // Nhờ vậy map đang ở mùa nào (kể cả bị lưu lẫn mùa) cũng đổi đúng về mùa đích.
    public Dictionary<TileBase, TileBase> BuildLookup(Season target)
    {
        Dictionary<TileBase, TileBase> lookup = new Dictionary<TileBase, TileBase>();
        foreach (Entry entry in entries)
        {
            TileBase targetTile = entry.Get(target);
            if (targetTile == null) continue;

            foreach (TileBase variant in new[] { entry.spring, entry.summer, entry.autumn, entry.winter })
            {
                if (variant != null && variant != targetTile) lookup[variant] = targetTile;
            }
        }
        return lookup;
    }
}
