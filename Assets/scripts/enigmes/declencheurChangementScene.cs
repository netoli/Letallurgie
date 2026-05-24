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

public class declencheurChangementScene : MonoBehaviour
{
    [Header("Navigation")]
    [Tooltip("Nom exact de la scène à charger (ex: 'SCENE2-Usine').")]
    [SerializeField] private string _nomSceneCible;

    [Header("Condition")]
    [Tooltip("Référence au script conditionIndicesEnigme de la scène. " +
             "Si null, la porte est toujours ouverte (pour tests).")]
    [SerializeField] private conditionIndicesEnigme _condition;

    [Header("Feedback joueur")]
    [Tooltip("Message affiché en console si le joueur entre avant " +
             "d'avoir assez d'indices (optionnel, pour debug).")]
    [SerializeField] private bool _debugPorteVerrouillee = true;

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

        Debug.Log($"[DeclencheurPorte] Chargement de '{_nomSceneCible}'");

        // Passer par l'écran de chargement si disponible,
        // sinon charger directement (fallback)
        if (gestionEcranChargement.Instance != null)
            gestionEcranChargement.Instance.ChargerScene(_nomSceneCible);
        else
            UnityEngine.SceneManagement.SceneManager
                .LoadScene(_nomSceneCible);
    }
}
