using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class gestionOptionsAudio : MonoBehaviour
{
    [Header("Volume General")]
    [SerializeField] private Slider sliderVolumeGeneral;

    [Header("Musique Ambiance")]
    [SerializeField] private Slider sliderMusiqueAmbiance;

    [Header("Effets Sonores Bouton")]
    [SerializeField] private Toggle toggleEffetsBouton;
    [SerializeField] private Slider sliderVolumeBouton;

    [Header("Effets Sonores Scroll")]
    [SerializeField] private Toggle toggleEffetsScroll;
    [SerializeField] private Slider sliderVolumeScroll;

    [Header("Effets Sonores Environnant")]
    [SerializeField] private Toggle toggleEffetsEnvironnant;
    [SerializeField] private Slider sliderVolumeEnvironnant;

    [Header("Dialogues")]
    [SerializeField] private Slider sliderVolumeDialogues;

    [Header("Cinematiques")]
    [SerializeField] private Slider sliderVolumeCinematiques;

    [Header("Sons de Pas")]
    [SerializeField] private Slider sliderVolumeSonsPas;

    [Header("Apparence Desactive")]
    [SerializeField] private float alphaDesactive;

    private bool enChargement = false;

    void Start()
    {
        ChargerPreferences();
        ConfigurerListeners();
        MettreAJourTousLesTextes();
        MettreAJourEtatsToggles();
    }

    void OnEnable()
    {
        ChargerPreferences();
        MettreAJourTousLesTextes();
        MettreAJourEtatsToggles();
    }

    private TMP_Text TrouverTexte(Slider slider)
    {
        Transform t = slider.transform.Find("pourcentage");
        if (t != null)
            return t.GetComponent<TMP_Text>();
        return null;
    }

    private gestionConfirmationOptions confirmationCache;

    private void MarquerModification()
    {
        if (enChargement) return;

        // Cache : avant, chaque drag de slider relancait un
        // FindFirstObjectByType (scan de scene a chaque frame de drag).
        if (confirmationCache == null)
            confirmationCache =
                FindFirstObjectByType<gestionConfirmationOptions>();
        if (confirmationCache != null)
            confirmationCache.MarquerModification();
    }

    private void ConfigurerListeners()
    {
        sliderVolumeGeneral.onValueChanged.AddListener(
            v => OnSliderChange("volumeGeneral", v, sliderVolumeGeneral));
        sliderMusiqueAmbiance.onValueChanged.AddListener(
            v => OnSliderChange("musiqueAmbiance", v, sliderMusiqueAmbiance));
        sliderVolumeBouton.onValueChanged.AddListener(
            v => OnSliderChange("volumeBouton", v, sliderVolumeBouton));
        sliderVolumeScroll.onValueChanged.AddListener(
            v => OnSliderChange("volumeScroll", v, sliderVolumeScroll));
        if (sliderVolumeEnvironnant != null)
            sliderVolumeEnvironnant.onValueChanged.AddListener(
                v => OnSliderChange("volumeEnvironnant", v, sliderVolumeEnvironnant));
        sliderVolumeDialogues.onValueChanged.AddListener(
            v => OnSliderChange("volumeDialogues", v, sliderVolumeDialogues));
        sliderVolumeCinematiques.onValueChanged.AddListener(
            v => OnSliderChange("volumeCinematiques", v, sliderVolumeCinematiques));
        sliderVolumeSonsPas.onValueChanged.AddListener(
            v => OnSliderChange("volumeSonsPas", v, sliderVolumeSonsPas));

        toggleEffetsBouton.onValueChanged.AddListener(
            actif => OnToggleChange("effetsBouton", actif,
                sliderVolumeBouton));
        toggleEffetsScroll.onValueChanged.AddListener(
            actif => OnToggleChange("effetsScroll", actif,
                sliderVolumeScroll));
        if (toggleEffetsEnvironnant != null)
            toggleEffetsEnvironnant.onValueChanged.AddListener(
                actif => OnToggleChange("effetsEnvironnant", actif,
                    sliderVolumeEnvironnant));
    }

    private void OnSliderChange(string cle, float valeur, Slider slider)
    {
        if (enChargement) return;

        PlayerPrefs.SetFloat(cle, valeur);

        TMP_Text texte = TrouverTexte(slider);
        if (texte != null)
            texte.text = Mathf.RoundToInt(valeur * 100) + "%";

        AppliquerVolumes();
        MarquerModification();
    }

    private void OnToggleChange(string cle, bool actif, Slider slider)
    {
        if (enChargement) return;

        PlayerPrefs.SetInt(cle, actif ? 1 : 0);
        ActiverDesactiverSlider(slider, actif);
        AppliquerVolumes();
        MarquerModification();
    }

    private void ActiverDesactiverSlider(Slider slider, bool actif)
    {
        if (slider == null) return;
        slider.interactable = actif;

        CanvasGroup groupeSlider = slider.GetComponent<CanvasGroup>();
        if (groupeSlider == null)
            groupeSlider = slider.gameObject.AddComponent<CanvasGroup>();

        groupeSlider.alpha = actif ? 1f : alphaDesactive;

        TMP_Text texte = TrouverTexte(slider);
        if (texte != null)
        {
            Color couleur = texte.color;
            couleur.a = actif ? 1f : alphaDesactive;
            texte.color = couleur;
        }
    }

    private void ChargerPreferences()
    {
        enChargement = true;

        // SetValueWithoutNotify : ne declenche aucun listener (ni les
        // notres, ni d'eventuels effets sonores accroches aux widgets)
        // quand on ne fait que REFLETER les prefs dans l'UI.
        sliderVolumeGeneral.SetValueWithoutNotify(
            PlayerPrefs.GetFloat("volumeGeneral", 1f));
        sliderMusiqueAmbiance.SetValueWithoutNotify(
            PlayerPrefs.GetFloat("musiqueAmbiance", 1f));
        sliderVolumeBouton.SetValueWithoutNotify(
            PlayerPrefs.GetFloat("volumeBouton", 1f));
        sliderVolumeScroll.SetValueWithoutNotify(
            PlayerPrefs.GetFloat("volumeScroll", 1f));
        if (sliderVolumeEnvironnant != null)
            sliderVolumeEnvironnant.SetValueWithoutNotify(
                PlayerPrefs.GetFloat("volumeEnvironnant", 1f));
        sliderVolumeDialogues.SetValueWithoutNotify(
            PlayerPrefs.GetFloat("volumeDialogues", 1f));
        sliderVolumeCinematiques.SetValueWithoutNotify(
            PlayerPrefs.GetFloat("volumeCinematiques", 1f));
        sliderVolumeSonsPas.SetValueWithoutNotify(
            PlayerPrefs.GetFloat("volumeSonsPas", 1f));

        toggleEffetsBouton.SetIsOnWithoutNotify(
            PlayerPrefs.GetInt("effetsBouton", 1) == 1);
        toggleEffetsScroll.SetIsOnWithoutNotify(
            PlayerPrefs.GetInt("effetsScroll", 1) == 1);
        if (toggleEffetsEnvironnant != null)
            toggleEffetsEnvironnant.SetIsOnWithoutNotify(
                PlayerPrefs.GetInt("effetsEnvironnant", 1) == 1);

        enChargement = false;
    }

    public void Reinitialiser()
    {
        enChargement = true;

        sliderVolumeGeneral.value = 1f;
        sliderMusiqueAmbiance.value = 1f;
        sliderVolumeBouton.value = 1f;
        sliderVolumeScroll.value = 1f;
        if (sliderVolumeEnvironnant != null)
            sliderVolumeEnvironnant.value = 1f;
        sliderVolumeDialogues.value = 1f;
        sliderVolumeCinematiques.value = 1f;
        sliderVolumeSonsPas.value = 1f;

        toggleEffetsBouton.isOn = true;
        toggleEffetsScroll.isOn = true;
        if (toggleEffetsEnvironnant != null)
            toggleEffetsEnvironnant.isOn = true;

        enChargement = false;

        PlayerPrefs.SetFloat("volumeGeneral", 1f);
        PlayerPrefs.SetFloat("musiqueAmbiance", 1f);
        PlayerPrefs.SetFloat("volumeBouton", 1f);
        PlayerPrefs.SetFloat("volumeScroll", 1f);
        PlayerPrefs.SetFloat("volumeEnvironnant", 1f);
        PlayerPrefs.SetFloat("volumeDialogues", 1f);
        PlayerPrefs.SetFloat("volumeCinematiques", 1f);
        PlayerPrefs.SetFloat("volumeSonsPas", 1f);
        PlayerPrefs.SetInt("effetsBouton", 1);
        PlayerPrefs.SetInt("effetsScroll", 1);
        PlayerPrefs.SetInt("effetsEnvironnant", 1);

        MettreAJourTousLesTextes();
        MettreAJourEtatsToggles();
        AppliquerVolumes();

        Debug.Log("Options audio reinitialisees");
    }

    public void RechargerPreferences()
    {
        ChargerPreferences();
        MettreAJourTousLesTextes();
        MettreAJourEtatsToggles();
        AppliquerVolumes();
    }

    private void MettreAJourTousLesTextes()
    {
        MettreAJourTexte(sliderVolumeGeneral);
        MettreAJourTexte(sliderMusiqueAmbiance);
        MettreAJourTexte(sliderVolumeBouton);
        MettreAJourTexte(sliderVolumeScroll);
        MettreAJourTexte(sliderVolumeEnvironnant);
        MettreAJourTexte(sliderVolumeDialogues);
        MettreAJourTexte(sliderVolumeCinematiques);
        MettreAJourTexte(sliderVolumeSonsPas);
    }

    private void MettreAJourTexte(Slider slider)
    {
        if (slider == null) return;
        TMP_Text texte = TrouverTexte(slider);
        if (texte != null)
            texte.text = Mathf.RoundToInt(slider.value * 100) + "%";
    }

    private void MettreAJourEtatsToggles()
    {
        ActiverDesactiverSlider(sliderVolumeBouton,
            toggleEffetsBouton.isOn);
        ActiverDesactiverSlider(sliderVolumeScroll,
            toggleEffetsScroll.isOn);
        if (toggleEffetsEnvironnant != null)
            ActiverDesactiverSlider(sliderVolumeEnvironnant,
                toggleEffetsEnvironnant.isOn);
    }

    private void AppliquerVolumes()
    {
        // "Bande son" (volumeGeneral) ne controle PLUS le volume maitre :
        // il pilote uniquement la musique des scenes, appliquee par
        // gestionAudio (qui lit la cle volumeGeneral). On ne touche donc
        // plus a AudioListener.volume ici. "Musique d'ambiance"
        // (musiqueAmbiance) pilote l'ambiance de piece, aussi via
        // gestionAudio (ObtenirSliderAmbiance).
        if (gestionAudio.Instance != null)
            gestionAudio.Instance.MettreAJourVolume();
    }

    public float ObtenirVolumeGeneral()
    {
        return PlayerPrefs.GetFloat("volumeGeneral", 1f);
    }

    public float ObtenirVolumeMusiqueAmbiance()
    {
        return PlayerPrefs.GetFloat("musiqueAmbiance", 1f);
    }

    public float ObtenirVolumeBouton()
    {
        if (PlayerPrefs.GetInt("effetsBouton", 1) == 0)
            return 0f;
        return PlayerPrefs.GetFloat("volumeBouton", 1f);
    }

    public float ObtenirVolumeScroll()
    {
        if (PlayerPrefs.GetInt("effetsScroll", 1) == 0)
            return 0f;
        return PlayerPrefs.GetFloat("volumeScroll", 1f);
    }

    public float ObtenirVolumeEnvironnant()
    {
        if (PlayerPrefs.GetInt("effetsEnvironnant", 1) == 0)
            return 0f;
        return PlayerPrefs.GetFloat("volumeEnvironnant", 1f);
    }

    public float ObtenirVolumeDialogues()
    {
        return PlayerPrefs.GetFloat("volumeDialogues", 1f);
    }

    public float ObtenirVolumeCinematiques()
    {
        return PlayerPrefs.GetFloat("volumeCinematiques", 1f);
    }

    public float ObtenirVolumeSonsPas()
    {
        return PlayerPrefs.GetFloat("volumeSonsPas", 1f);
    }
}