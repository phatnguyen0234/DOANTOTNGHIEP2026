using UnityEngine;

// Di chuyển + hoạt ảnh dùng chung cho mọi con vật: đi lang thang trong vùng cho phép, đứng ăn, ngủ, bỏ chạy.
// Hoạt ảnh đọc từ sheet chuẩn hoá 5 hàng x 4 khung (xem AnimalAnim); sprite gốc quay mặt sang trái nên đi sang phải thì lật ngang.
// Thân là Kinematic + collider trigger: người chơi không đẩy được, con vật tự quét vật cản (hàng rào, cây, nước) trước mỗi bước,
// và con vật trong bãi quây luôn bị giữ trong vùng bãi.
[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
public class AnimalMotor : MonoBehaviour
{
    private enum Mode { Idle, Walk, Eat, Sleep, Flee, Hold }

    [SerializeField] private Vector2 idleTime = new Vector2(1.5f, 4f);
    [SerializeField, Range(0f, 1f)] private float eatChance = 0.35f;
    [SerializeField, Min(1f)] private float fleeSpeedMultiplier = 2.2f;

    private Rigidbody2D body;
    private SpriteRenderer spriteRenderer;
    private Sprite[] frames;
    private float fps = 6f;
    private float speed = 1f;
    private float radius = 0.2f;
    private ContactFilter2D obstacleFilter;
    private readonly RaycastHit2D[] castHits = new RaycastHit2D[6];

    private Rect area;
    private bool hasArea;
    private Vector2 wanderCenter;
    private float wanderRadius = 4f;

    private Mode mode = Mode.Idle;
    private float modeTimer;
    private Vector2 target;
    private AnimalAnim facingRow = AnimalAnim.WalkDown;
    private bool faceRight;
    private float animTime;
    private Vector2 lastPosition;
    private float stuckTimer;
    private bool sleepRequested;

    public bool IsSleeping => mode == Mode.Sleep;
    public bool IsFleeing => mode == Mode.Flee;
    public Sprite CurrentSprite => spriteRenderer != null ? spriteRenderer.sprite : null;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;   // người chơi không đẩy được con vật
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Setup(Sprite[] animalFrames, float framesPerSecond, float moveSpeed, float collisionRadius)
    {
        frames = animalFrames;
        fps = framesPerSecond;
        speed = moveSpeed;
        radius = collisionRadius;
        obstacleFilter = new ContactFilter2D { useTriggers = false };
        lastPosition = transform.position;
        modeTimer = Random.Range(0.2f, idleTime.y);
        ApplyFrame();
    }

    public void SetFrames(Sprite[] animalFrames)
    {
        frames = animalFrames;
        ApplyFrame();
    }

    // Vùng hình chữ nhật cho con vật trong bãi quây.
    public void SetArea(Rect rect)
    {
        area = rect;
        hasArea = true;
    }

    // Đi lang thang quanh một điểm (thú rừng).
    public void SetWanderAround(Vector2 center, float radius)
    {
        wanderCenter = center;
        wanderRadius = radius;
        hasArea = false;
    }

    public void SetSleeping(bool sleep)
    {
        if (sleep == sleepRequested) return;
        sleepRequested = sleep;
        if (sleep) Enter(Mode.Sleep, 0f);
        else Enter(Mode.Idle, Random.Range(0.3f, 1.5f));
    }

    public void FleeFrom(Vector2 threat)
    {
        Vector2 away = (Vector2)transform.position - threat;
        if (away.sqrMagnitude < 0.0001f) away = Random.insideUnitCircle;
        target = PickPoint((Vector2)transform.position + away.normalized * 4f, 1.5f);
        Enter(Mode.Flee, 2.5f);
    }

    // Đứng yên quay về phía một điểm (khi được vuốt ve).
    public void HoldFacing(Vector2 point, float seconds)
    {
        FaceTowards(point - (Vector2)transform.position);
        Enter(Mode.Hold, seconds);
    }

    // Cúi xuống ăn trong `seconds` giây (khi được cho ăn).
    public void PlayEat(float seconds)
    {
        Enter(Mode.Eat, seconds);
    }

    public void Teleport(Vector2 position)
    {
        transform.position = position;
        if (body != null) body.position = position;
        lastPosition = position;
        Enter(Mode.Idle, Random.Range(0.5f, 1.5f));
    }

    private void Update()
    {
        if (frames == null) return;

        if (mode != Mode.Walk && mode != Mode.Sleep)
        {
            modeTimer -= Time.deltaTime;
            if (modeTimer <= 0f) NextDecision();
        }

        animTime += Time.deltaTime;
        ApplyFrame();
    }

    private void FixedUpdate()
    {
        if (frames == null) return;

        // Con vật trong bãi luôn nằm trong vùng bãi, kể cả khi vừa được thả / tải lại.
        if (hasArea && !area.Contains(body.position)) body.position = ClampToArea(body.position);
        if (mode != Mode.Walk && mode != Mode.Flee) return;

        Vector2 position = body.position;
        Vector2 delta = target - position;
        float step = speed * (mode == Mode.Flee ? fleeSpeedMultiplier : 1f) * Time.fixedDeltaTime;
        if (delta.magnitude <= step)
        {
            body.MovePosition(ClampToArea(target));
            Enter(sleepRequested ? Mode.Sleep : Mode.Idle, Random.Range(idleTime.x, idleTime.y));
            return;
        }

        Vector2 direction = delta.normalized;
        FaceTowards(direction);
        if (Blocked(position, direction, step))
        {
            // Phía trước có vật cản (hàng rào, máng, cây, nước, người chơi) -> đứng nghỉ rồi chọn điểm khác.
            Enter(Mode.Idle, Random.Range(0.3f, 1f));
            return;
        }
        body.MovePosition(ClampToArea(position + direction * step));

        bool barelyMoved = (position - lastPosition).sqrMagnitude < step * step * 0.04f;
        stuckTimer = barelyMoved ? stuckTimer + Time.fixedDeltaTime : 0f;
        lastPosition = position;
        if (stuckTimer > 0.6f)
        {
            stuckTimer = 0f;
            Enter(Mode.Idle, Random.Range(0.3f, 1f));
        }
    }

    // Quét hình tròn ở chân theo hướng đi, chỉ tính collider cứng (bỏ qua trigger như vùng click của con vật khác).
    private bool Blocked(Vector2 position, Vector2 direction, float distance)
    {
        Vector2 origin = position + new Vector2(0f, radius);
        int count = Physics2D.CircleCast(origin, radius * 0.9f, direction, obstacleFilter, castHits, distance + 0.03f);
        for (int i = 0; i < count; i++)
        {
            RaycastHit2D hit = castHits[i];
            if (hit.collider == null || hit.rigidbody == body) continue;
            if (hit.distance <= 0f && Vector2.Dot(hit.normal, direction) >= 0f) continue;   // đang chồng lên vật cản mà đi ra xa thì cho đi
            return true;
        }
        return false;
    }

    private Vector2 ClampToArea(Vector2 point)
    {
        if (!hasArea) return point;
        return new Vector2(Mathf.Clamp(point.x, area.xMin, area.xMax), Mathf.Clamp(point.y, area.yMin, area.yMax));
    }

    private void NextDecision()
    {
        if (sleepRequested)
        {
            Enter(Mode.Sleep, 0f);
            return;
        }
        if (mode == Mode.Idle && Random.value < eatChance)
        {
            Enter(Mode.Eat, Random.Range(2f, 4f));
            return;
        }
        Vector2 center = hasArea ? area.center : wanderCenter;
        float radius = hasArea ? 0f : wanderRadius;
        target = PickPoint(center, radius);
        Enter(Mode.Walk, 0f);
    }

    private void Enter(Mode newMode, float duration)
    {
        mode = newMode;
        modeTimer = duration;
        stuckTimer = 0f;
        animTime = 0f;
    }

    // Chọn một điểm đi được: trong vùng chữ nhật nếu có, ngược lại trong bán kính quanh `center`.
    private Vector2 PickPoint(Vector2 center, float radius)
    {
        WalkableGrid grid = WalkableGrid.Instance;
        for (int attempt = 0; attempt < 8; attempt++)
        {
            Vector2 point = hasArea && radius <= 0f
                ? new Vector2(Random.Range(area.xMin, area.xMax), Random.Range(area.yMin, area.yMax))
                : center + Random.insideUnitCircle * Mathf.Max(radius, 0.5f);
            if (hasArea) point = new Vector2(Mathf.Clamp(point.x, area.xMin, area.xMax), Mathf.Clamp(point.y, area.yMin, area.yMax));
            if (grid == null || !grid.IsBuilt || grid.IsWalkable((Vector3)point)) return point;
        }
        return transform.position;
    }

    private void FaceTowards(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.0001f) return;
        if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y))
        {
            facingRow = AnimalAnim.WalkSide;
            faceRight = direction.x > 0f;
        }
        else
        {
            facingRow = direction.y > 0f ? AnimalAnim.WalkUp : AnimalAnim.WalkDown;
        }
    }

    private void ApplyFrame()
    {
        if (frames == null || frames.Length < AnimalSpecies.FrameCount || spriteRenderer == null) return;

        AnimalAnim row;
        int column;
        switch (mode)
        {
            case Mode.Walk:
            case Mode.Flee:
                row = facingRow;
                column = Mathf.FloorToInt(animTime * fps * (mode == Mode.Flee ? 1.6f : 1f)) % 4;
                break;
            case Mode.Eat:
                row = AnimalAnim.Eat;
                column = Mathf.FloorToInt(animTime * fps * 0.6f) % 4;
                break;
            case Mode.Sleep:
                row = AnimalAnim.Sleep;
                column = Mathf.FloorToInt(animTime * fps * 0.3f) % 4;
                break;
            default:
                row = facingRow;
                column = 0;
                break;
        }

        Sprite sprite = frames[(int)row * 4 + column];
        if (sprite != null) spriteRenderer.sprite = sprite;

        // Hàng đi lên / đi xuống không lật; các hàng nhìn ngang (đi, ăn, ngủ) lật khi quay sang phải.
        spriteRenderer.flipX = row != AnimalAnim.WalkDown && row != AnimalAnim.WalkUp && faceRight;
    }
}
