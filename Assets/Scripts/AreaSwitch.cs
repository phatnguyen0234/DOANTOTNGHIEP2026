using UnityEngine;
using UnityEngine.SceneManagement;

public class AreaSwitch : MonoBehaviour
{
    [SerializeField] private string sceneToLoad;
    [SerializeField] private Transform startPoint;

    private void Start()
    {
        PlayerMovement.instance.transform.position = startPoint.position;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        SceneManager.LoadScene(sceneToLoad);
    }
}