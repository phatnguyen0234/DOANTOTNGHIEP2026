using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Quản lý chăn nuôi trong scene Main: giữ dữ liệu con vật (RanchData), sinh con vật ra bãi quây,
// xử lý ngày mới (đói lại, con non lớn), phím nhanh J (cho cả trang trại ăn) / K (thu hoạch tất cả) và lưu / tải cùng SaveManager.
public class RanchManager : MonoBehaviour
{
    [System.Serializable]
    public class StarterAnimal
    {
        public AnimalSpecies species;
        public int variant;
        public string houseId = "Coop";
        public bool adult = true;
    }

    public static RanchManager Instance { get; private set; }

    [SerializeField] private List<AnimalSpecies> species = new List<AnimalSpecies>();
    [SerializeField] private List<AnimalHouse> houses = new List<AnimalHouse>();
    [SerializeField] private FarmAnimal animalPrefab;
    [Tooltip("Con vật có sẵn khi bắt đầu game lần đầu.")]
    [SerializeField] private List<StarterAnimal> starterAnimals = new List<StarterAnimal>();
    [SerializeField, Range(0f, 1f)] private float largeProductChance = 0.6f;

    [Header("Phím nhanh (đứng trong khu trang trại)")]
    [SerializeField] private KeyCode feedAllKey = KeyCode.J;
    [SerializeField] private KeyCode harvestAllKey = KeyCode.K;
    [Tooltip("Khu trang trại = khung bao các bãi quây, nới thêm từng này ô (phía trên nới gấp đôi để gồm cả chuồng).")]
    [SerializeField, Min(0f)] private float quickActionMargin = 3f;

    [Header("Test (chỉ chạy trong Editor / bản Development)")]
    [Tooltip("Ấn phím này để nhận thức ăn test vào túi.")]
    [SerializeField] private KeyCode testFoodKey = KeyCode.F8;
    [SerializeField] private string[] testFoodIds = { "carrot", "rice" };
    [SerializeField, Min(1)] private int testFoodAmount = 20;

    private RanchData data;
    private readonly Dictionary<int, FarmAnimal> spawned = new Dictionary<int, FarmAnimal>();
    private bool initialized;
    private TimeManager subscribedTime;

    public IReadOnlyList<AnimalHouse> Houses => houses;
    public float LargeProductChance => largeProductChance;

    private void Awake()
    {
        Instance = this;
    }

    private IEnumerator Start()
    {
        if (TimeManager.Instance != null)
        {
            subscribedTime = TimeManager.Instance;
            subscribedTime.onNewDay += HandleNewDay;
        }

        // Chờ 1 frame để SaveManager.LoadGame() kịp đưa dữ liệu đã lưu vào (RestoreData).
        yield return null;
        EnsureInitialized();
    }

    private void OnDisable()
    {
        if (subscribedTime != null) subscribedTime.onNewDay -= HandleNewDay;
        subscribedTime = null;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (!initialized) return;

        if ((Application.isEditor || Debug.isDebugBuild) && Input.GetKeyDown(testFoodKey)) GiveTestFood();

        bool feed = Input.GetKeyDown(feedAllKey);
        bool harvest = !feed && Input.GetKeyDown(harvestAllKey);
        if (!feed && !harvest) return;

        PlayerMovement player = PlayerMovement.instance;
        if (player == null) return;
        Vector3 at = player.transform.position + new Vector3(0f, 0.6f, 0f);
        if (!InRanchZone(player.transform.position))
        {
            RanchFeedback.Text(at, "Lại gần trang trại để cho ăn (J) / thu hoạch (K)", RanchFeedback.Info);
            return;
        }

        if (feed) FeedAll(null, at);
        else HarvestAll(at);
    }

    // Phím test: thêm cà rốt + thóc vào túi để thử cho ăn.
    private void GiveTestFood()
    {
        PlayerMovement player = PlayerMovement.instance;
        if (player == null) return;

        List<string> given = new List<string>();
        foreach (string id in testFoodIds)
        {
            ItemData item = ItemDatabase.Instance != null ? ItemDatabase.Instance.GetItemById(id) : null;
            if (item == null) item = species.Where(s => s != null && s.foods != null).SelectMany(s => s.foods).FirstOrDefault(f => f != null && f.ItemID == id);
            if (item == null) continue;
            GiveItem(item, testFoodAmount, player.transform.position);
            given.Add($"{testFoodAmount} {ShortName(item)}");
        }
        if (given.Count > 0) RanchFeedback.Text(player.transform.position + new Vector3(0f, 0.6f, 0f), "+ " + string.Join(", ", given), RanchFeedback.Good);
    }

    // ---------- Lưu / tải (gọi từ SaveManager) ----------

    public void RestoreData(RanchData saved)
    {
        if (saved == null) return;   // save cũ chưa có phần chăn nuôi -> dùng khởi tạo mặc định
        data = saved;
        if (data.animals == null) data.animals = new List<AnimalRecord>();
        initialized = false;
        EnsureInitialized();
    }

    public RanchData CaptureData()
    {
        EnsureInitialized();
        foreach (FarmAnimal animal in spawned.Values)
        {
            if (animal != null) animal.Record.position = animal.transform.position;
        }
        return data;
    }

    private void EnsureInitialized()
    {
        if (initialized) return;
        initialized = true;

        if (data == null) data = new RanchData();
        // Chuồng trống hẳn (vd save cũ từ bản có chết đói) -> cấp lại đàn có sẵn để còn có con mà chơi.
        if (!data.starterAnimalsGiven || data.animals.Count == 0)
        {
            // Con vật có sẵn vừa được chủ cũ cho ăn sáng nay: hôm nay đã no, con trưởng thành có sẵn trứng / sữa để thu.
            foreach (StarterAnimal starter in starterAnimals)
            {
                AnimalHouse house = FindHouse(starter.houseId);
                if (starter.species == null || house == null) continue;
                AnimalRecord record = NewRecord(starter.species, starter.variant, house, starter.adult);
                record.fedToday = true;
                record.productReady = starter.adult && starter.species.ProductFor(starter.variant, false) != null;
                data.animals.Add(record);
            }
            data.starterAnimalsGiven = true;
        }

        SpawnAll();
        UpdateTroughs();
    }

    // ---------- Ngày mới ----------

    // Sang ngày mới mọi con lại đói. Bỏ đói không chết, chỉ không có sản phẩm và giảm thân thiết.
    private void HandleNewDay()
    {
        EnsureInitialized();
        foreach (AnimalRecord record in data.animals)
        {
            if (record.fedToday)
            {
                record.ageDays++;          // chỉ lớn vào những ngày được ăn
                record.daysHungry = 0;
            }
            else
            {
                record.daysHungry++;
                record.friendship -= 20;
            }

            if (!record.pettedToday) record.friendship -= 5;
            record.fedToday = false;
            record.pettedToday = false;
            record.friendship = Mathf.Clamp(record.friendship, 0, 1000);
        }

        foreach (FarmAnimal animal in spawned.Values)
        {
            if (animal != null) animal.Refresh();
        }
        UpdateTroughs();
    }

    // ---------- Cho ăn / thu hoạch ----------

    // Cho các con đang đói ăn bằng thức ăn trong túi (theo thứ tự ưu tiên của từng loài). house = null -> cả trang trại.
    public void FeedAll(AnimalHouse house, Vector3 textAt)
    {
        EnsureInitialized();
        Inventory inventory = Inventory.Instance;
        Dictionary<string, int> used = new Dictionary<string, int>();
        HashSet<string> missingFoods = new HashSet<string>();
        int fed = 0, stillHungry = 0;

        foreach (FarmAnimal animal in spawned.Values)
        {
            if (animal == null || !animal.IsHungry || (house != null && animal.House != house)) continue;

            ItemData food = inventory != null ? animal.Species.foods.FirstOrDefault(f => f != null && inventory.GetItemCount(f) > 0) : null;
            if (food == null || !inventory.TryRemoveItem(food, 1))
            {
                stillHungry++;
                if (animal.Species.FavoriteFood != null) missingFoods.Add(ShortName(animal.Species.FavoriteFood));
                continue;
            }

            animal.Feed(food, false);
            fed++;
            string name = ShortName(food);
            used[name] = used.TryGetValue(name, out int n) ? n + 1 : 1;
        }

        if (fed == 0 && stillHungry == 0)
        {
            RanchFeedback.Text(textAt, "Các con vật đều đã no hôm nay", RanchFeedback.Info);
            UpdateTroughs();
            return;
        }

        string missing = missingFoods.Count > 0 ? string.Join(", ", missingFoods) : "thức ăn";
        if (fed == 0)
        {
            RanchFeedback.Text(textAt, $"Không có thức ăn trong túi! Cần {missing}... (mua ở người bán trang trại)", RanchFeedback.Warn);
            return;
        }

        string list = string.Join(", ", used.Select(p => $"{p.Value} {p.Key}"));
        string tail = stillHungry > 0 ? $"\nCòn {stillHungry} con đói - thiếu {missing}" : "";
        RanchFeedback.Text(textAt, $"Đã cho {fed} con ăn ({list}){tail}", stillHungry > 0 ? RanchFeedback.Warn : RanchFeedback.Good);
        UpdateTroughs();
    }

    // K: thu trứng / sữa / len của mọi con đã sẵn sàng.
    public void HarvestAll(Vector3 textAt)
    {
        EnsureInitialized();
        Dictionary<string, int> got = new Dictionary<string, int>();
        foreach (FarmAnimal animal in spawned.Values)
        {
            if (animal == null || !animal.IsReadyToHarvest) continue;
            ItemData item = animal.Harvest(false);
            if (item == null) continue;
            string name = ShortName(item);
            got[name] = got.TryGetValue(name, out int n) ? n + 1 : 1;
        }

        if (got.Count == 0)
        {
            bool anyHungry = spawned.Values.Any(a => a != null && a.IsHungry);
            RanchFeedback.Text(textAt, anyHungry ? "Chưa có gì để thu - cho con vật ăn trước (J)" : "Chưa có gì để thu, mai quay lại nhé", RanchFeedback.Info);
            return;
        }
        RanchFeedback.Text(textAt, "Thu được: " + string.Join(", ", got.Select(p => $"{p.Value} {p.Key}")), RanchFeedback.Good);
    }

    // Máng hiện đầy khi mọi con trong chuồng đã được ăn hôm nay.
    public void UpdateTroughs()
    {
        if (data == null) return;
        foreach (AnimalHouse house in houses)
        {
            if (house == null || house.Trough == null) continue;
            int count = CountIn(house.HouseId);
            house.Trough.SetFilled(count > 0 && HungryIn(house.HouseId) == 0);
        }
    }

    // ---------- Thêm con vật ----------

    public int CountIn(string houseId)
    {
        EnsureInitialized();
        return data.animals.Count(a => a.houseId == houseId);
    }

    public int HungryIn(string houseId)
    {
        EnsureInitialized();
        return data.animals.Count(a => a.houseId == houseId && !a.fedToday);
    }

    public bool HasRoom(AnimalHome home)
    {
        return houses.Any(h => h != null && h.Home == home && CountIn(h.HouseId) < h.Capacity);
    }

    public AnimalSpecies SpeciesForPurchase(ItemData item)
    {
        return item == null ? null : species.FirstOrDefault(s => s != null && s.purchaseItem == item);
    }

    public bool AddAnimal(AnimalSpecies sp, AnimalHouse house, bool adult, int variant = -1, bool fed = false)
    {
        EnsureInitialized();
        if (sp == null || house == null || CountIn(house.HouseId) >= house.Capacity) return false;

        if (variant < 0) variant = Random.Range(0, Mathf.Max(1, sp.variants.Length));
        AnimalRecord record = NewRecord(sp, variant, house, adult);
        record.fedToday = fed;
        data.animals.Add(record);
        Spawn(record);
        UpdateTroughs();
        return true;
    }

    // Con hoang dã đã thuần hoá về chuồng hợp với loài, trưởng thành luôn, hôm nay đã được ăn.
    public bool AdoptWild(AnimalSpecies sp, int variant)
    {
        AnimalHouse house = houses.FirstOrDefault(h => h != null && h.Home == sp.home && CountIn(h.HouseId) < h.Capacity);
        return house != null && AddAnimal(sp, house, true, variant, true);
    }

    // ---------- Tiện ích ----------

    // Cho vật phẩm vào túi; túi đầy thì rơi ra đất cạnh con vật.
    public static void GiveItem(ItemData item, int amount, Vector3 at)
    {
        int left = amount;
        if (Inventory.Instance != null) left = Inventory.Instance.AddItem(item, amount);
        if (left > 0 && ItemDropSpawner.Instance != null) ItemDropSpawner.Instance.SpawnDrop(item, left, at);
    }

    // "Trứng Gà (Chicken Egg)" -> "Trứng Gà"
    public static string ShortName(ItemData item)
    {
        if (item == null) return "";
        string full = string.IsNullOrEmpty(item.ItemName) ? item.ItemID : item.ItemName;
        int bracket = full.IndexOf(" (");
        return bracket > 0 ? full.Substring(0, bracket) : full;
    }

    private bool InRanchZone(Vector2 point)
    {
        bool any = false;
        Rect zone = default;
        foreach (AnimalHouse house in houses)
        {
            if (house == null) continue;
            Rect pen = house.PenRect;
            if (!any)
            {
                zone = pen;
                any = true;
                continue;
            }
            zone = Rect.MinMaxRect(Mathf.Min(zone.xMin, pen.xMin), Mathf.Min(zone.yMin, pen.yMin), Mathf.Max(zone.xMax, pen.xMax), Mathf.Max(zone.yMax, pen.yMax));
        }
        if (!any) return false;

        float m = quickActionMargin;
        zone = Rect.MinMaxRect(zone.xMin - m, zone.yMin - m, zone.xMax + m, zone.yMax + m * 2f);
        return zone.Contains(point);
    }

    private AnimalRecord NewRecord(AnimalSpecies sp, int variant, AnimalHouse house, bool adult)
    {
        return new AnimalRecord
        {
            id = data.nextAnimalId++,
            speciesId = sp.id,
            variant = variant,
            houseId = house.HouseId,
            friendship = adult ? 300 : 100,
            ageDays = adult ? sp.daysToAdult : 0,
            position = house.RandomPointInPen()
        };
    }

    private void SpawnAll()
    {
        foreach (FarmAnimal animal in spawned.Values)
        {
            if (animal != null) Destroy(animal.gameObject);
        }
        spawned.Clear();
        foreach (AnimalRecord record in data.animals) Spawn(record);
    }

    private void Spawn(AnimalRecord record)
    {
        AnimalSpecies sp = FindSpecies(record.speciesId);
        AnimalHouse house = FindHouse(record.houseId);
        if (sp == null || house == null || animalPrefab == null) return;

        Vector2 position = house.PenRect.Contains(record.position) ? record.position : house.RandomPointInPen();
        FarmAnimal animal = Instantiate(animalPrefab, position, Quaternion.identity, transform);
        animal.Bind(record, sp, house);
        spawned[record.id] = animal;
    }

    private AnimalSpecies FindSpecies(string id) => species.FirstOrDefault(s => s != null && s.id == id);

    private AnimalHouse FindHouse(string id) => houses.FirstOrDefault(h => h != null && h.HouseId == id);
}
