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

    // Couleur de la vignette (palette du jeu). L'override URP par defaut
    // est NOIR : on force la couleur attendue #7F3E1D au moment ou on
    // recupere l'effet. Constante en code (precedent mouseLook/speed).
    private static readonly Color COULEUR_VIGNETTE =
        new Color32(0x7F, 0x3E, 0x1D, 0xFF);

    // Brouillard : le jeu utilise le fog NATIF de Unity (RenderSettings,
    // active dans les 5 scenes). L'ancien code ne cherchait que des
    // ParticleSystem nommes "*brouillard*" — il n'y en a AUCUN dans les
    // scenes (seul le libelle de l'option porte ce nom), d'ou le slider
    // sans effet. On pilote donc RenderSettings en ABSOLU : les densites
    // serialisees des scenes (0.001) sont imperceptibles en interieur,
    // un mapping relatif restait donc invisible. Plage du slider :
    // 0 = aucun brouillard, 1 = tres epais.
    private const float DENSITE_BROUILLARD_MAX = 0.025f;
    private const float FIN_LINEAIRE_MIN = 40f;
    private const float FIN_LINEAIRE_MAX = 500f;
    // Defaut du slider : reproduit le look leger des scenes actuelles
    // (densite ~0.001 = 0.05 x 0.025 — ancien defaut 1.0 = trop epais
    // avec le mapping absolu).
    private const float DEFAUT_BROUILLARD = 0.05f;

    private bool initialise = false;

    void Start()
    {
        InitialiserEtAppliquer();
    }

    /// <summary>
    /// Initialise une seule fois (post-processing, dropdowns, listeners)
    /// puis charge et applique les reglages graphiques sauvegardes.
    /// Appele par Start, ET par appliqueOptionsAuChargement a chaque
    /// chargement de scene — pour que luminosite / vignette / brouillard
    /// s'appliquent partout SANS avoir a ouvrir le menu options.
    /// </summary>
    public void InitialiserEtAppliquer()
    {
        if (!initialise)
        {
            RecupererEffetsPostProcessing();
            ConfigurerDropdownResolution();
            ConfigurerDropdownModeAffichage();
            ConfigurerListeners();
            initialise = true;
        }
        ChargerPreferences();
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
        // CORRECTIF "vignette visible en scene0 mais pas ailleurs" :
        // camera_principale a 'Post Processing' DECOCHE dans toutes les
        // scenes -> le Volume (vignette, luminosite) n'etait pas rendu
        // par la camera de jeu. On force le rendu post-processing sur
        // toutes les cameras de la scene pour un look uniforme.
        var cameras = FindObjectsByType<Camera>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var cam in cameras)
        {
            var dataCam = cam.GetUniversalAdditionalCameraData();
            if (dataCam != null)
                dataCam.renderPostProcessing = true;
        }

        // Auto-find du Volume global si non assigne dans l'Inspector
        // (cas observe : scene2_usine n'a pas le champ globalVolume
        // renseigne -> luminosite/vignette ne s'appliquaient pas). On
        // prend en priorite un Volume marque isGlobal, sinon le premier
        // trouve.
        if (globalVolume == null)
        {
            var volumes = FindObjectsByType<Volume>(
                FindObjectsSortMode.None);
            foreach (var v in volumes)
            {
                if (v.isGlobal) { globalVolume = v; break; }
            }
            if (globalVolume == null && volumes.Length > 0)
                globalVolume = volumes[0];
        }

        if (globalVolume == null) return;

        if (!globalVolume.profile.TryGet(out colorAdjustments))
            colorAdjustments = globalVolume.profile.Add<ColorAdjustments>(true);

        if (!globalVolume.profile.TryGet(out vignette))
            vignette = globalVolume.profile.Add<Vignette>(true);

        if (vignette != null)
        {
            vignetteOriginale = vignette.intensity.value;

            // Si le profil de la scene n'a pas de vignette configuree
            // (intensite 0), on aligne sur la valeur designer des autres
            // scenes (0.21 dans SampleSceneProfile) pour un defaut
            // coherent partout.
            if (vignetteOriginale <= 0.01f)
                vignetteOriginale = 0.21f;

            vignetteSliderDefaut = vignetteOriginale;

            // Couleur de vignette du jeu (sinon : noir par defaut URP).
            vignette.color.Override(COULEUR_VIGNETTE);
        }

        if (colorAdjustments != null)
        {
            luminositeOriginale = colorAdjustments.postExposure.value;
            luminositeSliderDefaut =
                0.5f + (luminositeOriginale / 4f);
        }
    }

    private gestionConfirmationOptions confirmationCache;

    private void MarquerModification()
    {
        if (enChargement) return;

        // Cache : evite un FindFirstObjectByType a chaque drag de slider.
        if (confirmationCache == null)
            confirmationCache =
                FindFirstObjectByType<gestionConfirmationOptions>();
        if (confirmationCache != null)
            confirmationCache.MarquerModification();
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
            colorAdjustments.postExposure.Override((lum - 0.5f) * 4f);
        }

        if (vignette != null)
        {
            vignette.intensity.Override(sliderIntensiteVignette.value);
        }

        AppliquerIntensiteBrouillard(sliderIntensiteBrouillard.value);
    }

    private void AppliquerIntensiteBrouillard(float intensite)
    {
        // 1) Fog natif Unity (RenderSettings) — le vrai brouillard du
        //    jeu. Mapping ABSOLU pour que le slider ait un effet visible
        //    dans toutes les scenes (les densites de scene, 0.001, sont
        //    invisibles aux distances d'interieur).
        RenderSettings.fog = true;
        if (RenderSettings.fogMode == FogMode.Linear)
        {
            RenderSettings.fogEndDistance = Mathf.Lerp(
                FIN_LINEAIRE_MAX, FIN_LINEAIRE_MIN, intensite);
        }
        else
        {
            RenderSettings.fogDensity =
                intensite * DENSITE_BROUILLARD_MAX;
        }

        // 2) Bonus : d'eventuels fx particules de brume (aucun dans les
        //    scenes actuelles). Insensible a la casse + objets inactifs.
        ParticleSystem[] brouillards =
            FindObjectsByType<ParticleSystem>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (ParticleSystem ps in brouillards)
        {
            string nom = ps.gameObject.name.ToLowerInvariant();
            if (nom.Contains("brouillard") || nom.Contains("brume"))
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
            PlayerPrefs.GetFloat("intensiteBrouillard",
                DEFAUT_BROUILLARD);

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
        sliderIntensiteBrouillard.value = DEFAUT_BROUILLARD;

        enChargement = false;

        PlayerPrefs.SetInt("resolution", 2);
        PlayerPrefs.SetInt("modeAffichage", 0);
        PlayerPrefs.SetFloat("luminosite", luminositeSliderDefaut);
        PlayerPrefs.SetFloat("intensiteVignette", vignetteSliderDefaut);
        PlayerPrefs.SetFloat("intensiteBrouillard", DEFAUT_BROUILLARD);

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