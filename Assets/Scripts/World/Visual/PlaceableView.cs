
public class PlaceableView : MonoBehaviour
{
    public string InstanceId { get; private set; }

    public void Initialize(
        PlaceableInstance instance)
    {
        InstanceId = instance.Id;
    }
}