using UnityEngine;

public class CropSpawner : MonoBehaviour
{
    public Crop cropPrefab;

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            SpawnCrop();
        }
    }

    private void SpawnCrop()
    {
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        mousePosition.z = 0f;

        Instantiate(
            cropPrefab,
            mousePosition,
            Quaternion.identity
        );
    }
}