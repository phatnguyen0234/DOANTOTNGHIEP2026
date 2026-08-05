using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float speed;
    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Vector2 lastDirection = Vector2.down;
    private Animator anim;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        moveInput = new Vector2(horizontal, vertical).normalized;
        UpdateAnimator(anim);
    }

    private void FixedUpdate()
    {
        Move();
    }

    public void Move()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
     //   Vector3 moveDirection = new Vector3(horizontal, vertical, 0).normalized;
        rb.MovePosition(rb.position + moveInput * speed * Time.fixedDeltaTime);
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
