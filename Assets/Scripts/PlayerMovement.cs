using System;
using Unity.Jobs;
using UnityEngine;  

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator anim;
  //  [SerializeField] private SpriteRenderer spriteRenderer;
    Vector2 lastDirection = Vector2.down;
    public Vector2 FacingDirection => lastDirection;
    public bool isUsingHoe = false;
    Vector2 moveDirection;
    private void Update()
    {
        if (isUsingHoe) return;
        Move();
    }

    // Update is called once per frame
    void Update()
    {
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

    public void Move()
    {
     //   Vector3 moveDirection = new Vector3(horizontal, vertical, 0).normalized;
        rb.MovePosition(rb.position + moveInput * speed * Time.fixedDeltaTime);
    }

    public void SetFacingDirection(Vector2 direction)
    {
        if (direction == Vector2.zero)
            return;

        // Game hi?n d�ng animation 4 h�?ng, n�n chu?t ch�o s? ch?n tr?c l?ch nhi?u h�n.
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            lastDirection = direction.x > 0 ? Vector2.right : Vector2.left;
        else
            lastDirection = direction.y > 0 ? Vector2.up : Vector2.down;

        // C?p nh?t h�?ng idle ngay, ch? khi player kh�ng di chuy?n.
        if (anim != null && moveInput == Vector2.zero)
        {
            anim.SetFloat("MoveX", lastDirection.x);
            anim.SetFloat("MoveY", lastDirection.y);
        }
    }

    void UpdateAnimator(Animator anim)
    {
        bool isMoving = moveInput != Vector2.zero;
        anim.SetBool("IsMoving", isMoving);
        if(isMoving)
        {
            lastDirection = moveInput;
            anim.SetFloat("MoveX", moveInput.x);
            anim.SetFloat("MoveY", moveInput.y);
        }
        else
        {
            anim.SetFloat("MoveX", lastDirection.x);
            anim.SetFloat("MoveY", lastDirection.y);
        }
    }
}
