using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
// flouArrierePlanPopup.cs
// ------------------------------------------------------------
// Floute l'arriere-plan quand un canvas de confirmation s'ouvre
// (canvas_reprise_enigme, canvas_confirmer_quitter,
//  canvas_confirmer_retourner_au_menu_principal,
//  canvas_confirmer_reinitialisation).
//
// TECHNIQUE : a l'ouverture, on cache le contenu du popup pendant
// UNE frame (CanvasGroup alpha 0), on capture l'ecran complet
// (ScreenCapture -> RenderTexture, INCLUT l'UI derriere, ex. le
// menu options), on le reduit puis re-agrandit (downscale 1/4 ->
// 1/8 -> 1/4 : le filtrage bilineaire fait office de flou gaussien
// bon marche), et on affiche le resultat dans une RawImage etiree
// plein ecran inseree comme PREMIER enfant du canvas — donc rendue
// derriere tout le contenu du popup. Capture statique : parfait
// pour un popup modal (le fond ne bouge pas, le jeu est en pause).
//
// AUCUNE manip Inspector : appliqueOptionsAuChargement pose ce
// composant automatiquement sur les 4 canvas a chaque scene.
// ============================================================

public class flouArrierePlanPopup : MonoBehaviour
{
    // Facteurs de reduction de la chaine de flou.
    private const int REDUCTION_1 = 4;
    private const int REDUCTION_2 = 8;

    // Teinte appliquee a la capture floutee (leger assombrissement
    // pour detacher le popup du fond).
    private static readonly Color TEINTE_FOND =
        new Color(0.72f, 0.70f, 0.68f, 1f);

    private RawImage imageFlou;
    private RenderTexture rtAffichage;
    private CanvasGroup groupe;
    private Coroutine captureEnCours;
    private float alphaAvantCapture;

    void OnEnable()
    {
        // Le contenu reste invisible UNE frame, le temps de capturer
        // l'ecran sans le popup dedans.
        groupe = GetComponent<CanvasGroup>();
        if (groupe == null)
            groupe = gameObject.AddComponent<CanvasGroup>();

        if (captureEnCours != null)
            StopCoroutine(captureEnCours);
        captureEnCours = StartCoroutine(CapturerEtFlouter());
    }

    void OnDisable()
    {
        // Si le popup se ferme PENDANT la frame de capture, la coroutine
        // meurt avant d'avoir restaure l'alpha : on le remet nous-memes,
        // sinon le popup resterait invisible a sa prochaine ouverture.
        if (captureEnCours != null)
        {
            if (groupe != null)
                groupe.alpha = alphaAvantCapture;
            captureEnCours = null;
        }
        if (imageFlou != null)
            imageFlou.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (rtAffichage != null)
        {
            rtAffichage.Release();
            Destroy(rtAffichage);
            rtAffichage = null;
        }
    }

    private IEnumerator CapturerEtFlouter()
    {
        alphaAvantCapture = groupe.alpha;
        float alphaOriginal = alphaAvantCapture;
        groupe.alpha = 0f;

        // Fin de frame : l'ecran est rendu (popup invisible dedans).
        yield return new WaitForEndOfFrame();

        int largeur = Mathf.Max(Screen.width, 8);
        int hauteur = Mathf.Max(Screen.height, 8);

        RenderTexture rtPlein = RenderTexture.GetTemporary(
            largeur, hauteur, 0);
        ScreenCapture.CaptureScreenshotIntoRenderTexture(rtPlein);

        RenderTexture rtA = RenderTexture.GetTemporary(
            largeur / REDUCTION_1, hauteur / REDUCTION_1, 0);
        RenderTexture rtB = RenderTexture.GetTemporary(
            largeur / REDUCTION_2, hauteur / REDUCTION_2, 0);

        // Down -> down -> up : chaque Blit bilineaire lisse l'image.
        Graphics.Blit(rtPlein, rtA);
        Graphics.Blit(rtA, rtB);
        Graphics.Blit(rtB, rtA);

        // Copie dans une RT persistante (les Temporary repartent au pool).
        if (rtAffichage == null
            || rtAffichage.width != largeur / REDUCTION_1
            || rtAffichage.height != hauteur / REDUCTION_1)
        {
            if (rtAffichage != null)
            {
                rtAffichage.Release();
                Destroy(rtAffichage);
            }
            rtAffichage = new RenderTexture(
                largeur / REDUCTION_1, hauteur / REDUCTION_1, 0);
            rtAffichage.filterMode = FilterMode.Bilinear;
        }
        Graphics.Blit(rtA, rtAffichage);

        RenderTexture.ReleaseTemporary(rtPlein);
        RenderTexture.ReleaseTemporary(rtA);
        RenderTexture.ReleaseTemporary(rtB);

        AfficherFond();

        // Reaffiche le contenu du popup (valeur d'origine : si un autre
        // script anime ce CanvasGroup, on lui rend la main telle quelle).
        groupe.alpha = alphaOriginal;
        captureEnCours = null;
    }

    private void AfficherFond()
    {
        if (imageFlou == null)
        {
            GameObject go = new GameObject("fond_floute",
                typeof(RectTransform), typeof(CanvasRenderer),
                typeof(RawImage));
            go.layer = gameObject.layer;
            go.transform.SetParent(transform, false);
            // Premier enfant : rendu DERRIERE tout le contenu du popup.
            go.transform.SetSiblingIndex(0);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            imageFlou = go.GetComponent<RawImage>();
            imageFlou.color = TEINTE_FOND;
            // Bloque les clics vers l'arriere-plan (popup modal).
            imageFlou.raycastTarget = true;
        }

        imageFlou.texture = rtAffichage;

        // Selon l'API graphique (Metal sur Mac), la capture peut etre
        // verticalement inversee : on retourne les UV dans ce cas.
        if (SystemInfo.graphicsUVStartsAtTop)
            imageFlou.uvRect = new Rect(0f, 1f, 1f, -1f);
        else
            imageFlou.uvRect = new Rect(0f, 0f, 1f, 1f);

        imageFlou.gameObject.SetActive(true);
    }
}
