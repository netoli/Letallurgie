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

    // Les anciens champs dureeMinimum / dureeMaximum ont ete retires :
    // chaque tuile reste affichee jusqu'a ce que le joueur fasse l'action
    // demandee (ou appuie sur ESC pour la fermer manuellement).
}