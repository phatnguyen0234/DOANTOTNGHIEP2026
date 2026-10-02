using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// Minimap cho khu vực Main.
// Một Camera phụ (không phải MainCamera) render toàn bộ map vào RenderTexture, hiển thị qua RawImage (MapView).
// Các icon UI (Player, sau này NPC / điểm câu cá...) được đặt đè lên MapView theo đúng vị trí thế giới.
// Script nằm trong scene Main nên khi chuyển sang scene indoor, minimap tự bị unload cùng scene.
public class MinimapUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Camera phụ render map vào RenderTexture (Tag phải là Untagged).")]
    [SerializeField] private Camera minimapCamera;

    [Tooltip("RectTransform của RawImage hiển thị RenderTexture.")]
    [SerializeField] private RectTransform mapView;

    [Tooltip("Icon Player, là con của MapView, anchor & pivot ở giữa.")]
    [SerializeField] private RectTransform playerIcon;

    [Header("Auto Fit")]
    [Tooltip("TilemapRenderer của Ground để tự căn camera bao trọn khu vực (để trống nếu muốn tự chỉnh camera).")]
    [SerializeField] private TilemapRenderer fitTarget;

    [Tooltip("Khoảng lề thêm quanh map (đơn vị world).")]
    [SerializeField, Min(0f)] private float padding = 1f;

    [Header("Follow Player")]
    [Tooltip("Bật: minimap chỉ hiển thị một vùng quanh Player và di chuyển theo. Tắt: hiển thị toàn bộ map.")]
    [SerializeField] private bool followPlayer = true;

    [Tooltip("Orthographic Size của camera minimap khi bám theo Player (càng nhỏ càng zoom gần).")]
    [SerializeField, Min(1f)] private float followSize = 15f;

    [Header("World Map (chỉ gán ở minimap góc màn hình)")]
    [Tooltip("Panel bản đồ toàn màn hình. Để trống nếu đây chính là bản đồ lớn.")]
    [SerializeField] private GameObject worldMapPanel;

    [Tooltip("Phím mở / đóng bản đồ lớn.")]
    [SerializeField] private KeyCode worldMapKey = KeyCode.M;

    // Các icon theo dõi thêm ngoài Player (NPC, điểm câu cá...).
    private readonly Dictionary<Transform, RectTransform> icons = new Dictionary<Transform, RectTransform>();
    private readonly List<Transform> removeBuffer = new List<Transform>();

    private Bounds mapBounds;
    private bool hasMapBounds;

    // Camera chỉ render khi UI của nó đang hiển thị (bản đồ lớn đóng thì camera của nó không tốn chi phí render).
    private void OnEnable()
    {
        if (minimapCamera != null) minimapCamera.enabled = true;
    }

    private void OnDisable()
    {
        if (minimapCamera != null) minimapCamera.enabled = false;
    }

    private void Start()
    {
        FitCameraToMap();
    }

    private void Update()
    {
        if (worldMapPanel != null && Input.GetKeyDown(worldMapKey))
        {
            worldMapPanel.SetActive(!worldMapPanel.activeSelf);
        }
    }

    private void LateUpdate()
    {
        // Luôn lấy Player qua singleton: Player được DontDestroyOnLoad nên bản trong scene Main có thể đã bị Destroy.
        Transform player = PlayerMovement.instance != null ? PlayerMovement.instance.transform : null;

        if (followPlayer)
        {
            FollowTarget(player);
        }

        UpdateIcon(player, playerIcon);

        foreach (var pair in icons)
        {
            if (pair.Key == null)
            {
                removeBuffer.Add(pair.Key);
                continue;
            }
            UpdateIcon(pair.Key, pair.Value);
        }

        for (int i = 0; i < removeBuffer.Count; i++)
        {
            RectTransform icon = icons[removeBuffer[i]];
            if (icon != null) Destroy(icon.gameObject);
            icons.Remove(removeBuffer[i]);
        }
        removeBuffer.Clear();
    }

    // Đăng ký thêm icon cho một đối tượng. Icon phải là con của MapView, anchor & pivot ở giữa.
    public void AddIcon(Transform target, RectTransform icon)
    {
        if (target == null || icon == null) return;
        icons[target] = icon;
    }

    public void RemoveIcon(Transform target)
    {
        if (target == null) return;
        icons.Remove(target);
    }

    private void FitCameraToMap()
    {
        if (minimapCamera == null || fitTarget == null) return;

        Bounds bounds = fitTarget.bounds;
        bounds.Expand(padding * 2f);
        mapBounds = bounds;
        hasMapBounds = true;

        if (followPlayer)
        {
            minimapCamera.orthographicSize = followSize;
            return;
        }

        minimapCamera.transform.position = new Vector3(bounds.center.x, bounds.center.y, minimapCamera.transform.position.z);

        // Camera.aspect lấy theo tỉ lệ RenderTexture khi đã gán Output Texture.
        float sizeByHeight = bounds.extents.y;
        float sizeByWidth = bounds.extents.x / minimapCamera.aspect;
        minimapCamera.orthographicSize = Mathf.Max(sizeByHeight, sizeByWidth);
    }

    // Camera minimap bám theo Player nhưng bị giới hạn trong biên map, để không lộ vùng trống ngoài map.
    private void FollowTarget(Transform target)
    {
        if (minimapCamera == null || target == null) return;

        Vector3 position = target.position;

        if (hasMapBounds)
        {
            float halfHeight = minimapCamera.orthographicSize;
            float halfWidth = halfHeight * minimapCamera.aspect;
            position.x = ClampAxis(position.x, mapBounds.min.x + halfWidth, mapBounds.max.x - halfWidth, mapBounds.center.x);
            position.y = ClampAxis(position.y, mapBounds.min.y + halfHeight, mapBounds.max.y - halfHeight, mapBounds.center.y);
        }

        minimapCamera.transform.position = new Vector3(position.x, position.y, minimapCamera.transform.position.z);
    }

    // Nếu vùng nhìn lớn hơn map theo trục này thì giữ camera ở giữa map.
    private static float ClampAxis(float value, float min, float max, float center)
    {
        return min > max ? center : Mathf.Clamp(value, min, max);
    }

    private void UpdateIcon(Transform target, RectTransform icon)
    {
        if (icon == null) return;

        bool visible = target != null && minimapCamera != null && mapView != null;
        Vector3 viewport = Vector3.zero;

        if (visible)
        {
            viewport = minimapCamera.WorldToViewportPoint(target.position);
            visible = viewport.x >= 0f && viewport.x <= 1f && viewport.y >= 0f && viewport.y <= 1f;
        }

        if (icon.gameObject.activeSelf != visible)
        {
            icon.gameObject.SetActive(visible);
        }
        if (!visible) return;

        Rect rect = mapView.rect;
        icon.anchoredPosition = new Vector2((viewport.x - 0.5f) * rect.width, (viewport.y - 0.5f) * rect.height);
    }
}