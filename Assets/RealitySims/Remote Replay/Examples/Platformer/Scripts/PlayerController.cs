using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpForce = 10f;
    public LayerMask groundLayer;

    private Rigidbody2D rb;
    private bool isGrounded;
    private Vector2 startTouchPosition, endTouchPosition;
    private float horizontalInput;

    // Start is called before the first frame update
    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    private void Update()
    {
        HandleKeyboardInput();
        HandleTouchInput();
        Move();
        Jump();
    }

    private void HandleKeyboardInput()
    {
        horizontalInput = Input.GetAxis("Horizontal"); // Get keyboard input
    }

    private void HandleTouchInput()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    startTouchPosition = touch.position;
                    break;

                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    endTouchPosition = touch.position;
                    horizontalInput = Mathf.Sign(endTouchPosition.x - startTouchPosition.x);
                    break;

                case TouchPhase.Ended:
                    startTouchPosition = endTouchPosition = Vector2.zero;
                    break;
            }
        }
    }

    private void Move()
    {
        rb.velocity = new Vector2(horizontalInput * moveSpeed, rb.velocity.y);
    }

    private void Jump()
    {
        isGrounded = Physics2D.Raycast((Vector2)transform.position + Vector2.down, Vector2.down, 0.1f, groundLayer);

        // Check for jump input (keyboard or touch)
        if ((Input.GetButtonDown("Jump") || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)) && isGrounded)
        {
            rb.velocity = Vector2.up * jumpForce;
        }
    }
}