// ============================================================
// ZoneDepotAntagoniste.cs
// ------------------------------------------------------------
// Auteur      : Fanny Fortier
// Date créée  : 12 mai 2026
// ------------------------------------------------------------
// Description :
//   Gère l'objet unique déposé par l'antagoniste sur son côté
//   de la balance. Notifie ControleurBalance à chaque changement.
// ------------------------------------------------------------
// Dépendances :
//   - ControleurBalance : reçoit les poids
//   - ZoneDepotJoueur   : fournit le poids adverse
//   - ObjetPesable      : composant sur l'objet antagoniste
// ============================================================

using UnityEngine;

public class ZoneDepotAntagoniste : MonoBehaviour
{
    // ===================== INSPECTEUR =====================
    [Header("Références")]
    [SerializeField] private controleurBalance _controleurBalance;
    [SerializeField] private ZoneDepotJoueur _zoneJoueur;

    // ===================== ÉTAT INTERNE =====================
    private objetPesable _objetActuel;

    // ===================== MÉTHODES PUBLIQUES =====================

    /// <summary>
    /// Appelé au démarrage de l'énigme pour définir
    /// l'objet de l'antagoniste.
    /// </summary>
    public void DefinirObjet(objetPesable objet)
    {
        _objetActuel = objet;
        NotifierBalance();
    }

    /// <summary>
    /// Appelé par ComportementAntagoniste après avoir
    /// modifié le poids de son objet.
    /// </summary>
    public void NotifierChangementPoids()
    {
        NotifierBalance();
    }

    public int CalculerPoidsTotal()
    {
        return _objetActuel != null ? _objetActuel.valeurPoids : 0;
    }

    // ===================== MÉTHODES PRIVÉES =====================

    private void NotifierBalance()
    {
        _controleurBalance.MettreAJourPoids(
            _zoneJoueur.CalculerPoidsTotal(),
            CalculerPoidsTotal()
        );
    }
}