using UnityEngine;
using UnityEngine.Tilemaps;

public class CropTile : MonoBehaviour, IDropSource
{
    [SerializeField] private CropData cropData;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Tilemap groundTileMap;
    [SerializeField] private TileBase driedSoil;
    public int currentGrowthStage = 0;
    
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
        Init(data,0);
    }

    public void Init(CropData data, int growthStage)
    {
        if (data != null)
        {
            cropData = data;
            currentGrowthStage = growthStage;
            isHavest = currentGrowthStage >= cropData.maxStage - 1;
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
            UpdateSprite(currentGrowthStage);
        }
    }

    public void Grow(bool isWatered)
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

    #region IDropSource Implementation

    public DropTable GetDropTable()
    {
        return cropData != null ? cropData.DropTable : null;
    }

    public Vector3 GetDropPosition()
    {
        return transform.position;
    }

    public DropContext GetDropContext()
    {
        return new DropContext(gameObject, null, null, 1, 1f, 0f);
    }

    #endregion
}
