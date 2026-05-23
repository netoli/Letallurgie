using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Bandeau horizontal d'informations non-bloquantes. Different de la
/// tuile tutoriel (qui est obligatoire et bloque le tuto), ce bandeau
/// sert a afficher des rappels contextuels courts qui s'enchainent.
///
/// Exemples d'utilisation :
///   gestionBandeauInfo.Afficher("Maintiens <gradient=...>esc</gradient> 1s pour ouvrir le menu pause", 5f);
///   gestionBandeauInfo.Afficher("Tu as trouve un indice !", 3f);
///   gestionBandeauInfo.Afficher("Reviens vers le tavernier", 4f);
///
/// SETUP UNITY (IMPORTANT) :
/// 1. Sous canvas_hud, creer un GameObject "bandeau_info" :
///    - Image de fond (semi-transparent, style horizontal pleine
///      largeur ou centre).
///    - TMP_Text enfant qui contient le texte (centre, style cohérent
///      avec les tuiles tuto, support rich text).
///    - CanvasGroup pour le fade.
/// 2. Attacher ce script gestionBandeauInfo sur le GameObject
///    "bandeau_info".
/// 3. Glisser les references dans l'Inspector :
///    - groupeBandeau (CanvasGroup)
///    - texteBandeau (TMP_Text)
/// 4. LE GAMEOBJECT bandeau_info DOIT ETRE COCHE (ACTIF) DANS
///    L'INSPECTOR. Sinon Awake() n'est jamais appele et le script ne
///    s'enregistre pas dans Instance. Le script gere lui-meme la
///    visibilite via alpha=0 et interactable=false (le bandeau est
///    invisible meme si le GameObject est actif).
///
/// FONCTIONNEMENT :
/// - File d'attente FIFO. Si on appelle Afficher() pendant qu'un
///   message est en cours, le nouveau est mis en queue et joue apres.
/// - Fade in (0.3s), affichage pendant 'duree', fade out (0.3s),
///   puis le suivant si la file n'est pas vide.
/// - Le GameObject reste TOUJOURS actif. Le bandeau est masque
///   uniquement via le CanvasGroup (alpha=0, blocksRaycasts=false).
/// </summary>
public class gestionBandeauInfo : MonoBehaviour
{
    public static gestionBandeauInfo Instance { get; private set; }

    [Header("References")]
    [SerializeField] private CanvasGroup groupeBandeau;
    [SerializeField] private TMP_Text texteBandeau;

    [Header("Animation")]
    [Tooltip("Duree (s) du fade in du bandeau.")]
    [SerializeField] private float dureeFadeIn = 0.3f;

    [Tooltip("Duree (s) du fade out du bandeau.")]
    [SerializeField] private float dureeFadeOut = 0.3f;

    [Tooltip("Petit delai (s) entre 2 messages consecutifs pour eviter " +
        "que ca enchaine trop vite.")]
    [SerializeField] private float delaiEntreMessages = 0.15f;

    // File d'attente des messages a afficher
    private readonly Queue<MessageBandeau> file = new Queue<MessageBandeau>();
    private bool affichageEnCours = false;

    // Pause externe : quand le joueur ouvre menu pause / options /
    // journal, le bandeau info doit disparaitre visuellement pendant
    // que le menu est affiche. Le compteur de duree d'affichage est
    // gele. A la reprise, le bandeau reapparait avec le TEMPS RESTANT.
    private bool bandeauPauseExterne = false;
    // Alpha qu'on doit restaurer apres la pause (memorise au moment
    // ou MettreEnPauseExterne est appele).
    private float alphaAvantPauseExterne = 0f;

    private struct MessageBandeau
    {
        public string texte;
        public float duree;
    }

    void Awake()
    {
        // Singleton DE SCENE : empeche les doublons accidentels.
        // PAS de DontDestroyOnLoad ici par defaut. Le bandeau est
        // conçu pour etre un PREFAB instancie dans chaque scene
        // (a l'interieur du canvas_hud de chaque scene). Modifier le
        // prefab applique automatiquement les changements partout.
        //
        // Si tu veux que le STATE du bandeau (file d'attente, message
        // en cours) PERSISTE entre scenes (rare), alors :
        //  - Mets le GameObject racine en DontDestroyOnLoad
        //    (depuis un autre script SystemesPersistants par exemple)
        //  - Le bandeau doit etre dans un Canvas dedie en
        //    Screen Space - Overlay (pas Camera), separe du canvas_hud
        if (Instance != null && Instance != this)
        {
            Debug.Log($"[BandeauInfo] Doublon detecte ('{name}') dans la " +
                "scene, destruction. L'instance existante est gardee.");
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Diagnostic : confirmer que le bandeau est bien initialise.
        Debug.Log($"[BandeauInfo] Awake sur '{name}'. " +
            $"groupeBandeau={(groupeBandeau != null ? "OK" : "NULL!")} " +
            $"texteBandeau={(texteBandeau != null ? "OK" : "NULL!")}");

        if (groupeBandeau == null)
        {
            Debug.LogError($"[BandeauInfo] {name} : groupeBandeau " +
                "n'est PAS assigne dans l'Inspector. Le bandeau ne " +
                "pourra pas faire son fade. Glisse le CanvasGroup du " +
                "GameObject dans le champ groupeBandeau.");
        }
        if (texteBandeau == null)
        {
            Debug.LogError($"[BandeauInfo] {name} : texteBandeau n'est " +
                "PAS assigne dans l'Inspector. Le texte ne s'affichera " +
                "pas. Glisse le TMP_Text enfant dans le champ texteBandeau.");
        }

        // Masquer visuellement le bandeau au demarrage SANS desactiver
        // le GameObject (sinon Awake ne serait pas appele si le user
        // decoche le GameObject dans l'Inspector, et Instance resterait
        // null). On force alpha=0 et blocksRaycasts=false pour qu'il
        // soit invisible et non-interactif.
        if (groupeBandeau != null)
        {
            groupeBandeau.alpha = 0f;
            groupeBandeau.blocksRaycasts = false;
            groupeBandeau.interactable = false;
        }
    }

    /// <summary>
    /// Affiche un message dans le bandeau. Si un message est deja en
    /// cours, celui-ci est mis en file d'attente et joue apres.
    /// </summary>
    /// <param name="texte">Texte a afficher (rich text supporte).</param>
    /// <param name="duree">Duree d'affichage en secondes.</param>
    public static void Afficher(string texte, float duree = 4f)
    {
        if (Instance == null)
        {
            Debug.LogWarning("[BandeauInfo] Instance null - le " +
                "GameObject bandeau_info n'existe pas dans la scene OU " +
                "il est DECOCHE dans l'Inspector (case decochee = Awake " +
                "jamais appele). Texte qui aurait du s'afficher: " +
                $"'{texte}'");
            return;
        }
        Debug.Log($"[BandeauInfo] Afficher() : '{texte}' (duree {duree}s)");
        Instance.MettreEnFile(texte, duree);
    }

    /// <summary>
    /// Vide la file d'attente et masque immediatement le bandeau.
    /// Utile pour les transitions de scene ou cinematiques.
    /// </summary>
    public static void Effacer()
    {
        if (Instance == null) return;
        Instance.file.Clear();
        Instance.StopAllCoroutines();
        Instance.affichageEnCours = false;
        Instance.bandeauPauseExterne = false;
        if (Instance.groupeBandeau != null)
            Instance.groupeBandeau.alpha = 0f;
    }

    /// <summary>
    /// Met en pause externe l'affichage du bandeau (ex : le joueur a
    /// ouvert le menu pause / options / journal). Le bandeau disparait
    /// visuellement et le compteur de duree d'affichage est gele.
    /// Quand ReprendreExterne est appele, le bandeau reapparait pour
    /// le TEMPS RESTANT de l'affichage.
    /// </summary>
    public static void MettreEnPauseExterne()
    {
        if (Instance == null) return;
        if (Instance.bandeauPauseExterne) return;
        Instance.bandeauPauseExterne = true;
        if (Instance.groupeBandeau != null)
        {
            // Memoriser l'alpha actuel pour le restaurer apres la pause.
            Instance.alphaAvantPauseExterne = Instance.groupeBandeau.alpha;
            Instance.groupeBandeau.alpha = 0f;
        }
        Debug.Log("[BandeauInfo] Pause externe activee (menu ouvert).");
    }

    /// <summary>
    /// Reprend l'affichage du bandeau apres une pause externe. Le
    /// bandeau reapparait visuellement avec l'alpha qu'il avait avant
    /// la pause, et le compteur de duree d'affichage redemarre.
    /// </summary>
    public static void ReprendreExterne()
    {
        if (Instance == null) return;
        if (!Instance.bandeauPauseExterne) return;
        Instance.bandeauPauseExterne = false;
        if (Instance.groupeBandeau != null)
        {
            Instance.groupeBandeau.alpha = Instance.alphaAvantPauseExterne;
        }
        Debug.Log("[BandeauInfo] Reprise externe (menu ferme).");
    }

    private void MettreEnFile(string texte, float duree)
    {
        file.Enqueue(new MessageBandeau
        {
            texte = texte,
            duree = duree
        });

        if (!affichageEnCours)
            StartCoroutine(JouerFile());
    }

    private IEnumerator JouerFile()
    {
        affichageEnCours = true;

        while (file.Count > 0)
        {
            MessageBandeau msg = file.Dequeue();

            if (texteBandeau != null)
                texteBandeau.text = msg.texte;

            Debug.Log($"[BandeauInfo] Affichage en cours: '{msg.texte}'");

            // Fade in : si une pause externe survient pendant le fade,
            // on attend qu'elle finisse pour reprendre le fade au point
            // ou il en etait.
            yield return StartCoroutine(FadeAvecPause(0f, 1f, dureeFadeIn));

            // Affichage : le compteur t est gele quand pause externe.
            // Le bandeau reapparait avec le temps restant a la reprise.
            float t = 0f;
            while (t < msg.duree)
            {
                if (!bandeauPauseExterne)
                    t += Time.unscaledDeltaTime;
                yield return null;
            }

            // Fade out
            yield return StartCoroutine(FadeAvecPause(1f, 0f, dureeFadeOut));

            // Petit delai entre messages
            if (file.Count > 0)
                yield return new WaitForSecondsRealtime(delaiEntreMessages);
        }

        affichageEnCours = false;
    }

    private IEnumerator FadeAlpha(float depart, float cible, float duree)
    {
        if (groupeBandeau == null)
        {
            Debug.LogError("[BandeauInfo] FadeAlpha : groupeBandeau " +
                "est null, fade ignore.");
            yield break;
        }

        float t = 0f;
        while (t < duree)
        {
            t += Time.unscaledDeltaTime;
            groupeBandeau.alpha = Mathf.Lerp(
                depart, cible, Mathf.Clamp01(t / duree));
            yield return null;
        }
        groupeBandeau.alpha = cible;
    }

    /// <summary>
    /// Fade qui prend en compte la pause externe : si une pause survient
    /// pendant le fade, on gele le compteur et on force alpha = 0.
    /// Quand la pause s'arrete, on reprend le fade au point ou il
    /// en etait, en partant de alpha = 0 vers la cible (et non depuis
    /// depart, sinon ca casserait la continuite).
    /// </summary>
    private IEnumerator FadeAvecPause(float depart, float cible, float duree)
    {
        if (groupeBandeau == null) yield break;

        float t = 0f;
        while (t < duree)
        {
            if (bandeauPauseExterne)
            {
                // Pendant la pause : alpha 0 (deja fait par MettreEnPauseExterne).
                // On ne touche pas a alpha ici pour ne pas lutter contre la pause.
                yield return null;
                continue;
            }
            t += Time.unscaledDeltaTime;
            groupeBandeau.alpha = Mathf.Lerp(
                depart, cible, Mathf.Clamp01(t / duree));
            yield return null;
        }
        // En fin de fade, restaurer alpha cible si on n'est pas en pause.
        if (!bandeauPauseExterne)
            groupeBandeau.alpha = cible;
        // On memorise la cible comme alpha a restaurer si une pause
        // survient apres : a la fin du fade in, alpha=1 doit etre
        // restaure a la reprise.
        alphaAvantPauseExterne = cible;
    }
}
