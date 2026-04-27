// ============================================================
// objetInventaire.cs
// ------------------------------------------------------------
// Auteur      : Olivier Vernet
// Date créé   : 
// Dernière modification : 25/04/2026 - Fanny Fortier
// ------------------------------------------------------------
// Description :
//   ScriptableObject décrivant un item d'inventaire.
//   Ajout d'un champ prefab3D pour permettre de ré-instancier
//   l'objet dans le monde lors d'un drop depuis l'UI.(Interaction Drag and drop)
// ------------------------------------------------------------
// Dépendances :
//   - utilisé par objetRamassable / gestionInventaire
// ============================================================

using UnityEngine;

[CreateAssetMenu(fileName = "NouvelObjet",
    menuName = "Letallurgie/Objet Inventaire")]
public class objetInventaire : ScriptableObject
{
    public string nomObjet;
    public Sprite icone;
    public CategorieObjet categorie;
    public int quantiteMax;

    [Header("Prefab 3D")]
    [Tooltip("Prefab 3D à instancier dans le monde quand l'objet est dragged depuis l'inventaire")]
    public GameObject prefab3D;
    [Tooltip("Identifiant unique")]
    public string id;
}

public enum CategorieObjet
{
    Tuyaux,
    Morse,
    Cartographie,
    Alchimie
}