using UnityEngine;
using TMPro;

public class gestionSousTitre : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text texteInterlocuteur;
    [SerializeField] private TMP_Text textePropos;
    [SerializeField] private GameObject conteneurSousTitre;

    [Header("Parametres")]
    [SerializeField] private float dureeAffichage;

    private float tailleOriginaleInterlocuteur;
    private float tailleOriginalePropos;
    private Coroutine coroutineMasquage;

    void Start()
    {
        tailleOriginaleInterlocuteur = texteInterlocuteur.fontSize;
        tailleOriginalePropos = textePropos.fontSize;

        if (conteneurSousTitre != null)
            conteneurSousTitre.SetActive(false);
    }

    public void AfficherSousTitre(string interlocuteur, string propos,
        float dureeCustom = -1f)
    {
        if (coroutineMasquage != null)
            StopCoroutine(coroutineMasquage);

        texteInterlocuteur.text = interlocuteur + " :";
        textePropos.text = propos;

        AppliquerOptionsAccessibilite();

        conteneurSousTitre.SetActive(true);

        float duree = dureeCustom > 0 ? dureeCustom : dureeAffichage;
        coroutineMasquage = StartCoroutine(MasquerApresDelai(duree));
    }

    public void RafraichirOptions()
    {
        if (conteneurSousTitre != null
            && conteneurSousTitre.activeSelf)
        {
            AppliquerOptionsAccessibilite();
        }
    }

    private void AppliquerOptionsAccessibilite()
    {
        // Taille
        int indexTaille = PlayerPrefs.GetInt("tailleSousTitre", 1);
        float[] multiplicateurs = { 0.75f, 1f, 1.35f };

        if (indexTaille >= 0 && indexTaille < multiplicateurs.Length)
        {
            float mult = multiplicateurs[indexTaille];
            texteInterlocuteur.fontSize =
                tailleOriginaleInterlocuteur * mult;
            textePropos.fontSize =
                tailleOriginalePropos * mult;
        }

        // Couleur
        int indexCouleur = PlayerPrefs.GetInt("couleurSousTitre", 0);
        Color[] couleurs = { Color.white, Color.yellow, Color.cyan };

        if (indexCouleur >= 0 && indexCouleur < couleurs.Length)
        {
            texteInterlocuteur.color = couleurs[indexCouleur];
            textePropos.color = couleurs[indexCouleur];
        }
    }

    public void MasquerSousTitre()
    {
        if (coroutineMasquage != null)
            StopCoroutine(coroutineMasquage);

        conteneurSousTitre.SetActive(false);
    }

    private System.Collections.IEnumerator MasquerApresDelai(float delai)
    {
        yield return new WaitForSeconds(delai);
        conteneurSousTitre.SetActive(false);
    }
}