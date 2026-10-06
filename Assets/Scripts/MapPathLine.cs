using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Vẽ đường đi của MapNavigator lên bản đồ (UI). Gắn lên một object con phủ kín ảnh bản đồ
// (WorldMapView của bản đồ lớn, hoặc MapView của minimap góc), kèm camera đang render bản đồ đó.
// Đường được vẽ từ vị trí Player hiện tại tới đích và ngắn dần khi Player đi.
[RequireComponent(typeof(CanvasRenderer))]
public class MapPathLine : MaskableGraphic
{
    [Tooltip("Camera render bản đồ này (WorldMapCamera hoặc MinimapCamera).")]
    [SerializeField] private Camera mapCamera;

    [Tooltip("Độ dày nét vẽ (đơn vị UI).")]
    [SerializeField, Min(0.5f)] private float thickness = 3f;

    private readonly List<Vector3> worldPoints = new List<Vector3>();
    private readonly List<Vector2> localPoints = new List<Vector2>();
    private bool hadPoints;

#if UNITY_EDITOR
    protected override void Reset()
    {
        base.Reset();
        color = new Color(1f, 0.85f, 0.2f, 1f);
        raycastTarget = false;
    }
#endif

    protected override void Awake()
    {
        base.Awake();
        raycastTarget = false;   // không chặn click lên bản đồ
    }

    private void LateUpdate()
    {
        MapNavigator navigator = MapNavigator.Instance;
        if (navigator != null) navigator.GetRemainingPoints(worldPoints);
        else worldPoints.Clear();

        bool hasPoints = worldPoints.Count >= 2 && mapCamera != null;
        if (!hasPoints && !hadPoints) return;   // không có đường và đã xoá nét cũ -> khỏi vẽ lại
        hadPoints = hasPoints;

        localPoints.Clear();
        if (hasPoints)
        {
            Rect rect = rectTransform.rect;
            foreach (Vector3 point in worldPoints)
            {
                Vector3 viewport = mapCamera.WorldToViewportPoint(point);
                localPoints.Add(new Vector2(rect.x + viewport.x * rect.width, rect.y + viewport.y * rect.height));
            }
        }
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (localPoints.Count < 2) return;

        float half = thickness / 2f;
        for (int i = 0; i < localPoints.Count - 1; i++)
        {
            AddSegment(vh, localPoints[i], localPoints[i + 1], half);
        }

        // Ô vuông ở mỗi điểm rẽ để nét liền mạch, không bị hở góc.
        for (int i = 1; i < localPoints.Count; i++)
        {
            AddSquare(vh, localPoints[i], half);
        }
    }

    private void AddSegment(VertexHelper vh, Vector2 a, Vector2 b, float half)
    {
        Vector2 direction = b - a;
        if (direction.sqrMagnitude < 0.0001f) return;
        Vector2 normal = new Vector2(-direction.y, direction.x).normalized * half;
        AddQuad(vh, a - normal, a + normal, b + normal, b - normal);
    }

    private void AddSquare(VertexHelper vh, Vector2 center, float half)
    {
        AddQuad(vh, center + new Vector2(-half, -half), center + new Vector2(-half, half),
            center + new Vector2(half, half), center + new Vector2(half, -half));
    }

    private void AddQuad(VertexHelper vh, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
    {
        int start = vh.currentVertCount;
        vh.AddVert(p0, color, Vector2.zero);
        vh.AddVert(p1, color, Vector2.zero);
        vh.AddVert(p2, color, Vector2.zero);
        vh.AddVert(p3, color, Vector2.zero);
        vh.AddTriangle(start, start + 1, start + 2);
        vh.AddTriangle(start + 2, start + 3, start);
    }
}
