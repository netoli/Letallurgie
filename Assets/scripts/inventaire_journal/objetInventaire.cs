using UnityEngine;

[CreateAssetMenu(fileName = "NouvelObjet",
    menuName = "Letallurgie/Objet Inventaire")]
public class objetInventaire : ScriptableObject
{
    public string nomObjet;
    public Sprite icone;
    public CategorieObjet categorie;
    public int quantiteMax;
}

public enum CategorieObjet
{
    Tuyaux,
    Morse,
    Cartographie,
    Alchimie
}