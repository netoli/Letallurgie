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

    [Header("HUD")]
    [SerializeField] private TMP_Text texteNombreObjetsHud;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        //MettreAJourHudInv();
    }

    private void Start()
    {
        //MettreAJourHudInv();
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

        //onInventaireModifie?.Invoke();
        NotifierModif();
        MettreAJourHudInv();
    }

    public void RetirerObjet(objetInventaire objet, int quantite = 1)
    {
        if (!objets.ContainsKey(objet)) return;

        objets[objet] -= quantite;

        if (objets[objet] <= 0)
            objets.Remove(objet);

        //onInventaireModifie?.Invoke();
        NotifierModif();
        MettreAJourHudInv();
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
        //onInventaireModifie?.Invoke();
        NotifierModif();
        MettreAJourHudInv();
    }

    public void MettreAJourHudInv()
    {
        if (texteNombreObjetsHud != null)
        {
            Debug.Log($"[HUD] InstanceID={texteNombreObjetsHud.GetInstanceID()} TMP: {texteNombreObjetsHud.gameObject.name} | path={GetFullPath(texteNombreObjetsHud.gameObject)}", texteNombreObjetsHud.gameObject);
        }
        else Debug.Log("[HUD] texteNombreObjetsHud is NULL");

        int total = ObtenirTotalObjets();
        if (texteNombreObjetsHud == null) return;

        Debug.Log($"[gestionInventaire] MettreAJourHud called. total={total} | texteNombreObjetsHud={(texteNombreObjetsHud != null ? texteNombreObjetsHud.name : "NULL")} | activeInHierarchy={(texteNombreObjetsHud != null ? texteNombreObjetsHud.gameObject.activeInHierarchy.ToString() : "N/A")}");

        texteNombreObjetsHud.text = total.ToString();
    }

    private string GetFullPath(GameObject go)
    {
        string path = go.name;
        Transform t = go.transform.parent;
        while (t != null)
        {
            path = t.name + "/" + path;
            t = t.parent;
        }
        return path;
    }

    private void NotifierModif()
    {
        int total = ObtenirTotalObjets();
        onInventaireModifie?.Invoke();
        onInventaireModifieHud?.Invoke(total);
    }
}