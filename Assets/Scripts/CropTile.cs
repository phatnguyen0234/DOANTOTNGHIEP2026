using UnityEngine;
using UnityEngine.Tilemaps;

public class CropTile : MonoBehaviour
{
    [SerializeField] private CropData cropData;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Tilemap groundTileMap;
    [SerializeField] private TileBase driedSoil;
    public int currentGrowthStage = 0;
    public bool isWatered { get; set; } = false;
    public bool isHavest { get; set; } = false;

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
        if (currentGrowthStage == cropData.maxStage - 1)
        {
            isHavest = true;
            return;
        }
        if (isWatered)
        {
            currentGrowthStage++;
            UpdateSprite(currentGrowthStage);
            Vector3Int cell = groundTileMap.WorldToCell(transform.position);
            groundTileMap.SetTile(cell, driedSoil);
            isWatered = false;
        }
    }

    public void UpdateSprite(int step)
    {
        Debug.Log(step);
        spriteRenderer.sprite = cropData.stageSprites[step];
    }
}
