using System.Collections;
using UnityEngine;
using TMPro;

public class compteurInventaireHud : MonoBehaviour
{
    [SerializeField] private TMP_Text texteNombreObjetsHud;

    [Header("Flash rouge")]
    [Tooltip("Durée en secondes pendant laquelle le compteur reste " +
             "rouge quand le nombre d'objets augmente.")]
    [SerializeField] private float _dureeFlashRouge = 2f;

    // ── État interne ──────────────────────────────────────────

    private Color _couleurNormale = Color.white;
    private int _totalPrecedent = 0;
    private Coroutine _coroutineFlash;

    // Guard : vrai si l'abonnement à onInventaireModifieHud est actif.
    // Nécessaire car OnEnable peut s'exécuter avant que gestionInventaire.Instance
    // soit prêt (timing au chargement de scène). Start() tente l'abonnement
    // en fallback si OnEnable a raté.
    private bool _estAbonne = false;

    // ── Unity ────────────────────────────────────────────────

    void Start()
    {
        // Sauvegarder la couleur d'origine du texte
        if (texteNombreObjetsHud != null)
            _couleurNormale = texteNombreObjetsHud.color;

        // Fallback : si OnEnable s'est exécuté avant que le singleton
        // soit disponible, on s'abonne ici (Start() est garanti après
        // tous les Awake(), donc le singleton est prêt).
        TenterAbonnement();
    }

    void OnEnable()
    {
        TenterAbonnement();
    }

    void OnDisable()
    {
        if (_estAbonne && gestionInventaire.Instance != null)
            gestionInventaire.Instance.onInventaireModifieHud -= MettreAJour;

        _estAbonne = false;

        // Remettre la couleur proprement si le composant se désactive en plein flash
        if (texteNombreObjetsHud != null)
            texteNombreObjetsHud.color = _couleurNormale;
    }

    // ── Abonnement ────────────────────────────────────────────

    /// <summary>
    /// Tente de s'abonner à onInventaireModifieHud.
    /// Idempotent : sans effet si déjà abonné ou si le singleton n'est pas prêt.
    /// </summary>
    private void TenterAbonnement()
    {
        if (_estAbonne) return;
        if (gestionInventaire.Instance == null) return;

        gestionInventaire.Instance.onInventaireModifieHud += MettreAJour;
        _estAbonne = true;

        // Synchroniser l'affichage avec l'état actuel de l'inventaire
        // (objets éventuellement ramassés dans une scène précédente).
        int total = gestionInventaire.Instance.ObtenirTotalObjets();
        _totalPrecedent = total;
        if (texteNombreObjetsHud != null)
            texteNombreObjetsHud.text = total.ToString();
    }

    // ── Mise à jour ───────────────────────────────────────────

    private void MettreAJour(int total)
    {
        if (texteNombreObjetsHud == null) return;

        texteNombreObjetsHud.text = total.ToString();

        // Flash rouge seulement si le nombre a augmenté
        if (total > _totalPrecedent)
        {
            if (_coroutineFlash != null)
                StopCoroutine(_coroutineFlash);
            _coroutineFlash = StartCoroutine(FlashRouge());
        }

        _totalPrecedent = total;
    }

    // ── Coroutine flash ───────────────────────────────────────

    private IEnumerator FlashRouge()
    {
        texteNombreObjetsHud.color = Color.red;
        yield return new WaitForSecondsRealtime(_dureeFlashRouge);
        texteNombreObjetsHud.color = _couleurNormale;
        _coroutineFlash = null;
    }
}
