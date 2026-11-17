using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance { get; private set; }

    // 24 in-game hours / 900 real seconds (15 minutes) = 0.02666667f
    public const float REALISTIC_15MIN_DAY_SPEED = 24f / 900f;

    [Header("External Configuration (Git-Safe ScriptableObject)")]
    [Tooltip("Assign DefaultTimeConfig asset here so lighting, clock sprites, and holidays are stored safely outside the Scene.")]
    [SerializeField] private TimeLightingConfig timeConfig;

    [Header("Calendar & Time Settings")]
    [SerializeField] private int startYear = 2026;
    [SerializeField] private int startMonth = 1;
    [SerializeField] private int startDay = 1;

    [Tooltip("Initial hour when starting a new game (0 - 24).")]
    public float currentHour = 6f;

    [Tooltip("Speed multiplier: 24 / 900 = 0.02667 makes 1 full day equal 15 real-world minutes.")]
    public float timeSpeed = REALISTIC_15MIN_DAY_SPEED;

    [Header("Sleep Settings")]
    [Tooltip("Hour when the player wakes up after sleeping.")]
    public float wakeUpHour = 6f;

    [Header("Scene UI & Lighting References")]
    [SerializeField] private TextMeshProUGUI dateText;
    [SerializeField] private TextMeshProUGUI seasonText;
    public Image clockHandImage;
    public Light2D globalLight;

    [Header("Legacy Fallback Settings (Used only if Time Config is unassigned)")]
    public Sprite[] clockHandSprites;
    public Gradient lightColor;
    public AnimationCurve lightIntensity;

    // Runtime Calendar State ([NonSerialized] prevents Unity from dirtying the Scene YAML during Play Mode)
    [NonSerialized] public int totalDaysElapsed = 0;
    [NonSerialized] public int currentDay = 1;
    [NonSerialized] public int currentMonth = 1;
    [NonSerialized] public int currentYear = 2026;
    [NonSerialized] public Season currentSeason = Season.Spring;

    // Events (Preserved signatures for full backward compatibility)
    public static Action<Season> OnSeasonChanged;
    public static event Action<HolidayEventData> OnHolidayTriggered;
    public event Action onNewDay;

    private static int pendingSleeps;
    private bool hasPassedOutToday = false;

    // Built-in Lunisolar calendar converter for perpetual Lunar holidays
    private static readonly ChineseLunisolarCalendar LunarCalendar = new ChineseLunisolarCalendar();

    public DateTime CurrentDate => new DateTime(startYear, startMonth, startDay).AddDays(totalDaysElapsed);

    private Sprite[] ActiveClockSprites =>
        (timeConfig != null && timeConfig.clockHandSprites != null && timeConfig.clockHandSprites.Length > 0)
            ? timeConfig.clockHandSprites
            : clockHandSprites;

    private Gradient ActiveLightColor =>
        (timeConfig != null && timeConfig.lightColor != null)
            ? timeConfig.lightColor
            : lightColor;

    private AnimationCurve ActiveLightIntensity =>
        (timeConfig != null && timeConfig.lightIntensity != null && timeConfig.lightIntensity.length > 0)
            ? timeConfig.lightIntensity
            : lightIntensity;

    private List<HolidayEventData> ActiveHolidays =>
        (timeConfig != null && timeConfig.vietnameseHolidays != null)
            ? timeConfig.vietnameseHolidays
            : null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        RecalculateCalendarFromTotalDays(notifyChanges: false);
    }

    private void Start()
    {
        UpdateDateUI();
        UpdateClockUI();
        UpdateLighting();
        CheckAndTriggerHoliday();
    }

    private void Update()
    {
        while (pendingSleeps > 0)
        {
            pendingSleeps--;
            SleepUntilMorning();
        }

        // TODO [Multiplayer]: Wrap time progression inside `if (IsServer)` and sync `currentHour` & `totalDaysElapsed`
        currentHour += Time.deltaTime * timeSpeed;

        if (currentHour >= 24f)
        {
            currentHour -= 24f;
            AdvanceToNextDay();
        }

        UpdateClockUI();
        UpdateLighting();
    }

    private void AdvanceToNextDay()
    {
        totalDaysElapsed++;
        RecalculateCalendarFromTotalDays(notifyChanges: true);
        UpdateDateUI();

        onNewDay?.Invoke();
        CheckAndTriggerHoliday();
    }

    /// <summary>
    /// Deterministically derives Day, Month, Year, and Season from totalDaysElapsed.
    /// Guarantees identical calendar calculations across Server and all Clients in Multiplayer.
    /// </summary>
    public void RecalculateCalendarFromTotalDays(bool notifyChanges)
    {
        DateTime date = CurrentDate;
        currentDay = date.Day;
        currentMonth = date.Month;
        currentYear = date.Year;

        Season calculatedSeason = GetSeasonForMonth(currentMonth);
        if (calculatedSeason != currentSeason)
        {
            currentSeason = calculatedSeason;
            if (notifyChanges)
            {
                Debug.Log($"[TimeManager] Season changed to: {currentSeason} ({currentDay:00}/{currentMonth:00}/{currentYear})");
                OnSeasonChanged?.Invoke(currentSeason);
            }
        }
    }

    public static Season GetSeasonForMonth(int month)
    {
        if (month >= 1 && month <= 3) return Season.Spring;
        if (month >= 4 && month <= 6) return Season.Summer;
        if (month >= 7 && month <= 9) return Season.Autumn;
        return Season.Winter; // Months 10, 11, 12
    }

    private void CheckAndTriggerHoliday()
    {
        List<HolidayEventData> holidays = ActiveHolidays;
        if (holidays == null || holidays.Count == 0) return;

        GetLunarDate(CurrentDate, out int lunarDay, out int lunarMonth, out bool isLeapMonth);

        foreach (HolidayEventData holiday in holidays)
        {
            bool isMatch = false;

            if (holiday.calendarType == CalendarType.Solar)
            {
                isMatch = (holiday.day == currentDay && holiday.month == currentMonth);
            }
            else if (holiday.calendarType == CalendarType.Lunar && !isLeapMonth)
            {
                isMatch = (holiday.day == lunarDay && holiday.month == lunarMonth);
            }

            if (isMatch)
            {
                string formattedDesc = holiday.GetFormattedDescription(currentYear);
                Debug.Log($"<color=yellow>[Holiday Event] {holiday.holidayTitle} ({currentDay:00}/{currentMonth:00}/{currentYear}): {formattedDesc}</color>");
                OnHolidayTriggered?.Invoke(holiday);
            }
        }
    }

    /// <summary>
    /// Converts a Gregorian DateTime into Lunar Day, Lunar Month, and Leap Month flag.
    /// Works perpetually across all years without hardcoded year dates.
    /// </summary>
    public static void GetLunarDate(DateTime solarDate, out int lunarDay, out int lunarMonth, out bool isLeapMonth)
    {
        int lunarYear = LunarCalendar.GetYear(solarDate);
        int rawMonth = LunarCalendar.GetMonth(solarDate);
        lunarDay = LunarCalendar.GetDayOfMonth(solarDate);

        int leapMonth = LunarCalendar.GetLeapMonth(lunarYear);
        isLeapMonth = false;

        if (leapMonth > 0)
        {
            if (rawMonth == leapMonth)
            {
                isLeapMonth = true;
                lunarMonth = rawMonth - 1;
            }
            else if (rawMonth > leapMonth)
            {
                lunarMonth = rawMonth - 1;
            }
            else
            {
                lunarMonth = rawMonth;
            }
        }
        else
        {
            lunarMonth = rawMonth;
        }
    }

    public static void GoToSleep()
    {
        // TODO [Multiplayer]: Replace direct call with ServerRpc sleep vote request
        if (Instance != null)
        {
            Instance.SleepUntilMorning();
        }
        else
        {
            pendingSleeps++;
        }
    }

    private void SleepUntilMorning()
    {
        if (currentHour >= wakeUpHour)
        {
            AdvanceToNextDay();
        }

        currentHour = wakeUpHour;
        hasPassedOutToday = false;

        UpdateDateUI();
        UpdateClockUI();
        UpdateLighting();

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveGame();
        }
    }

    private void UpdateDateUI()
    {
        if (dateText != null)
        {
            dateText.text = $"{currentDay:00}/{currentMonth:00}/{currentYear}";
        }

        if (seasonText != null)
        {
            seasonText.text = currentSeason.ToString();
        }
    }

    private void UpdateClockUI()
    {
        Sprite[] sprites = ActiveClockSprites;
        if (clockHandImage == null || sprites == null || sprites.Length == 0) return;

        float dayProgress = currentHour / 24f;
        int spriteIndex = Mathf.FloorToInt(dayProgress * sprites.Length);
        spriteIndex = Mathf.Clamp(spriteIndex, 0, sprites.Length - 1);
        clockHandImage.sprite = sprites[spriteIndex];
    }

    private void UpdateLighting()
    {
        if (globalLight == null) return;

        float dayProgress = currentHour / 24f;

        Gradient colorGrad = ActiveLightColor;
        if (colorGrad != null)
        {
            globalLight.color = colorGrad.Evaluate(dayProgress);
        }

        AnimationCurve intensityCurve = ActiveLightIntensity;
        if (intensityCurve != null && intensityCurve.length > 0)
        {
            globalLight.intensity = intensityCurve.Evaluate(dayProgress);
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetPendingSleeps()
    {
        pendingSleeps = 0;
    }

#if UNITY_EDITOR
    [ContextMenu("Debug: Skip 1 Day")]
    private void DebugSkipOneDay()
    {
        AdvanceToNextDay();
    }

    [ContextMenu("Debug: Skip 30 Days")]
    private void DebugSkipThirtyDays()
    {
        for (int i = 0; i < 30; i++) AdvanceToNextDay();
    }
#endif
}