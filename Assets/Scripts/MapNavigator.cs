using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Điều hướng Player bằng bản đồ:
// 1. Click một ô trên bản đồ lớn -> A* tìm đường từ ô dưới chân Player, vẽ đường và hiện nhắc "Ấn L để tự động di chuyển".
// 2. Chế độ chỉ đường: người chơi tự đi, đường luôn bám theo vị trí Player (đi lệch thì tự tìm lại đường).
// 3. Ấn L: Player tự đi theo đường. Bấm phím di chuyển hoặc ấn L lần nữa -> quay về chế độ chỉ đường (không mất đường).
// Đường bị xoá khi tới nơi hoặc click chuột phải trên bản đồ.
// Chạy FixedUpdate trước PlayerMovement để hướng tự đi luôn là mới nhất trong mỗi bước vật lý.
[DefaultExecutionOrder(-50)]
public class MapNavigator : MonoBehaviour
{
    public static MapNavigator Instance { get; private set; }

    private enum State { None, Guiding, AutoMoving }

    [Header("References")]
    [Tooltip("Script click trên bản đồ lớn (WorldMapView). Để trống sẽ tự tìm.")]
    [SerializeField] private WorldMapClick worldMap;

    [Tooltip("Panel bản đồ lớn, dùng khi bật 'Close Map On Auto Move' (có thể để trống).")]
    [SerializeField] private GameObject worldMapPanel;

    [Tooltip("Dòng chữ nhắc trên màn hình (TextMeshPro). Có thể để trống.")]
    [SerializeField] private TMP_Text promptText;

    [Header("Prompt")]
    [SerializeField] private KeyCode autoMoveKey = KeyCode.L;
    [SerializeField] private string guidingMessage = "Ấn L để tự động di chuyển";
    [SerializeField] private string autoMovingMessage = "Đang tự động di chuyển - ấn L hoặc phím di chuyển để dừng";

    [Header("Settings")]
    [Tooltip("Ô đích bị chặn (cây, nhà, nước...) thì đi tới ô trống gần nhất trong bán kính này.")]
    [SerializeField, Min(0)] private int snapRadius = 3;

    [Tooltip("Đóng bản đồ lớn khi bắt đầu tự đi.")]
    [SerializeField] private bool closeMapOnAutoMove = true;

    [Tooltip("Khoảng cách coi như đã tới một điểm trên đường khi tự đi (đơn vị world).")]
    [SerializeField, Min(0.01f)] private float arriveDistance = 0.05f;

    [Tooltip("Thời gian tối thiểu giữa 2 lần tìm lại đường khi người chơi đi lệch (giây).")]
    [SerializeField, Min(0f)] private float replanInterval = 0.25f;

    [Tooltip("Đứng yên quá số giây này khi đang tự đi thì coi là bị kẹt.")]
    [SerializeField, Min(0.1f)] private float stuckTime = 0.6f;

    [Tooltip("Số lần tìm lại đường tối đa khi bị kẹt lúc tự đi, quá thì chuyển về chỉ đường.")]
    [SerializeField, Min(0)] private int maxRepath = 2;

    public bool IsNavigating => state != State.None;
    public bool IsAutoMoving => state == State.AutoMoving;

    private State state = State.None;
    private Vector3Int goalCell;

    // Đường đầy đủ từng ô + vị trí từng ô trong đường (để biết Player đang ở đâu trên đường).
    private readonly List<Vector3Int> pathCells = new List<Vector3Int>();
    private readonly Dictionary<Vector3Int, int> pathIndexOf = new Dictionary<Vector3Int, int>();
    private int pathProgress;

    // Các điểm rẽ (vị trí chân Player) của phần đường còn lại.
    private readonly List<Vector3> waypoints = new List<Vector3>();
    private int waypointIndex;

    private PlayerMovement player;
    private Rigidbody2D playerBody;
    private Collider2D playerCollider;

    private Vector3Int lastFootCell;
    private float lastReplanTime = -999f;
    private Vector2 lastFootPosition;
    private float stuckTimer;
    private int repathCount;

    private void Awake()
    {
        Instance = this;
        UpdatePrompt();
    }

    private void OnEnable()
    {
        if (worldMap == null) worldMap = FindAnyObjectByType<WorldMapClick>(FindObjectsInactive.Include);
        if (worldMap != null)
        {
            worldMap.OnCellSelected += HandleCellSelected;
            worldMap.OnSelectionCleared += Stop;
        }
    }

    private void OnDisable()
    {
        if (worldMap != null)
        {
            worldMap.OnCellSelected -= HandleCellSelected;
            worldMap.OnSelectionCleared -= Stop;
        }
        Stop();
    }

    private void HandleCellSelected(Vector3Int cell, bool walkable)
    {
        if (PlanPath(cell)) SetState(State.Guiding);
        else Stop();
    }

    private void Update()
    {
        if (state == State.None) return;

        if (Input.GetKeyDown(autoMoveKey))
        {
            if (state == State.Guiding) StartAutoMove();
            else SetState(State.Guiding);
            return;
        }

        if (state == State.Guiding) TrackManualProgress();
    }

    private void FixedUpdate()
    {
        if (state != State.AutoMoving) return;
        if (!ResolvePlayer())
        {
            Stop();
            return;
        }

        // Người chơi tự bấm phím -> thôi tự đi nhưng vẫn giữ đường để chỉ đường.
        if (player.HasManualInput)
        {
            SetState(State.Guiding);
            return;
        }

        Vector2 foot = FootPosition();
        Vector2 delta = (Vector2)waypoints[waypointIndex] - foot;
        while (delta.magnitude <= arriveDistance)
        {
            waypointIndex++;
            if (waypointIndex >= waypoints.Count)
            {
                Arrive();
                return;
            }
            delta = (Vector2)waypoints[waypointIndex] - foot;
        }

        // Hướng có độ lớn tối đa 1; khi gần tới điểm thì nhỏ lại để bước vừa đúng tới nơi, không bị vượt quá.
        float step = player.MoveSpeed * Time.fixedDeltaTime;
        player.SetAutoMoveDirection(step > 0f ? Vector2.ClampMagnitude(delta / step, 1f) : Vector2.zero);

        CheckStuck(foot, step, delta);
    }

    // ---------- Tìm đường ----------

    // Tìm đường từ ô dưới chân Player tới ô đích (đích bị chặn thì lấy ô trống gần nhất). Trả về false nếu không có đường.
    private bool PlanPath(Vector3Int targetCell)
    {
        WalkableGrid grid = WalkableGrid.Instance;
        if (grid == null || !ResolvePlayer()) return false;

        Vector3Int footCell = grid.WorldToCell(FootPosition());
        if (!grid.TryGetNearestWalkable(footCell, 2, out Vector3Int start))
        {
            Debug.LogWarning("[MapNavigator] Player đang đứng ở ô không đi được.");
            return false;
        }

        if (!grid.TryGetNearestWalkable(targetCell, snapRadius, out Vector3Int goal))
        {
            Debug.Log("[MapNavigator] Không có ô đi được gần điểm đã chọn.");
            return false;
        }

        List<Vector3Int> path = AStarPathfinder.FindPath(grid, start, goal);
        if (path == null)
        {
            Debug.Log($"[MapNavigator] Không tìm được đường từ ({start.x}, {start.y}) tới ({goal.x}, {goal.y}).");
            return false;
        }

        goalCell = goal;
        pathCells.Clear();
        pathCells.AddRange(path);
        pathIndexOf.Clear();
        for (int i = 0; i < pathCells.Count; i++)
        {
            pathIndexOf[pathCells[i]] = i;
        }
        pathProgress = 0;
        lastFootCell = footCell;
        RebuildWaypoints();

        Debug.Log($"[MapNavigator] Tìm thấy đường dài {path.Count} ô tới ({goal.x}, {goal.y}).");
        return true;
    }

    // Điểm rẽ của phần đường từ vị trí hiện tại trên đường tới đích.
    private void RebuildWaypoints()
    {
        waypoints.Clear();
        waypointIndex = 0;

        WalkableGrid grid = WalkableGrid.Instance;
        if (grid == null) return;

        List<Vector3Int> remaining = pathCells.GetRange(pathProgress, pathCells.Count - pathProgress);
        foreach (Vector3Int cell in AStarPathfinder.Simplify(remaining))
        {
            waypoints.Add(grid.CellCenter(cell));
        }
    }

    // Chế độ chỉ đường: theo dõi Player đi tay. Đứng trên đường -> cắt bớt phần đã qua; đi lệch -> tìm lại đường.
    private void TrackManualProgress()
    {
        WalkableGrid grid = WalkableGrid.Instance;
        if (grid == null || !ResolvePlayer()) return;

        Vector3Int footCell = grid.WorldToCell(FootPosition());
        if (footCell == goalCell)
        {
            Arrive();
            return;
        }
        if (footCell == lastFootCell) return;

        if (pathIndexOf.TryGetValue(footCell, out int index))
        {
            lastFootCell = footCell;
            pathProgress = index;
            RebuildWaypoints();
        }
        else if (Time.time - lastReplanTime >= replanInterval)
        {
            lastReplanTime = Time.time;
            lastFootCell = footCell;
            PlanPath(goalCell);
        }
    }

    // ---------- Tự đi ----------

    private void StartAutoMove()
    {
        // Tìm lại từ đúng chỗ đang đứng để Player đi thẳng vào đường, không quay lại điểm cũ.
        if (!PlanPath(goalCell)) return;

        repathCount = 0;
        stuckTimer = 0f;
        lastFootPosition = FootPosition();
        SetState(State.AutoMoving);

        if (closeMapOnAutoMove && worldMapPanel != null) worldMapPanel.SetActive(false);
    }

    private void CheckStuck(Vector2 foot, float step, Vector2 delta)
    {
        if (player.isUsingHoe)
        {
            stuckTimer = 0f;   // đang cuốc đất thì đứng yên là bình thường
            lastFootPosition = foot;
            return;
        }

        float moved = (foot - lastFootPosition).magnitude;
        lastFootPosition = foot;
        stuckTimer = moved < step * 0.1f ? stuckTimer + Time.fixedDeltaTime : 0f;
        if (stuckTimer < stuckTime) return;

        stuckTimer = 0f;
        if (repathCount >= maxRepath)
        {
            Debug.Log("[MapNavigator] Bị kẹt, chuyển về chế độ chỉ đường.");
            SetState(State.Guiding);
            return;
        }

        // Vật cản có thể vừa thay đổi (cây mới mọc, object mới đặt...) -> quét lại quanh chỗ kẹt rồi tìm đường lại.
        repathCount++;
        if (WalkableGrid.Instance != null) WalkableGrid.Instance.RefreshArea(foot + delta.normalized, 1);
        if (!PlanPath(goalCell)) SetState(State.Guiding);
    }

    // ---------- Trạng thái ----------

    private void SetState(State newState)
    {
        if (state == State.AutoMoving && newState != State.AutoMoving)
        {
            if (player != null) player.SetAutoMoveDirection(Vector2.zero);

            // Buộc chế độ chỉ đường đồng bộ lại vị trí trên đường ngay frame sau.
            lastFootCell = new Vector3Int(int.MinValue, int.MinValue, 0);
        }

        state = newState;
        UpdatePrompt();
    }

    public void Stop()
    {
        SetState(State.None);
        pathCells.Clear();
        pathIndexOf.Clear();
        waypoints.Clear();
    }

    private void Arrive()
    {
        Debug.Log("[MapNavigator] Đã tới nơi.");
        Stop();
        if (worldMap != null) worldMap.ClearSelection();
    }

    private void UpdatePrompt()
    {
        if (promptText == null) return;

        bool visible = state != State.None;
        if (promptText.gameObject.activeSelf != visible) promptText.gameObject.SetActive(visible);
        if (visible) promptText.text = state == State.AutoMoving ? autoMovingMessage : guidingMessage;
    }

    // ---------- Vẽ đường ----------

    // Các điểm còn lại phải đi (bắt đầu từ vị trí chân Player hiện tại), dùng để vẽ đường lên bản đồ.
    public void GetRemainingPoints(List<Vector3> result)
    {
        result.Clear();
        if (state == State.None || player == null) return;

        result.Add(FootPosition());
        for (int i = waypointIndex; i < waypoints.Count; i++)
        {
            result.Add(waypoints[i]);
        }
    }

    // ---------- Player ----------

    private Vector2 FootPosition()
    {
        // Lấy tâm collider (ở chân nhân vật) làm vị trí đứng trên lưới.
        return playerCollider != null ? (Vector2)playerCollider.bounds.center : playerBody.position;
    }

    private bool ResolvePlayer()
    {
        if (player != null && playerBody != null) return true;

        player = PlayerMovement.instance;
        if (player == null) return false;

        playerBody = player.GetComponent<Rigidbody2D>();
        foreach (Collider2D collider in player.GetComponents<Collider2D>())
        {
            if (!collider.isTrigger)
            {
                playerCollider = collider;
                break;
            }
        }
        return playerBody != null;
    }
}
