// ============================================================
// chaineActionsAvecDelai.cs
// ------------------------------------------------------------
// Script générique qui écoute une idAction signalée et, après un
// délai, fait l'une (ou plusieurs) de ces choses :
//   - Démarre un chapitre (bannière)
//   - Signale une autre idAction
//   - Active ou désactive un GameObject cible
//
// USAGE TYPIQUE pour la séquence narrative énigme tuyauterie :
//
//   1. Contact tavernier (signale 'tavernier_trouve')
//      → chaineActionsAvecDelai écoute 'tavernier_trouve'
//      → délai 3s
//      → démarre chapitre 'enigme_tuyauterie' (bannière)
//
//   2. Fin bannière (signale 'chapitre_enigme_tuyauterie_banniere_terminee')
//      → chaineActionsAvecDelai écoute cette action
//      → délai 3s
//      → active GameObject pointeur_enigme
//
//   3. Contact pointeur_enigme (signale 'joueur_a_touche_pointeur_enigme')
//      → chaineActionsAvecDelai écoute cette action
//      → délai 1s
//      → active GameObject tuyaux_a_placer
//
//   4. Victoire énigme : sur gestionEnigmeTuyauterie.onVictoire ajouter
//      un appel à gestionChapitres.SignalerAction('enigme_tuyauterie_reussie')
//      → chaineActionsAvecDelai écoute 'enigme_tuyauterie_reussie'
//      → délai 3s
//      → active GameObject pointeur_porte_sortie
//
// SETUP UNITY :
//   1. Sur un GameObject de la scène, Add Component →
//      chaineActionsAvecDelai
//   2. Inspector : configurer idActionEcoutee, délai, et au moins une
//      action de sortie (signaler, démarrer chapitre, activer GO)
//   3. Tester
// ============================================================
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class chaineActionsAvecDelai : MonoBehaviour
{
    [Header("Entrée : action à écouter")]
    [Tooltip("idAction signalée à gestionChapitres qu'on écoute pour " +
        "déclencher la chaîne. Ex : 'tavernier_trouve'.")]
    [SerializeField] private string idActionEcoutee;

    [Tooltip("Délai (s) entre la réception de l'action et l'exécution " +
        "des actions de sortie. Defaut : 0.")]
    [SerializeField] private float delaiAvantSuite = 0f;

    [Header("Sortie : que faire après le délai (1 ou +)")]
    [Tooltip("(Optionnel) Si rempli, démarre ce chapitre via " +
        "gestionChapitres.DemarrerChapitre(...). Utile pour déclencher " +
        "une bannière 'Énigme tuyauterie'.")]
    [SerializeField] private string idChapitreADemarrer;

    [Tooltip("(Optionnel) Si rempli, signale cette idAction via " +
        "gestionChapitres.SignalerAction(...). Permet de chaîner avec " +
        "d'autres chaineActionsAvecDelai, gestionActivationAction, ou " +
        "DonneesBandeauInfo.")]
    [SerializeField] private string idActionASignaler;

    [Tooltip("(Optionnel) GameObject à activer (SetActive true) après " +
        "le délai. Ex : pointeur_enigme, tuyaux_a_placer.")]
    [SerializeField] private GameObject objetAActiver;

    [Tooltip("(Optionnel) GameObject à désactiver (SetActive false) " +
        "après le délai. Ex : pour cacher la tuile explicative.")]
    [SerializeField] private GameObject objetADesactiver;

    [Header("Options")]
    [Tooltip("Si coché, la chaîne peut se déclencher plusieurs fois. " +
        "Sinon, une seule fois par session. Defaut : non.")]
    [SerializeField] private bool autoReset = false;

    [Tooltip("Si coché, log les étapes dans la console (debug).")]
    [SerializeField] private bool debug = true;

    private bool dejaDeclenche = false;
    private bool estInscrit = false;

    // Auto-inscription apres chaque chargement de scene (meme pattern
    // que gestionActivationAction). Robuste face au timing :
    // gestionChapitres.Instance peut etre null au OnEnable de cette
    // instance — on retente via RuntimeInitializeOnLoadMethod qui
    // s'execute APRES que toutes les scenes/singletons soient prets.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InitGlobalHook()
    {
        SceneManager.sceneLoaded -= OnSceneChargee;
        SceneManager.sceneLoaded += OnSceneChargee;
        InscrireToutes();
    }

    private static void OnSceneChargee(Scene s, LoadSceneMode m)
    {
        InscrireToutes();
    }

    public static void InscrireToutes()
    {
        var tous = Object.FindObjectsByType<chaineActionsAvecDelai>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var inst in tous) inst.SInscrire();
    }

    private void SInscrire()
    {
        if (estInscrit) return;
        if (string.IsNullOrEmpty(idActionEcoutee)) return;
        if (gestionChapitres.Instance == null) return;
        gestionChapitres.Instance.OnActionSignalee += SurActionSignalee;
        estInscrit = true;
        Log($"Inscrit a OnActionSignalee, ecoute '{idActionEcoutee}'.");
    }

    void OnEnable()
    {
        SInscrire();
    }

    void Start()
    {
        // Garde-fou : si AfterSceneLoad n'a pas inscrit (cas rare ou
        // singleton tardif), on retente ici.
        SInscrire();
    }

    void OnDestroy()
    {
        if (gestionChapitres.Instance != null && estInscrit)
            gestionChapitres.Instance.OnActionSignalee -= SurActionSignalee;
        estInscrit = false;
    }

    private void SurActionSignalee(string idAction)
    {
        if (idAction != idActionEcoutee) return;
        if (dejaDeclenche && !autoReset) return;
        Log($"Action '{idAction}' recue, declenchement dans " +
            $"{delaiAvantSuite}s.");
        StartCoroutine(LancerApresDelai());
    }

    private IEnumerator LancerApresDelai()
    {
        dejaDeclenche = true;
        if (delaiAvantSuite > 0f)
            yield return new WaitForSeconds(delaiAvantSuite);

        // 1. Démarrer un chapitre si renseigné
        if (!string.IsNullOrEmpty(idChapitreADemarrer)
            && gestionChapitres.Instance != null)
        {
            gestionChapitres.Instance.DemarrerChapitre(idChapitreADemarrer);
            Log($"Chapitre démarré : '{idChapitreADemarrer}'.");
        }

        // 2. Signaler une autre action si renseigné
        if (!string.IsNullOrEmpty(idActionASignaler)
            && gestionChapitres.Instance != null)
        {
            gestionChapitres.Instance.SignalerAction(idActionASignaler);
            Log($"Action signalée : '{idActionASignaler}'.");
        }

        // 3. Activer un GameObject
        if (objetAActiver != null)
        {
            objetAActiver.SetActive(true);
            Log($"GameObject activé : {objetAActiver.name}.");
        }

        // 4. Désactiver un GameObject
        if (objetADesactiver != null)
        {
            objetADesactiver.SetActive(false);
            Log($"GameObject désactivé : {objetADesactiver.name}.");
        }
    }

    private void Log(string msg)
    {
        if (debug) Debug.Log($"[chaineActions:{name}] {msg}");
    }
}
