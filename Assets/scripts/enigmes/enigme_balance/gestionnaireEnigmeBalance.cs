// ============================================================
// GestionnaireEnigmeBalance.cs
// ------------------------------------------------------------
// Auteur      : Fanny Fortier
// Date créée  : 13 mai 2026
// ------------------------------------------------------------
// Description :
//   Chef d'orchestre de l'énigme. Écoute ControleurBalance,
//   gère la progression des phases et déclenche les réactions
//   de l'antagoniste au bon moment.
// ------------------------------------------------------------
// Dépendances :
//   - controleurBalance       : événement OnEquilibre
//   - comportementAntagoniste : ExecuterAction, OnActionTerminee
//   - ZoneDepotJoueur         : ViderSansNotifier (reset entre phases)
// ============================================================

using System;
using UnityEngine;

public class gestionnaireEnigmeBalance : MonoBehaviour
{
    // ===================== INSPECTEUR =====================
    [Header("Références")]
    [SerializeField] private controleurBalance _controleurBalance;
    [SerializeField] private comportementAntagoniste _antagoniste;

    // ===================== ÉTAT INTERNE =====================
    public enum PhaseEnigme { Phase1, Phase2, Phase3, Terminee }
    private PhaseEnigme _phaseActuelle = PhaseEnigme.Phase1;
    private bool _enAttente = false;

    // ===================== ÉVÉNEMENTS =====================
    public event Action OnEnigmeTerminee;

    // ===================== UNITY =====================

    void Start()
    {
        _controleurBalance.OnEquilibre += GererEquilibre;
        _antagoniste.OnActionTerminee += LibererAttente;

        Debug.Log("[GestionnaireEnigme] Énigme démarrée — Phase 1");
    }

    void OnDestroy()
    {
        _controleurBalance.OnEquilibre -= GererEquilibre;
        _antagoniste.OnActionTerminee -= LibererAttente;
    }

    // ===================== MÉTHODES PRIVÉES =====================

    private void GererEquilibre()
    {
        if (_enAttente || _phaseActuelle == PhaseEnigme.Terminee) return;
        _enAttente = true;

        Debug.Log($"[GestionnaireEnigme] Équilibre détecté — {_phaseActuelle}");

        switch (_phaseActuelle)
        {
            case PhaseEnigme.Phase1:
                _phaseActuelle = PhaseEnigme.Phase2;
                _antagoniste.ExecuterAction(PhaseEnigme.Phase1);
                break;

            case PhaseEnigme.Phase2:
                _phaseActuelle = PhaseEnigme.Phase3;
                _antagoniste.ExecuterAction(PhaseEnigme.Phase2);
                break;

            case PhaseEnigme.Phase3:
                _phaseActuelle = PhaseEnigme.Terminee;
                DeclencherVictoire();
                break;
        }
    }

    private void LibererAttente()
    {
        _enAttente = false;
        Debug.Log($"[GestionnaireEnigme] Antagoniste terminé " +
                  $"— joueur peut agir ({_phaseActuelle})");
    }

    private void DeclencherVictoire()
    {
        Debug.Log("[GestionnaireEnigme] VICTOIRE — énigme résolue!");
        OnEnigmeTerminee?.Invoke();
    }
}