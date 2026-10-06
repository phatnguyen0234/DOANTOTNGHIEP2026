using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

// Gắn lên RawImage của bản đồ lớn (WorldMapView, phải bật Raycast Target).
// Click chuột trái: đổi điểm click trên bản đồ -> toạ độ world -> ô trên Tilemap Ground, đặt dấu đánh dấu tại ô đó
// (xanh = đi được, đỏ = bị chặn theo WalkableGrid). Click chuột phải: bỏ chọn.
// Các hệ thống khác (tìm đường A*) lắng nghe sự kiện OnCellSelected để lấy ô đích.
[RequireComponent(typeof(RectTransform))]
public class WorldMapClick : MonoBehaviour, IPointerClickHandler
{
    [Header("References")]
    [Tooltip("Camera render bản đồ lớn (WorldMapCamera).")]
    [SerializeField] private Camera mapCamera;

    [Tooltip("Tilemap Ground để đổi toạ độ world sang ô (để trống sẽ lấy từ WalkableGrid hoặc object có tag 'Ground').")]
    [SerializeField] private Tilemap groundTilemap;

    [Tooltip("Dấu đánh dấu ô được chọn, là con của bản đồ, anchor & pivot ở giữa (có thể để trống).")]
    [SerializeField] private RectTransform marker;

    [Header("Marker Colors")]
    [SerializeField] private Color walkableColor = new Color(0.3f, 1f, 0.3f, 1f);
    [SerializeField] private Color blockedColor = new Color(1f, 0.3f, 0.3f, 1f);

    // Ô vừa chọn và ô đó có đi được không.
    public event Action<Vector3Int, bool> OnCellSelected;
    public event Action OnSelectionCleared;

    public bool HasSelection { get; private set; }
    public Vector3Int SelectedCell { get; private set; }

    private RectTransform mapRect;
    private Image markerImage;

    private void Awake()
    {
        mapRect = (RectTransform)transform;
        if (marker != null)
        {
            markerImage = marker.GetComponent<Image>();
            marker.gameObject.SetActive(false);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            ClearSelection();
            return;
        }
        if (eventData.button != PointerEventData.InputButton.Left) return;

        if (!TryScreenToCell(eventData.position, eventData.pressEventCamera, out Vector3Int cell)) return;

        bool walkable = WalkableGrid.Instance != null && WalkableGrid.Instance.IsWalkable(cell);
        SelectedCell = cell;
        HasSelection = true;

        if (markerImage != null) markerImage.color = walkable ? walkableColor : blockedColor;
        Debug.Log($"[WorldMap] Chọn ô ({cell.x}, {cell.y}) - {(walkable ? "đi được" : "bị chặn")}");

        OnCellSelected?.Invoke(cell, walkable);
    }

    public void ClearSelection()
    {
        if (!HasSelection) return;
        HasSelection = false;
        OnSelectionCleared?.Invoke();
    }

    // Điểm trên màn hình -> vị trí trong ảnh bản đồ (0..1) -> toạ độ world qua camera bản đồ -> ô trên Tilemap.
    public bool TryScreenToCell(Vector2 screenPosition, Camera eventCamera, out Vector3Int cell)
    {
        cell = default;
        Tilemap tilemap = ResolveTilemap();
        if (mapCamera == null || tilemap == null) return false;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(mapRect, screenPosition, eventCamera, out Vector2 local))
        {
            return false;
        }

        Rect rect = mapRect.rect;
        Vector2 viewport = new Vector2((local.x - rect.x) / rect.width, (local.y - rect.y) / rect.height);
        if (viewport.x < 0f || viewport.x > 1f || viewport.y < 0f || viewport.y > 1f) return false;

        Vector3 world = mapCamera.ViewportToWorldPoint(new Vector3(viewport.x, viewport.y, 0f));
        cell = tilemap.WorldToCell(new Vector3(world.x, world.y, 0f));
        return true;
    }

    // Đặt dấu đánh dấu đúng tâm ô đã chọn (cập nhật mỗi frame phòng khi camera bản đồ thay đổi).
    private void LateUpdate()
    {
        if (marker == null) return;

        Tilemap tilemap = ResolveTilemap();
        bool visible = HasSelection && mapCamera != null && tilemap != null;
        if (marker.gameObject.activeSelf != visible) marker.gameObject.SetActive(visible);
        if (!visible) return;

        Vector3 viewport = mapCamera.WorldToViewportPoint(tilemap.GetCellCenterWorld(SelectedCell));
        Rect rect = mapRect.rect;
        marker.anchoredPosition = new Vector2((viewport.x - 0.5f) * rect.width, (viewport.y - 0.5f) * rect.height);
    }

    private Tilemap ResolveTilemap()
    {
        if (groundTilemap != null) return groundTilemap;

        if (WalkableGrid.Instance != null && WalkableGrid.Instance.GroundTilemap != null)
        {
            groundTilemap = WalkableGrid.Instance.GroundTilemap;
        }
        else
        {
            GameObject ground = GameObject.FindWithTag("Ground");
            if (ground != null) groundTilemap = ground.GetComponent<Tilemap>();
        }
        return groundTilemap;
    }
}