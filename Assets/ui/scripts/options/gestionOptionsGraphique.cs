using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TMPro;
using System.Collections.Generic;

public class gestionOptionsGraphiques : MonoBehaviour
{
    [Header("Dropdowns")]
    [SerializeField] private TMP_Dropdown dropdownResolution;
    [SerializeField] private TMP_Dropdown dropdownModeAffichage;

    [Header("Sliders")]
    [SerializeField] private Slider sliderLuminosite;
    [SerializeField] private Slider sliderIntensiteVignette;
    [SerializeField] private Slider sliderIntensiteBrouillard;

    [Header("Post Processing")]
    [SerializeField] private Volume globalVolume;

    private ColorAdjustments colorAdjustments;
    private Vignette vignette;
    private float vignetteOriginale;
    private float luminositeOriginale;
    private float luminositeSliderDefaut;
    private float vignetteSliderDefaut;
    private bool enChargement = false;

    void Start()
    {
        RecupererEffetsPostProcessing();
        ConfigurerDropdownResolution();
        ConfigurerDropdownModeAffichage();
        ChargerPreferences();
        ConfigurerListeners();
        MettreAJourTousLesTextes();
        AppliquerEffets();
    }

    void OnEnable()
    {
        ChargerPreferences();
        MettreAJourTousLesTextes();
        AppliquerEffets();
    }

    private void RecupererEffetsPostProcessing()
    {
        if (globalVolume == null) return;

        globalVolume.profile.TryGet(out colorAdjustments);
        globalVolume.profile.TryGet(out vignette);

        if (vignette != null)
        {
            vignetteOriginale = vignette.intensity.value;
            vignetteSliderDefaut = vignetteOriginale;
        }

        if (colorAdjustments != null)
        {
            luminositeOriginale = colorAdjustments.postExposure.value;
            luminositeSliderDefaut =
                0.5f + (luminositeOriginale / 4f);
        }
    }

    private void MarquerModification()
    {
        if (enChargement) return;

        gestionConfirmationOptions confirmation =
            FindFirstObjectByType<gestionConfirmationOptions>();
        if (confirmation != null)
            confirmation.MarquerModification();
    }

    private void ConfigurerDropdownResolution()
    {
        dropdownResolution.ClearOptions();

        List<string> options = new List<string>
        {
            "1280 x 720",
            "1600 x 900",
            "1920 x 1080"
        };

        dropdownResolution.AddOptions(options);

        int indexSauvegarde = PlayerPrefs.GetInt("resolution", 2);
        dropdownResolution.value = indexSauvegarde;
        dropdownResolution.RefreshShownValue();
    }

    private void ConfigurerDropdownModeAffichage()
    {
        dropdownModeAffichage.ClearOptions();

        List<string> options = new List<string>
        {
            "Plein ecran",
            "Fenetre",
            "Fenetre sans bordure"
        };

        dropdownModeAffichage.AddOptions(options);

        int modeSauvegarde = PlayerPrefs.GetInt("modeAffichage", 0);
        dropdownModeAffichage.value = modeSauvegarde;
        dropdownModeAffichage.RefreshShownValue();
    }

    private void ConfigurerListeners()
    {
        dropdownResolution.onValueChanged.AddListener(OnResolutionChange);
        dropdownModeAffichage.onValueChanged.AddListener(
            OnModeAffichageChange);

        sliderLuminosite.onValueChanged.AddListener(
            v => OnSliderChange("luminosite", v, sliderLuminosite));
        sliderIntensiteVignette.onValueChanged.AddListener(
            v => OnSliderChange("intensiteVignette", v,
                sliderIntensiteVignette));
        sliderIntensiteBrouillard.onValueChanged.AddListener(
            v => OnSliderChange("intensiteBrouillard", v,
                sliderIntensiteBrouillard));
    }

    private void OnResolutionChange(int index)
    {
        if (enChargement) return;

        switch (index)
        {
            case 0:
                Screen.SetResolution(1280, 720, Screen.fullScreenMode);
                break;
            case 1:
                Screen.SetResolution(1600, 900, Screen.fullScreenMode);
                break;
            case 2:
                Screen.SetResolution(1920, 1080, Screen.fullScreenMode);
                break;
        }

        PlayerPrefs.SetInt("resolution", index);
        MarquerModification();
    }

    private void OnModeAffichageChange(int index)
    {
        if (enChargement) return;

        switch (index)
        {
            case 0:
                Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
                break;
            case 1:
                Screen.fullScreenMode = FullScreenMode.Windowed;
                break;
            case 2:
                Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
                break;
        }

        PlayerPrefs.SetInt("modeAffichage", index);
        MarquerModification();
    }

    private void OnSliderChange(string cle, float valeur, Slider slider)
    {
        if (enChargement) return;

        PlayerPrefs.SetFloat(cle, valeur);

        TMP_Text texte = TrouverTexte(slider);
        if (texte != null)
            texte.text = Mathf.RoundToInt(valeur * 100) + "%";

        AppliquerEffets();
        MarquerModification();
    }

    private void AppliquerEffets()
    {
        if (colorAdjustments != null)
        {
            float lum = sliderLuminosite.value;
            colorAdjustments.postExposure.value = (lum - 0.5f) * 4f;
        }

        if (vignette != null)
        {
            vignette.intensity.value = sliderIntensiteVignette.value;
        }

        AppliquerIntensiteBrouillard(sliderIntensiteBrouillard.value);
    }

    private void AppliquerIntensiteBrouillard(float intensite)
    {
        ParticleSystem[] brouillards =
            FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None);

        foreach (ParticleSystem ps in brouillards)
        {
            if (ps.gameObject.name.Contains("brouillard"))
            {
                var emission = ps.emission;
                emission.rateOverTimeMultiplier = intensite;
            }
        }
    }

    private void ChargerPreferences()
    {
        enChargement = true;

        float lum = PlayerPrefs.GetFloat("luminosite", -1f);
        float vig = PlayerPrefs.GetFloat("intensiteVignette", -1f);

        if (lum < 0)
            sliderLuminosite.value = luminositeSliderDefaut;
        else
            sliderLuminosite.value = lum;

        if (vig < 0)
            sliderIntensiteVignette.value = vignetteSliderDefaut;
        else
            sliderIntensiteVignette.value = vig;

        sliderIntensiteBrouillard.value =
            PlayerPrefs.GetFloat("intensiteBrouillard", 1f);

        dropdownResolution.value =
            PlayerPrefs.GetInt("resolution", 2);
        dropdownResolution.RefreshShownValue();

        dropdownModeAffichage.value =
            PlayerPrefs.GetInt("modeAffichage", 0);
        dropdownModeAffichage.RefreshShownValue();

        enChargement = false;
    }

    public void Reinitialiser()
    {
        enChargement = true;

        dropdownResolution.value = 2;
        dropdownModeAffichage.value = 0;
        sliderLuminosite.value = luminositeSliderDefaut;
        sliderIntensiteVignette.value = vignetteSliderDefaut;
        sliderIntensiteBrouillard.value = 1f;

        enChargement = false;

        PlayerPrefs.SetInt("resolution", 2);
        PlayerPrefs.SetInt("modeAffichage", 0);
        PlayerPrefs.SetFloat("luminosite", luminositeSliderDefaut);
        PlayerPrefs.SetFloat("intensiteVignette", vignetteSliderDefaut);
        PlayerPrefs.SetFloat("intensiteBrouillard", 1f);

        Screen.SetResolution(1920, 1080,
            FullScreenMode.ExclusiveFullScreen);

        dropdownResolution.RefreshShownValue();
        dropdownModeAffichage.RefreshShownValue();
        MettreAJourTousLesTextes();
        AppliquerEffets();

        Debug.Log("Options graphiques reinitialisees");
    }

    public void RechargerPreferences()
    {
        ChargerPreferences();
        MettreAJourTousLesTextes();
        AppliquerEffets();
    }

    private void MettreAJourTousLesTextes()
    {
        MettreAJourTexte(sliderLuminosite);
        MettreAJourTexte(sliderIntensiteVignette);
        MettreAJourTexte(sliderIntensiteBrouillard);
    }

    private void MettreAJourTexte(Slider slider)
    {
        TMP_Text texte = TrouverTexte(slider);
        if (texte != null)
            texte.text = Mathf.RoundToInt(slider.value * 100) + "%";
    }

    private TMP_Text TrouverTexte(Slider slider)
    {
        Transform t = slider.transform.Find("pourcentage");
        if (t != null)
            return t.GetComponent<TMP_Text>();
        return null;
    }
}