using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class iconeFlottanteCurseur : MonoBehaviour
{
    [Header("Visuels")]
    [SerializeField] private CanvasGroup groupeCanvas;
    [SerializeField] private Image iconeObjet;
    [SerializeField] private TMP_Text texteRotation;

    [Header("Cible a suivre")]
    [Tooltip("Si renseigne, l'icone se place a cote de ce RectTransform " +
        "(typiquement le pointeur_centre / reticule visuel). Sinon, " +
        "l'icone suit le curseur OS quand la souris est libre, ou le " +
        "centre de l'ecran quand la souris est verrouillee. " +
        "RECOMMANDE : glisser le pointeur_centre ici pour que l'icone " +
        "reste toujours alignee avec le reticule, peu importe l'etat " +
        "de la souris.")]
    [SerializeField] private RectTransform pointeurCentreUI;

    [Header("Offset par rapport au curseur")]
    [SerializeField] private Vector2 offsetPixels;

    [Header("Fade")]
    [SerializeField] private float vitesseFade;

    [Header("Canvas parent")]
    [SerializeField] private Canvas canvasParent;

    private RectTransform rectTransform;
    private RectTransform canvasRectTransform;
    private orientationTuyau rotationActuelle;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (canvasParent != null)
        {
            canvasRectTransform = canvasParent.GetComponent<RectTransform>();
        }
        if (groupeCanvas != null)
        {
            groupeCanvas.alpha = 0f;
            // Empeche l'icone flottante de capturer les clics : sans
            // ca, elle bloque les clics destines aux slots d'inventaire
            // (le curseur Windows est souvent sur l'icone elle-meme
            // a cause de l'offset minime), rendant impossible la
            // deselection par re-clic.
            groupeCanvas.blocksRaycasts = false;
            groupeCanvas.interactable = false;
        }

        // Securite supplementaire : forcer raycastTarget = false sur
        // tous les Graphics enfants (Image, Text) pour s'assurer qu'ils
        // sont transparents aux clics meme si le CanvasGroup est mal
        // configure.
        foreach (var g in GetComponentsInChildren<Graphic>(true))
        {
            g.raycastTarget = false;
        }
    }

    void OnEnable()
    {
        if (gestionSelectionInventaire.Instance != null)
        {
            gestionSelectionInventaire.Instance.onSelectionChangee
                += SurSelectionChangee;
        }
    }

    void OnDisable()
    {
        if (gestionSelectionInventaire.Instance != null)
        {
            gestionSelectionInventaire.Instance.onSelectionChangee
                -= SurSelectionChangee;
        }
    }

    void Update()
    {
        if (groupeCanvas != null && groupeCanvas.alpha > 0f)
        {
            if (canvasRectTransform != null)
            {
                // PRIORITE 1 : si un pointeurCentreUI est reference dans
                // l'Inspector, l'icone se cale sur sa position (typiquement
                // le pointeur_centre visuel). C'est le comportement le
                // plus propre : peu importe que la souris soit verrouillee
                // ou libre, l'icone reste alignee avec le reticule visuel.
                if (pointeurCentreUI != null)
                {
                    rectTransform.anchoredPosition =
                        pointeurCentreUI.anchoredPosition + offsetPixels;
                }
                // PRIORITE 2 : fallback sur la souris OS. Si verrouillee
                // (mode FPS), on calque sur le centre de l'ecran. Sinon
                // sur la position OS.
                else if (Mouse.current != null)
                {
                    Vector2 positionSouris;
                    if (Cursor.lockState == CursorLockMode.Locked)
                        positionSouris = new Vector2(
                            Screen.width * 0.5f, Screen.height * 0.5f);
                    else
                        positionSouris = Mouse.current.position.ReadValue();

                    Vector2 positionLocale;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        canvasRectTransform,
                        positionSouris,
                        canvasParent.renderMode == RenderMode.ScreenSpaceOverlay
                            ? null
                            : canvasParent.worldCamera,
                        out positionLocale);

                    rectTransform.anchoredPosition = positionLocale + offsetPixels;
                }
            }
        }

        if (groupeCanvas != null)
        {
            bool aSelection = gestionSelectionInventaire.Instance != null
                && gestionSelectionInventaire.Instance.AQuelqueChoseDeSelectionne();

            float cible = aSelection ? 1f : 0f;
            groupeCanvas.alpha = Mathf.MoveTowards(
                groupeCanvas.alpha, cible, Time.deltaTime * vitesseFade);
        }
    }

    private void SurSelectionChangee(objetInventaire nouvelle)
    {
        if (nouvelle != null && iconeObjet != null)
        {
            iconeObjet.sprite = nouvelle.icone;
        }
    }

    public void MettreAJourRotation(orientationTuyau rotation)
    {
        rotationActuelle = rotation;
        if (texteRotation != null)
        {
            texteRotation.text = rotation.EnDegres() + "°";
        }

        // Faire tourner visuellement l'icône
        if (iconeObjet != null)
        {
            iconeObjet.rectTransform.localEulerAngles =
                new Vector3(0f, 0f, rotation.EnDegres());
        }
    }
}