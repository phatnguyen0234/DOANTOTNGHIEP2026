using UnityEngine;
using UnityEngine.Tilemaps;

public class GridCursor : MonoBehaviour
{
    public Tilemap groundTilemap;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        UpdateCursor();
    }

    private void UpdateCursor()
    {
        if (groundTilemap == null) return;

        // 1. Get mouse position on screen and convert it to World Space
        Vector3 mouseScreenPos = Input.mousePosition;
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        mouseWorldPos.z = 0f; // Force Z to 0 for 2D games

        // 2. Convert World Position to Grid Cell Position
        Vector3Int cellPosition = groundTilemap.WorldToCell(mouseWorldPos);

        // 3. Optional: Only show/move cursor if pointing at a valid ground tile
        if (groundTilemap.HasTile(cellPosition))
        {
            // Convert back to World Position to get the exact center of the cell
            Vector3 cursorWorldPos = groundTilemap.GetCellCenterWorld(cellPosition);

            // Move the highlight object to this center position
            transform.position = cursorWorldPos;
        }
    }
}
