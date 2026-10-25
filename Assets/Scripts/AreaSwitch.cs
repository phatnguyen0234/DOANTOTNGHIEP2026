using UnityEngine;
using UnityEngine.SceneManagement;

// Start chạy sau SaveManager: LoadGame đặt Player về vị trí trong file save (lúc ra khỏi nhà đó là toạ độ bên trong nhà),
// nên cửa phải đặt Player về startPoint sau cùng thì mới không bị nhảy ra chỗ khác trên map.
[DefaultExecutionOrder(100)]
public class AreaSwitch : MonoBehaviour
{
    [SerializeField] private string sceneToLoad;
    [Tooltip("Tên cặp cửa: cửa đi vào và cửa đi ra phải đặt cùng một tên (vd: House, FarmStore, FishStore).")]
    [SerializeField] private string transitionScene;
    [SerializeField] private Transform startPoint;

    public static string currentScene;

    // Enter Play Mode không reload domain -> xoá cửa vừa đi qua của lần Play trước.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCurrentScene()
    {
        currentScene = null;
    }

    private void Start()
    {
        if (!string.IsNullOrEmpty(currentScene) && transitionScene == currentScene)
        {
            PlayerMovement.instance.transform.position = startPoint.position;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        SaveManager.Instance.SaveGame();

        currentScene = transitionScene;

        SceneManager.LoadScene(sceneToLoad);
    }
}