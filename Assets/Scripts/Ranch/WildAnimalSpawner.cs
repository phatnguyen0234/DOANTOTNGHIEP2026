using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;

// Thả thú hoang (thỏ, hươu, cáo) lên các ô rừng đi được, cách xa người chơi; sang ngày mới thì bù lại số con đã bị thuần hoá.
// Thú hoang không lưu vào save: mỗi lần vào Main sẽ thả lại.
public class WildAnimalSpawner : MonoBehaviour
{
    [Serializable]
    public class Entry
    {
        public AnimalSpecies species;
        [Min(0)] public int count = 3;
        [Min(1f)] public float wanderRadius = 5f;
    }

    [SerializeField] private List<Entry> entries = new List<Entry>();
    [SerializeField] private WildAnimal wildPrefab;
    [Tooltip("Tilemap cỏ rừng. Để trống = tự tìm tilemap tên 'Forest'.")]
    [SerializeField] private Tilemap forestTilemap;
    [SerializeField, Min(0f)] private float minDistanceFromPlayer = 12f;

    private readonly List<WildAnimal> alive = new List<WildAnimal>();
    private readonly List<Vector3Int> forestCells = new List<Vector3Int>();
    private TimeManager subscribedTime;

    private IEnumerator Start()
    {
        // Chờ lưới ô đi được quét xong để không thả con vật vào chỗ có cây, vách.
        float waited = 0f;
        while ((WalkableGrid.Instance == null || !WalkableGrid.Instance.IsBuilt) && waited < 3f)
        {
            waited += Time.deltaTime;
            yield return null;
        }

        CollectCells();
        Populate();

        if (TimeManager.Instance != null)
        {
            subscribedTime = TimeManager.Instance;
            subscribedTime.onNewDay += Populate;
        }
    }

    private void OnDisable()
    {
        if (subscribedTime != null) subscribedTime.onNewDay -= Populate;
        subscribedTime = null;
    }

    public void NotifyRemoved(WildAnimal animal)
    {
        alive.Remove(animal);
    }

    private void CollectCells()
    {
        forestCells.Clear();
        if (forestTilemap == null)
        {
            foreach (Tilemap tilemap in FindObjectsByType<Tilemap>())
            {
                if (tilemap.name == "Forest") forestTilemap = tilemap;
            }
        }
        if (forestTilemap == null) return;

        WalkableGrid grid = WalkableGrid.Instance;
        BoundsInt bounds = forestTilemap.cellBounds;
        foreach (Vector3Int cell in bounds.allPositionsWithin)
        {
            if (!forestTilemap.HasTile(cell)) continue;
            if (grid != null && grid.IsBuilt && !grid.IsWalkable(cell)) continue;
            forestCells.Add(cell);
        }
    }

    private void Populate()
    {
        alive.RemoveAll(a => a == null);
        if (forestCells.Count == 0 || wildPrefab == null) return;

        foreach (Entry entry in entries)
        {
            if (entry.species == null) continue;
            int current = alive.FindAll(a => a.Species == entry.species).Count;
            for (int i = current; i < entry.count; i++) SpawnOne(entry);
        }
    }

    private void SpawnOne(Entry entry)
    {
        Vector3 playerPosition = PlayerMovement.instance != null ? PlayerMovement.instance.transform.position : Vector3.positiveInfinity;
        for (int attempt = 0; attempt < 20; attempt++)
        {
            Vector3Int cell = forestCells[Random.Range(0, forestCells.Count)];
            Vector3 position = forestTilemap.GetCellCenterWorld(cell);
            if (Vector2.Distance(position, playerPosition) < minDistanceFromPlayer) continue;

            WildAnimal animal = Instantiate(wildPrefab, position, Quaternion.identity, transform);
            animal.Spawner = this;
            animal.Setup(entry.species, Random.Range(0, Mathf.Max(1, entry.species.variants.Length)), position, entry.wanderRadius);
            alive.Add(animal);
            return;
        }
    }
}
