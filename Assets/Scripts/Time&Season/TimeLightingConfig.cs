using System;
using System.Collections.Generic;
using UnityEngine;

public enum Season
{
    Spring,
    Summer,
    Autumn,
    Winter
}

public enum CalendarType
{
    Solar, // Dương lịch (VD: 30/4, 2/9)
    Lunar  // Âm lịch (VD: Mùng 1 Tết, 10/3 Giỗ Tổ, 15/8 Trung Thu)
}

[Serializable]
public class HolidayEventData
{
    public string eventId;
    public string holidayTitle;
    public CalendarType calendarType = CalendarType.Solar;
    [Range(1, 31)] public int day = 1;
    [Range(1, 12)] public int month = 1;

    [TextArea(2, 4)]
    [Tooltip("You can use {YEAR} placeholder to dynamically insert the current in-game year.")]
    public string description;
    public int bonusGoldReward = 200;

    public string GetFormattedDescription(int year)
    {
        if (string.IsNullOrEmpty(description)) return string.Empty;
        return description.Replace("{YEAR}", year.ToString());
    }
}

[CreateAssetMenu(fileName = "DefaultTimeConfig", menuName = "Farming/Time & Lighting Config")]
public class TimeLightingConfig : ScriptableObject
{
    [Header("Clock Hand Sprites (e.g., 8 or 24 frames)")]
    public Sprite[] clockHandSprites;

    [Header("2D Global Lighting Cycle (0h -> 24h)")]
    public Gradient lightColor = new Gradient();
    public AnimationCurve lightIntensity = AnimationCurve.Linear(0f, 0.2f, 1f, 1f);

    [Header("Vietnamese Holidays & Special Events")]
    public List<HolidayEventData> vietnameseHolidays = new List<HolidayEventData>();

    private void Reset()
    {
        PopulateDefaultVietnameseHolidays();
    }

    [ContextMenu("Populate Default Vietnamese Holidays")]
    public void PopulateDefaultVietnameseHolidays()
    {
        vietnameseHolidays = new List<HolidayEventData>
        {
            new HolidayEventData
            {
                eventId = "new_year",
                holidayTitle = "Tết Dương Lịch",
                calendarType = CalendarType.Solar,
                day = 1, month = 1,
                description = "Chào mừng năm mới {YEAR}! Dân làng gửi tặng bạn một phần quà khởi đầu năm mới thuận lợi.",
                bonusGoldReward = 300
            },
            new HolidayEventData
            {
                eventId = "lunar_new_year",
                holidayTitle = "Tết Nguyên Đán (Mùng 1 Tết)",
                calendarType = CalendarType.Lunar,
                day = 1, month = 1,
                description = "Chúc mừng năm mới Âm lịch! Chúc nông trại một năm mùa màng bội thu, vạn sự như ý.",
                bonusGoldReward = 1000
            },
            new HolidayEventData
            {
                eventId = "hung_kings",
                holidayTitle = "Giỗ Tổ Hùng Vương (10/3 Âm lịch)",
                calendarType = CalendarType.Lunar,
                day = 10, month = 3,
                description = "Ngày lễ hướng về cội nguồn dân tộc. Cửa hàng gửi tặng phần quà tri ân cho nông trại!",
                bonusGoldReward = 500
            },
            new HolidayEventData
            {
                eventId = "reunification_day",
                holidayTitle = "Ngày Giải Phóng Miền Nam (30/4)",
                calendarType = CalendarType.Solar,
                day = 30, month = 4,
                description = "Kỷ niệm ngày thống nhất đất nước 30/04. Chúc bạn một ngày làm việc thật nhiều niềm vui!",
                bonusGoldReward = 500
            },
            new HolidayEventData
            {
                eventId = "labor_day",
                holidayTitle = "Ngày Quốc Tế Lao Động (1/5)",
                calendarType = CalendarType.Solar,
                day = 1, month = 5,
                description = "Tôn vinh sức lao động! Thành quả chăm sóc nông trại của bạn luôn xứng đáng được trân trọng.",
                bonusGoldReward = 400
            },
            new HolidayEventData
            {
                eventId = "national_day",
                holidayTitle = "Quốc Khánh Việt Nam (2/9)",
                calendarType = CalendarType.Solar,
                day = 2, month = 9,
                description = "Chào mừng ngày Quốc Khánh 02/09! Cả thị trấn rực rỡ cờ hoa đón lễ.",
                bonusGoldReward = 800
            },
            new HolidayEventData
            {
                eventId = "mid_autumn",
                holidayTitle = "Tết Trung Thu (15/8 Âm lịch)",
                calendarType = CalendarType.Lunar,
                day = 15, month = 8,
                description = "Đêm hội trăng rằm! Dân làng gửi tặng quà mừng mùa thu đoàn viên.",
                bonusGoldReward = 500
            },
            new HolidayEventData
            {
                eventId = "vietnamese_women_day",
                holidayTitle = "Ngày Phụ Nữ Việt Nam (20/10)",
                calendarType = CalendarType.Solar,
                day = 20, month = 10,
                description = "Chúc mừng ngày Phụ nữ Việt Nam 20/10! Gửi lời chúc tốt đẹp nhất tới mọi người trong thị trấn.",
                bonusGoldReward = 300
            },
            new HolidayEventData
            {
                eventId = "teachers_day",
                holidayTitle = "Ngày Nhà Giáo Việt Nam (20/11)",
                calendarType = CalendarType.Solar,
                day = 20, month = 11,
                description = "Tri ân ngày Nhà giáo Việt Nam 20/11! Chúc hành trình khám phá và học hỏi của bạn luôn tiến xa.",
                bonusGoldReward = 300
            }
        };
    }
}