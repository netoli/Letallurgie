using UnityEngine;

/// <summary>
/// ScriptableObject decrivant un message a afficher dans le BANDEAU
/// INFO horizontal (gestionBandeauInfo). Different des DonneesTutoriel :
/// non-bloquant, sert juste a rappeler contextuellement au joueur une
/// information utile (ex: "Maintiens [esc] 1s pour ouvrir le menu
/// pause"). N'attend pas d'action de la part du joueur.
///
/// CREATION : Right-click dans Project > Create > Letallurgie >
/// Donnees bandeau info. Le fichier doit etre prefixe par
/// "bandeauInfos_" pour bien le differencier des donneesTutoriel.
///
/// DECLENCHEMENT : configure dans le composant gestionDeclencheurBandeau
/// (a placer dans la scene). Le bandeau est declenche par une action
/// signalee via gestionChapitres.SignalerAction().
/// </summary>
[CreateAssetMenu(
    fileName = "bandeauInfos_",
    menuName = "Letallurgie/Donnees bandeau info",
    order = 4)]
public class DonneesBandeauInfo : ScriptableObject
{
    [Header("Identification")]
    [Tooltip("ID unique pour identifier ce bandeau dans la console et " +
        "eviter qu'il s'affiche 2 fois. Ex: 'menu_pause', 'options'.")]
    public string idBandeau;

    [Header("Contenu")]
    [TextArea(2, 5)]
    [Tooltip("Texte a afficher dans le bandeau. Rich text supporte : " +
        "<size=50>, <b>, <font=...>, <gradient=...>, etc.")]
    public string texte;

    [Tooltip("Duree (s) d'affichage du bandeau. Defaut : 5.")]
    public float dureeAffichage = 5f;

    [Header("Declenchement")]
    [Tooltip("ID d'action qui declenche l'affichage de ce bandeau. " +
        "Quand cette action est signalee par gestionChapitres." +
        "SignalerAction(), le bandeau s'affiche apres " +
        "delaiAvantApparition secondes. Ex: 'dialogue_tav1_debute' " +
        "pour afficher le bandeau pendant le 1er dialogue.")]
    public string idActionDeclenchement;

    [Tooltip("Delai (s) entre le signal de declenchement et l'affichage " +
        "reel du bandeau. Utile pour laisser respirer le joueur entre " +
        "le declenchement et l'apparition du message. Defaut 0.")]
    public float delaiAvantApparition = 0f;

    [Tooltip("(Optionnel) Si cette action a deja ete signalee AVANT " +
        "le declenchement de ce bandeau, le bandeau est annule (pas " +
        "affiche). Permet de skipper un rappel qui serait devenu " +
        "obsolete. Ex: ne pas afficher 'consulte ton inventaire' si " +
        "le joueur l'a deja ouvert. Laisse vide pour toujours afficher.")]
    public string idActionAnnulation;

    [Header("Affichage repete")]
    [Tooltip("Si coche, le bandeau peut etre affiche plusieurs fois " +
        "si l'action de declenchement est signalee a nouveau. Par " +
        "defaut, un bandeau ne s'affiche qu'une seule fois par session.")]
    public bool autoriserMultipleAffichages = false;

    [Header("Synchronisation a l'apparition")]
    [Tooltip("(Optionnel) ID d'action signalee au moment OU le bandeau " +
        "apparait effectivement a l'ecran (apres delaiAvantApparition). " +
        "Different de idActionDeclenchement qui declenche le compte a " +
        "rebours. Utile pour synchroniser l'activation d'un indicateur " +
        "visuel (ex : prefab_pointeur_personnage) avec l'apparition du " +
        "bandeau, sans avoir a dupliquer le delai. Laisse vide si pas " +
        "necessaire.")]
    public string idActionAApparition;

    [Header("Synchronisation a la fin d'affichage")]
    [Tooltip("(Optionnel) ID d'action signalee au moment OU le bandeau " +
        "disparait de l'ecran (apres dureeAffichage). Utile pour " +
        "declencher l'apparition d'un pointeur OU une etape suivante " +
        "APRES que le joueur ait fini de lire le message. Different de " +
        "idActionAApparition qui se declenche au DEBUT de l'affichage. " +
        "Laisse vide si pas necessaire.")]
    public string idActionAFinAffichage;
}
