// ============================================================
// conditionIndicesEnigme.cs
// ------------------------------------------------------------
// Auteur      : Fanny Fortier
// Date créée  : 2026-05-23
// ------------------------------------------------------------
// Description :
//   Surveille le nombre d'entrées dans le journal. Quand le
//   joueur a ramassé au moins N indices (configurable), l'énigme
//   se déverrouille :
//     1. Déclenche une bannière de chapitre via gestionChapitres
//     2. Active le particle system / détecteur devant la porte
//
//   À placer dans scene1_taverne1 et scene3_taverne2.
//   Le déclencheur de porte (declencheurChangementScene) lit
//   la propriété EnigmeDeverrouillee de ce script.
// ============================================================

using UnityEngine;

public class conditionIndicesEnigme : MonoBehaviour
{
    [Header("Condition")]
    [Tooltip("Nombre d'indices nécessaires pour déverrouiller la porte.")]
    [SerializeField] private int _nombreIndicesRequis = 4;

    [Header("Porte — déclencheur visuel")]
    [Tooltip("Particle system et/ou collider trigger devant la porte. " +
             "Désactivé par défaut, activé quand l'énigme est résolue.")]
    [SerializeField] private GameObject _detecteurPorte;

    [Header("Bannière de chapitre")]
    [Tooltip("ID du chapitre à annoncer quand l'énigme se déverrouille " +
             "(ex: 'enigme_resolue'). Laisse vide pour ne pas afficher " +
             "de bannière.")]
    [SerializeField] private string _idChapitreBanniere = "enigme_resolue";

    [Header("Action à signaler")]
    [Tooltip("(Optionnel) ID d'action signalée à gestionChapitres au " +
             "moment du déverrouillage. Permet à d'autres systèmes " +
             "(bandeau info, activation pointeur, etc.) de réagir " +
             "sans coupler la logique. Convention: 'tous_indices_ramasses'.")]
    [SerializeField] private string _idActionASignaler = "tous_indices_ramasses";

    // ── État ─────────────────────────────────────────────────

    /// <summary>
    /// True quand le joueur a ramassé assez d'indices.
    /// Lue par declencheurChangementScene pour autoriser le passage.
    /// </summary>
    public bool EnigmeDeverrouillee { get; private set; } = false;

    private bool _banniereAffichee = false;

    // ── Unity ────────────────────────────────────────────────

    void Start()
    {
        // S'assurer que la porte est invisible au départ
        if (_detecteurPorte != null)
            _detecteurPorte.SetActive(false);

        // Vérification initiale (si le joueur arrive avec des
        // indices déjà ramassés dans une scène précédente)
        VerifierCondition();
    }

    void Update()
    {
        // Polling léger : on vérifie seulement si pas encore déverrouillé
        if (!EnigmeDeverrouillee)
            VerifierCondition();
    }

    // ── Logique ──────────────────────────────────────────────

    private void VerifierCondition()
    {
        if (JournalManager.Instance == null) return;

        int nbIndices = JournalManager.Instance.entrees.Count;

        if (nbIndices >= _nombreIndicesRequis)
            DeverrouillerEnigme();
    }

    private void DeverrouillerEnigme()
    {
        if (EnigmeDeverrouillee) return;

        EnigmeDeverrouillee = true;

        Debug.Log("[ConditionIndices] Énigme déverrouillée ! " +
                  $"({JournalManager.Instance.entrees.Count} indices)");

        // 1. Activer le détecteur de porte
        if (_detecteurPorte != null)
            _detecteurPorte.SetActive(true);

        // 2. Déclencher la bannière de chapitre
        if (!_banniereAffichee
            && !string.IsNullOrEmpty(_idChapitreBanniere)
            && gestionChapitres.Instance != null)
        {
            _banniereAffichee = true;
            gestionChapitres.Instance.DemarrerChapitre(
                _idChapitreBanniere);
        }

        // 3. Signaler l'action générique pour les bandeaux/pointeurs
        //    qui doivent réagir au déverrouillage (ex : bandeau
        //    "Trouve la porte usine" + activation prefab_pointeur_porte).
        if (!string.IsNullOrEmpty(_idActionASignaler)
            && gestionChapitres.Instance != null)
        {
            Debug.Log($"[ConditionIndices] Signal action: " +
                      $"'{_idActionASignaler}'.");
            gestionChapitres.Instance.SignalerAction(_idActionASignaler);
        }
    }
}
