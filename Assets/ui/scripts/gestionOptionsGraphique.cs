using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TMPro;
using System.Collections.Generic;

public class gestionOptionsGraphique : MonoBehaviour
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

    private Resolution[] resolutionsDisponibles;
    private ColorAdjustments colorAdjustments;
    private Vignette vignette;

    void Start()
    {
        RecupererEffetsPostProcessing();
        ConfigurerDropdownResolution();
        ConfigurerDropdownModeAffichage();
        ChargerPreferences();
        ConfigurerListeners();
        MettreAJourTousLesTextes();
    }

    private void RecupererEffetsPostProcessing()
    {
        if (globalVolume == null) return;

        globalVolume.profile.TryGet(out colorAdjustments);
        globalVolume.profile.TryGet(out vignette);
    }

    // ===== RESOLUTION =====

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

    // ===== MODE AFFICHAGE =====

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

    // ===== LISTENERS =====

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
    }

    private void OnModeAffichageChange(int index)
    {
        switch (index)
        {
            case 0:
                Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
                break;
            case 1:
                Screen.fullScreenMode = FullScreenMode.Windowed;
                break;
            case 2:
                Screen.fullScreenMode =
                    FullScreenMode.FullScreenWindow;
                break;
        }

        PlayerPrefs.SetInt("modeAffichage", index);
    }

    private void OnSliderChange(string cle, float valeur, Slider slider)
    {
        PlayerPrefs.SetFloat(cle, valeur);

        TMP_Text texte = TrouverTexte(slider);
        if (texte != null)
            texte.text = Mathf.RoundToInt(valeur * 100) + "%";

        AppliquerEffets();
    }

    // ===== APPLIQUER EFFETS =====

    private void AppliquerEffets()
    {
        // Luminosite
        if (colorAdjustments != null)
        {
            // Convertir 0-1 en -1 a 1 (0.5 = normal)
            float lum = sliderLuminosite.value;
            colorAdjustments.postExposure.value = (lum - 0.5f) * 2f;
        }

        // Vignette
        if (vignette != null)
        {
            vignette.intensity.value = sliderIntensiteVignette.value;
        }

        // Brouillard
        AppliquerIntensiteBrouillard(sliderIntensiteBrouillard.value);
    }

    private void AppliquerIntensiteBrouillard(float intensite)
    {
        // Chercher les particle systems de brouillard
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

    // ===== CHARGER / SAUVEGARDER =====

    private void ChargerPreferences()
    {
        sliderLuminosite.value =
            PlayerPrefs.GetFloat("luminosite", 0.5f);
        sliderIntensiteVignette.value =
            PlayerPrefs.GetFloat("intensiteVignette", 0.5f);
        sliderIntensiteBrouillard.value =
            PlayerPrefs.GetFloat("intensiteBrouillard", 1f);

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