using UnityEngine;
using UnityEngine.Tilemaps;

public class TileHighlighter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Tilemap targetTilemap;
    [SerializeField] private Sprite rendererSprite; // Sprite viền xanh
    [SerializeField] private Color highlightColor = new Color(0f, 0.8f, 1f, 1f); // Màu xanh dương/xanh lá

    private GameObject highlightObject;
    private SpriteRenderer highlightRenderer;
    private Vector3Int lastCellPosition = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);

    void Start()
    {
        // Tự động tìm Tilemap nếu chưa được gán
        if (targetTilemap == null)
        {
            targetTilemap = GetComponent<Tilemap>();
            if (targetTilemap == null)
            {
#if UNITY_2023_1_OR_NEWER
                targetTilemap = FindFirstObjectByType<Tilemap>();
#else
                targetTilemap = FindObjectOfType<Tilemap>();
#endif
            }
        }
        CreateHighlightIndicator();
    }

    void Update()
    {
        if (targetTilemap == null || highlightObject == null) return;

        // 1. Chuyển đổi vị trí chuột sang World Position
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0; // Đảm bảo trục Z bằng 0 trong môi trường 2D

        // 2. Lấy tọa độ ô Grid tương ứng
        Vector3Int cellPosition = targetTilemap.WorldToCell(mouseWorldPos);

        // 3. Kiểm tra xem ô đó có thay đổi so với khung hình trước hay không
        if (cellPosition != lastCellPosition)
        {
            lastCellPosition = cellPosition;

            // 4. Kiểm tra xem tại vị trí đó có Tile nào không
            if (targetTilemap.HasTile(cellPosition))
            {
                // Hiển thị viền xanh và dịch chuyển đến tâm ô Grid
                highlightObject.SetActive(true);
                highlightObject.transform.position = targetTilemap.GetCellCenterWorld(cellPosition);
            }
            else
            {
                // Ẩn viền xanh nếu di chuột ra ngoài Tilemap
                highlightObject.SetActive(false);
            }
        }
    }

    private void CreateHighlightIndicator()
    {
        // Khởi tạo GameObject highlight
        highlightObject = new GameObject("TileHighlightIndicator");
        highlightObject.transform.SetParent(transform);
        
        highlightRenderer = highlightObject.AddComponent<SpriteRenderer>();
        highlightRenderer.color = highlightColor;
        highlightRenderer.sortingOrder = 10; // Đảm bảo luôn hiển thị trên cùng các Tile bình thường

        if (rendererSprite != null)
        {
            highlightRenderer.sprite = rendererSprite;
        }
        else
        {
            // Nếu chưa gán Sprite trong Editor, ta sẽ tạo một Sprite viền vuông mặc định bằng code
            highlightRenderer.sprite = CreateDefaultBorderSprite();
        }
        highlightObject.SetActive(false);
    }

    // Tạo Sprite viền ô vuông mặc định bằng code nếu không có sprite sẵn
    private Sprite CreateDefaultBorderSprite()
    {
        int size = 64;
        Texture2D texture = new Texture2D(size, size);
        texture.filterMode = FilterMode.Point;
        Color transparent = new Color(0, 0, 0, 0);
        Color white = Color.white;

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                // Vẽ viền dày 4 pixel
                if (x < 4 || x >= size - 4 || y < 4 || y >= size - 4)
                {
                    texture.SetPixel(x, y, white);
                }
                else
                {
                    texture.SetPixel(x, y, transparent);
                }
            }
        }
        texture.Apply();
        
        // Tạo Sprite từ Texture mới vẽ
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
