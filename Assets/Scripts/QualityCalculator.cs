using UnityEngine;

public static class QualityCalculator
{
    public static CropQuality DetermineQuality(FertilizerType fertilizer)
    {
        float roll = Random.value;
        switch (fertilizer)
        {
            case FertilizerType.DeluxeFertilizer:
                if (roll < 0.15f) return CropQuality.Iridium;
                else if (roll < 0.6f) return CropQuality.Gold;
                else return CropQuality.Silver;
            case FertilizerType.QualityFertilizer:
                if (roll < 0.2f) return CropQuality.Normal;
                else if (roll < 0.65f) return CropQuality.Silver;
                else return CropQuality.Gold;
            case FertilizerType.BasicFertilizer:
                if (roll < 0.2f) return CropQuality.Gold;
                else if(roll < 0.6f) return CropQuality.Silver;
                else return CropQuality.Normal;
            case FertilizerType.None:
            default:
                if(roll < 0.1f) return CropQuality.Gold;
                else if (roll < 0.4f) return CropQuality.Silver;
                else return CropQuality.Normal;
        }
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
