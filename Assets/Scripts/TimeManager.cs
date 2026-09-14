using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using System;
public enum Season
{
    Spring,
    Summer,
    Autumn,
    Winter
}

public class TimeManager : MonoBehaviour
{
    [Header("Time Settings")]
    public float currentHour = 12f;
    public float timeSpeed = 1f;
    public int currentDay = 1;
    public int daysPerSeason = 30;
    public Season currentSeason = Season.Spring;

    public static Action<Season> OnSeasonChanged;

    [Header("Sprite and Lighting Settings")]
    public Image clockHandImage;
    public Sprite[] clockHandSprites;
    public Light2D globalLight;
    public Gradient lightColor;
    public AnimationCurve lightIntensity;
    
    public event Action onNewDay; 
    public static TimeManager Instance { get; private set; }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Instance = this;
    }

    // Update is called once per frame
    void Update()
    {
        currentHour += Time.deltaTime * timeSpeed;
        if (currentHour >= 24f)
        {
            currentHour = 0f;
            HandleNewDay(); // --> chuyen HandleNewDay() cua ae xuong duoi
            //Them event ngay moi o day
            onNewDay?.Invoke();
        }
        UpdateClockUI();
        UpdateLighting();
    }
    private void HandleNewDay()
    {
        currentDay++;
        if (currentDay > daysPerSeason)
        {
            currentDay = 1;
            AdvanceToNextSeason();
        }
    }

    private void AdvanceToNextSeason()
    {
        if (currentSeason == Season.Winter)
        {
            currentSeason = Season.Spring;
        }
        else
        {
            currentSeason++;
        }

        OnSeasonChanged?.Invoke(currentSeason); // thong bao su kien thay doi mua
    }

    private void UpdateClockUI()
    {
        if (clockHandImage == null || clockHandSprites.Length == 0) return;

        float dayProgress = currentHour / 24f;

        int spriteIndex = Mathf.FloorToInt(dayProgress * clockHandSprites.Length);
        spriteIndex = Mathf.Clamp(spriteIndex, 0, clockHandSprites.Length - 1); //ep gia tri trong khoang 0 den clockHandSprites.Length - 1
        clockHandImage.sprite = clockHandSprites[spriteIndex];
    }

    private void UpdateLighting()
    {
        if (globalLight == null) return;

        float dayProgress = currentHour / 24f;

        globalLight.color = lightColor.Evaluate(dayProgress); //gan mau sac anh sang
        globalLight.intensity = lightIntensity.Evaluate(dayProgress); //gan cuong do anh sang   
    }
}
