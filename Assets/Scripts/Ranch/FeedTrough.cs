using UnityEngine;

// Máng ăn của một chuồng. Chuột phải: cho cả chuồng ăn bằng thức ăn trong túi (giống ấn J nhưng chỉ chuồng này).
// Máng hiện đầy khi mọi con trong chuồng đã được ăn hôm nay, sang ngày mới lại trống.
public class FeedTrough : MonoBehaviour, IInteractable
{
    [SerializeField] private string troughId = "Coop";
    [SerializeField] private AnimalHouse house;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite fullSprite;
    [SerializeField] private Sprite emptySprite;
    [SerializeField, Min(0.5f)] private float maxInteractDistance = 2.5f;

    public string TroughId => troughId;
    public bool IsFilled { get; private set; }

    public string InteractionPrompt => "Cho cả chuồng ăn";

    private void Awake()
    {
        SetFilled(false);
    }

    public void SetFilled(bool filled)
    {
        IsFilled = filled;
        if (spriteRenderer != null) spriteRenderer.sprite = filled ? fullSprite : emptySprite;
    }

    public bool CanInteract(GameObject interactor)
    {
        return interactor != null && Vector2.Distance(interactor.transform.position, transform.position) <= maxInteractDistance;
    }

    public void Interact(GameObject interactor)
    {
        if (!CanInteract(interactor) || RanchManager.Instance == null) return;
        RanchManager.Instance.FeedAll(house, transform.position);
    }
}
