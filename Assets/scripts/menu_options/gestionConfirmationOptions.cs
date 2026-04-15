using UnityEngine;

public class gestionConfirmationOptions : MonoBehaviour
{
    private bool optionsModifiees = false;

    // Audio
    private float ancienVolumeGeneral;
    private float ancienneMusiqueAmbiance;
    private float ancienVolumeBouton;
    private float ancienVolumeScroll;
    private float ancienVolumeEnvironnant;
    private float ancienVolumeDialogues;
    private float ancienVolumeCinematiques;
    private float ancienVolumeSonsPas;
    private int ancienEffetsBouton;
    private int ancienEffetsScroll;
    private int ancienEffetsEnvironnant;

    // Graphique
    private int ancienneResolution;
    private int ancienModeAffichage;
    private float ancienneLuminosite;
    private float ancienneVignette;
    private float ancienBrouillard;

    // Controle
    private float ancienneSensibilite;
    private int ancienInverserV;
    private int ancienInverserH;
    private float ancienFov;

    // Accessibilite
    private int ancienGlitch;
    private int ancienneTailleUI;
    private int ancienneTailleBouton;
    private int ancienneTailleSousTitre;
    private int ancienneCouleurSousTitre;
    private int ancienIndicateurCorrosion;
    private int ancienneVitesseCorrosion;
    private int ancienneDescriptionAudio;

    public void SauvegarderEtatActuel()
    {
        // Audio
        ancienVolumeGeneral =
            PlayerPrefs.GetFloat("volumeGeneral", 1f);
        ancienneMusiqueAmbiance =
            PlayerPrefs.GetFloat("musiqueAmbiance", 1f);
        ancienVolumeBouton =
            PlayerPrefs.GetFloat("volumeBouton", 1f);
        ancienVolumeScroll =
            PlayerPrefs.GetFloat("volumeScroll", 1f);
        ancienVolumeEnvironnant =
            PlayerPrefs.GetFloat("volumeEnvironnant", 1f);
        ancienVolumeDialogues =
            PlayerPrefs.GetFloat("volumeDialogues", 1f);
        ancienVolumeCinematiques =
            PlayerPrefs.GetFloat("volumeCinematiques", 1f);
        ancienVolumeSonsPas =
            PlayerPrefs.GetFloat("volumeSonsPas", 1f);
        ancienEffetsBouton =
            PlayerPrefs.GetInt("effetsBouton", 1);
        ancienEffetsScroll =
            PlayerPrefs.GetInt("effetsScroll", 1);
        ancienEffetsEnvironnant =
            PlayerPrefs.GetInt("effetsEnvironnant", 1);

        // Graphique
        ancienneResolution =
            PlayerPrefs.GetInt("resolution", 2);
        ancienModeAffichage =
            PlayerPrefs.GetInt("modeAffichage", 0);
        ancienneLuminosite =
            PlayerPrefs.GetFloat("luminosite", 0.5f);
        ancienneVignette =
            PlayerPrefs.GetFloat("intensiteVignette", 0.5f);
        ancienBrouillard =
            PlayerPrefs.GetFloat("intensiteBrouillard", 1f);

        // Controle
        ancienneSensibilite =
            PlayerPrefs.GetFloat("sensibiliteCamera", 1f);
        ancienInverserV =
            PlayerPrefs.GetInt("inverserAxeVertical", 0);
        ancienInverserH =
            PlayerPrefs.GetInt("inverserAxeHorizontal", 0);
        ancienFov =
            PlayerPrefs.GetFloat("champDeVision", 90f);

        // Accessibilite
        ancienGlitch =
            PlayerPrefs.GetInt("glitchTexte", 1);
        ancienneTailleUI =
            PlayerPrefs.GetInt("tailleTexteUI", 1);
        ancienneTailleBouton =
            PlayerPrefs.GetInt("tailleTexteBouton", 1);
        ancienneTailleSousTitre =
            PlayerPrefs.GetInt("tailleSousTitre", 1);
        ancienneCouleurSousTitre =
            PlayerPrefs.GetInt("couleurSousTitre", 0);
        ancienIndicateurCorrosion =
            PlayerPrefs.GetInt("indicateurCorrosion", 0);
        ancienneVitesseCorrosion =
            PlayerPrefs.GetInt("vitesseCorrosion", 1);
        ancienneDescriptionAudio =
            PlayerPrefs.GetInt("descriptionAudio", 0);

        optionsModifiees = false;
    }

    public void MarquerModification()
    {
        optionsModifiees = true;
    }

    public void ConfirmerChangements()
    {
        PlayerPrefs.Save();
        SauvegarderEtatActuel();
        optionsModifiees = false;
        Debug.Log("Options confirmees");
    }

    public void AnnulerChangements()
    {
        if (!optionsModifiees) return;

        // Audio
        PlayerPrefs.SetFloat("volumeGeneral", ancienVolumeGeneral);
        PlayerPrefs.SetFloat("musiqueAmbiance", ancienneMusiqueAmbiance);
        PlayerPrefs.SetFloat("volumeBouton", ancienVolumeBouton);
        PlayerPrefs.SetFloat("volumeScroll", ancienVolumeScroll);
        PlayerPrefs.SetFloat("volumeEnvironnant", ancienVolumeEnvironnant);
        PlayerPrefs.SetFloat("volumeDialogues", ancienVolumeDialogues);
        PlayerPrefs.SetFloat("volumeCinematiques", ancienVolumeCinematiques);
        PlayerPrefs.SetFloat("volumeSonsPas", ancienVolumeSonsPas);
        PlayerPrefs.SetInt("effetsBouton", ancienEffetsBouton);
        PlayerPrefs.SetInt("effetsScroll", ancienEffetsScroll);
        PlayerPrefs.SetInt("effetsEnvironnant", ancienEffetsEnvironnant);

        // Graphique
        PlayerPrefs.SetInt("resolution", ancienneResolution);
        PlayerPrefs.SetInt("modeAffichage", ancienModeAffichage);
        PlayerPrefs.SetFloat("luminosite", ancienneLuminosite);
        PlayerPrefs.SetFloat("intensiteVignette", ancienneVignette);
        PlayerPrefs.SetFloat("intensiteBrouillard", ancienBrouillard);

        // Controle
        PlayerPrefs.SetFloat("sensibiliteCamera", ancienneSensibilite);
        PlayerPrefs.SetInt("inverserAxeVertical", ancienInverserV);
        PlayerPrefs.SetInt("inverserAxeHorizontal", ancienInverserH);
        PlayerPrefs.SetFloat("champDeVision", ancienFov);

        // Accessibilite
        PlayerPrefs.SetInt("glitchTexte", ancienGlitch);
        PlayerPrefs.SetInt("tailleTexteUI", ancienneTailleUI);
        PlayerPrefs.SetInt("tailleTexteBouton", ancienneTailleBouton);
        PlayerPrefs.SetInt("tailleSousTitre", ancienneTailleSousTitre);
        PlayerPrefs.SetInt("couleurSousTitre", ancienneCouleurSousTitre);
        PlayerPrefs.SetInt("indicateurCorrosion", ancienIndicateurCorrosion);
        PlayerPrefs.SetInt("vitesseCorrosion", ancienneVitesseCorrosion);
        PlayerPrefs.SetInt("descriptionAudio", ancienneDescriptionAudio);

        optionsModifiees = false;
        Debug.Log("Options annulees");
    }

    public bool ADesModifications()
    {
        return optionsModifiees;
    }
}