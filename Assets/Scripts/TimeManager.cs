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

    [Header("Sleep Settings")]
    [Tooltip("Giờ thức dậy sau khi ngủ.")]
    public float wakeUpHour = 6f;

    public static Action<Season> OnSeasonChanged;

    // Số đêm người chơi đã ngủ nhưng chưa tua giờ: giường nằm trong scene nhà (không có TimeManager),
    // nên TimeManager của Main sẽ tua khi người chơi quay lại.
    private static int pendingSleeps;

    [Header("Sprite and Lighting Settings")]
    public Image clockHandImage;
    public Sprite[] clockHandSprites;
    public Light2D globalLight;
    public Gradient lightColor;
    public AnimationCurve lightIntensity;
    
    public event Action onNewDay; 
    public static TimeManager Instance { get; private set; }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        Instance = this;
    }

    // Update is called once per frame
    void Update()
    {
        // Đã ngủ trong nhà: tua ở frame đầu tiên của Main, lúc SaveManager đã load giờ và SoilManager đã nghe onNewDay.
        while (pendingSleeps > 0)
        {
            pendingSleeps--;
            SleepUntilMorning();
        }

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

        //if (farmManager != null)
        //{
        //    farmManager.HandleNewDay(); // "chuyen phan goi farmManager.HandleNewDay() o day"
        //}
    }

    // Người chơi đi ngủ (gọi từ Bed). TimeManager đang chạy thì tua ngay, còn đang ở trong nhà thì để dành tới khi về Main.
    public static void GoToSleep()
    {
        if (Instance != null)
        {
            Instance.SleepUntilMorning();
        }
        else
        {
            pendingSleeps++;
        }
    }

    // Tua tới giờ thức dậy sáng hôm sau rồi lưu game.
    // Ngủ trước nửa đêm: sang ngày mới (cây lớn, đất khô...) giống lúc đồng hồ qua 24h.
    // Ngủ sau nửa đêm: ngày mới đã được xử lý lúc 0h nên chỉ tua giờ.
    private void SleepUntilMorning()
    {
        if (currentHour >= wakeUpHour)
        {
            HandleNewDay();
            onNewDay?.Invoke();
        }
        currentHour = wakeUpHour;

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveGame();
        }
    }

    // Enter Play Mode không reload domain -> xoá số đêm còn treo từ lần Play trước.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetPendingSleeps()
    {
        pendingSleeps = 0;
    }

    private void AdvanceToNextSeason()
    {
        Debug.Log(currentSeason);
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
