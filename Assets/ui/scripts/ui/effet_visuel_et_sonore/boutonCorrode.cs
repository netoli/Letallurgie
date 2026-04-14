using UnityEngine;
using UnityEngine.UI;

public class boutonCorrode : MonoBehaviour
{
    [Header("Sprites normaux")]
    [SerializeField] private Sprite normalPropre;
    [SerializeField] private Sprite hoverPropre;
    [SerializeField] private Sprite appuyerPropre;
    [SerializeField] private Sprite selectPropre;

    [Header("Sprites corrodes")]
    [SerializeField] private Sprite normalCorrode;
    [SerializeField] private Sprite hoverCorrode;
    [SerializeField] private Sprite appuyerCorrode;
    [SerializeField] private Sprite selectCorrode;

    [Header("Seuil de corrosion (0 a 1)")]
    [SerializeField] private float seuilCorrosion;

    private Image image;
    private Button bouton;

    void Start()
    {
        image = GetComponent<Image>();
        bouton = GetComponent<Button>();
    }

    public void AppliquerCorrosion(float niveau)
    {
        if (image == null) image = GetComponent<Image>();
        if (bouton == null) bouton = GetComponent<Button>();

        SpriteState etat = bouton.spriteState;

        if (niveau < seuilCorrosion)
        {
            image.sprite = normalPropre;
            etat.highlightedSprite = hoverPropre;
            etat.pressedSprite = appuyerPropre;
            etat.selectedSprite = selectPropre;
        }
        else
        {
            image.sprite = normalCorrode;
            etat.highlightedSprite = hoverCorrode;
            etat.pressedSprite = appuyerCorrode;
            etat.selectedSprite = selectCorrode;
        }

        bouton.spriteState = etat;
    }

    [ContextMenu("Tester Propre")]
    public void TesterPropre()
    {
        AppliquerCorrosion(0f);
    }

    [ContextMenu("Tester Corrode")]
    public void TesterCorrode()
    {
        AppliquerCorrosion(1f);
    }
}