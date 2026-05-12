using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 5f;
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
        if (controller == null || !controller.enabled) return;

        // Tant que la premiere tuile de tuto n'est pas affichee
        // (banniere de chapitre, delais d'intro), on ignore les
        // touches de deplacement. Le regard a la souris reste libre
        // (gere par mouseLook / CinemachineInputAxisController).
        bool deplacementBloque =
            gestionChapitres.Instance != null
            && !gestionChapitres.Instance.MouvementAutorise;

        float x = 0f;
        float z = 0f;

        if (!deplacementBloque)
        {
            if (Keyboard.current.dKey.isPressed
                || Keyboard.current.rightArrowKey.isPressed) x += 1f;
            if (Keyboard.current.aKey.isPressed
                || Keyboard.current.leftArrowKey.isPressed) x -= 1f;
            if (Keyboard.current.wKey.isPressed
                || Keyboard.current.upArrowKey.isPressed) z += 1f;
            if (Keyboard.current.sKey.isPressed
                || Keyboard.current.downArrowKey.isPressed) z -= 1f;
        }

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

        // NOTE : on NE signale PLUS automatiquement "deplacement" ici.
        // C'est le detecteurTuto place au pointeur (devant le bar) qui
        // doit fermer la tuile #1 quand le joueur l'atteint. Sinon la
        // tuile se fermait des le premier pas, avant que le joueur
        // n'arrive a la cible.
    }
}