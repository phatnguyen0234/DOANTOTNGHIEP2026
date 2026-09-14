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

    public CropData CropData => cropData;

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        GameObject ground = GameObject.FindWithTag("Ground");
        if (ground != null)
        {
            groundTileMap = ground.GetComponent<Tilemap>();
        }
    }

    public void Init(CropData data)
    {
        if (data != null)
        {
            cropData = data;
            currentGrowthStage = 0;
            isWatered = false;
            isHavest = false;
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
            UpdateSprite(0);
        }
    }

    private void OnEnable()
    {
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.onNewDay += OnNewDay;
        }
    }

    private void OnDisable()
    {
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.onNewDay -= OnNewDay;
        }
    }

    private void OnNewDay()
    {
        if (cropData == null) return;

        if (currentGrowthStage >= cropData.maxStage - 1)
        {
            isHavest = true;
            return;
        }

        if (isWatered)
        {
            currentGrowthStage++;
            UpdateSprite(currentGrowthStage);

            if (groundTileMap != null && driedSoil != null)
            {
                Vector3Int cell = groundTileMap.WorldToCell(transform.position);
                groundTileMap.SetTile(cell, driedSoil);
            }

            isWatered = false;

            if (currentGrowthStage >= cropData.maxStage - 1)
            {
                isHavest = true;
            }
        }
    }

    public void UpdateSprite(int step)
    {
        if (cropData == null || cropData.stageSprites == null || cropData.stageSprites.Length == 0) return;
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

        if (step >= 0 && step < cropData.stageSprites.Length)
        {
            spriteRenderer.sprite = cropData.stageSprites[step];
        }
    }
}
