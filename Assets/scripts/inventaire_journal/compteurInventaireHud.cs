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

    // ── Unity ────────────────────────────────────────────────

    void Start()
    {
        // Sauvegarder la couleur d'origine du texte
        if (texteNombreObjetsHud != null)
            _couleurNormale = texteNombreObjetsHud.color;
    }

    void OnEnable()
    {
        if (gestionInventaire.Instance != null)
        {
            gestionInventaire.Instance.onInventaireModifieHud += MettreAJour;

            // Initialisation immédiate sans déclencher le flash
            // (les objets déjà ramassés avant l'activation ne comptent pas)
            _totalPrecedent = gestionInventaire.Instance.ObtenirTotalObjets();

            if (texteNombreObjetsHud != null)
                texteNombreObjetsHud.text = _totalPrecedent.ToString();
        }
    }

    void OnDisable()
    {
        if (gestionInventaire.Instance != null)
            gestionInventaire.Instance.onInventaireModifieHud -= MettreAJour;

        // Remettre la couleur proprement si le composant se désactive
        // en plein flash
        if (texteNombreObjetsHud != null)
            texteNombreObjetsHud.color = _couleurNormale;
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
