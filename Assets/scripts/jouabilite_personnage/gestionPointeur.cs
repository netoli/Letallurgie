using UnityEngine;
using UnityEngine.UI;

public class gestionPointeur : MonoBehaviour
{
    public enum EtatPointeur
    {
        Defaut,
        Interactif,
        PNJ,
        Mecanique
    }

    [Header("Sprites des etats")]
    public Sprite spriteDefaut;
    public Sprite spriteInteractif;
    public Sprite spritePNJ;
    public Sprite spriteMecanique;

    [Header("Tailles des etats")]
    public float tailleDefaut;
    public float tailleInteractif;
    public float taillePNJ;
    public float tailleMecanique;

    [Header("References")]
    public Image imagePointeur;
    public RectTransform rectPointeur;

    private EtatPointeur etatActuel;

    void Start()
    {
        ChangerEtat(EtatPointeur.Defaut);
    }

    public void ChangerEtat(EtatPointeur nouvelEtat)
    {
        if (etatActuel == nouvelEtat) return;
        etatActuel = nouvelEtat;

        switch (nouvelEtat)
        {
            case EtatPointeur.Defaut:
                AppliquerApparence(spriteDefaut, tailleDefaut);
                break;
            case EtatPointeur.Interactif:
                AppliquerApparence(spriteInteractif, tailleInteractif);
                break;
            case EtatPointeur.PNJ:
                AppliquerApparence(spritePNJ, taillePNJ);
                break;
            case EtatPointeur.Mecanique:
                AppliquerApparence(spriteMecanique, tailleMecanique);
                break;
        }
    }

    private void AppliquerApparence(Sprite sprite, float taille)
    {
        imagePointeur.sprite = sprite;
        rectPointeur.sizeDelta = new Vector2(taille, taille);
    }

    public EtatPointeur ObtenirEtatActuel()
    {
        return etatActuel;
    }
}