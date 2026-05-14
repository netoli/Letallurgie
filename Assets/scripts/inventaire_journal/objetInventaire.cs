// ============================================================
// objetInventaire.cs
// ------------------------------------------------------------
// Auteur      : Olivier Vernet
// Date cr      :
// Derniere modification : 12/05/2026 - fusion enigme tuyaux
// ------------------------------------------------------------
// Description :
//   ScriptableObject decrivant un item d'inventaire.
//   Fusion entre la version tutoriel (enum Tuto + champ id) et la
//   version enigme (prefabModele3D, estLeurre, descriptionInspection).
// ------------------------------------------------------------
// Dependances :
//   - utilise par objetRamassable / gestionInventaire /
//     pointAncrageTuyau / controleurPlacementTuyau /
//     iconeFlottanteCurseur / slotObjetInventaire
// ============================================================

using UnityEngine;

[CreateAssetMenu(fileName = "NouvelObjet",
    menuName = "Letallurgie/Objet Inventaire")]
public class objetInventaire : ScriptableObject
{
    [Header("Proprietes communes")]
    public string nomObjet;
    public Sprite icone;
    public CategorieObjet categorie;
    public int quantiteMax;

    [Header("Specifique aux tuyaux (laisser vide pour autres categories)")]
    [Tooltip("Prefab 3D instancie quand l'objet est place dans un " +
        "pointAncrageTuyau (et utilise comme ghost). Utilise aussi " +
        "pour drag-and-drop depuis l'inventaire.")]
    public GameObject prefabModele3D;
    public bool estLeurre;
    public string descriptionInspection;

    [Header("Identifiant unique (sauvegarde, references)")]
    public string id;
}

public enum CategorieObjet
{
    Tuto,
    Tuyaux,
    Alchimie
}
