using UnityEngine;

// Con vật nuôi trong trang trại. Chuột phải vào con vật:
//   có sản phẩm -> thu hoạch;  đang đói + cầm đúng thức ăn -> cho ăn;  đang đói + tay không -> nhắc cần ăn gì (hoặc ấn J);
//   đã no -> vuốt ve (mỗi ngày 1 lần), lần sau xem tình trạng.
// Chỉ sau khi được ăn mới có trứng / sữa; trạng thái thật nằm trong AnimalRecord do RanchManager giữ và lưu.
[RequireComponent(typeof(AnimalMotor))]
public class FarmAnimal : MonoBehaviour, IInteractable
{
    private const float EatSeconds = 2f;
    private static readonly Color HungryTint = new Color(1f, 0.62f, 0.62f);

    [SerializeField, Min(0.5f)] private float maxInteractDistance = 2.2f;
    [SerializeField] private AnimalBubble bubble;
    [SerializeField] private CircleCollider2D bodyCollider;
    [SerializeField] private BoxCollider2D clickArea;

    private AnimalMotor motor;
    private HotbarController hotbar;
    private bool wasAdult;
    private float readyAt;          // sản phẩm hiện ra sau khi ăn xong
    private bool revealPending;

    public AnimalRecord Record { get; private set; }
    public AnimalSpecies Species { get; private set; }
    public AnimalHouse House { get; private set; }

    public bool IsHungry => Record != null && !Record.fedToday;
    public bool IsReadyToHarvest => Record != null && Record.productReady && Time.time >= readyAt;

    public string InteractionPrompt
    {
        get
        {
            if (Record == null) return "";
            if (IsReadyToHarvest) return $"Thu {ProductName()}";
            return IsHungry ? $"Cho {Species.displayName} ăn" : $"Vuốt ve {Species.displayName}";
        }
    }

    private void Awake()
    {
        motor = GetComponent<AnimalMotor>();
    }

    public void Bind(AnimalRecord record, AnimalSpecies species, AnimalHouse house)
    {
        Record = record;
        Species = species;
        House = house;
        wasAdult = species.IsAdult(record.ageDays);
        name = $"{species.displayName} #{record.id}";

        SetupBody();
        if (house != null) motor.SetArea(house.PenRect);
        Refresh();
    }

    // Gọi lại sau ngày mới / khi ăn / khi thu: đổi hình con non -> trưởng thành, cập nhật bong bóng.
    public void Refresh()
    {
        if (Record == null) return;

        bool adult = Species.IsAdult(Record.ageDays);
        if (adult != wasAdult)
        {
            wasAdult = adult;
            SetupBody();
        }

        if (bubble == null) return;
        if (IsReadyToHarvest)
        {
            ItemData product = Species.ProductFor(Record.variant, Record.productLarge);
            bubble.SetPersistent(product != null ? product.Icon : null);
        }
        else if (IsHungry)
        {
            ItemData food = Species.FavoriteFood;
            bubble.SetPersistent(food != null ? food.Icon : RanchFeedback.Heart, HungryTint);
        }
        else
        {
            bubble.SetPersistent(null);
        }
    }

    private void Update()
    {
        if (Record == null) return;

        // Ngủ từ 20h tới 6h sáng.
        TimeManager time = TimeManager.Instance;
        if (time != null) motor.SetSleeping(time.currentHour >= 20f || time.currentHour < 6f);
        Record.position = transform.position;

        if (revealPending && Time.time >= readyAt)
        {
            revealPending = false;
            Refresh();
        }
    }

    public bool CanInteract(GameObject interactor)
    {
        return interactor != null && Record != null
            && Vector2.Distance(interactor.transform.position, transform.position) <= maxInteractDistance;
    }

    public void Interact(GameObject interactor)
    {
        if (!CanInteract(interactor)) return;

        if (IsReadyToHarvest)
        {
            Harvest(true);
            return;
        }

        if (IsHungry)
        {
            ItemData held = HeldItem();
            if (Species.Eats(held))
            {
                if (Inventory.Instance != null && Inventory.Instance.TryRemoveItem(held, 1)) Feed(held, true);
                return;
            }
            motor.HoldFacing(interactor.transform.position, 1f);
            RanchFeedback.Text(transform.position, $"{Species.displayName} đang đói, chưa có gì để thu!\nCầm {FoodNames(3)} rồi chuột phải, hoặc ấn J", RanchFeedback.Warn);
            return;
        }

        motor.HoldFacing(interactor.transform.position, 1.2f);
        if (!Record.pettedToday)
        {
            Record.pettedToday = true;
            Record.friendship = Mathf.Min(1000, Record.friendship + 15);
            if (bubble != null) bubble.FlashHeart();
            RanchFeedback.Text(transform.position, $"{Species.displayName} vui lắm! ({Hearts()}/5 tim)", RanchFeedback.Good);
            return;
        }

        string status = !Species.IsAdult(Record.ageDays)
            ? $"còn {Species.daysToAdult - Record.ageDays} ngày được ăn nữa thì lớn"
            : "hôm nay đã no và được vuốt ve";
        RanchFeedback.Text(transform.position, $"{Species.displayName} ({Hearts()}/5 tim) - {status}", RanchFeedback.Info);
    }

    // Ăn 1 phần thức ăn (đã trừ khỏi túi ở nơi gọi): no hôm nay, thêm thân thiết; đủ số bữa thì có sản phẩm, hiện ra khi ăn xong.
    public void Feed(ItemData food, bool announce)
    {
        if (Record == null || Record.fedToday) return;

        Record.fedToday = true;
        Record.daysHungry = 0;
        Record.friendship = Mathf.Min(1000, Record.friendship + 10);

        if (Species.IsAdult(Record.ageDays) && !Record.productReady && Species.ProductFor(Record.variant, false) != null)
        {
            Record.fedDays++;
            if (Record.fedDays >= Species.productEveryDays)
            {
                Record.fedDays = 0;
                Record.productReady = true;
                float chance = RanchManager.Instance != null ? RanchManager.Instance.LargeProductChance : 0.5f;
                Record.productLarge = Record.friendship >= Species.largeProductFriendship && Random.value < chance;
                readyAt = Time.time + EatSeconds;
                revealPending = true;
            }
        }

        motor.PlayEat(EatSeconds);
        if (bubble != null) bubble.FlashHeart();
        Refresh();
        if (announce) RanchFeedback.Text(transform.position, $"{Species.displayName} ăn {RanchManager.ShortName(food)} ngon lành", RanchFeedback.Good);
    }

    // Thu sản phẩm vào túi (túi đầy thì rơi ra đất). Trả về vật phẩm đã thu, null nếu chưa có.
    public ItemData Harvest(bool announce)
    {
        if (!IsReadyToHarvest) return null;

        ItemData item = Species.ProductFor(Record.variant, Record.productLarge);
        Record.productReady = false;
        Record.productLarge = false;
        if (item != null)
        {
            RanchManager.GiveItem(item, 1, transform.position);
            Record.friendship = Mathf.Min(1000, Record.friendship + 5);
            if (announce) RanchFeedback.Text(transform.position, $"+1 {RanchManager.ShortName(item)}", RanchFeedback.Good);
        }
        Refresh();
        return item;
    }

    private int Hearts() => Mathf.Clamp(Mathf.FloorToInt(Record.friendship / 200f), 0, 5);

    private string ProductName()
    {
        ItemData item = Species.ProductFor(Record.variant, Record.productLarge);
        return item != null ? RanchManager.ShortName(item) : "sản phẩm";
    }

    private string FoodNames(int max)
    {
        string names = "";
        int count = 0;
        foreach (ItemData food in Species.foods)
        {
            if (food == null || count >= max) continue;
            names += (count > 0 ? " / " : "") + RanchManager.ShortName(food);
            count++;
        }
        return count > 0 ? names : "thức ăn";
    }

    private ItemData HeldItem()
    {
        if (hotbar == null) hotbar = FindAnyObjectByType<HotbarController>();
        InventorySlot slot = hotbar != null ? hotbar.SelectedSlot : null;
        return slot != null && !slot.IsEmpty() ? slot.ItemData : null;
    }

    // Hình + tốc độ + bán kính dò vật cản theo tuổi; collider chân và vùng click khớp với hình.
    private void SetupBody()
    {
        float radius = Species.colliderRadius * (wasAdult ? 1f : 0.7f);
        motor.Setup(Species.FramesFor(Record.variant, wasAdult), Species.framesPerSecond, Species.moveSpeed * (wasAdult ? 1f : 1.15f), radius);

        if (bodyCollider != null)
        {
            bodyCollider.isTrigger = true;
            bodyCollider.radius = radius;
            bodyCollider.offset = new Vector2(0f, radius);
        }
        Sprite sprite = motor.CurrentSprite;
        if (clickArea != null && sprite != null)
        {
            Vector2 size = sprite.bounds.size;
            clickArea.size = new Vector2(size.x * 0.7f, size.y * 0.7f);
            clickArea.offset = new Vector2(0f, size.y * 0.4f);
            if (bubble != null) bubble.SetHeight(size.y * 0.75f);
        }
    }
}
