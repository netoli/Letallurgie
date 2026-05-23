// ============================================================
// ControleurBalance.cs
// ------------------------------------------------------------
// Auteur      : Fanny Fortier
// Date créée  : 8 mai 2026
// ------------------------------------------------------------
// Description :
//   Reçoit les poids des deux côtés de la balance et calcule
//   l'état (-1 penche gauche, 0 équilibre, 1 penche droite).
//   Déclenche OnEquilibre quand les deux côtés sont égaux.
//   Accroché sur l'objet vide gestion_enigme_balance
// ------------------------------------------------------------
// Dépendances :
//   - ZoneDepotJoueur, ZoneDepotAntagoniste : appellent MettreAJourPoids
//   - GestionnaireEnigmeBalance : écoute OnEquilibre
// ============================================================

using System;
using UnityEngine;

public class controleurBalance : MonoBehaviour
{
    // ===================== INSPECTEUR =====================
    [Header("Animation")]
    [SerializeField] private Animator _animateurBalance;

    // ===================== ÉVÉNEMENTS =====================
    public event Action OnEquilibre;

    // ===================== ÉTAT INTERNE =====================
    private int _poidsGauche = 0;
    private int _poidsDroit = 0;

    // ===================== MÉTHODES PUBLIQUES =====================

    /// <summary>
    /// Appelé par ZoneDepotJoueur et ZoneDepotAntagoniste
    /// à chaque changement de poids.
    /// </summary>
    public void MettreAJourPoids(int poidsGauche, int poidsDroit)
    {
        _poidsGauche = poidsGauche;
        _poidsDroit = poidsDroit;

        int etat = CalculerEtat();

        Debug.Log($"[ControleurBalance] Gauche={_poidsGauche} " +
                  $"Droite={_poidsDroit} - état={etat}");

        AppliquerAnimationBalance(etat);

        // N'invoquer OnEquilibre que si les deux cÃ´tÃ©s ont du poids :
        // 0 == 0 (balance vide) ne compte pas comme un Ã©quilibre.
        if (etat == 0 && _poidsGauche > 0)
            OnEquilibre?.Invoke();
    }

    // ===================== MÉTHODES PRIVÉES =====================

    private int CalculerEtat()
    {
        if (_poidsGauche > _poidsDroit) return -1;
        if (_poidsGauche < _poidsDroit) return 1;
        return 0;
    }

    private void AppliquerAnimationBalance(int etat)
    {
        if (_animateurBalance == null) return;
        _animateurBalance.SetInteger("EtatBalance", etat);
    }
}