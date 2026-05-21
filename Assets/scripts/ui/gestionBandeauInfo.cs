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
/// SETUP UNITY :
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
/// 4. Le bandeau doit etre DESACTIVE par defaut (case decochee dans
///    l'Inspector). Le script l'active quand un message arrive.
///
/// FONCTIONNEMENT :
/// - File d'attente FIFO. Si on appelle Afficher() pendant qu'un
///   message est en cours, le nouveau est mis en queue et joue apres.
/// - Fade in (0.3s), affichage pendant 'duree', fade out (0.3s),
///   puis le suivant si la file n'est pas vide.
/// - Le canvas est desactive quand la file est vide.
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

    private struct MessageBandeau
    {
        public string texte;
        public float duree;
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // S'assurer que le bandeau est invisible au demarrage
        if (groupeBandeau != null) groupeBandeau.alpha = 0f;
        gameObject.SetActive(false);
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
            Debug.LogWarning("[BandeauInfo] Instance null. Le bandeau " +
                "n'est pas dans la scene ou pas encore initialise.");
            return;
        }
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
        if (Instance.groupeBandeau != null)
            Instance.groupeBandeau.alpha = 0f;
        Instance.gameObject.SetActive(false);
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
        gameObject.SetActive(true);

        while (file.Count > 0)
        {
            MessageBandeau msg = file.Dequeue();

            if (texteBandeau != null)
                texteBandeau.text = msg.texte;

            // Fade in
            yield return StartCoroutine(FadeAlpha(0f, 1f, dureeFadeIn));

            // Affichage
            yield return new WaitForSecondsRealtime(msg.duree);

            // Fade out
            yield return StartCoroutine(FadeAlpha(1f, 0f, dureeFadeOut));

            // Petit delai entre messages
            if (file.Count > 0)
                yield return new WaitForSecondsRealtime(delaiEntreMessages);
        }

        affichageEnCours = false;
        gameObject.SetActive(false);
    }

    private IEnumerator FadeAlpha(float depart, float cible, float duree)
    {
        if (groupeBandeau == null) yield break;

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
}
