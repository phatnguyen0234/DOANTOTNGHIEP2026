using UnityEngine;

// Chuồng gà / chuồng bò. Bãi quây phía trước là vùng con vật đi lại.
// Chuột phải khi cầm vật phẩm con giống đúng loại -> thả con non vào chuồng; tay không -> xem số con và số con đang đói.
public class AnimalHouse : MonoBehaviour, IInteractable
{
    [SerializeField] private string houseId = "Coop";
    [SerializeField] private string displayName = "Chuồng gà";
    [SerializeField] private AnimalHome home = AnimalHome.Coop;
    [SerializeField, Min(1)] private int capacity = 8;
    [SerializeField] private FeedTrough trough;
    [Tooltip("Vùng bãi quây (toạ độ thế giới) cho con vật đi lại.")]
    [SerializeField] private Rect penRect = new Rect(0f, 0f, 6f, 4f);
    [SerializeField, Min(0.5f)] private float maxInteractDistance = 4f;

    private HotbarController hotbar;

    public string HouseId => houseId;
    public string DisplayName => displayName;
    public AnimalHome Home => home;
    public int Capacity => capacity;
    public FeedTrough Trough => trough;
    public Rect PenRect => penRect;

    public string InteractionPrompt => displayName;

    public Vector2 RandomPointInPen()
    {
        return new Vector2(Random.Range(penRect.xMin, penRect.xMax), Random.Range(penRect.yMin, penRect.yMax));
    }

    public bool CanInteract(GameObject interactor)
    {
        return interactor != null && Vector2.Distance(interactor.transform.position, ClosestPoint(interactor.transform.position)) <= maxInteractDistance;
    }

    public void Interact(GameObject interactor)
    {
        RanchManager ranch = RanchManager.Instance;
        if (ranch == null) return;

        ItemData held = HeldItem();
        AnimalSpecies species = held != null ? ranch.SpeciesForPurchase(held) : null;
        Vector3 textAt = transform.position + new Vector3(0f, 2.5f, 0f);

        if (species != null)
        {
            if (species.home != home)
            {
                string other = species.home == AnimalHome.Coop ? "chuồng gà" : "chuồng bò";
                RanchFeedback.Text(textAt, $"{species.displayName} phải thả vào {other}", RanchFeedback.Warn);
                return;
            }
            if (ranch.CountIn(houseId) >= capacity)
            {
                RanchFeedback.Text(textAt, $"{displayName} đã đầy ({capacity} con)", RanchFeedback.Warn);
                return;
            }
            if (Inventory.Instance != null && Inventory.Instance.TryRemoveItem(held, 1) && ranch.AddAnimal(species, this, false))
            {
                RanchFeedback.Text(textAt, $"Chào mừng {species.displayName} con về {displayName}!", RanchFeedback.Good);
            }
            return;
        }

        int count = ranch.CountIn(houseId);
        int hungry = ranch.HungryIn(houseId);
        string state = count == 0 ? "" : (hungry > 0 ? $" - {hungry} con đang đói (ấn J để cho ăn)" : " - tất cả đã no");
        RanchFeedback.Text(textAt, $"{displayName}: {count}/{capacity} con{state}", hungry > 0 ? RanchFeedback.Warn : RanchFeedback.Info);
    }

    private ItemData HeldItem()
    {
        if (hotbar == null) hotbar = FindAnyObjectByType<HotbarController>();
        InventorySlot slot = hotbar != null ? hotbar.SelectedSlot : null;
        return slot != null && !slot.IsEmpty() ? slot.ItemData : null;
    }

    private Vector2 ClosestPoint(Vector2 from)
    {
        Collider2D col = GetComponent<Collider2D>();
        return col != null ? col.ClosestPoint(from) : (Vector2)transform.position;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.8f);
        Gizmos.DrawWireCube(penRect.center, penRect.size);
    }
}
