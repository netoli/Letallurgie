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
        // ARCHITECTURE CAMERA DU JEU (NE PAS forcer le post-processing
        // sur toutes les cameras!) : camera_capture = Base URP
        // (environnement, post-proc ON serialise — vignette/DoF/color
        // adjustments rendus par elle) + camera_principale = Overlay
        // dans sa stack (UI seulement, post-proc OFF serialise — les
        // canvas restent NETS par construction). Un ancien correctif
        // forcait renderPostProcessing=true partout : il appliquait le
        // DoF a la camera UI et floutait les menus. Retire.

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

        // L'INSPECTOR fait foi : le profil PARTAGE (SampleSceneProfile,
        // maintenant reference par TOUTES les scenes, usine et manoir
        // inclus) porte vignette, depth of field et color adjustments
        // regles par l'equipe. Le script ne force plus ni couleur ni
        // intensite : il applique seulement le delta des options joueur
        // (et uniquement si le joueur a touche au reglage — voir
        // AppliquerEffets). Modifier le profil dans scene0 se repercute
        // donc partout, en Inspector comme en jeu.
        bool vignetteAjoutee = false;
        if (!globalVolume.profile.TryGet(out vignette))
        {
            vignette = globalVolume.profile.Add<Vignette>(true);
            vignetteAjoutee = true;
        }

        if (vignette != null)
        {
            // Filet : profil sans vignette -> defauts du jeu. Ne touche
            // JAMAIS un profil qui a deja sa vignette configuree.
            if (vignetteAjoutee)
            {
                vignette.intensity.Override(0.21f);
                vignette.color.Override(COULEUR_VIGNETTE);
            }

            vignetteOriginale = vignette.intensity.value;
            vignetteSliderDefaut = vignetteOriginale;
        }

        if (colorAdjustments != null)
        {
            luminositeOriginale = colorAdjustments.postExposure.value;
            luminositeSliderDefaut =
                0.5f + (luminositeOriginale / 4f);
        }

        // BASE de la scene pour le brouillard (restauration au R).
        fogSceneActif = RenderSettings.fog;
        fogSceneMode = RenderSettings.fogMode;
        fogSceneDensite = RenderSettings.fogDensity;
        fogSceneFin = RenderSettings.fogEndDistance;

        // Log diagnostic UNIQUE : dit exactement ce que ce composant a
        // trouve. Si "vignette/slider INTROUVABLE" apparait, c'est la
        // cause directe d'un slider sans effet — me donner cette ligne.
        int nbComposants = FindObjectsByType<gestionOptionsGraphiques>(
            FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
        // Etat des prefs joueur : explique en un coup d'oeil pourquoi
        // l'Instance Profile differe de l'asset en mode Play.
        string prefs = "";
        if (PlayerPrefs.HasKey("luminosite"))
            prefs += " luminosite="
                + PlayerPrefs.GetFloat("luminosite").ToString("F2");
        if (PlayerPrefs.HasKey("intensiteVignette"))
            prefs += " vignette="
                + PlayerPrefs.GetFloat("intensiteVignette")
                    .ToString("F2");
        if (PlayerPrefs.HasKey("intensiteBrouillard"))
            prefs += " brouillard="
                + PlayerPrefs.GetFloat("intensiteBrouillard")
                    .ToString("F2");
        if (prefs == "")
            prefs = " AUCUNE (valeurs Inspector pures)";
        else
            prefs += " -> R dans l'onglet Graphique pour purger";

        Debug.Log("[gestionOptionsGraphiques] volume="
            + (globalVolume != null ? globalVolume.name : "INTROUVABLE")
            + " | vignette="
            + (vignette != null
                ? "ok (base " + vignetteOriginale.ToString("F2") + ")"
                : "INTROUVABLE")
            + " | colorAdj="
            + (colorAdjustments != null ? "ok" : "INTROUVABLE")
            + " | sliders lum/vig/brouillard="
            + (sliderLuminosite != null ? "ok" : "NULL") + "/"
            + (sliderIntensiteVignette != null ? "ok" : "NULL") + "/"
            + (sliderIntensiteBrouillard != null ? "ok" : "NULL")
            + " | instances=" + nbComposants
            + (nbComposants > 1 ? " (DOUBLON dans la scene!)" : "")
            + " | prefs joueur :" + prefs);
    }

    // Valeurs designer du brouillard de LA scene courante (capturees a
    // l'init, restaurees quand le joueur reinitialise les options).
    private bool fogSceneActif;
    private FogMode fogSceneMode;
    private float fogSceneDensite;
    private float fogSceneFin;

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
        if (dropdownResolution == null) return;
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
        if (dropdownModeAffichage == null) return;
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
        // Null-guards : une reference morte ici interrompait TOUTE la
        // suite de l'initialisation (NRE) — les widgets suivants ne
        // recevaient jamais leur listener. Cas observe : luminosite
        // fonctionnelle mais vignette et brouillard muets.
        if (dropdownResolution != null)
            dropdownResolution.onValueChanged.AddListener(
                OnResolutionChange);
        if (dropdownModeAffichage != null)
            dropdownModeAffichage.onValueChanged.AddListener(
                OnModeAffichageChange);

        if (sliderLuminosite != null)
            sliderLuminosite.onValueChanged.AddListener(
                v => OnSliderChange("luminosite", v, sliderLuminosite));
        if (sliderIntensiteVignette != null)
            sliderIntensiteVignette.onValueChanged.AddListener(
                v => OnSliderChange("intensiteVignette", v,
                    sliderIntensiteVignette));
        if (sliderIntensiteBrouillard != null)
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
        // Le script n'ecrase l'Inspector QUE si le joueur a deja touche
        // au reglage (pref existante). Sinon, la base du profil partage
        // et les RenderSettings de la scene restent intacts — l'equipe
        // garde le controle artistique, les options ne sont qu'un delta.
        // (Les listeners ecrivent la pref AVANT d'appeler ici, donc le
        // premier mouvement de slider prend effet immediatement.)
        // Source de verite : les PREFS, pas les sliders (les listeners
        // ecrivent la pref avant d'appeler ici, et l'application
        // fonctionne meme dans une scene aux references UI mortes).
        if (colorAdjustments != null
            && PlayerPrefs.HasKey("luminosite"))
        {
            float lum = PlayerPrefs.GetFloat("luminosite", 0.5f);
            colorAdjustments.postExposure.Override((lum - 0.5f) * 4f);
        }

        if (vignette != null
            && PlayerPrefs.HasKey("intensiteVignette"))
        {
            // Le joueur a explicitement demande une vignette : on
            // s'assure que l'effet est actif meme si la base Inspector
            // le laisse desactive.
            vignette.active = true;
            vignette.intensity.Override(
                PlayerPrefs.GetFloat("intensiteVignette", 0.21f));
        }

        if (PlayerPrefs.HasKey("intensiteBrouillard"))
            AppliquerIntensiteBrouillard(PlayerPrefs.GetFloat(
                "intensiteBrouillard", DEFAUT_BROUILLARD));
    }

    // Position de slider equivalente au brouillard ACTUEL de la scene
    // (utilisee quand le joueur n'a encore rien regle).
    private float IntensiteBrouillardDeLaScene()
    {
        if (!RenderSettings.fog) return 0f;
        if (RenderSettings.fogMode == FogMode.Linear)
            return Mathf.Clamp01(Mathf.InverseLerp(
                FIN_LINEAIRE_MAX, FIN_LINEAIRE_MIN,
                RenderSettings.fogEndDistance));
        return Mathf.Clamp01(
            RenderSettings.fogDensity / DENSITE_BROUILLARD_MAX);
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

        // (Le scan des ParticleSystem nommes *brouillard* a ete RETIRE :
        // il etouffait les fx decoratifs du jeu — ex. fx_brouillard des
        // canvas — avec la pref du slider. Le slider ne pilote que le
        // fog de scene; les fx particules appartiennent aux scenes.)
    }

    private void ChargerPreferences()
    {
        enChargement = true;

        // Sans pref : les sliders REFLETENT la base Inspector (profil
        // partage / RenderSettings de la scene). SetValueWithoutNotify
        // + null-guards : pas de listeners parasites, pas de NRE sur
        // les copies de scene aux references mortes.
        if (sliderLuminosite != null)
        {
            float lum = PlayerPrefs.GetFloat("luminosite", -1f);
            sliderLuminosite.SetValueWithoutNotify(
                lum < 0 ? luminositeSliderDefaut : lum);
        }

        if (sliderIntensiteVignette != null)
        {
            float vig = PlayerPrefs.GetFloat("intensiteVignette", -1f);
            sliderIntensiteVignette.SetValueWithoutNotify(
                vig < 0 ? vignetteSliderDefaut : vig);
        }

        if (sliderIntensiteBrouillard != null)
        {
            sliderIntensiteBrouillard.SetValueWithoutNotify(
                PlayerPrefs.HasKey("intensiteBrouillard")
                    ? PlayerPrefs.GetFloat("intensiteBrouillard",
                        DEFAUT_BROUILLARD)
                    : IntensiteBrouillardDeLaScene());
        }

        if (dropdownResolution != null)
        {
            dropdownResolution.SetValueWithoutNotify(
                PlayerPrefs.GetInt("resolution", 2));
            dropdownResolution.RefreshShownValue();
        }

        if (dropdownModeAffichage != null)
        {
            dropdownModeAffichage.SetValueWithoutNotify(
                PlayerPrefs.GetInt("modeAffichage", 0));
            dropdownModeAffichage.RefreshShownValue();
        }

        enChargement = false;
    }

    public void Reinitialiser()
    {
        // REINITIALISER = revenir aux valeurs INSPECTOR (le profil
        // partage et les RenderSettings de la scene sont LA reference,
        // configuree par l'equipe). On efface donc les prefs visuelles
        // (le jeu repasse en mode "l'Inspector fait foi") et on restaure
        // explicitement la base sur le profil runtime + le fog de scene.
        PlayerPrefs.DeleteKey("luminosite");
        PlayerPrefs.DeleteKey("intensiteVignette");
        PlayerPrefs.DeleteKey("intensiteBrouillard");

        if (colorAdjustments != null)
            colorAdjustments.postExposure.Override(luminositeOriginale);
        if (vignette != null)
            vignette.intensity.Override(vignetteOriginale);

        RenderSettings.fog = fogSceneActif;
        RenderSettings.fogMode = fogSceneMode;
        RenderSettings.fogDensity = fogSceneDensite;
        RenderSettings.fogEndDistance = fogSceneFin;

        // Resolution / affichage : defauts du jeu (sans effet dans
        // l'editeur — Game view a taille fixe; visible en build).
        PlayerPrefs.SetInt("resolution", 2);
        PlayerPrefs.SetInt("modeAffichage", 0);
        Screen.SetResolution(1920, 1080,
            FullScreenMode.ExclusiveFullScreen);

        // Recharge l'UI depuis ce nouvel etat (sliders -> bases).
        ChargerPreferences();
        MettreAJourTousLesTextes();

        Debug.Log("Options graphiques reinitialisees "
            + "(retour aux valeurs Inspector)");
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
        if (slider == null) return;
        TMP_Text texte = TrouverTexte(slider);
        if (texte != null)
            texte.text = Mathf.RoundToInt(slider.value * 100) + "%";
    }

    private TMP_Text TrouverTexte(Slider slider)
    {
        if (slider == null) return null;
        Transform t = slider.transform.Find("pourcentage");
        if (t != null)
            return t.GetComponent<TMP_Text>();
        return null;
    }
}