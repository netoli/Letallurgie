using UnityEngine;
using UnityEngine.InputSystem;

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

        // Le curseur est géré par gestionsTransitions — ne pas locker ici
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        // Nouveau Input System
        float x = 0f;
        float z = 0f;

        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) x += 1f;
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)  x -= 1f;
        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)    z += 1f;
        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)  z -= 1f;

        bool sprint = Keyboard.current.leftShiftKey.isPressed;
        float currentSpeed = sprint ? sprintSpeed : speed;

        // Gravité
        if (controller.isGrounded && velocity.y < 0)
            velocity.y = -2f;

        velocity.y += gravity * Time.deltaTime;

        // Un seul Move qui combine déplacement + gravité
        Vector3 move = (transform.right * x + transform.forward * z) * currentSpeed;
        move.y = velocity.y;
        controller.Move(move * Time.deltaTime);

        // Animations
        float moveAmount = new Vector2(x, z).magnitude;
        animator.SetBool("isWalking", moveAmount > 0.1f);
    }
}
    