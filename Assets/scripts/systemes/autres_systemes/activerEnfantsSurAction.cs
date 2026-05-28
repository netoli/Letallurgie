// ============================================================
// activerEnfantsSurAction.cs
// ------------------------------------------------------------
// Active TOUS les enfants directs d'un GameObject quand une
// idAction est signalee via gestionChapitres.SignalerAction.
//
// Difference avec gestionActivationAction (qui n'active qu'UN
// GameObject) : ce script active CHAQUE enfant direct
// individuellement. Necessaire quand les enfants ont leur
// propre m_IsActive: 0 — dans ce cas, activer le parent ne
// rend pas les enfants visibles (Unity respecte le state
// individuel des enfants).
//
// CAS D'USAGE TYPIQUE — scene2_usine :
// - tuyaux_a_placer (parent, actif au demarrage) porte ce
//   script avec idActionActivation = "joueur_a_atteint_enigme".
// - Ses 9 enfants (tuyau_droit_petit, etc.) sont desactives
//   au demarrage.
// - Au contact du pointeur_enigme → joueur_a_atteint_enigme
//   signalee → ce script active les 9 enfants apres delai.
//
// SETUP UNITY :
// 1. Ajouter ce composant sur le PARENT actif (ex :
//    tuyaux_a_placer).
// 2. Inspector : renseigner idActionActivation, delai.
// 3. Tous les enfants directs seront SetActive(true) au signal.
// ============================================================

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class activerEnfantsSurAction : MonoBehaviour
{
    [Header("Declenchement")]
    [Tooltip("idAction qui declenche l'activation des enfants. " +
        "Doit etre signalee via gestionChapitres.SignalerAction. " +
        "Ex : 'joueur_a_atteint_enigme'.")]
    [SerializeField] private string idActionActivation;

    [Tooltip("Delai (s) entre la reception de l'action et " +
        "l'activation des enfants. Defaut : 0.")]
    [SerializeField] private float delaiAvantActivation = 0f;

    [Header("Options")]
    [Tooltip("Si coche, active aussi recursivement les petits-enfants " +
        "(toute la hierarchie sous ce GameObject). Sinon, uniquement " +
        "les enfants directs. Defaut : non.")]
    [SerializeField] private bool inclureDescendants = false;

    [Tooltip("Si coche, le composant peut etre redeclenche plusieurs " +
        "fois. Sinon une seule fois par session.")]
    [SerializeField] private bool autoReset = false;

    [Tooltip("Si coche, log les etapes dans la console (debug).")]
    [SerializeField] private bool debug = true;

    private bool dejaDeclenche = false;
    private bool estInscrit = false;

    // Auto-inscription apres chaque chargement de scene, meme
    // pattern que gestionActivationAction — robuste face au timing.
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
        var tous = Object.FindObjectsByType<activerEnfantsSurAction>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var inst in tous) inst.SInscrire();
    }

    private void SInscrire()
    {
        if (estInscrit) return;
        if (string.IsNullOrEmpty(idActionActivation)) return;
        if (gestionChapitres.Instance == null) return;
        gestionChapitres.Instance.OnActionSignalee += AuActionSignalee;
        estInscrit = true;
        Log($"Inscrit a OnActionSignalee, ecoute '{idActionActivation}'.");
    }

    void OnEnable() { SInscrire(); }
    void Start()    { SInscrire(); }

    void OnDestroy()
    {
        if (gestionChapitres.Instance != null && estInscrit)
            gestionChapitres.Instance.OnActionSignalee -= AuActionSignalee;
        estInscrit = false;
    }

    private void AuActionSignalee(string idAction)
    {
        if (idAction != idActionActivation) return;
        if (dejaDeclenche && !autoReset) return;
        dejaDeclenche = true;
        Log($"Action '{idAction}' recue, activation des enfants dans " +
            $"{delaiAvantActivation}s.");
        if (delaiAvantActivation > 0f)
            StartCoroutine(ActiverApresDelai());
        else
            ActiverMaintenant();
    }

    private IEnumerator ActiverApresDelai()
    {
        yield return new WaitForSecondsRealtime(delaiAvantActivation);
        ActiverMaintenant();
    }

    private void ActiverMaintenant()
    {
        int n = 0;
        if (inclureDescendants)
        {
            // Toute la hierarchie sous ce GameObject (recursif).
            var tous = GetComponentsInChildren<Transform>(true);
            foreach (var t in tous)
            {
                if (t.gameObject == gameObject) continue;
                if (!t.gameObject.activeSelf)
                {
                    t.gameObject.SetActive(true);
                    n++;
                }
            }
        }
        else
        {
            // Uniquement les enfants directs.
            foreach (Transform enfant in transform)
            {
                if (!enfant.gameObject.activeSelf)
                {
                    enfant.gameObject.SetActive(true);
                    n++;
                }
            }
        }
        Log($"{n} enfant(s) active(s).");
    }

    public void Reinitialiser() { dejaDeclenche = false; }

    private void Log(string msg)
    {
        if (debug) Debug.Log($"[ActiverEnfants:{name}] {msg}");
    }
}
