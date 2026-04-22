using UnityEngine;

[CreateAssetMenu(
    fileName = "donneesTutoriel_",
    menuName = "Letallurgie/Données tutoriel",
    order = 2)]
public class DonneesTutoriel : ScriptableObject
{
    [Header("Identification")]
    [Tooltip("ID unique utilisé par les triggers pour déclencher ce tuto. Ex: 'se_deplacer'")]
    public string idDeclencheur;

    [Tooltip("ID de l'action qui ferme ce tuto automatiquement. Ex: 'deplacement'. Laisse vide si le tuto ne se ferme que par ESC.")]
    public string idActionRequise;

    [Header("Contenu affiché")]
    public string titre;

    [TextArea(2, 5)]
    public string explication;

    public Sprite image;

    [Header("Timing")]
    [Tooltip("Durée minimum d'affichage en secondes (pour laisser le temps de lire)")]
    public float dureeMinimum = 3f;

    [Tooltip("Durée maximum d'affichage en secondes (si le joueur ne fait jamais l'action)")]
    public float dureeMaximum = 30f;
}