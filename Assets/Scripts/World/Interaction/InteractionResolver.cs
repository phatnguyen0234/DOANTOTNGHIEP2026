
public class InteractionResolver : MonoBehaviour
{
    [SerializeField]
    private FarmManager farmManager;

    [SerializeField]
    private PlaceableManager placeableManager;

    [SerializeField]
    private SurfaceManager surfaceManager;

    public bool Use(InteractionContext context)
    {
        var item = context.SelectedItem;

        if (item == null)
            return false;

        switch (item.Category)
        {
            case ItemCategory.Tool:
                return UseTool(context);

            case ItemCategory.Seed:
                return UseSeed(context);

            case ItemCategory.Placeable:
                return UsePlaceable(context);
        }

        return false;
    }
}