//using UnityEngine;

//public class GameTimeManager : MonoBehaviour
//{
//    [SerializeField] private FarmManager farmManager;

//    [Header("Time Settings")]
//    [SerializeField] private float gameMinutesPerRealSecond = 10f;
//    [SerializeField] private int startHour = 6;
//    [SerializeField] private int endHour = 2;

//    public int CurrentDay { get; private set; } = 1;
//    public float CurrentGameMinutes { get; private set; }

//    private float StartMinutes => startHour * 60f;

//    // 02:00 của ngày tiếp theo = 26:00 theo bộ đếm liên tục.
//    private float EndMinutes => (24 + endHour) * 60f;

//    private void Awake()
//    {
//        CurrentGameMinutes = StartMinutes;
//    }

//    private void Update()
//    {
//        CurrentGameMinutes += Time.deltaTime * gameMinutesPerRealSecond;

//        // Phím debug để qua ngày ngay.
//        if (Input.GetKeyDown(KeyCode.N))
//        {
//            EndDay();
//        }

//        // Tự chuyển ngày khi đồng hồ đến 02:00.
//        if (CurrentGameMinutes >= EndMinutes)
//        {
//            EndDay();
//        }
//    }

//    public void EndDay()
//    {
//        farmManager.HandleNewDay();

//        CurrentDay++;
//        CurrentGameMinutes = StartMinutes;

//        Debug.Log("Day " + CurrentDay + " started at 06:00.");
//    }

//    public string GetTimeText()
//    {
//        int totalMinutes = Mathf.FloorToInt(CurrentGameMinutes) % (24 * 60);
//        int hours = totalMinutes / 60;
//        int minutes = totalMinutes % 60;

//        return hours.ToString("00") + ":" + minutes.ToString("00");
//    }
//}