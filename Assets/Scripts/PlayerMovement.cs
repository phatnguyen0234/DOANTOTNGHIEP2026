using System;
using UnityEngine;  

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator anim;
  //  [SerializeField] private SpriteRenderer spriteRenderer;
    Vector2 lastDirection;
    Vector2 moveDirection;
    private void Update()
    {
        Move();
    }

    private void Move()
    {
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