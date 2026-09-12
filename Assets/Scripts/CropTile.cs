using UnityEngine;
using UnityEngine.Tilemaps;

public class CropTile : MonoBehaviour
{
    [SerializeField] private CropData cropData;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Tilemap groundTileMap;
    [SerializeField] private TileBase driedSoil;
    private int currentGrowthStage = 0;

    private void Awake()
    {
        GameObject ground = GameObject.FindWithTag("Ground");
        groundTileMap = ground.GetComponent<Tilemap>();
    }
    private void OnEnable()
    {
        TimeManager.Instance.onNewDay += OnNewDay;
    }

    private void OnDisable()
    {
        TimeManager.Instance.onNewDay -= OnNewDay;
    }

    private void OnNewDay()
    {
        if (currentGrowthStage > cropData.maxStage - 1) return;
        if (FarmInputController.isWater)
        {
            currentGrowthStage++;
            Debug.Log(currentGrowthStage);
            spriteRenderer.sprite = cropData.stageSprites[currentGrowthStage];
            Vector3Int cell = groundTileMap.WorldToCell(transform.position);
            groundTileMap.SetTile(cell, driedSoil);
            FarmInputController.isWater = false;
        }
    }
}
