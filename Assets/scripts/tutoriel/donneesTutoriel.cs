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

    [Tooltip("ID d'action qui ANNULE cette tuile : si cette action a " +
        "deja ete signalee avant l'affichage, la tuile est sautee " +
        "(comme si le joueur l'avait deja vue). Si elle est signalee " +
        "pendant l'affichage, la tuile se ferme sans attendre. Utile " +
        "pour les tuiles informatives (ex : menu_pause, options) qui " +
        "ne doivent pas s'afficher apres que la 1ere etape du dialogue " +
        "soit terminee. Laisse vide pour le comportement par defaut.")]
    public string idActionAnnulation;

    [Header("Contenu affiché")]
    public string titre;

    [TextArea(2, 5)]
    public string explication;

    public Sprite image;

    // Les anciens champs dureeMinimum / dureeMaximum ont ete retires :
    // chaque tuile reste affichee jusqu'a ce que le joueur fasse l'action
    // demandee (ou appuie sur ESC pour la fermer manuellement).

    [Header("Affichage")]
    [Tooltip("Si coche, cette etape s'affiche comme une banniere " +
        "annonce-chapitre (non interactive, disparait apres " +
        "dureeBanniere secondes) au lieu d'une tuile tutoriel " +
        "classique. Utile pour des indications narratives qui ne " +
        "sont pas vraiment du tuto (ex : 'Reparler au tavernier').")]
    public bool afficherCommeBanniere = false;

    [Tooltip("Duree (s) d'affichage de la banniere si " +
        "afficherCommeBanniere est coche. Pendant cette duree, " +
        "l'idActionRequise reste actif et fermera l'etape si " +
        "declenche (typiquement la fin d'un dialogue PNJ).")]
    public float dureeBanniere = 3.6f;

    [Tooltip("Si > 0, la tuile tutoriel se ferme automatiquement " +
        "apres ce delai (s), MEME si idActionRequise n'a pas ete " +
        "signalee. Utile pour les tuiles purement informatives " +
        "(ex : 'esc maintenu = menu pause') qui ne doivent pas " +
        "bloquer la progression du chapitre. 0 (defaut) = la " +
        "tuile attend l'action requise comme avant.")]
    public float dureeAuto = 0f;

    [Tooltip("Delai (s) avant l'affichage de cette tuile lors du " +
        "passage depuis la tuile precedente. Defaut 0.25s. Permet de " +
        "laisser respirer entre 2 tuiles, ou de retarder une tuile " +
        "informative pour qu'elle apparaisse a un moment narratif " +
        "specifique (ex: 3s apres la fermeture de la tuile precedente).")]
    public float delaiAvantApparition = 0.25f;
}