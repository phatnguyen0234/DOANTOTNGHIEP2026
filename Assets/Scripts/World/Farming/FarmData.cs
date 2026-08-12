[System.Serializable]
public class FarmData
{
    public bool IsTilled;
    public bool IsWatered;
    public float Moisture;
    public float Fertility;
    public float PH;

    public CropInstance Crop;

    public bool HasCrop => Crop != null;
}
