// ============================================================
// ZoneDepotAntagoniste.cs
// ------------------------------------------------------------
// Auteur      : Fanny Fortier
// Date cr��e  : 12 mai 2026
// ------------------------------------------------------------
// Description :
//   G�re l'objet unique d�pos� par l'antagoniste sur son c�t�
//   de la balance. Notifie ControleurBalance � chaque changement.
// ------------------------------------------------------------
// D�pendances :
//   - ControleurBalance : re�oit les poids
//   - ZoneDepotJoueur   : fournit le poids adverse
//   - ObjetPesable      : composant sur l'objet antagoniste
// ============================================================

using UnityEngine;

public class ZoneDepotAntagoniste : MonoBehaviour
{
    // ===================== INSPECTEUR =====================
    [Header("R�f�rences")]
    [SerializeField] private controleurBalance _controleurBalance;

    // ===================== �TAT INTERNE =====================
    private objetPesable _objetActuel;

    // ===================== M�THODES PUBLIQUES =====================

    /// <summary>
    /// Appel� au d�marrage de l'�nigme pour d�finir
    /// l'objet de l'antagoniste.
    /// </summary>
    public void DefinirObjet(objetPesable objet)
    {
        _objetActuel = objet;
        NotifierBalance();
    }

    /// <summary>
    /// Appel� par ComportementAntagoniste apr�s avoir
    /// modifi� le poids de son objet.
    /// </summary>
    public void NotifierChangementPoids()
    {
        NotifierBalance();
    }

    public int CalculerPoidsTotal()
    {
        return _objetActuel != null ? _objetActuel.valeurPoids : 0;
    }

    // ===================== M�THODES PRIV�ES =====================

    private void NotifierBalance()
    {
        // Met à jour uniquement le côté droit (antagoniste).
        // Le côté gauche (joueur) est géré indépendamment
        // par ZoneDepotJoueur — plus de dépendance croisée.
        _controleurBalance.MettreAJourPoidsDroit(CalculerPoidsTotal());
    }
}