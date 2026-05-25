// ============================================================
// signaleurZone.cs
// ------------------------------------------------------------
// Composant generique a poser sur un GameObject avec un Collider
// configure en Is Trigger. Quand le joueur entre dans la zone,
// signale une idAction via gestionChapitres.SignalerAction.
//
// Usage typique :
//   - Marquer qu'une piece a ete trouvee
//     (ex : "tavernier_prisonnier_trouve" dans scene2_usine)
//   - Declencher l'apparition d'un bandeau / pointeur quand le
//     joueur s'approche d'une zone d'enigme
//   - Confirmer que le joueur a atteint le pointeur de porte
//     pour declencher l'ecran de chargement vers scene2
//
// SETUP UNITY :
// 1. Creer un GameObject vide a l'emplacement souhaite
// 2. Ajouter un BoxCollider (ou SphereCollider) avec
//    "Is Trigger" coche
// 3. Ajuster la taille du collider a la zone voulue
// 4. Add Component → signaleurZone
// 5. Renseigner idActionASignaler
// 6. Verifier que le GameObject du joueur a bien le tag attendu
//    (ou laisser vide pour reagir a tout collider)
// ============================================================

using UnityEngine;

[RequireComponent(typeof(Collider))]
public class signaleurZone : MonoBehaviour
{
    [Header("Action a signaler")]
    [Tooltip("L'idAction qui sera signalee a gestionChapitres quand " +
        "le joueur entre dans la zone. Ex : 'tavernier_prisonnier_trouve'.")]
    [SerializeField] private string idActionASignaler;

    [Header("Filtrage")]
    [Tooltip("Si renseigne, seuls les colliders avec ce tag declencheront " +
        "le signal. Laisser vide pour reagir a tout collider. " +
        "Recommande : 'Player' ou le tag de ton joueur.")]
    [SerializeField] private string tagDuJoueur = "Player";

    [Header("Options")]
    [Tooltip("Si coche, le signal est envoye une seule fois et le " +
        "composant se desactive ensuite. Recommande pour les marqueurs " +
        "de progression narrative.")]
    [SerializeField] private bool uneSeuleFois = true;

    [Tooltip("Si coche, le GameObject entier est detruit apres le signal. " +
        "Utile pour nettoyer la scene quand la zone n'a plus d'utilite.")]
    [SerializeField] private bool detruireApres = false;

    [Tooltip("Si coche, logue chaque entree dans la console.")]
    [SerializeField] private bool debugLogs = true;

    private bool dejaSignale = false;

    void Reset()
    {
        // Assure que le collider est en mode trigger des l'ajout du
        // composant. Si l'utilisateur a deja un collider, on ne touche
        // pas ses autres parametres.
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    void OnTriggerEnter(Collider autre)
    {
        if (uneSeuleFois && dejaSignale) return;
        if (string.IsNullOrEmpty(idActionASignaler))
        {
            Debug.LogWarning($"[signaleurZone] {name} : " +
                "idActionASignaler vide. Aucun signal envoye.");
            return;
        }

        // Filtrage par tag si configure
        if (!string.IsNullOrEmpty(tagDuJoueur)
            && !autre.CompareTag(tagDuJoueur))
        {
            return;
        }

        if (gestionChapitres.Instance == null)
        {
            Debug.LogWarning($"[signaleurZone] {name} : " +
                "gestionChapitres.Instance introuvable, signal annule.");
            return;
        }

        if (debugLogs)
        {
            Debug.Log($"[signaleurZone] {name} : zone declenchee par " +
                $"'{autre.name}', signal '{idActionASignaler}'.");
        }

        dejaSignale = true;
        gestionChapitres.Instance.SignalerAction(idActionASignaler);

        if (detruireApres)
        {
            Destroy(gameObject);
        }
    }
}
