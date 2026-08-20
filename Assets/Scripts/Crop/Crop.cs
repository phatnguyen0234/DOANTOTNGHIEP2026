using UnityEngine;

public class Crop : MonoBehaviour
{
    public CropData cropData;

    private int currentStage = 0;
    private float elapsedTime = 0f;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        UpdateSprite();
    }

    private void Update()
    {
        elapsedTime += Time.deltaTime;

        UpdateGrowth();
    }

    private void UpdateGrowth()
    {
        float stageTime = cropData.growthTime / cropData.maxStage;

        int newStage = Mathf.FloorToInt(elapsedTime / stageTime);

        newStage = Mathf.Clamp(
            newStage,
            0,
            cropData.maxStage
        );

        if (newStage != currentStage)
        {
            currentStage = newStage;
            UpdateSprite();
        }
    }

    private void UpdateSprite()
    {
        spriteRenderer.sprite =
            cropData.stageSprites[currentStage];
    }
}