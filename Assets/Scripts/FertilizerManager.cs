using UnityEngine;

public static class FertilizerManager 
{

    public static float GetSpeedBonus(FertilizerType fertilizer) => fertilizer switch
    {
        FertilizerType.SpeedGro => 0.10f,
        FertilizerType.DeluxeSpeedGro => 0.25f,
        FertilizerType.HyperSpeedGro => 0.33f,
        _ => 0f
    };

    public static int CalculateAdjustedGrowthDays(int baseGrowthDays, FertilizerType fertilizer)
    {
        float speedBonus = GetSpeedBonus(fertilizer);
        int newGrowthDays;
        if (speedBonus > 0)
        {
            newGrowthDays = Mathf.Max(1, Mathf.RoundToInt(baseGrowthDays * (1f - speedBonus)));
            return newGrowthDays;
        }
        return baseGrowthDays;
    }

    public static bool ShouldRetainWater(FertilizerType fertilizer) 
    {
        float retainChance = fertilizer switch
        {
            FertilizerType.BasicRetainingSoil => 0.33f,
            FertilizerType.QualityRetainingSoil => 0.66f,
            FertilizerType.DeluxeRetainingSoil => 1.00f,
            _ => 0f
        };
        if (retainChance >= 1f) return true;
        if(retainChance > 0f)
        {
            float random = Random.value;
            if (random <= retainChance) return true;
        }
        return false;
    }
}
