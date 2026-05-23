using UnityEngine;

/// <summary>
/// Active automatiquement un GameObject (SetActive true) lorsqu'une
/// action specifique est signalee via gestionChapitres.SignalerAction.
/// Miroir de gestionDisparitionAction (qui desactive sur signal).
/// Generique : reutilisable pour faire APPARAITRE tout indicateur,
/// pointeur visuel, helper UI, etc. au bon moment narratif.
///
/// EXEMPLE D'USAGE :
/// - prefab_pointeur_personnage devant la table du npc, desactive par
///   defaut. On ajoute ce script avec idActionActivation =
///   "indice_voir_npc_visible" (signalee a l'apparition du bandeau
///   "Va voir le client"). Le pointeur apparait en meme temps que le
///   bandeau, guidant le joueur vers le npc.
///
/// SETUP UNITY :
/// 1. Selectionner le GameObject a faire apparaitre (ex :
///    prefab_pointeur_personnage).
/// 2. IMPORTANT : DECOCHER la case du GameObject dans l'Inspector pour
///    qu'il soit invisible au demarrage (sera active par le script).
/// 3. Add Component → gestionActivationAction.
/// 4. Inspector : renseigner idActionActivation avec l'idAction qui doit
///    declencher l'apparition.
///
/// SOURCES TYPIQUES DE L'ACTION :
/// - idActionAApparition d'un DonneesBandeauInfo (synchronise avec le
///   bandeau qui apparait).
/// - idActionApresBanniere d'un DonneesTutoriel (bannière tuile).
/// - "chapitre_<idChapitre>_banniere_terminee" (fin de la banniere
///   annonce-chapitre, signalee automatiquement par gestionChapitres).
/// - Un signal manuel depuis un autre script.
///
/// LIMITES :
/// - Le GameObject doit etre desactive au demarrage (sinon il sera
///   visible immediatement, ce qui defait l'interet). Le script lui-meme
///   doit etre sur ce GameObject : Awake ne tournera donc pas tant que
///   le GameObject est inactif. Pour contourner ca, on a 2 options :
///     a) Mettre ce script sur un GameObject PARENT actif, et configurer
///        un champ "objetAActiver" qui pointe sur l'enfant inactif.
///     b) Mettre le GameObject lui-meme actif mais cacher visuellement
///        (alpha 0, scale 0). Plus complexe.
///   Cette version implemente l'option (a).
/// </summary>
public class gestionActivationAction : MonoBehaviour
{
    [Header("Declenchement")]
    [Tooltip("ID d'action qui declenche l'activation de l'objet cible. " +
        "Doit correspondre a une action signalee via gestionChapitres." +
        "SignalerAction. Ex : 'indice_voir_npc_visible'.")]
    [SerializeField] private string idActionActivation;

    [Header("Cible (optionnel)")]
    [Tooltip("GameObject a activer. Si vide, c'est ce GameObject lui-meme " +
        "qui sera active — MAIS attention : si CE GameObject est " +
        "desactive au demarrage, le script ne tournera pas (Awake/Start " +
        "ne sont pas appeles sur un GameObject inactif). Recommande : " +
        "placer ce script sur un GameObject parent actif, et glisser " +
        "l'enfant a activer dans ce champ.")]
    [SerializeField] private GameObject objetAActiver;

    private bool activationDeclenchee = false;

    void Start()
    {
        if (string.IsNullOrEmpty(idActionActivation))
        {
            Debug.LogWarning($"[ActivationAction] {name} : " +
                "idActionActivation n'est pas renseigne, le script ne " +
                "fera rien. Renseigne-le dans l'Inspector.");
            return;
        }

        if (gestionChapitres.Instance == null)
        {
            Debug.LogWarning($"[ActivationAction] {name} : " +
                "gestionChapitres.Instance introuvable au Start. " +
                "Le script ne pourra pas ecouter les actions.");
            return;
        }

        gestionChapitres.Instance.OnActionSignalee += AuActionSignalee;
    }

    void OnDestroy()
    {
        if (gestionChapitres.Instance != null)
            gestionChapitres.Instance.OnActionSignalee -= AuActionSignalee;
    }

    private void AuActionSignalee(string idAction)
    {
        if (activationDeclenchee) return;
        if (idAction != idActionActivation) return;

        activationDeclenchee = true;

        GameObject cible = objetAActiver != null ? objetAActiver : gameObject;
        Debug.Log($"[ActivationAction] {name} : action " +
            $"'{idAction}' signalee, activation de '{cible.name}'.");
        cible.SetActive(true);
    }
}
