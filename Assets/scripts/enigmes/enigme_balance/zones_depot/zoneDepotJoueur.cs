// ============================================================
// ZoneDepotJoueur.cs
// ------------------------------------------------------------
// Auteur      : Fanny Fortier
// Date cr��e  : 12 mai 2026
// ------------------------------------------------------------
// Description :
//   G�re la liste des objets d�pos�s par le joueur sur son
//   c�t� de la balance. Recalcule le total � chaque ajout
//   ou retrait et notifie ControleurBalance.
// ------------------------------------------------------------
// D�pendances :
//   - controleurBalance      : re�oit les poids
//   - ZoneDepotAntagoniste   : fournit le poids adverse
//   - objetPesable           : composant sur chaque objet
// ============================================================

using System.Collections.Generic;
using UnityEngine;

public class ZoneDepotJoueur : MonoBehaviour
{
    // ===================== INSPECTEUR =====================
    [Header("R�f�rences")]
    [SerializeField] private controleurBalance _controleurBalance;

    // ===================== �TAT INTERNE =====================
    private List<objetPesable> _objetsDeposes = new List<objetPesable>();

    // ===================== M�THODES PUBLIQUES =====================

    public void AjouterObjet(objetPesable objet)
    {
        if (objet == null || _objetsDeposes.Contains(objet)) return;
        _objetsDeposes.Add(objet);

        Debug.Log($"[ZoneDepotJoueur] Ajout : {objet.gameObject.name} " +
                  $"(poids {objet.valeurPoids}) � total={CalculerPoidsTotal()}");

        NotifierBalance();
    }

    public void RetirerObjet(objetPesable objet)
    {
        if (!_objetsDeposes.Remove(objet)) return;

        Debug.Log($"[ZoneDepotJoueur] Retrait : {objet.gameObject.name} " +
                  $"� total={CalculerPoidsTotal()}");

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
    /// Utilis� entre les phases pour reset proprement.
    /// </summary>
    /// <summary>
    /// Retire un objet détruit extérieurement (ex: ramassé par objetRamassable).
    /// Utilise ReferenceEquals car l'opérateur == d'Unity retourne true
    /// pour les objets détruits, ce qui empêcherait Remove de les trouver.
    /// </summary>
    public void SupprimerObjetDetruit(objetPesable objet)
    {
        for (int i = _objetsDeposes.Count - 1; i >= 0; i--)
        {
            if (object.ReferenceEquals(_objetsDeposes[i], objet))
            {
                _objetsDeposes.RemoveAt(i);
                Debug.Log($"[ZoneDepotJoueur] Objet ramassé détecté. Total={CalculerPoidsTotal()}");
                NotifierBalance();
                return;
            }
        }
    }

    public void ViderSansNotifier()
    {
        _objetsDeposes.Clear();
    }

    // ===================== M�THODES PRIV�ES =====================

    private void NotifierBalance()
    {
        // Met à jour uniquement le côté gauche (joueur).
        // Le côté droit (antagoniste) est géré indépendamment
        // par ZoneDepotAntagoniste — plus de dépendance croisée.
        _controleurBalance.MettreAJourPoidsGauche(CalculerPoidsTotal());
    }
}