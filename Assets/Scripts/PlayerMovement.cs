using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float speed;
    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Vector2 lastDirection = Vector2.down;
    private Animator anim;
    public Vector2 LastDirection => lastDirection;
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
     //   Vector3 moveDirection = new Vector3(horizontal, vertical, 0).normalized;
        rb.MovePosition(rb.position + moveInput * speed * Time.fixedDeltaTime);
    }

    public void SetFacingDirection(Vector2 direction)
    {
        if (direction == Vector2.zero)
            return;

        // Game hi?n dùng animation 4 hý?ng, nên chu?t chéo s? ch?n tr?c l?ch nhi?u hõn.
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            lastDirection = direction.x > 0 ? Vector2.right : Vector2.left;
        else
            lastDirection = direction.y > 0 ? Vector2.up : Vector2.down;

        // C?p nh?t hý?ng idle ngay, ch? khi player không di chuy?n.
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
