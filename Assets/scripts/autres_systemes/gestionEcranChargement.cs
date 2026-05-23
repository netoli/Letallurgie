// ============================================================
// gestionEcranChargement.cs
// ------------------------------------------------------------
// Auteur      : Fanny Fortier
// Date créée  : 2026-05-23
// ------------------------------------------------------------
// Description :
//   Manager singleton DontDestroyOnLoad qui affiche un écran
//   de chargement animé entre chaque changement de scène.
//
//   Setup Unity :
//     1. Créer un GameObject vide "gestion_ecran_chargement"
//        dans SCENE0-Menu-Tuto (scène de départ).
//     2. Y ajouter ce script + un Canvas enfant avec ton
//        animation de loading (Animator avec trigger "Jouer").
//     3. Assigner le Canvas et l'Animator dans l'Inspector.
//     4. Tous les changements de scène du projet doivent
//        appeler gestionEcranChargement.ChargerScene("NomScene")
//        au lieu de SceneManager.LoadScene directement.
// ============================================================

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class gestionEcranChargement : MonoBehaviour
{
    public static gestionEcranChargement Instance { get; private set; }

    [Header("Références")]
    [Tooltip("Canvas racine de l'écran de chargement. " +
             "Doit être désactivé par défaut.")]
    [SerializeField] private GameObject canvasChargement;

    [Tooltip("Animator sur le Canvas (optionnel). " +
             "Si assigné, le trigger 'Jouer' sera déclenché.")]
    [SerializeField] private Animator animateurChargement;

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
    public void ChargerScene(string nomScene)
    {
        if (_chargementEnCours) return;
        StartCoroutine(SequenceChargement(nomScene));
    }

    // ── Coroutines ────────────────────────────────────────────

    private IEnumerator SequenceChargement(string nomScene)
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

        // 2. Déclencher l'animation si disponible
        if (animateurChargement != null)
            animateurChargement.SetTrigger("Jouer");

        // 3. Attendre la durée minimale
        yield return new WaitForSecondsRealtime(dureeMinimale);

        // 4. Charger la scène
        SceneManager.LoadScene(nomScene);

        // 5. Attendre une frame pour que la scène soit chargée
        yield return null;

        // 6. Fade out et cacher l'écran
        if (canvasChargement != null && _groupeCanvas != null)
        {
            yield return StartCoroutine(FaderVers(0f, dureeFade));
            canvasChargement.SetActive(false);
        }

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
