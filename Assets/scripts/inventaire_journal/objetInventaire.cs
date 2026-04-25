using UnityEngine;

[CreateAssetMenu(fileName = "NouvelObjet",
    menuName = "Letallurgie/Objet Inventaire")]
public class objetInventaire : ScriptableObject
{
    [Header("Propriétés communes")]
    public string nomObjet;
    public Sprite icone;
    public CategorieObjet categorie;
    public int quantiteMax;

    [Header("Spécifique aux tuyaux (laisser vide pour autres catégories)")]
    public GameObject prefabModele3D;
    public bool estLeurre;
    public string descriptionInspection;
}

public enum CategorieObjet
{
    Tuyaux,
    Alchimie
}