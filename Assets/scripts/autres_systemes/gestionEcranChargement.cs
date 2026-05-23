// ============================================================
// gestionEcranChargement.cs
// ------------------------------------------------------------
// Auteur      : Fanny Fortier
// Date créée  : 2026-05-23
// ------------------------------------------------------------
// Description :
//   Manager singleton DontDestroyOnLoad qui affiche un écran
//   de chargement vidéo (MP4) entre chaque changement de scène.
//
//   Setup Unity :
//     1. Créer un GameObject vide "gestion_ecran_chargement"
//        dans SCENE0-Menu-Tuto (scène de départ).
//     2. Créer un Canvas enfant "canvas_ecran_chargement" :
//          - Sort Order : 999 (par-dessus tout)
//          - Désactivé par défaut
//     3. Dans ce Canvas, créer un RawImage plein écran (Anchor : stretch).
//     4. Créer un RenderTexture (Assets > Create > Render Texture).
//          - Assigner la RenderTexture au champ Texture du RawImage.
//     5. Sur "gestion_ecran_chargement", ajouter un VideoPlayer :
//          - Source          : Video Clip → ton MP4
//          - Render Mode     : Render Texture → la même RenderTexture
//          - Loop            : ✓
//          - Play On Awake   : ✗
//     6. Assigner dans l'Inspector de ce script :
//          - canvasChargement  → le Canvas enfant
//          - lecteurChargement → le VideoPlayer
//     7. Tous les changements de scène appellent
//        gestionEcranChargement.ChargerScene("NomScene").
// ============================================================

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class gestionEcranChargement : MonoBehaviour
{
    public static gestionEcranChargement Instance { get; private set; }

    [Header("Références")]
    [Tooltip("Canvas racine de l'écran de chargement. " +
             "Doit être désactivé par défaut. Sort Order 999.")]
    [SerializeField] private GameObject canvasChargement;

    [Tooltip("VideoPlayer qui joue le MP4 de chargement. " +
             "Loop ✓, Play On Awake ✗, Render Mode → Render Texture.")]
    [SerializeField] private VideoPlayer lecteurChargement;

    [Header("Paramètres")]
    [Tooltip("Durée minimale d'affichage de l'écran (secondes). " +
             "Évite un flash trop rapide si la scène charge vite.")]
    [SerializeField] private float dureeMinimale = 2f;

    [Tooltip("Durée du fade in/out du canvas (secondes).")]
    [SerializeField] private float dureeFade = 0.3f;

    private CanvasGroup _groupeCanvas;
    private bool _chargementEnCours = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Récupérer ou ajouter un CanvasGroup pour le fade
        if (canvasChargement != null)
        {
            _groupeCanvas = canvasChargement
                .GetComponent<CanvasGroup>();
            if (_groupeCanvas == null)
                _groupeCanvas = canvasChargement
                    .AddComponent<CanvasGroup>();
        }

        // S'assurer que le canvas est caché au départ
        if (canvasChargement != null)
            canvasChargement.SetActive(false);
    }

    // ── API publique ──────────────────────────────────────────

    /// <summary>
    /// Charge une scène en affichant l'écran de chargement animé.
    /// À utiliser partout dans le projet à la place de
    /// SceneManager.LoadScene().
    /// </summary>
    /// <param name="nomScene">Nom exact de la scène à charger.</param>
    /// <param name="onSceneChargee">Callback facultatif exécuté une frame
    /// après le LoadScene (scène prête, avant le fade out). Utiliser pour
    /// démarrer la musique, initialiser des systèmes, etc.</param>
    public void ChargerScene(string nomScene,
        System.Action onSceneChargee = null)
    {
        if (_chargementEnCours) return;
        StartCoroutine(SequenceChargement(nomScene, onSceneChargee));
    }

    // ── Coroutines ────────────────────────────────────────────

    private IEnumerator SequenceChargement(string nomScene,
        System.Action onSceneChargee)
    {
        _chargementEnCours = true;

        // 1. Afficher l'écran de chargement (fade in)
        if (canvasChargement != null)
        {
            canvasChargement.SetActive(true);
            if (_groupeCanvas != null)
            {
                _groupeCanvas.alpha = 0f;
                yield return StartCoroutine(
                    FaderVers(1f, dureeFade));
            }
        }

        // 2. Lancer la vidéo de chargement
        if (lecteurChargement != null)
            lecteurChargement.Play();

        // 3. Attendre la durée minimale (la vidéo joue pendant ce temps)
        yield return new WaitForSecondsRealtime(dureeMinimale);

        // 4. Charger la scène (le loading screen reste visible par-dessus)
        SceneManager.LoadScene(nomScene);

        // 5. Attendre une frame pour que la scène soit initialisée
        yield return null;

        // 6. Callback post-chargement (musique, init, etc.)
        onSceneChargee?.Invoke();

        // 7. Fade out, arrêter la vidéo et cacher l'écran
        if (canvasChargement != null && _groupeCanvas != null)
        {
            yield return StartCoroutine(FaderVers(0f, dureeFade));
            canvasChargement.SetActive(false);
        }

        if (lecteurChargement != null)
            lecteurChargement.Stop();

        _chargementEnCours = false;
    }

    private IEnumerator FaderVers(float cible, float duree)
    {
        if (_groupeCanvas == null) yield break;

        float depart = _groupeCanvas.alpha;
        float temps = 0f;

        while (temps < duree)
        {
            temps += Time.unscaledDeltaTime;
            _groupeCanvas.alpha = Mathf.Lerp(
                depart, cible, temps / duree);
            yield return null;
        }

        _groupeCanvas.alpha = cible;
    }
}
