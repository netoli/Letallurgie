// ============================================================
// transitionSceneSurAction.cs
// ------------------------------------------------------------
// Charge une scène avec un fondu noir quand une idAction est
// signalée. Utilise transitionAvecFondu (déjà existant) pour la
// transition.
//
// USAGE TYPIQUE pour la fin de l'énigme tuyauterie :
//   1. Le pointeur_porte_sortie signale 'joueur_va_a_scene3' au
//      contact (via detecteurTuto).
//   2. Sur un GameObject "transition_scene3", on ajoute ce script :
//      - idActionEcoutee = 'joueur_va_a_scene3'
//      - delaiAvantTransition = 1
//      - nomScene = 'scene3_taverne2'
//   3. Quand le joueur touche le pointeur, après 1s, fondu noir
//      puis scene3 chargée.
//
// PRÉREQUIS :
//   - La scène cible doit être ajoutée dans File → Build Settings
//   - transitionAvecFondu doit exister dans le projet (déjà OK)
// ============================================================
using UnityEngine;

public class transitionSceneSurAction : MonoBehaviour
{
    [Header("Entrée : action à écouter")]
    [Tooltip("idAction signalée à gestionChapitres qu'on écoute. Ex : " +
        "'joueur_va_a_scene3'.")]
    [SerializeField] private string idActionEcoutee;

    [Header("Sortie : transition de scène")]
    [Tooltip("Nom EXACT de la scène à charger (sans .unity). Doit " +
        "être ajoutée dans Build Settings. Ex : 'scene3_taverne2'.")]
    [SerializeField] private string nomScene;

    [Tooltip("Délai (s) avant de lancer la transition (utile pour " +
        "laisser une animation/son finir). Defaut : 1s.")]
    [SerializeField] private float delaiAvantTransition = 1f;

    [Tooltip("Durée du fade-in (écran noir) avant le chargement. " +
        "Defaut : 0.5s.")]
    [SerializeField] private float dureeFadeIn = 0.5f;

    [Tooltip("Durée du fade-out (écran noir → scène visible) après " +
        "le chargement. Defaut : 0.5s.")]
    [SerializeField] private float dureeFadeOut = 0.5f;

    [Tooltip("Si coché, log dans la console (debug).")]
    [SerializeField] private bool debug = true;

    private bool dejaDeclenche = false;

    void OnEnable()
    {
        if (gestionChapitres.Instance != null)
            gestionChapitres.Instance.OnActionSignalee += SurActionSignalee;
    }

    void OnDisable()
    {
        if (gestionChapitres.Instance != null)
            gestionChapitres.Instance.OnActionSignalee -= SurActionSignalee;
    }

    private void SurActionSignalee(string idAction)
    {
        if (idAction != idActionEcoutee) return;
        if (dejaDeclenche) return;
        if (string.IsNullOrEmpty(nomScene))
        {
            Debug.LogWarning($"[transitionSceneSurAction:{name}] nomScene " +
                "vide, transition ignorée.");
            return;
        }

        dejaDeclenche = true;
        if (debug)
            Debug.Log($"[transitionSceneSurAction:{name}] Action '" +
                $"{idAction}' reçue. Transition vers '{nomScene}' dans " +
                $"{delaiAvantTransition}s.");

        transitionAvecFondu.ChargerSceneAvecFondu(
            nomScene,
            dureeFadeIn: dureeFadeIn,
            dureeFadeOut: dureeFadeOut,
            attenteApresChargement: 0.2f,
            delaiAvantTransition: delaiAvantTransition);
    }
}
