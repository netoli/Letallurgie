// ============================================================
// declencheurChangementScene.cs
// ------------------------------------------------------------
// Auteur      : Fanny Fortier
// Date créée  : 2026-05-23
// ------------------------------------------------------------
// Description :
//   Trigger de zone qui charge la scène suivante quand le
//   joueur entre en contact, SEULEMENT si l'énigme est
//   déverrouillée (lu depuis conditionIndicesEnigme).
//
//   Setup Unity (sur le même GameObject que le particle system
//   de porte, activé par conditionIndicesEnigme) :
//     1. Ajouter un Collider (ex: Sphere/Box) avec Is Trigger ✓
//     2. Ajouter ce script
//     3. Assigner la scène cible et la référence à
//        conditionIndicesEnigme dans l'Inspector
//
//   Scènes cibles :
//     scene1_taverne1 → "scene2_usine"
//     scene3_taverne2 → "scene4_manoir"
// ============================================================

using UnityEngine;
using UnityEngine.Video;

public class declencheurChangementScene : MonoBehaviour
{
    [Header("Navigation")]
    [Tooltip("Nom exact de la scène à charger (ex: 'SCENE2-Usine').")]
    [SerializeField] private string _nomSceneCible;

    [Header("Écran de chargement (optionnel)")]
    [Tooltip("VideoClip à jouer en plein écran pendant le chargement " +
             "(ex: Letallurgie-loadingAnim.mp4). Si renseigné, la vidéo " +
             "remplace l'écran noir. Si vide, fallback gestionEcranChargement " +
             "ou cut direct.")]
    [SerializeField] private VideoClip _videoChargement;

    [Tooltip("Durée minimale d'affichage de la vidéo (s). La transition " +
             "attendra au moins ce temps avant de basculer dans la nouvelle " +
             "scène, même si elle a fini de charger plus tôt. Permet de " +
             "donner du temps au joueur pour voir l'animation.")]
    [SerializeField] private float _minDureeAffichageVideo = 2f;

    [Header("Condition")]
    [Tooltip("Référence au script conditionIndicesEnigme de la scène. " +
             "Si null, la porte est toujours ouverte (pour tests).")]
    [SerializeField] private conditionIndicesEnigme _condition;

    [Header("Feedback joueur")]
    [Tooltip("Message affiché en console si le joueur entre avant " +
             "d'avoir assez d'indices (optionnel, pour debug).")]
    [SerializeField] private bool _debugPorteVerrouillee = true;

    [Header("Délai")]
    [Tooltip("Délai (s) entre le contact joueur et le déclenchement de " +
             "l'écran de chargement. Permet de laisser respirer la " +
             "transition. 0 = chargement immédiat.")]
    [SerializeField] private float _delaiAvantChargement = 0f;

    private bool _transitionEnCours = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (_transitionEnCours) return;

        // Vérifier la condition si elle est assignée
        bool enigmeResolue = _condition == null
            || _condition.EnigmeDeverrouillee;

        if (!enigmeResolue)
        {
            if (_debugPorteVerrouillee)
                Debug.Log("[DeclencheurPorte] Porte verrouillée — " +
                          "pas assez d'indices dans le journal.");
            return;
        }

        if (string.IsNullOrEmpty(_nomSceneCible))
        {
            Debug.LogWarning("[DeclencheurPorte] Aucune scène cible " +
                             "configurée dans l'Inspector !");
            return;
        }

        _transitionEnCours = true;

        Debug.Log($"[DeclencheurPorte] Contact joueur. Chargement de " +
                  $"'{_nomSceneCible}' dans {_delaiAvantChargement}s.");

        // IMPORTANT : ne PAS utiliser StartCoroutine ici. Le GameObject
        // qui porte ce script peut etre desactive entre OnTriggerEnter
        // et le demarrage de la coroutine (ex: prefab_pointeur_porte
        // qui se desactive a son contact via gestionDisparitionAction).
        // On delegue le delai a transitionAvecFondu qui survit grace
        // a DontDestroyOnLoad.
        DeclencherChargement(_delaiAvantChargement);
    }

    private void DeclencherChargement(float delaiAvant)
    {
        // Priorité 1 : vidéo de chargement custom configurée dans l'Inspector
        // (utilise transitionAvecFondu qui se setup au runtime, AUCUN
        //  GameObject/Canvas à configurer dans la scène).
        if (_videoChargement != null)
        {
            transitionAvecFondu.ChargerSceneAvecVideo(
                _nomSceneCible,
                _videoChargement,
                _minDureeAffichageVideo,
                delaiAvantTransition: delaiAvant);
            return;
        }

        // Priorité 2 : système gestionEcranChargement complet (si configuré
        // dans la scène avec Canvas + RawImage + RenderTexture).
        if (gestionEcranChargement.Instance != null)
        {
            gestionEcranChargement.Instance.ChargerScene(_nomSceneCible);
            return;
        }

        // Priorité 3 : fondu noir simple via transitionAvecFondu (toujours
        // dispo, aucun setup requis).
        transitionAvecFondu.ChargerSceneAvecFondu(
            _nomSceneCible,
            delaiAvantTransition: delaiAvant);
    }
}
