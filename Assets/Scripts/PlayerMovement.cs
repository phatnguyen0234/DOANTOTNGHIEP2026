using System;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator anim;
    Vector2 lastDirection = Vector2.down;
    public Vector2 FacingDirection => lastDirection;
    public bool isUsingHoe = false;
    public bool isUsingWater = false;
    public bool isUsingAxe = false;
    public bool isUsingPickaxe = false;
    Vector2 moveDirection;
    [SerializeField] private FarmInputController controller;
    [SerializeField] private GameObject particleLeafPrefab;
    [SerializeField] private GameObject particleRockPrefab;
    Vector3 currentTreePostion;
    Vector3 currentRockPosition;

    public static PlayerMovement instance;

    // Hướng tự đi do MapNavigator (tìm đường A*) đặt mỗi bước vật lý; phím bấm luôn được ưu tiên hơn.
    private Vector2 autoMoveDirection;
    public bool HasManualInput { get; private set; }
    public float MoveSpeed => moveSpeed;

    public void SetAutoMoveDirection(Vector2 direction)
    {
        autoMoveDirection = Vector2.ClampMagnitude(direction, 1f);
    }

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        controller = FindAnyObjectByType<FarmInputController>(FindObjectsInactive.Exclude);
    }

    // Update is called once per frame
    void Update()
    {
        bool isFishing = (FishingController.Instance != null && FishingController.Instance.IsFishing) ||
                         (FishingQuickTester.Instance != null && FishingQuickTester.Instance.IsFishing);

        if (isUsingHoe || isFishing)
        {
            moveDirection = Vector2.zero;
            anim.SetFloat("Speed", 0f);
            return;
        }

        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");

        moveDirection = new Vector2(x, y).normalized;
        HasManualInput = moveDirection != Vector2.zero;
        if (!HasManualInput) moveDirection = autoMoveDirection;

        UpdateAnimation();
    }

    public bool TryUseHoe(Vector2 requestFaceDirection)
    {
        if (isUsingHoe) return false;
        if(requestFaceDirection != Vector2.zero)
        {
            lastDirection = requestFaceDirection.normalized;
        }
        isUsingHoe = true;
        moveDirection = Vector2.zero;

        anim.SetFloat("MoveX", lastDirection.x);
        anim.SetFloat("MoveY", lastDirection.y);
        anim.SetFloat("Speed", 0f);
        anim.SetTrigger("UseHoe");
        return true;
    }

    public void OnHoeAnimationComplete()
    {
        isUsingHoe = false;
        controller.OnHoeAnimationComplete();
    }

    public void UsingWater(Vector2 requestFaceDirection)
    {
        if (isUsingWater) return;

        if (requestFaceDirection != Vector2.zero)
        {
            lastDirection = requestFaceDirection.normalized;
        }
        isUsingWater = true;
        moveDirection = Vector2.zero;
        anim.SetFloat("MoveX", lastDirection.x);
        anim.SetFloat("MoveY", lastDirection.y);
        anim.SetFloat("Speed", 0f);
        anim.SetTrigger("UseWater");
    }

    public void OnWaterAnimationComplete()
    {
        isUsingWater = false;
        controller.OnWaterAnimationComplete();
    }

    public void UsingAxe(Vector2 requestFaceDirect, Vector3 treePosition)
    {
        if (isUsingAxe) return;
        if(requestFaceDirect != Vector2.zero)
        {
            lastDirection = requestFaceDirect.normalized;
        }
        currentTreePostion = treePosition;
        isUsingAxe = true;
        moveDirection = Vector2.zero;
        anim.SetFloat("MoveX", lastDirection.x);
        anim.SetFloat("MoveY", lastDirection.y);
        anim.SetFloat("Speed", 0);
        anim.SetTrigger("UseAxe");
    }

    public void OnAxeAnimationComplete()
    {
        isUsingAxe = false;
    }

    public void UsingPickaxe(Vector2 requestDirection, Vector3 rockPosition)
    {
        if (isUsingPickaxe) return;
        lastDirection = requestDirection;
        currentRockPosition = rockPosition;
        isUsingPickaxe = true;
        moveDirection = Vector2.zero;
        anim.SetFloat("MoveX", lastDirection.x);
        anim.SetFloat("MoveY", lastDirection.y);
        anim.SetFloat("Speed", 0);
        anim.SetTrigger("UsePickaxe");
    }

    public void OnCompletePickaxe()
    {
        isUsingPickaxe = false;
    }
    public void LeafEffect()
    {
        GameObject effect = Instantiate(particleLeafPrefab, currentTreePostion, Quaternion.identity);
        Destroy(effect, 2f);
    }

    public void RockEffect()
    {
        GameObject effect = Instantiate(particleRockPrefab, currentRockPosition, Quaternion.identity);
        Destroy(effect, 2f);
    }

    public void SetFacingDirection(Vector2 direction)
    {
        if (direction != Vector2.zero)
        {
            if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y))
            {
                lastDirection = new Vector2(Mathf.Sign(direction.x), 0f);
            }
            else
            {
                lastDirection = new Vector2(0f, Mathf.Sign(direction.y));
            }

            if (anim != null)
            {
                anim.SetFloat("MoveX", lastDirection.x);
                anim.SetFloat("MoveY", lastDirection.y);
                anim.SetFloat("Speed", 0f);
            }
        }
    }

    private void UpdateAnimation()
    {
        if(moveDirection != Vector2.zero)
        {
            lastDirection = moveDirection;
        }
        
        anim.SetFloat("MoveX", lastDirection.x);
        anim.SetFloat("MoveY", lastDirection.y);
        anim.SetFloat("Speed", moveDirection.magnitude);
    }

    private void FixedUpdate()
    {
        bool isFishing = (FishingController.Instance != null && FishingController.Instance.IsFishing) ||
                         (FishingQuickTester.Instance != null && FishingQuickTester.Instance.IsFishing);

        if (isUsingHoe || isFishing) return;

        Vector2 direction = HasManualInput ? moveDirection : autoMoveDirection;
        Vector2 movePosition = rb.position + direction * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(movePosition);
    }
}