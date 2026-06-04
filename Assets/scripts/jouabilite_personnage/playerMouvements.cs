using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 18f;
    public float gravity = -20f;

    [Header("Sons de pas")]
    [Tooltip("AudioSource utilisee pour jouer les sons de pas. Le " +
        "clip est configure directement sur l'AudioSource (champ " +
        "AudioClip). Pour un clip multi-pas (boucle), coche 'Loop' " +
        "sur l'AudioSource. Decoche son Play On Awake. Si null, " +
        "aucun son n'est joue.")]
    [SerializeField] private AudioSource audioSourcePas;

    private CharacterController controller;
    private Vector3 velocity;
    private Animator animator;

    void Awake()
    {
        // Force la vitesse a 18 dans TOUTES les scenes, peu importe la
        // valeur serialisee de l'instance. C'est la cause du probleme
        // "la meme vitesse ne se comporte pas pareil d'une scene a
        // l'autre" : le joueur etant un prefab (ou un objet distinct)
        // par scene, chaque scene avait sa propre valeur via un override
        // d'instance — scene0=12, scene1=12, scene2_usine=24, scene3=12,
        // scene4=12, environnement_manoir=7.5, prefab joueur=7.5. En
        // forcant ici, plus besoin de regler la vitesse scene par scene.
        // Meme approche que mouseLook qui force mouseSensitivity au Awake.
        speed = 18f;
    }

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();

        if (animator == null)
            Debug.LogWarning("Animator non trouvé !");

        // Si une position a ete capturee avant le precedent LoadScene
        // (ex : transition tutoriel -> recherche_indices apres la
        // cinematique), on la restaure ici pour que le joueur garde
        // sa derniere position. Le CharacterController est
        // temporairement desactive le temps de la teleportation.
        PositionPlayerEntreScenes.AppliquerSiDisponible(
            transform, controller);
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

        // Sons de pas : on demarre l'AudioSource quand le perso
        // commence a bouger (au sol), et on l'arrete des qu'il
        // s'immobilise ou saute. Le clip contient plusieurs pas en
        // boucle (Loop coche sur l'AudioSource).
        bool doitJouerPas = enMarche && controller.isGrounded;
        if (audioSourcePas != null && audioSourcePas.clip != null)
        {
            if (doitJouerPas && !audioSourcePas.isPlaying)
            {
                // Volume des pas : suit la preference de l'onglet audio
                // (avant, le slider "sons de pas" n'etait lu par personne).
                audioSourcePas.volume =
                    PlayerPrefs.GetFloat("volumeSonsPas", 1f);
                audioSourcePas.Play();
            }
            else if (!doitJouerPas && audioSourcePas.isPlaying)
            {
                audioSourcePas.Stop();
            }
        }

        // NOTE : on NE signale PLUS automatiquement "deplacement" ici.
        // C'est le detecteurTuto place au pointeur (devant le bar) qui
        // doit fermer la tuile #1 quand le joueur l'atteint. Sinon la
        // tuile se fermait des le premier pas, avant que le joueur
        // n'arrive a la cible.
    }

}