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

    void Start()
    {
        ChargerPreferences();
        ConfigurerListeners();
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
        toggleEffetsEnvironnant.onValueChanged.AddListener(
            actif => OnToggleChange("effetsEnvironnant", actif,
                sliderVolumeEnvironnant));
    }

    private void OnSliderChange(string cle, float valeur, Slider slider)
    {
        PlayerPrefs.SetFloat(cle, valeur);
        TMP_Text texte = TrouverTexte(slider);
        if (texte != null)
            texte.text = Mathf.RoundToInt(valeur * 100) + "%";
        AppliquerVolumes();
    }

    private void OnToggleChange(string cle, bool actif, Slider slider)
    {
        PlayerPrefs.SetInt(cle, actif ? 1 : 0);
        ActiverDesactiverSlider(slider, actif);
        AppliquerVolumes();
    }

    private void ActiverDesactiverSlider(Slider slider, bool actif)
    {
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
        sliderVolumeGeneral.value =
            PlayerPrefs.GetFloat("volumeGeneral", 1f);
        sliderMusiqueAmbiance.value =
            PlayerPrefs.GetFloat("musiqueAmbiance", 1f);
        sliderVolumeBouton.value =
            PlayerPrefs.GetFloat("volumeBouton", 1f);
        sliderVolumeScroll.value =
            PlayerPrefs.GetFloat("volumeScroll", 1f);
        sliderVolumeEnvironnant.value =
            PlayerPrefs.GetFloat("volumeEnvironnant", 1f);
        sliderVolumeDialogues.value =
            PlayerPrefs.GetFloat("volumeDialogues", 1f);
        sliderVolumeCinematiques.value =
            PlayerPrefs.GetFloat("volumeCinematiques", 1f);
        sliderVolumeSonsPas.value =
            PlayerPrefs.GetFloat("volumeSonsPas", 1f);

        toggleEffetsBouton.isOn =
            PlayerPrefs.GetInt("effetsBouton", 1) == 1;
        toggleEffetsScroll.isOn =
            PlayerPrefs.GetInt("effetsScroll", 1) == 1;
        toggleEffetsEnvironnant.isOn =
            PlayerPrefs.GetInt("effetsEnvironnant", 1) == 1;
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
        ActiverDesactiverSlider(sliderVolumeEnvironnant,
            toggleEffetsEnvironnant.isOn);
    }

    private void AppliquerVolumes()
    {
        float general = sliderVolumeGeneral.value;
        AudioListener.volume = general;
    }

    // ===== METHODES PUBLIQUES =====

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