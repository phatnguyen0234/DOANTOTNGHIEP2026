using UnityEngine;
using UnityEngine.Tilemaps;

// Define Tree Group structure (Visible in Inspector)
[System.Serializable]
public struct TreeGroup
{
    public string groupName; // E.g., "Pine Trees", "Fruit Trees"
    public GameObject[] prefabs; // Assign trees of the same type/tag here
}

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }
    public Tilemap groundTilemap;

    [Header("Resource Groups")]
    public TreeGroup[] treeGroups; // Array of tree groups instead of single trees
    public GameObject[] rockPrefabs;

    [Header("Sparse Random Settings")]
    [Range(0f, 1f)] public float sparseTreeChance = 0.02f;
    [Range(0f, 1f)] public float sparseRockChance = 0.05f;

    [Header("Cluster Settings (Perlin Noise)")]
    public float noiseScale = 0.15f;

    [Range(0f, 1f)] public float treeClusterThreshold = 0.8f;

    [Range(0f, 1f)] public float rockClusterThreshold = 0.1f;

    [Range(0f, 1f)] public float rockClusterDensity = 0.4f;

    // Offsets for unique map generation per run
    private float offsetX, offsetY;
    private float biomeOffsetX, biomeOffsetY;

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Initialize random offsets
        offsetX = Random.Range(0f, 9999f);
        offsetY = Random.Range(0f, 9999f);
        biomeOffsetX = Random.Range(0f, 9999f);
        biomeOffsetY = Random.Range(0f, 9999f);

        GenerateResources();
    }

    private void GenerateResources()
    {
        BoundsInt bounds = groundTilemap.cellBounds;

        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int cellPosition = new Vector3Int(x, y, 0);

                if (groundTilemap.HasTile(cellPosition))
                {
                    Vector3 spawnPos = groundTilemap.GetCellCenterWorld(cellPosition);

                    float pX = x * noiseScale + offsetX;
                    float pY = y * noiseScale + offsetY;
                    float clusterNoise = Mathf.PerlinNoise(pX, pY);

                    // --- HANDLE CLUSTERS ---
                    if (clusterNoise > treeClusterThreshold && treeGroups.Length > 0)
                    {
                        // 1. Spawn same-type Tree Clusters
                        float biomeNoise = Mathf.PerlinNoise(x * 0.02f + biomeOffsetX, y * 0.02f + biomeOffsetY);

                        // Pick a random tree group based on Biome Noise
                        int groupIndex = Mathf.FloorToInt(biomeNoise * treeGroups.Length);
                        groupIndex = Mathf.Clamp(groupIndex, 0, treeGroups.Length - 1);
                        TreeGroup selectedGroup = treeGroups[groupIndex];

                        // Pick a random tree prefab from the selected group
                        if (selectedGroup.prefabs.Length > 0)
                        {
                            int treeIndex = Random.Range(0, selectedGroup.prefabs.Length);
                            Instantiate(selectedGroup.prefabs[treeIndex], spawnPos, Quaternion.identity);
                        }
                        continue;
                    }
                    else if (clusterNoise < rockClusterThreshold && rockPrefabs.Length > 0)
                    {
                        // 2. Sparse Rock Clusters
                        // Spawn rock only if it passes the density check
                        if (Random.value < rockClusterDensity)
                        {
                            int randomIndex = Random.Range(0, rockPrefabs.Length);
                            Instantiate(rockPrefabs[randomIndex], spawnPos, Quaternion.identity);
                        }
                        continue;
                    }

                    // --- HANDLE SPARSE GENERATION ---
                    float randomValue = Random.value;
                    if (randomValue < sparseTreeChance && treeGroups.Length > 0)
                    {
                        // Spawn sparse tree: pick random group, then random tree
                        int gIndex = Random.Range(0, treeGroups.Length);
                        if (treeGroups[gIndex].prefabs.Length > 0)
                        {
                            int tIndex = Random.Range(0, treeGroups[gIndex].prefabs.Length);
                            Instantiate(treeGroups[gIndex].prefabs[tIndex], spawnPos, Quaternion.identity);
                        }
                    }
                    else if (randomValue < sparseTreeChance + sparseRockChance && rockPrefabs.Length > 0)
                    {
                        // Spawn sparse rock
                        int randomIndex = Random.Range(0, rockPrefabs.Length);
                        Instantiate(rockPrefabs[randomIndex], spawnPos, Quaternion.identity);
                    }
                }
            }
        }
    }
}