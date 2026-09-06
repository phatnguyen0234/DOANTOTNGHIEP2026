using UnityEngine;
using UnityEngine.Tilemaps;

public class GridCursor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private PlayerMovement player;

    [Header("Interaction")]
    [SerializeField, Min(0.1f)] private float interactionRangeInCells = 2f;

    private bool interactionEnabled;

    public Vector3Int CurrentCell { get; private set; }
    public bool HasValidTarget { get; private set; }

    private SpriteRenderer cursorRenderer;

    private void Awake()
    {
        cursorRenderer = GetComponent<SpriteRenderer>();
        SetCursorVisible(false);
        interactionEnabled = false;
    }

    private void Update()
    {
        UpdateCursor();
    }

    private void UpdateCursor()
    {
        HasValidTarget = false;
        if (!interactionEnabled)
        {
            SetCursorVisible(false);
            return;
        }

        if (groundTilemap == null || player == null || Camera.main == null)
        {
            SetCursorVisible(false);
            return;
        }

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;

        Vector3Int mouseCell = groundTilemap.WorldToCell(mouseWorld);
        Vector3Int playerCell = groundTilemap.WorldToCell(player.transform.position);

        Vector3Int delta = mouseCell - playerCell;
        float distanceSquared = delta.x * delta.x + delta.y * delta.y;

        // Bán kính tròn 2 cell: 2² = 4.
        bool isInRange = delta != Vector3Int.zero && distanceSquared <= 4f;
        bool isOnGround = groundTilemap.HasTile(mouseCell);

        if (!isInRange || !isOnGround)
        {
            SetCursorVisible(false);
            return;
        }

        CurrentCell = mouseCell;
        HasValidTarget = true;

        transform.position = groundTilemap.GetCellCenterWorld(mouseCell);
        SetCursorVisible(true);

        // Chỉ khi mouse trong tầm mới làm player quay.
        player.SetFacingDirection(new Vector2(delta.x, delta.y));
    }

    private void SetCursorVisible(bool visible)
    {
        if (cursorRenderer != null)
            cursorRenderer.enabled = visible;
    }

    public void SetInteractionEnabled(bool enabled)
    {
        interactionEnabled = enabled;

        if (!interactionEnabled)
        {
            HasValidTarget = false;
            SetCursorVisible(false);
        }
    }
}