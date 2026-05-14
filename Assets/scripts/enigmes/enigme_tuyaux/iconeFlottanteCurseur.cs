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
            if (Mouse.current != null && canvasRectTransform != null)
            {
                Vector2 positionSouris = Mouse.current.position.ReadValue();

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