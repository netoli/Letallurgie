// ============================================================
// ControleurBalance.cs
// ------------------------------------------------------------
// Auteur      : Fanny Fortier
// Date cr��e  : 8 mai 2026
// ------------------------------------------------------------
// Description :
//   Re�oit les poids des deux c�t�s de la balance et calcule
//   l'�tat (-1 penche gauche, 0 �quilibre, 1 penche droite).
//   D�clenche OnEquilibre quand les deux c�t�s sont �gaux.
//   Accroch� sur l'objet vide gestion_enigme_balance
// ------------------------------------------------------------
// D�pendances :
//   - ZoneDepotJoueur, ZoneDepotAntagoniste : appellent MettreAJourPoids
//   - GestionnaireEnigmeBalance : �coute OnEquilibre
// ============================================================

using System;
using UnityEngine;

public class controleurBalance : MonoBehaviour
{
    // ===================== INSPECTEUR =====================

    [Header("Visuel balance")]
    [Tooltip("Script balance_apparence sur le modèle 3D de la balance. " +
             "Reçoit un float [-1 ; 1] proportionnel à l'écart de poids.")]
    [SerializeField] private balance_apparence _apparenceBalance;

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
                  $"Droite={_poidsDroit} — État={etat}");

        AppliquerVisuBalance();

        // N'invoquer OnEquilibre que si les deux côtés ont du poids :
        // 0 == 0 (balance vide) ne compte pas comme un équilibre.
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

    private void AppliquerVisuBalance()
    {
        if (_apparenceBalance == null) return;

        int total = _poidsGauche + _poidsDroit;

        // Si les deux côtés sont vides, on centre la balance.
        if (total == 0)
        {
            _apparenceBalance.DefinirRotation(0f);
            return;
        }

        // Ratio proportionnel à l'écart de poids :
        //   droite > gauche → positif (penche droite)
        //   gauche > droite → négatif (penche gauche)
        //   égal            → 0 (horizontal)
        // Ex : gauche=1 droite=3 → (3-1)/(3+1) = 0.5 (légèrement penché)
        //      gauche=0 droite=5 → (5-0)/(5+0) = 1.0 (complètement penché)
        float ratio = (_poidsDroit - _poidsGauche) / (float)total;
        _apparenceBalance.DefinirRotation(ratio);
    }
}