using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 5f;
    public float sprintSpeed = 9f;
    public float gravity = -20f;

    private CharacterController controller;
    private Vector3 velocity;
    private Animator animator;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();

        if (animator == null)
            Debug.LogWarning("Animator non trouvé !");
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        float x = 0f;
        float z = 0f;

        if (Keyboard.current.dKey.isPressed
            || Keyboard.current.rightArrowKey.isPressed) x += 1f;
        if (Keyboard.current.aKey.isPressed
            || Keyboard.current.leftArrowKey.isPressed) x -= 1f;
        if (Keyboard.current.wKey.isPressed
            || Keyboard.current.upArrowKey.isPressed) z += 1f;
        if (Keyboard.current.sKey.isPressed
            || Keyboard.current.downArrowKey.isPressed) z -= 1f;

        if (controller.isGrounded && velocity.y < 0)
            velocity.y = -2f;

        velocity.y += gravity * Time.deltaTime;

        Vector3 move = (transform.right * x
            + transform.forward * z) * speed;
        move.y = velocity.y;
        controller.Move(move * Time.deltaTime);

        float moveAmount = new Vector2(x, z).magnitude;
        bool enMarche = moveAmount > 0.1f;

        if (animator != null)
        {
            animator.SetBool("isWalking", enMarche);
            animator.SetBool("strafeGauche", x < -0.1f && enMarche);
            animator.SetBool("strafeDroit", x > 0.1f && enMarche);
        }
    }
}