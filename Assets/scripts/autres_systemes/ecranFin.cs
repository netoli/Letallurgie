// ============================================================
// ecranFin.cs
// ------------------------------------------------------------
// Auteur      : Fanny Fortier
// Date créée  : 2026-05-24
// Dernière modification : 2026-05-25
// ------------------------------------------------------------
// Description :
//   Écran de fin affiché après la cinématique finale.
//   Séquence d'animation :
//     1. Fondu de l'image de fond (0→1) pendant que la dernière
//        frame de la cinématique est encore visible
//     2. Musique du menu principal lancée dès l'activation
//     3. Slide du titre (entre dans le cadre depuis le haut)
//     4. Fondu séquentiel des éléments (texte de fin, boutons)
//   Boutons : "Menu principal" et "Crédits"
//
//   S'abonne à gestionnaireEnigmeBalance.OnEnigmeTerminee.
// ------------------------------------------------------------
// Setup Unity :
//   Canvas (Sort Order 99, Screen Space - Overlay)
//     └── CanvasGroup (alpha=0) ← assigner dans _groupeEcranFin
//         ├── Image plein écran (sprite Létallurgie-ending)
//         ├── titre                  ← RectTransform dans _rectTitre
//         │     └── CanvasGroup (alpha=0, optionnel) ← _groupeTitre
//         └── Éléments séquentiels (dans _elementsSequentiels) :
//               chaque objet doit avoir un CanvasGroup, alpha=0
//               Ordre recommandé : texte de fin, puis boutons
//                 ou conteneur boutons.
//
//   Laisser le Canvas actif au départ (le CanvasGroup à alpha 0
//   le rend invisible — l'abonnement à l'événement nécessite
//   que le script soit actif).
// ============================================================

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ecranFin : MonoBehaviour
{
    // ===================== INSPECTEUR =====================

    [Header("Canvas principal")]
    [Tooltip("CanvasGroup sur le GameObject racine du canvas_ecran_fin. " +
             "Alpha = 0 au départ.")]
    [SerializeField] private CanvasGroup _groupeEcranFin;

    [Header("Panel crédits")]
    [Tooltip("GameObject du panel crédits — désactivé par défaut.")]
    [SerializeField] private GameObject _panelCredits;

    [Header("Source de l'événement de fin")]
    [Tooltip("GestionnaireEnigmeBalance de la scène.")]
    [SerializeField] private gestionnaireEnigmeBalance _gestionnaire;

    [Header("Navigation")]
    [Tooltip("Nom exact de la scène du menu principal dans Build Settings.")]
    [SerializeField] private string _nomSceneMenuPrincipal = "scene_menu";

    [Header("Animation — fondu de fond")]
    [Tooltip("Durée du fondu de l'image de fond (0→1). " +
             "S'enchaîne sur la dernière frame de la cinématique.")]
    [SerializeField] private float _dureeFadeIn = 1.5f;
    [Tooltip("Délai avant le début du fondu (respiration narrative).")]
    [SerializeField] private float _delaiAvantFade = 0f;

    [Header("Animation — titre")]
    [Tooltip("RectTransform du titre. Glisse depuis le haut vers sa " +
             "position finale configurée dans le Canvas.")]
    [SerializeField] private RectTransform _rectTitre;
    [Tooltip("CanvasGroup sur le titre pour un fondu simultané au slide. " +
             "Optionnel — si non assigné, le titre est opaque dès le départ.")]
    [SerializeField] private CanvasGroup _groupeTitre;
    [Tooltip("Distance (pixels) au-dessus de sa position finale " +
             "où le titre commence son entrée.")]
    [SerializeField] private float _offsetDepartTitre = 250f;
    [Tooltip("Durée du glissement du titre (SmoothStep).")]
    [SerializeField] private float _dureeSlideTitre = 0.8f;
    [Tooltip("Délai entre la fin du fondu de fond et le début du slide.")]
    [SerializeField] private float _delaiAvantSlide = 0.15f;

    [Header("Animation — brouillard")]
    [Tooltip("RectTransform du brouillard. Glisse depuis le bas vers sa " +
             "position finale configurée dans le Canvas. " +
             "Démarre en parallèle du slide du titre.")]
    [SerializeField] private RectTransform _rectBrouillard;
    [Tooltip("CanvasGroup sur le brouillard pour un fondu simultané au slide. " +
             "Optionnel — si non assigné, le brouillard est opaque dès le départ.")]
    [SerializeField] private CanvasGroup _groupeBrouillard;
    [Tooltip("Distance (pixels) sous sa position finale où le brouillard commence.")]
    [SerializeField] private float _offsetDepartBrouillard = 200f;
    [Tooltip("Durée du glissement du brouillard (SmoothStep). " +
             "Plus long que le titre pour un effet traînant et atmosphérique.")]
    [SerializeField] private float _dureeSlideBrouillard = 2f;
    [Tooltip("Délai avant le début du slide du brouillard, à partir du moment " +
             "où le fondu de fond est terminé. Permet de décaler le brouillard " +
             "par rapport au titre.")]
    [SerializeField] private float _delaiAvantBrouillard = 0f;

    [Header("Animation — éléments séquentiels")]
    [Tooltip("Chaque CanvasGroup fade-in à la suite du titre.\n" +
             "Ordre recommandé : texte de fin, puis boutons " +
             "(ou conteneur boutons si tu veux qu'ils apparaissent ensemble).\n" +
             "Chaque objet doit avoir son propre CanvasGroup, alpha = 0 au départ.")]
    [SerializeField] private CanvasGroup[] _elementsSequentiels;
    [Tooltip("Durée du fondu d'entrée de chaque élément.")]
    [SerializeField] private float _dureeFadeElement = 0.5f;
    [Tooltip("Délai entre la fin d'un élément et le début du suivant.")]
    [SerializeField] private float _delaiEntreElements = 0.3f;

    // ===================== ÉTAT INTERNE =====================

    private Vector2 _posFinale;
    private Vector2 _posFinaleBrouillard;

    // ===================== UNITY =====================

    void Awake()
    {
        // Initialiser le canvas invisible et non-interactif
        if (_groupeEcranFin != null)
        {
            _groupeEcranFin.alpha          = 0f;
            _groupeEcranFin.interactable   = false;
            _groupeEcranFin.blocksRaycasts = false;
        }

        // Éléments séquentiels : tous invisibles au départ
        if (_elementsSequentiels != null)
            foreach (var cg in _elementsSequentiels)
                if (cg != null) cg.alpha = 0f;

        // Titre : sauvegarder sa position finale et le décaler vers le haut
        if (_rectTitre != null)
        {
            _posFinale = _rectTitre.anchoredPosition;
            _rectTitre.anchoredPosition = new Vector2(
                _posFinale.x,
                _posFinale.y + _offsetDepartTitre);
        }

        // Titre (CanvasGroup optionnel) : invisible si assigné
        if (_groupeTitre != null)
            _groupeTitre.alpha = 0f;

        // Brouillard : sauvegarder sa position finale et le décaler vers le bas
        if (_rectBrouillard != null)
        {
            _posFinaleBrouillard = _rectBrouillard.anchoredPosition;
            _rectBrouillard.anchoredPosition = new Vector2(
                _posFinaleBrouillard.x,
                _posFinaleBrouillard.y - _offsetDepartBrouillard);
        }

        // Brouillard (CanvasGroup optionnel) : invisible si assigné
        if (_groupeBrouillard != null)
            _groupeBrouillard.alpha = 0f;

        if (_panelCredits != null)
            _panelCredits.SetActive(false);
    }

    void OnEnable()
    {
        if (_gestionnaire != null)
            _gestionnaire.OnEnigmeTerminee += AfficherEcranFin;
    }

    void OnDisable()
    {
        if (_gestionnaire != null)
            _gestionnaire.OnEnigmeTerminee -= AfficherEcranFin;
    }

    // ===================== DÉCLENCHEMENT =====================

    private void AfficherEcranFin()
    {
        // Déverrouiller le curseur pour les boutons
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        // Lancer la musique du menu principal immédiatement
        gestionAudio.Instance?.JouerMusiquesIntro();

        StartCoroutine(SequenceAnimation());
    }

    // ===================== SÉQUENCE D'ANIMATION =====================

    private IEnumerator SequenceAnimation()
    {
        if (_groupeEcranFin == null) yield break;

        // ── 1. Délai narratif initial ────────────────────────────────
        if (_delaiAvantFade > 0f)
            yield return new WaitForSecondsRealtime(_delaiAvantFade);

        // ── 2. Fondu de l'image de fond (0 → 1) ─────────────────────
        // La dernière frame de la cinématique est encore visible derrière.
        // On fade par-dessus — si les images correspondent, c'est invisible.
        float t = 0f;
        while (t < _dureeFadeIn)
        {
            t += Time.unscaledDeltaTime;
            _groupeEcranFin.alpha = Mathf.Clamp01(t / _dureeFadeIn);
            yield return null;
        }
        _groupeEcranFin.alpha          = 1f;
        _groupeEcranFin.blocksRaycasts = true; // bloquer les clics accidentels

        // ── 3. Délai avant le slide du titre ────────────────────────
        if (_delaiAvantSlide > 0f)
            yield return new WaitForSecondsRealtime(_delaiAvantSlide);

        // ── 4a. Brouillard (parallèle au titre) ─────────────────────
        // Lancé en coroutine indépendante pour ne pas bloquer le titre.
        if (_rectBrouillard != null)
            StartCoroutine(SlideBrouillard());

        // ── 4b. Slide du titre (depuis le haut, SmoothStep) ─────────
        if (_rectTitre != null)
        {
            Vector2 posDepart = _rectTitre.anchoredPosition;
            t = 0f;
            while (t < _dureeSlideTitre)
            {
                t += Time.unscaledDeltaTime;
                float alpha = Mathf.SmoothStep(0f, 1f,
                    Mathf.Clamp01(t / _dureeSlideTitre));

                _rectTitre.anchoredPosition = Vector2.Lerp(
                    posDepart, _posFinale, alpha);

                // Fondu simultané si un CanvasGroup est assigné sur le titre
                if (_groupeTitre != null)
                    _groupeTitre.alpha = alpha;

                yield return null;
            }
            _rectTitre.anchoredPosition = _posFinale;
            if (_groupeTitre != null) _groupeTitre.alpha = 1f;
        }

        // ── 5. Fondu séquentiel des éléments ────────────────────────
        if (_elementsSequentiels != null)
        {
            foreach (var cg in _elementsSequentiels)
            {
                if (cg == null) continue;

                t = 0f;
                while (t < _dureeFadeElement)
                {
                    t += Time.unscaledDeltaTime;
                    cg.alpha = Mathf.Clamp01(t / _dureeFadeElement);
                    yield return null;
                }
                cg.alpha = 1f;

                if (_delaiEntreElements > 0f)
                    yield return new WaitForSecondsRealtime(_delaiEntreElements);
            }
        }

        // ── 6. Tout est visible — activer l'interactivité ────────────
        _groupeEcranFin.interactable = true;

        Debug.Log("[EcranFin] Séquence d'animation terminée.");
    }

    // ===================== BROUILLARD =====================

    /// <summary>
    /// Glisse le brouillard depuis le bas vers sa position finale.
    /// Lancé en parallèle du slide du titre.
    /// </summary>
    private IEnumerator SlideBrouillard()
    {
        if (_delaiAvantBrouillard > 0f)
            yield return new WaitForSecondsRealtime(_delaiAvantBrouillard);

        Vector2 posDepart = _rectBrouillard.anchoredPosition;
        float t = 0f;

        while (t < _dureeSlideBrouillard)
        {
            t += Time.unscaledDeltaTime;
            float alpha = Mathf.SmoothStep(0f, 1f,
                Mathf.Clamp01(t / _dureeSlideBrouillard));

            _rectBrouillard.anchoredPosition = Vector2.Lerp(
                posDepart, _posFinaleBrouillard, alpha);

            if (_groupeBrouillard != null)
                _groupeBrouillard.alpha = alpha;

            yield return null;
        }

        _rectBrouillard.anchoredPosition = _posFinaleBrouillard;
        if (_groupeBrouillard != null) _groupeBrouillard.alpha = 1f;
    }

    // ===================== BOUTONS =====================

    /// <summary>
    /// Appelé par le bouton "Menu principal".
    /// </summary>
    public void BoutonMenuPrincipal()
    {
        Debug.Log("[EcranFin] Retour au menu principal.");

        if (gestionEcranChargement.Instance != null)
            gestionEcranChargement.Instance.ChargerScene(_nomSceneMenuPrincipal);
        else
            SceneManager.LoadScene(_nomSceneMenuPrincipal);
    }

    /// <summary>
    /// Appelé par le bouton "Crédits".
    /// Affiche ou cache le panel crédits.
    /// </summary>
    public void BoutonCredits()
    {
        if (_panelCredits == null) return;
        _panelCredits.SetActive(!_panelCredits.activeSelf);

        Debug.Log($"[EcranFin] Panel crédits : " +
                  $"{(_panelCredits.activeSelf ? "affiché" : "caché")}.");
    }
}
