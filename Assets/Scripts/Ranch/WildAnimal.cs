using UnityEngine;

// Thú hoang trong rừng: đi lang thang, thấy người chơi lại gần thì bỏ chạy.
// Loài thuần hoá được (có tameFood): cầm đúng thức ăn thì con vật đứng yên tò mò; chuột phải cho ăn đủ số lần -> về chuồng.
[RequireComponent(typeof(AnimalMotor))]
public class WildAnimal : MonoBehaviour, IInteractable
{
    [SerializeField, Min(0.5f)] private float maxInteractDistance = 2.2f;
    [SerializeField] private AnimalBubble bubble;
    [SerializeField] private CircleCollider2D bodyCollider;
    [SerializeField] private BoxCollider2D clickArea;

    private AnimalMotor motor;
    private HotbarController hotbar;
    private int trust;
    private float nextFleeCheck;

    public AnimalSpecies Species { get; private set; }
    public int Variant { get; private set; }
    public WildAnimalSpawner Spawner { get; set; }

    public string InteractionPrompt => Species != null && Species.IsTamable ? $"Cho {Species.displayName} ăn" : Species?.displayName;

    private void Awake()
    {
        motor = GetComponent<AnimalMotor>();
    }

    public void Setup(AnimalSpecies species, int variant, Vector2 home, float wanderRadius)
    {
        Species = species;
        Variant = variant;
        name = $"{species.displayName} hoang dã";
        motor.Setup(species.FramesFor(variant, true), species.framesPerSecond, species.moveSpeed, species.colliderRadius);
        motor.SetWanderAround(home, wanderRadius);

        Sprite sprite = motor.CurrentSprite;
        if (bodyCollider != null)
        {
            bodyCollider.isTrigger = true;
            bodyCollider.radius = species.colliderRadius;
            bodyCollider.offset = new Vector2(0f, species.colliderRadius);
        }
        if (clickArea != null && sprite != null)
        {
            Vector2 size = sprite.bounds.size;
            clickArea.size = new Vector2(size.x * 0.7f, size.y * 0.7f);
            clickArea.offset = new Vector2(0f, size.y * 0.4f);
            if (bubble != null) bubble.SetHeight(size.y * 0.75f);
        }
    }

    private void Update()
    {
        if (Species == null || Time.time < nextFleeCheck) return;
        nextFleeCheck = Time.time + 0.2f;

        PlayerMovement player = PlayerMovement.instance;
        if (player == null) return;

        float distance = Vector2.Distance(player.transform.position, transform.position);
        if (distance > Species.fleeDistance || motor.IsFleeing) return;

        // Cầm đúng thức ăn thì con vật không sợ, đứng nhìn người chơi.
        if (Species.IsTamable && IsHoldingTameFood())
        {
            motor.HoldFacing(player.transform.position, 0.5f);
            return;
        }
        motor.FleeFrom(player.transform.position);
    }

    public bool CanInteract(GameObject interactor)
    {
        return interactor != null && Species != null
            && Vector2.Distance(interactor.transform.position, transform.position) <= maxInteractDistance;
    }

    public void Interact(GameObject interactor)
    {
        if (!CanInteract(interactor)) return;

        if (!Species.IsTamable)
        {
            RanchFeedback.Text(transform.position, $"{Species.displayName} hoang dã, không thuần hoá được", RanchFeedback.Info);
            motor.FleeFrom(interactor.transform.position);
            return;
        }
        if (!IsHoldingTameFood())
        {
            RanchFeedback.Text(transform.position, $"Hãy cầm {RanchManager.ShortName(Species.tameFood)} để làm quen", RanchFeedback.Info);
            return;
        }

        RanchManager ranch = RanchManager.Instance;
        if (ranch == null || !ranch.HasRoom(Species.home))
        {
            RanchFeedback.Text(transform.position, "Chuồng đã đầy, không thể nhận thêm", RanchFeedback.Warn);
            return;
        }

        Inventory inventory = Inventory.Instance;
        if (inventory == null || !inventory.TryRemoveItem(Species.tameFood, 1)) return;

        trust++;
        motor.HoldFacing(interactor.transform.position, 1.5f);
        if (bubble != null) bubble.FlashHeart();

        if (trust < Species.tameFeedsNeeded)
        {
            RanchFeedback.Text(transform.position, $"{Species.displayName} tin bạn hơn ({trust}/{Species.tameFeedsNeeded})", RanchFeedback.Good);
            return;
        }

        if (ranch.AdoptWild(Species, Variant))   // vừa được cho ăn nên về chuồng là đã no hôm nay
        {
            RanchFeedback.Text(transform.position, $"Đã thuần hoá {Species.displayName}! Nó đã về chuồng", RanchFeedback.Good);
            if (Spawner != null) Spawner.NotifyRemoved(this);
            Destroy(gameObject, 0.1f);
        }
    }

    private bool IsHoldingTameFood()
    {
        if (hotbar == null) hotbar = FindAnyObjectByType<HotbarController>();
        InventorySlot slot = hotbar != null ? hotbar.SelectedSlot : null;
        return slot != null && !slot.IsEmpty() && slot.ItemData == Species.tameFood;
    }
}
