using System;
using Unity.Jobs;
using UnityEngine;  

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

    // Update is called once per frame
    void Update()
    {
        if (isUsingHoe) return;
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");

        moveDirection = new Vector2(x, y).normalized;
     
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
        if (isUsingHoe) return;

        Vector2 movePosition = rb.position + moveDirection * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(movePosition);
    }
}