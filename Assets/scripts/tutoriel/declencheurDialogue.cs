using UnityEngine;

/// <summary>
/// Trigger de proximite qui demarre (ou reprend) un DialogueTuto cible
/// quand le joueur entre dans sa zone. Complement au clic existant :
/// le clic reste possible, mais le joueur peut aussi simplement
/// s'approcher pour declencher le dialogue.
///
/// SETUP UNITY :
/// 1. Creer un GameObject "detecteur_dialogue_tavernier" (par ex.)
///    devant le tavernier ou la table du PNJ.
/// 2. Ajouter un Collider (ex: SphereCollider, BoxCollider) avec
///    "Is Trigger" coche. Dimensionner la zone (rayon 2-3m typique).
/// 3. Attacher ce script declencheurDialogue.
/// 4. Glisser le DialogueTuto du PNJ correspondant dans le champ
///    'dialogueCible' de l'Inspector.
/// 5. Le GameObject Player doit avoir le tag "Player".
///
/// COMPORTEMENT :
/// - 1er passage du joueur : demarre le dialogue (DemarrerAuto).
/// - Si le joueur sort puis revient : si le dialogue est en pause
///   distance, le reprend (ReprendreExterne). Sinon (deja fini),
///   ignore.
/// </summary>
[RequireComponent(typeof(Collider))]
public class declencheurDialogue : MonoBehaviour
{
    [Header("Cible")]
    [Tooltip("Le DialogueTuto a demarrer ou reprendre quand le joueur " +
        "entre dans cette zone.")]
    [SerializeField] private DialogueTuto dialogueCible;

    [Header("Configuration")]
    [Tooltip("Tag du joueur. Par defaut : 'Player'.")]
    [SerializeField] private string tagJoueur = "Player";

    [Tooltip("Si coche, le detecteur ne se declenche qu'une fois (le " +
        "1er passage du joueur). Decoche pour permettre la reprise du " +
        "dialogue si le joueur s'est eloigne (pause distance) puis " +
        "revient. Recommande : decoche.")]
    [SerializeField] private bool declenchementUnique = false;

    private bool dejaDeclenche = false;

    void Awake()
    {
        // S'assurer que le collider est en mode trigger
        var col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            Debug.LogWarning($"[DeclencheurDialogue] {name} : le " +
                "collider doit etre en mode 'Is Trigger'. Activation " +
                "automatique.");
            col.isTrigger = true;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(tagJoueur)) return;

        if (dialogueCible == null)
        {
            Debug.LogWarning($"[DeclencheurDialogue] {name} : aucun " +
                "DialogueTuto assigne.");
            return;
        }

        if (declenchementUnique && dejaDeclenche)
        {
            Debug.Log($"[DeclencheurDialogue] {name} : deja declenche, " +
                "ignore (declenchementUnique=true).");
            return;
        }

        // Cas 1 : le dialogue est deja en cours et en pause distance
        // -> on reprend (le joueur est revenu apres s'etre eloigne).
        if (dialogueCible.DialogueEnCours
            && dialogueCible.EstEnPauseDistance)
        {
            Debug.Log($"[DeclencheurDialogue] {name} : reprise du " +
                "dialogue (etait en pause distance).");
            dialogueCible.SortiePauseDistance();
            return;
        }

        // Cas 2 : le dialogue n'est pas en cours -> on le demarre.
        if (!dialogueCible.DialogueEnCours)
        {
            Debug.Log($"[DeclencheurDialogue] {name} : demarrage du " +
                "dialogue par proximite.");
            dialogueCible.DemarrerAuto();
            dejaDeclenche = true;
            return;
        }

        // Cas 3 : le dialogue est en cours mais pas en pause distance
        // (le joueur est juste passe par la zone). On ignore.
        Debug.Log($"[DeclencheurDialogue] {name} : dialogue deja en " +
            "cours, pas de pause distance, ignore.");
    }
}
