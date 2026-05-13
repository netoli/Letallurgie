// ============================================================
// ZoneDepotJoueur.cs
// ------------------------------------------------------------
// Auteur      : Fanny Fortier
// Date créée  : 12 mai 2026
// ------------------------------------------------------------
// Description :
//   Gère la liste des objets déposés par le joueur sur son
//   côté de la balance. Recalcule le total à chaque ajout
//   ou retrait et notifie ControleurBalance.
// ------------------------------------------------------------
// Dépendances :
//   - controleurBalance      : reçoit les poids
//   - ZoneDepotAntagoniste   : fournit le poids adverse
//   - objetPesable           : composant sur chaque objet
// ============================================================

using System.Collections.Generic;
using UnityEngine;

public class ZoneDepotJoueur : MonoBehaviour
{
    // ===================== INSPECTEUR =====================
    [Header("Références")]
    [SerializeField] private controleurBalance _controleurBalance;
    [SerializeField] private ZoneDepotAntagoniste _zoneAdverse;

    // ===================== ÉTAT INTERNE =====================
    private List<objetPesable> _objetsDeposes = new List<objetPesable>();

    // ===================== MÉTHODES PUBLIQUES =====================

    public void AjouterObjet(objetPesable objet)
    {
        if (objet == null || _objetsDeposes.Contains(objet)) return;
        _objetsDeposes.Add(objet);

        Debug.Log($"[ZoneDepotJoueur] Ajout : {objet.gameObject.name} " +
                  $"(poids {objet.valeurPoids}) — total={CalculerPoidsTotal()}");

        NotifierBalance();
    }

    public void RetirerObjet(objetPesable objet)
    {
        if (!_objetsDeposes.Remove(objet)) return;

        Debug.Log($"[ZoneDepotJoueur] Retrait : {objet.gameObject.name} " +
                  $"— total={CalculerPoidsTotal()}");

        NotifierBalance();
    }

    public int CalculerPoidsTotal()
    {
        int total = 0;
        foreach (objetPesable objet in _objetsDeposes)
            total += objet.valeurPoids;
        return total;
    }

    /// <summary>
    /// Vide le plateau sans notifier la balance.
    /// Utilisé entre les phases pour reset proprement.
    /// </summary>
    public void ViderSansNotifier()
    {
        _objetsDeposes.Clear();
    }

    // ===================== MÉTHODES PRIVÉES =====================

    private void NotifierBalance()
    {
        _controleurBalance.MettreAJourPoids(
            CalculerPoidsTotal(),
            _zoneAdverse.CalculerPoidsTotal()
        );
    }
}