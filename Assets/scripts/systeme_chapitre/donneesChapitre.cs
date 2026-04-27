using UnityEngine;

[CreateAssetMenu(
    fileName = "donneesChapitre_",
    menuName = "Letallurgie/Données chapitre",
    order = 3)]
public class DonneesChapitre : ScriptableObject
{
    [Header("Identification")]
    public string idChapitre;
    public string nomAffiche;

    [Header("Bannière")]
    [Tooltip("Durée totale pendant laquelle la bannière reste à l'écran (incluant fade in/out)")]
    public float dureeAffichageBanniere = 4f;

    [Tooltip("Délai avant que la bannière apparaisse (après le démarrage du chapitre)")]
    public float delaiApparitionBanniere = 0.5f;

    [Tooltip("Délai après la disparition de la bannière avant que le premier tuto s'affiche")]
    public float delaiAvantPremierTuto = 0.5f;

    [Header("Tutoriels du chapitre")]
    [Tooltip("Tous les tutoriels de ce chapitre. Le premier sera affiché automatiquement au démarrage.")]
    public DonneesTutoriel[] tutoriels;
}