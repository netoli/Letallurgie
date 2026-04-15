using System;
using System.Collections.Generic;
using UnityEngine;

public class gestionInventaire : MonoBehaviour
{
    public static gestionInventaire Instance;

    private Dictionary<objetInventaire, int> objets =
        new Dictionary<objetInventaire, int>();

    public event Action onInventaireModifie;

    void Awake()
    {
        Instance = this;
    }

    public void AjouterObjet(objetInventaire objet, int quantite = 1)
    {
        if (objets.ContainsKey(objet))
        {
            objets[objet] += quantite;

            if (objet.quantiteMax > 0
                && objets[objet] > objet.quantiteMax)
                objets[objet] = objet.quantiteMax;
        }
        else
        {
            objets[objet] = quantite;
        }

        Debug.Log("Ajoute: " + objet.nomObjet
            + " x" + objets[objet]);

        onInventaireModifie?.Invoke();
    }

    public void RetirerObjet(objetInventaire objet, int quantite = 1)
    {
        if (!objets.ContainsKey(objet)) return;

        objets[objet] -= quantite;

        if (objets[objet] <= 0)
            objets.Remove(objet);

        onInventaireModifie?.Invoke();
    }

    public int ObtenirQuantite(objetInventaire objet)
    {
        if (objets.ContainsKey(objet))
            return objets[objet];
        return 0;
    }

    public List<KeyValuePair<objetInventaire, int>> ObtenirParCategorie(
        CategorieObjet categorie)
    {
        List<KeyValuePair<objetInventaire, int>> resultats =
            new List<KeyValuePair<objetInventaire, int>>();

        foreach (var paire in objets)
        {
            if (paire.Key.categorie == categorie)
                resultats.Add(paire);
        }

        return resultats;
    }

    public int ObtenirTotalObjets()
    {
        int total = 0;
        foreach (var paire in objets)
            total += paire.Value;
        return total;
    }

    public void ViderInventaire()
    {
        objets.Clear();
        onInventaireModifie?.Invoke();
    }
}