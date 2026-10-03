using UnityEngine;
using UnityEngine.SceneManagement;

public class AreaSwitch : MonoBehaviour
{
    [SerializeField] private string sceneToLoad;
    [SerializeField] private string transitionScene;
    [SerializeField] private Transform startPoint;

    public static string currentScene;
    private void Start()
    {
        if (transitionScene == currentScene)
        {
            PlayerMovement.instance.transform.position = startPoint.position;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        SoilManager.Instance.GetFarmDataJson();

        currentScene = transitionScene;

        SceneManager.LoadScene(sceneToLoad);
    }
}