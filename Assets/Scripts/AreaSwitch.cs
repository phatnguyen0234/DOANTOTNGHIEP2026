using UnityEngine;
using UnityEngine.SceneManagement;

public class AreaSwitch : MonoBehaviour
{
    public string sceneToLoad;

    public Transform startPoint;
    void Start()
    {
        PlayerMovement.instance.transform.position = startPoint.position;
    }

    
    void Update()
    {
        
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.tag == "Player")
        {
            SceneManager.LoadScene(sceneToLoad);
        }
    }
}
