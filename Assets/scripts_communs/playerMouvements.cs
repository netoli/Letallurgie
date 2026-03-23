using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 5f;
    public float sprintSpeed = 9f;
    public float gravity = -9.81f;

    private CharacterController controller;
    private Vector3 velocity;
    private Animator animator;
    

void Start()
{
    controller = GetComponent<CharacterController>();
    animator = GetComponentInChildren<Animator>();

    if (animator == null)
        Debug.LogWarning("Animator non trouvé sur les enfants du perso !");

    Cursor.lockState = CursorLockMode.Locked;
    Cursor.visible = false;
}

    void Update()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : speed;

        Vector3 move = transform.right * x + transform.forward * z;
        controller.Move(move * currentSpeed * Time.deltaTime);

        // Gravité
        if (controller.isGrounded && velocity.y < 0)
            velocity.y = -2f;

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // Animations
        float moveAmount = new Vector2(x, z).magnitude;
        animator.SetBool("isWalking", moveAmount > 0.1f);
    }
}