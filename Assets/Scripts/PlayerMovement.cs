using System;
using UnityEngine;  

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator anim;
    // [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Movement Control")]
    [Tooltip("Cho phép nhân vật di chuyển hay không.")]
    [SerializeField] private bool canMove = true;

    Vector2 lastDirection;
    Vector2 moveDirection;

    public bool CanMove
    {
        get => canMove;
        set => SetCanMove(value);
    }

    private void OnEnable()
    {
        InventoryUI.OnBagToggled += HandleBagToggled;
    }

    private void OnDisable()
    {
        InventoryUI.OnBagToggled -= HandleBagToggled;
    }

    private void Start()
    {
        // Đồng bộ trạng thái di chuyển theo trạng thái túi đồ lúc bắt đầu
        if (InventoryUI.Instance != null && InventoryUI.Instance.IsOpen)
        {
            SetCanMove(false);
        }
    }

    private void HandleBagToggled(bool isBagOpen)
    {
        SetCanMove(!isBagOpen);
    }

    public void SetCanMove(bool allow)
    {
        canMove = allow;
        if (!canMove)
        {
            moveDirection = Vector2.zero;
            if (anim != null)
            {
                anim.SetFloat("Speed", 0f);
            }
        }
    }

    private void Update()
    {
        Move();
    }

    private void Move()
    {
        if (!canMove)
        {
            moveDirection = Vector2.zero;
            if (anim != null)
            {
                anim.SetFloat("Speed", 0f);
            }
            return;
        }

        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");

        moveDirection = new Vector2(x, y).normalized;
     
        UpdateAnimation();
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
        Vector2 movePosition = rb.position + moveDirection * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(movePosition);
    }
}