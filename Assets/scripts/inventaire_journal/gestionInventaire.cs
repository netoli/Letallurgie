using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class gestionInventaire : MonoBehaviour
{
    public static gestionInventaire Instance;

    private Dictionary<objetInventaire, int> objets =
        new Dictionary<objetInventaire, int>();

    public event Action onInventaireModifie;
    public event Action<int> onInventaireModifieHud;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

    }

    private void Start()
    {

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

        NotifierModif();
    }

    public void RetirerObjet(objetInventaire objet, int quantite = 1)
    {
        if (!objets.ContainsKey(objet)) return;

        objets[objet] -= quantite;

        if (objets[objet] <= 0)
            objets.Remove(objet);

        NotifierModif();
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
        NotifierModif();
    }

   

    private void NotifierModif()
    {
        int total = ObtenirTotalObjets();
        onInventaireModifie?.Invoke();
        onInventaireModifieHud?.Invoke(total);
    }
}