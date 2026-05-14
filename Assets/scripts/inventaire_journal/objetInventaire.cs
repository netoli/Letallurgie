// ============================================================
// objetInventaire.cs
// ------------------------------------------------------------
// Auteur      : Olivier Vernet
// Date cr      :
// Derniere modification : 14/05/2026 - merge integration_prototype_build_2
// ------------------------------------------------------------
// Description :
//   ScriptableObject decrivant un item d'inventaire.
//   Fusion des versions tutoriel + enigme tuyaux + enigme balance.
//   Conserve les deux champs prefab3D et prefabModele3D pendant la
//   periode de transition (l'enigme balance utilise prefab3D, les
//   tuyaux et le tutoriel utilisent prefabModele3D).
// ------------------------------------------------------------
// Dependances :
//   - utilise par objetRamassable / gestionInventaire /
//     pointAncrageTuyau / controleurPlacementTuyau /
//     iconeFlottanteCurseur / slotObjetInventaire /
//     DEBUGBalancePlateau / controleurDeposeObjet
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

    [Header("Specifique aux tuyaux et au drag-and-drop tuto")]
    [Tooltip("Prefab 3D instancie quand l'objet est place dans un " +
        "pointAncrageTuyau (utilise aussi comme ghost). Utilise par " +
        "le tutoriel pour la bouteille/verre.")]
    public GameObject prefabModele3D;
    public bool estLeurre;
    public string descriptionInspection;

    [Header("Specifique a l'enigme balance (legacy)")]
    [Tooltip("Prefab 3D utilise par l'enigme balance (SCENE4-Manoir). " +
        "A terme, fusionner avec prefabModele3D.")]
    public GameObject prefab3D;

    [Header("Identifiant unique (sauvegarde, references)")]
    public string id;
}

public enum CategorieObjet
{
    Tuto,
    Tuyaux,
    Alchimie
}
