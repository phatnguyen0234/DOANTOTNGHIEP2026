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
    Vector2 moveDirection;
    [SerializeField] FarmInputController controller;

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