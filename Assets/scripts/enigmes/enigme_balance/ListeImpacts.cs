// ============================================================
// ListeImpacts.cs
// ------------------------------------------------------------
// Auteur      : Fanny Fortier
// Date créée  : 2026
// ------------------------------------------------------------
// Description :
//   ScriptableObject définissant les impacts possibles de
//   l'antagoniste pour chaque phase.
// ------------------------------------------------------------

using UnityEngine;

[CreateAssetMenu(
    fileName = "ListeImpacts",
    menuName = "Letallurgie/Liste Impacts")]
public class ListeImpacts : ScriptableObject
{
    [Header("Impacts phase 1 (après premier équilibre)")]
    public impactAntagoniste[] impactsPhase1;

    [Header("Impacts phase 2 (après deuxième équilibre)")]
    public impactAntagoniste[] impactsPhase2;
}

[System.Serializable]
public struct impactAntagoniste
{
    [Tooltip("Multiplicateur appliqué au poids. " +
             "3 = triple, 0.5 = moitié.")]
    public float multiplicateur;

    [Tooltip("True = animation grossit, False = animation rapetisser")]
    public bool grossit;
}