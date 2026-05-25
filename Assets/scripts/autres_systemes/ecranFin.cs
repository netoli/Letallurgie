// ============================================================
// ecranFin.cs
// ------------------------------------------------------------
// Auteur      : Fanny Fortier
// Date créée  : 2026-05-24
// ------------------------------------------------------------
// Description :
//   Écran de fin affiché après la cinématique finale.
//   - Fade-in du sprite Letallurgie-ending (même image que
//     la dernière frame de cinematique4)
//   - Texte "Merci d'avoir joué"
//   - Bouton "Menu principal" → charge la scène menu
//   - Bouton "Crédits" → affiche/cache le panel crédits
//
//   S'abonne à gestionnaireEnigmeBalance.OnEnigmeTerminee
//   pour se déclencher automatiquement en fin d'énigme.
// ------------------------------------------------------------
// Setup Unity :
//   1. Créer un Canvas (Screen Space - Overlay, Sort Order 99)
//      → nommer "canvas_ecran_fin"
//   2. Ajouter un CanvasGroup sur ce même GameObject Canvas
//      → alpha = 0, Interactable = false, Blocks Raycasts = false
//   3. Enfants directs du canvas :
//        - Image plein écran avec le sprite Letallurgie-ending
//          (Anchor : stretch/stretch, Left/Right/Top/Bottom = 0)
//        - TMP_Text "Merci d'avoir joué" (positionner librement)
//        - Button "bouton_menu_principal"
//            → OnClick : ecranFin.BoutonMenuPrincipal()
//        - Button "bouton_credits"
//            → OnClick : ecranFin.BoutonCredits()
//        - GameObject "panel_credits" (désactivé par défaut)
//            → contient un ScrollRect avec le texte des crédits
//   4. Assigner toutes les références dans l'Inspector
//   5. Laisser le Canvas actif au départ (le CanvasGroup à
//      alpha 0 le rend invisible — l'abonnement à l'événement
//      nécessite que le script soit actif dès le départ)
// ============================================================

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ecranFin : MonoBehaviour
{
    // ===================== INSPECTEUR =====================

    [Header("Canvas principal")]
    [Tooltip("CanvasGroup sur le GameObject racine du canvas_ecran_fin. " +
             "Alpha = 0 au départ — le script le fait monter à 1 au fade-in.")]
    [SerializeField] private CanvasGroup _groupeEcranFin;

    [Header("Panel crédits")]
    [Tooltip("GameObject du panel crédits — désactivé par défaut, " +
             "activé/caché par le bouton Crédits.")]
    [SerializeField] private GameObject _panelCredits;

    [Header("Source de l'événement de fin")]
    [Tooltip("GestionnaireEnigmeBalance de la scène. " +
             "ecranFin s'abonne à son événement OnEnigmeTerminee.")]
    [SerializeField] private gestionnaireEnigmeBalance _gestionnaire;

    [Header("Navigation")]
    [Tooltip("Nom exact de la scène du menu principal dans Build Settings.")]
    [SerializeField] private string _nomSceneMenuPrincipal = "scene_menu";

    [Header("Animation")]
    [Tooltip("Durée en secondes du fade-in du fond et du texte.")]
    [SerializeField] private float _dureeFadeIn = 2f;
    [Tooltip("Délai avant le début du fade-in — laisse le temps à " +
             "l'écran noir post-cinématique de s'installer.")]
    [SerializeField] private float _delaiAvantFade = 0.5f;

    // ===================== UNITY =====================

    void Awake()
    {
        // Initialiser invisible et non-interactif
        if (_groupeEcranFin != null)
        {
            _groupeEcranFin.alpha = 0f;
            _groupeEcranFin.interactable = false;
            _groupeEcranFin.blocksRaycasts = false;
        }

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
        // S'assurer que le curseur est visible pour les boutons
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        StartCoroutine(SequenceFadeIn());
    }

    private IEnumerator SequenceFadeIn()
    {
        if (_groupeEcranFin == null) yield break;

        // Délai narratif avant le fade (respiration après la cinématique)
        if (_delaiAvantFade > 0f)
            yield return new WaitForSecondsRealtime(_delaiAvantFade);

        // Fade-in du fond + texte ensemble
        float t = 0f;
        while (t < _dureeFadeIn)
        {
            t += Time.unscaledDeltaTime;
            _groupeEcranFin.alpha = Mathf.Clamp01(t / _dureeFadeIn);
            yield return null;
        }

        _groupeEcranFin.alpha = 1f;

        // Activer l'interactivité seulement une fois le fade terminé
        // pour que le joueur ne clique pas accidentellement un bouton
        // pendant l'animation.
        _groupeEcranFin.interactable = true;
        _groupeEcranFin.blocksRaycasts = true;

        Debug.Log("[EcranFin] Écran de fin affiché.");
    }

    // ===================== BOUTONS =====================

    /// <summary>
    /// Appelé par le bouton "Menu principal".
    /// Passe par l'écran de chargement si disponible.
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
