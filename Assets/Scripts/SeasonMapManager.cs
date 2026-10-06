using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// Đổi hình map theo mùa: nghe TimeManager.OnSeasonChanged và thay mọi tile địa hình (cỏ, đất, rừng, viền nước, vách)
// sang phiên bản của mùa mới theo bảng SeasonTileSet (tạo bằng Tools > Map > Build Season Tiles).
// Khi vào scene sẽ áp ngay mùa hiện tại của TimeManager. Tile không có trong bảng (đất cuốc, cầu, nước...) giữ nguyên.
public class SeasonMapManager : MonoBehaviour
{
    [Tooltip("Bảng tile 4 mùa (tự gán khi chạy Tools > Map > Build Season Tiles).")]
    [SerializeField] private SeasonTileSet tileSet;

    [Tooltip("Các tilemap đổi theo mùa. Để trống = mọi tilemap trong scene.")]
    [SerializeField] private List<Tilemap> tilemaps = new List<Tilemap>();

    [Tooltip("Phím thử chuyển hình map sang mùa kế tiếp (chỉ đổi hình, không đổi mùa của TimeManager). None = tắt.")]
    [SerializeField] private KeyCode debugNextSeasonKey = KeyCode.None;

    public Season CurrentMapSeason { get; private set; } = Season.Spring;

    // Mùa đang hiển thị, dùng chung cho các object đổi theo mùa (SeasonalSprite) khi chúng được bật.
    public static Season ActiveSeason { get; private set; } = Season.Spring;

    private void OnEnable()
    {
        TimeManager.OnSeasonChanged += ApplySeason;
    }

    private void OnDisable()
    {
        TimeManager.OnSeasonChanged -= ApplySeason;
    }

    private void Start()
    {
        TimeManager timeManager = FindAnyObjectByType<TimeManager>();
        ApplySeason(timeManager != null ? timeManager.currentSeason : Season.Spring);
    }

    private void Update()
    {
        if (debugNextSeasonKey != KeyCode.None && Input.GetKeyDown(debugNextSeasonKey))
        {
            ApplySeason((Season)(((int)CurrentMapSeason + 1) % 4));
        }
    }

    public void ApplySeason(Season season)
    {
        CurrentMapSeason = season;
        ActiveSeason = season;

        // Cây, bụi... đổi sprite theo mùa.
        SeasonalSprite.ApplyAll(season);

        if (tileSet == null)
        {
            Debug.LogWarning("[SeasonMapManager] Chưa gán SeasonTileSet, chỉ đổi được cây / bụi.", this);
            return;
        }

        Dictionary<TileBase, TileBase> lookup = tileSet.BuildLookup(season);
        int changed = 0;
        foreach (Tilemap tilemap in TargetTilemaps())
        {
            changed += ApplyToTilemap(tilemap, lookup);
        }

        Debug.Log($"[SeasonMapManager] Đổi map sang mùa {season} ({changed} ô).");
    }

    private IEnumerable<Tilemap> TargetTilemaps()
    {
        if (tilemaps != null && tilemaps.Count > 0) return tilemaps;
        return FindObjectsByType<Tilemap>(FindObjectsInactive.Include);
    }

    // Đổi các ô có tile nằm trong bảng tra; trả về số ô đã đổi. Dùng chung cho cả xem trước trong Editor.
    public static int ApplyToTilemap(Tilemap tilemap, Dictionary<TileBase, TileBase> lookup)
    {
        if (tilemap == null) return 0;

        BoundsInt bounds = tilemap.cellBounds;
        TileBase[] tiles = tilemap.GetTilesBlock(bounds);
        List<Vector3Int> positions = new List<Vector3Int>();
        List<TileBase> newTiles = new List<TileBase>();

        int index = 0;
        foreach (Vector3Int position in bounds.allPositionsWithin)
        {
            TileBase tile = tiles[index++];
            if (tile != null && lookup.TryGetValue(tile, out TileBase replacement))
            {
                positions.Add(position);
                newTiles.Add(replacement);
            }
        }

        if (positions.Count > 0) tilemap.SetTiles(positions.ToArray(), newTiles.ToArray());
        return positions.Count;
    }
}
