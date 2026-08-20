using UnityEngine;
using UnityEngine.Tilemaps;

public class GridManager : MonoBehaviour
{
    public Tilemap groundTilemap;

    public GameObject treePrefab;
    public GameObject rockPrefab;

    public float treeSpawnChance = 0.1f;
    public float rockSpawnChance = 0.05f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GenerateResources();
    }

    // Update is called once per frame
    void Update()
    {

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
                    float randomValue = Random.value;
                    if (randomValue < treeSpawnChance)
                    {
                        Instantiate(treePrefab, groundTilemap.GetCellCenterWorld(cellPosition), Quaternion.identity);
                    }
                    else if (randomValue < treeSpawnChance + rockSpawnChance)
                    {
                        Instantiate(rockPrefab, groundTilemap.GetCellCenterWorld(cellPosition), Quaternion.identity);
                    }
                }
            }
        }
    }
}
