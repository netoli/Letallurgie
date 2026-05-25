// ============================================================
// GestionnaireEnigmeBalance.cs
// ------------------------------------------------------------
// Auteur      : Fanny Fortier
// Date cr��e  : 13 mai 2026
// ------------------------------------------------------------
// Description :
//   Chef d'orchestre de l'�nigme. �coute ControleurBalance,
//   g�re la progression des phases et d�clenche les r�actions
//   de l'antagoniste au bon moment.
// ------------------------------------------------------------
// D�pendances :
//   - controleurBalance       : �v�nement OnEquilibre
//   - comportementAntagoniste : ExecuterAction, OnActionTerminee
//   - ZoneDepotJoueur         : ViderSansNotifier (reset entre phases)
// ============================================================

using System;
using System.Collections;
using UnityEngine;

public class gestionnaireEnigmeBalance : MonoBehaviour
{
    // ===================== INSPECTEUR =====================
    [Header("R�f�rences")]
    [SerializeField] private controleurBalance _controleurBalance;
    [SerializeField] private comportementAntagoniste _antagoniste;

    [Header("Chapitres (bannières d'étape)")]
    [Tooltip("idChapitre du ScriptableObject DonneesChapitre à afficher après la réaction de Phase 1 (équilibre 1/3).")]
    [SerializeField] private string _idChapitreEtape2 = "etape_manoir_2";
    [Tooltip("idChapitre du ScriptableObject DonneesChapitre à afficher après la réaction de Phase 2 (équilibre 2/3).")]
    [SerializeField] private string _idChapitreEtape3 = "etape_manoir_3";

    // ===================== �TAT INTERNE =====================
    public enum PhaseEnigme { Phase1, Phase2, Phase3, Terminee }
    private PhaseEnigme _phaseActuelle = PhaseEnigme.Phase1;
    private bool _enAttente = false;

    // ===================== �V�NEMENTS =====================
    public event Action OnEnigmeTerminee;

    // ===================== UNITY =====================

    void Start()
    {
        _controleurBalance.OnEquilibre += GererEquilibre;
        _antagoniste.OnActionTerminee += LibererAttente;

        Debug.Log("[GestionnaireEnigme] �nigme d�marr�e � Phase 1");
    }

    void OnDestroy()
    {
        _controleurBalance.OnEquilibre -= GererEquilibre;
        _antagoniste.OnActionTerminee -= LibererAttente;
    }

    // ===================== M�THODES PRIV�ES =====================

    private void GererEquilibre()
    {
        if (_enAttente || _phaseActuelle == PhaseEnigme.Terminee) return;
        _enAttente = true;

        Debug.Log($"[GestionnaireEnigme] Équilibre détecté — {_phaseActuelle}");

        switch (_phaseActuelle)
        {
            case PhaseEnigme.Phase1:
                _phaseActuelle = PhaseEnigme.Phase2;
                StartCoroutine(SequenceReactionPhase(PhaseEnigme.Phase1, _idChapitreEtape2));
                break;

            case PhaseEnigme.Phase2:
                _phaseActuelle = PhaseEnigme.Phase3;
                StartCoroutine(SequenceReactionPhase(PhaseEnigme.Phase2, _idChapitreEtape3));
                break;

            case PhaseEnigme.Phase3:
                _phaseActuelle = PhaseEnigme.Terminee;
                // OnActionTerminee à la fin de JouerDefaiteSequence → LibererAttente()
                // → détecte Terminee → DeclencherVictoire()
                _antagoniste.JouerDefaite();
                break;
        }
    }

    /// <summary>
    /// Coroutine de réaction de phase (Phase1 ou Phase2) :
    ///   - Lance DemarrerReactionPhase sur l'antagoniste
    ///   - Attend ReactionPhaseTerminee (flag posé par comportementAntagoniste)
    ///   - Affiche le bandeau Étape X/3
    ///   - Libère _enAttente pour que le joueur puisse rejouer
    /// </summary>
    private IEnumerator SequenceReactionPhase(PhaseEnigme phaseReaction, string idChapitre)
    {
        _antagoniste.DemarrerReactionPhase(phaseReaction);
        yield return new WaitUntil(() => _antagoniste.ReactionPhaseTerminee);

        gestionChapitres.Instance?.DemarrerChapitre(idChapitre);
        Debug.Log($"[GestionnaireEnigme] Bannière chapitre '{idChapitre}' déclenchée.");

        _enAttente = false;
        Debug.Log($"[GestionnaireEnigme] Séquence {phaseReaction} terminée — " +
                  $"joueur peut agir ({_phaseActuelle})");
    }

    private void LibererAttente()
    {
        _enAttente = false;
        Debug.Log($"[GestionnaireEnigme] Antagoniste terminé — joueur peut agir ({_phaseActuelle})");

        // Appelé par OnActionTerminee à la fin de JouerDefaiteSequence.
        // Si on est en phase Terminee, déclencher la victoire.
        if (_phaseActuelle == PhaseEnigme.Terminee)
            DeclencherVictoire();
    }

    private void DeclencherVictoire()
    {
        Debug.Log("[GestionnaireEnigme] VICTOIRE � �nigme r�solue!");
        OnEnigmeTerminee?.Invoke();
    }
}