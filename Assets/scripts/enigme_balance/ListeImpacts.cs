// ============================================================
// ListeImpacts.cs
// ------------------------------------------------------------
// Auteur      : Fanny Fortier
// Date créée  : 13 mai 2026
// ------------------------------------------------------------
// Description :
//   ScriptableObject définissant les impacts possibles de
//   l'antagoniste pour chaque phase. Permet de configurer
//   le game design directement dans l'Inspector sans toucher
//   au code.
// ------------------------------------------------------------
// Utilisation :
//   Clic droit dans Project → Create → Létallurgie → Liste Impacts
// ============================================================

using UnityEngine;

[CreateAssetMenu(
    fileName = "ListeImpacts",
    menuName = "Letallurgie/Liste Impacts")]
public class ListeImpacts : ScriptableObject
{
    [Header("Impacts phase 1 (après premier équilibre)")]
    public ImpactAntagoniste[] impactsPhase1;

    [Header("Impacts phase 2 (après deuxième équilibre)")]
    public ImpactAntagoniste[] impactsPhase2;
}

[System.Serializable]
public struct ImpactAntagoniste
{
    [Tooltip("Nom du trigger dans l'Animator de l'antagoniste")]
    public string nomAnimation;

    [Tooltip("Multiplicateur appliqué au poids. " +
             "2 = double, 0.5 = moitié, 3 = triple.")]
    public float multiplicateur;
}