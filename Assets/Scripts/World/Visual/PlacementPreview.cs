
public class PlacementPreview : MonoBehaviour
{
    [SerializeField]
    private SpriteRenderer previewRenderer;

    public void Show(
        PlaceableData data,
        Vector3 position,
        bool valid)
    {
        transform.position = position;

        gameObject.SetActive(true);

        // đổi visual valid / invalid
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}