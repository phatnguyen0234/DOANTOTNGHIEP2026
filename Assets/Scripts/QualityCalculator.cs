using UnityEngine;

public static class QualityCalculator
{
    public static CropQuality DetermineQuality()
    {
        float roll = Random.value;

        if (roll < 0.05f) return CropQuality.Gold;
        else if (roll < 0.2f) return CropQuality.Silver;
        else return CropQuality.Normal;

    }

    public static float GetPrice(CropQuality quality) 
    {
        switch (quality)
        {
            case CropQuality.Silver:
                return 1.25f;
            case CropQuality.Gold:
                return 1.5f;
            case CropQuality.Iridium:
                return 2f;
            default:
                return 1f;
        }
    }
}
