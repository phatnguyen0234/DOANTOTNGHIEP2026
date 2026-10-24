using UnityEngine;
using UnityEngine.Tilemaps;

public class CropTile : MonoBehaviour, IDropSource
{
    [SerializeField] private CropData cropData;
    [SerializeField] private SpriteRenderer spriteRenderer;

    public int currentGrowthStage = 0;
    public bool isWithered;
    public int unWateredDay = 0;
    public bool isHavest { get; set; } = false;

    public CropData CropData => cropData;

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    public void Init(CropData data)
    {
        Init(data,0,false);
    }

    public void Init(CropData data, int growthStage, bool isWithered)
    {
        if (data != null)
        {
            cropData = data;
            currentGrowthStage = growthStage;
            isHavest = currentGrowthStage >= cropData.maxStage - 1;
            UpdateSprite(currentGrowthStage);
            if (isWithered) SetSprite();
        }
    }

    public void Grow(bool isWatered, FertilizerType fertilizer)
    {
        if (cropData == null) return;
        if (currentGrowthStage >= cropData.maxStage - 1)
        {
            isHavest = true;
            return;
        }
        if (isWatered)
        {
            int step = 1;
            float bonus = FertilizerManager.GetSpeedBonus(fertilizer);
            float random = Random.value;
            if (random <= bonus) step++;
            currentGrowthStage += step;
            if (currentGrowthStage > cropData.maxStage - 1) currentGrowthStage = Mathf.Min(currentGrowthStage, cropData.maxStage - 1);
            UpdateSprite(currentGrowthStage);


            if (currentGrowthStage >= cropData.maxStage - 1)
            {
                isHavest = true;
            }
        }
    }

    public bool Harvest()
    {
        if (!isHavest) return false;
        if(cropData != null && cropData.isReharvest)
        {
            currentGrowthStage = cropData.regrowStage;
            isHavest = false;
            UpdateSprite(currentGrowthStage);
            return true;
        }
        return false;
    }

    public void Check(bool isWatered, FarmCellState state)
    {
        if (isHavest) return;
        if (!cropData.allowedSeason.Contains(TimeManager.Instance.currentSeason))
        {
            SetSprite();
            isWithered = true;
            return;
        }
        if (!isWatered)
        {
            if(state != FarmCellState.Empty && state != FarmCellState.Tilled)
            {
                unWateredDay++;
                if (unWateredDay > cropData.maxUnwateredDays)
                {
                    SetSprite();
                    isWithered = true;
                    return;
                }
            }
        }
        else
        {
            unWateredDay = 0;
        }
    }

    public void UpdateSprite(int step)
    {

        if (step >= 0 && step < cropData.stageSprites.Length)
        {
            spriteRenderer.sprite = cropData.stageSprites[step];
        }
    }

    public void SetSprite()
    {
        spriteRenderer.color = new Color(0.5f, 0.4f, 0.3f, 1f);
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
