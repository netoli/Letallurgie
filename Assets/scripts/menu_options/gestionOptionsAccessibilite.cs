using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class gestionOptionsAccessibilite : MonoBehaviour
{
    [Header("Visuelles - Glitch")]
    [SerializeField] private Toggle toggleGlitchTexte;

    [Header("Visuelles - Taille Texte UI")]
    [SerializeField] private TMP_Dropdown dropdownTailleTexteUI;

    [Header("Visuelles - Taille Texte Bouton")]
    [SerializeField] private TMP_Dropdown dropdownTailleTexteBouton;

    [Header("Sous-titre - Taille")]
    [SerializeField] private TMP_Dropdown dropdownTailleSousTitre;

    [Header("Sous-titre - Couleur")]
    [SerializeField] private TMP_Dropdown dropdownCouleurSousTitre;

    [Header("Corrosion - Indicateur Simplifie")]
    [SerializeField] private Toggle toggleIndicateurCorrosion;
    [SerializeField] private GameObject indicateurCorrosionSimplifie;

    [Header("Corrosion - Vitesse")]
    [SerializeField] private TMP_Dropdown dropdownVitesseCorrosion;

    [Header("Audio - Description")]
    [SerializeField] private Toggle toggleDescriptionAudio;

    private readonly float[] multiplicateursTaille =
        { 0.75f, 1f, 1.2f };

    private readonly Color[] couleursSousTitre =
    {
        Color.white,
        Color.yellow,
        Color.cyan
    };

    private readonly float[] vitessesCorrosion = { 0.5f, 1f, 1.5f };

    private Dictionary<TMP_Text, float> taillesOriginales =
        new Dictionary<TMP_Text, float>();

    private List<GameObject> elementsCorrosionCache =
        new List<GameObject>();

    private bool enChargement = false;

    void Start()
    {
        SauvegarderTaillesOriginales();
        ConfigurerDropdowns();
        ChargerPreferences();
        ConfigurerListeners();
        AppliquerToutesLesOptions();
    }

    void OnEnable()
    {
        ChargerPreferences();
        AppliquerToutesLesOptions();
    }

    private void MarquerModification()
    {
        if (enChargement) return;

        gestionConfirmationOptions confirmation =
            FindFirstObjectByType<gestionConfirmationOptions>();
        if (confirmation != null)
            confirmation.MarquerModification();
    }

    private void SauvegarderTaillesOriginales()
    {
        TMP_Text[] tousLesTextes =
            FindObjectsByType<TMP_Text>(FindObjectsSortMode.None);

        foreach (TMP_Text texte in tousLesTextes)
        {
            if (texte != null)
                taillesOriginales[texte] = texte.fontSize;
        }
    }

    private TMP_Text[] TrouverTextesParTag(string tag)
    {
        GameObject[] objets = GameObject.FindGameObjectsWithTag(tag);
        List<TMP_Text> textes = new List<TMP_Text>();

        foreach (GameObject obj in objets)
        {
            TMP_Text texte = obj.GetComponent<TMP_Text>();
            if (texte != null)
                textes.Add(texte);
        }

        return textes.ToArray();
    }

    private void ConfigurerDropdowns()
    {
        dropdownTailleTexteUI.ClearOptions();
        dropdownTailleTexteUI.AddOptions(new List<string>
        {
            "Petit",
            "Normal",
            "Grand"
        });

        dropdownTailleTexteBouton.ClearOptions();
        dropdownTailleTexteBouton.AddOptions(new List<string>
        {
            "Petit",
            "Normal",
            "Grand"
        });

        dropdownTailleSousTitre.ClearOptions();
        dropdownTailleSousTitre.AddOptions(new List<string>
        {
            "Petit",
            "Normal",
            "Grand"
        });

        dropdownCouleurSousTitre.ClearOptions();
        dropdownCouleurSousTitre.AddOptions(new List<string>
        {
            "Blanc",
            "Jaune",
            "Cyan"
        });

        dropdownVitesseCorrosion.ClearOptions();
        dropdownVitesseCorrosion.AddOptions(new List<string>
        {
            "Lente",
            "Normale",
            "Rapide"
        });
    }

    private void ConfigurerListeners()
    {
        toggleGlitchTexte.onValueChanged.AddListener(OnGlitchChange);

        dropdownTailleTexteUI.onValueChanged.AddListener(
            OnTailleTexteUIChange);
        dropdownTailleTexteBouton.onValueChanged.AddListener(
            OnTailleTexteBoutonChange);
        dropdownTailleSousTitre.onValueChanged.AddListener(
            OnTailleSousTitreChange);
        dropdownCouleurSousTitre.onValueChanged.AddListener(
            OnCouleurSousTitreChange);

        toggleIndicateurCorrosion.onValueChanged.AddListener(
            OnIndicateurCorrosionChange);
        dropdownVitesseCorrosion.onValueChanged.AddListener(
            OnVitesseCorrosionChange);

        toggleDescriptionAudio.onValueChanged.AddListener(
            OnDescriptionAudioChange);
    }

    // ===== GLITCH =====

    private void OnGlitchChange(bool actif)
    {
        if (enChargement) return;
        PlayerPrefs.SetInt("glitchTexte", actif ? 1 : 0);
        AppliquerGlitch(actif);
        MarquerModification();
    }

    private void AppliquerGlitch(bool actif)
    {
        TMP_Text[] textes = TrouverTextesParTag("texte_glitch");

        foreach (TMP_Text texte in textes)
        {
            MonoBehaviour[] effets = texte.GetComponents<MonoBehaviour>();

            foreach (var effet in effets)
            {
                if (effet.GetType().Name.Contains("Glitch")
                    || effet.GetType().Name.Contains("glitch"))
                {
                    effet.enabled = actif;
                }
            }
        }
    }

    // ===== TAILLE TEXTE UI =====

    private void OnTailleTexteUIChange(int index)
    {
        if (enChargement) return;
        PlayerPrefs.SetInt("tailleTexteUI", index);
        AppliquerTailleTexteUI(index);
        MarquerModification();
    }

    private void AppliquerTailleTexteUI(int index)
    {
        if (index < 0 || index >= multiplicateursTaille.Length) return;

        float multiplicateur = multiplicateursTaille[index];
        TMP_Text[] textes = TrouverTextesParTag("texte_ui");

        foreach (TMP_Text texte in textes)
        {
            if (taillesOriginales.ContainsKey(texte))
                texte.fontSize =
                    taillesOriginales[texte] * multiplicateur;
        }
    }

    // ===== TAILLE TEXTE BOUTON =====

    private void OnTailleTexteBoutonChange(int index)
    {
        if (enChargement) return;
        PlayerPrefs.SetInt("tailleTexteBouton", index);
        AppliquerTailleTexteBouton(index);
        MarquerModification();
    }

    private void AppliquerTailleTexteBouton(int index)
    {
        if (index < 0 || index >= multiplicateursTaille.Length) return;

        float multiplicateur = multiplicateursTaille[index];
        TMP_Text[] textes = TrouverTextesParTag("texte_bouton");

        foreach (TMP_Text texte in textes)
        {
            if (taillesOriginales.ContainsKey(texte))
                texte.fontSize =
                    taillesOriginales[texte] * multiplicateur;
        }
    }

    // ===== TAILLE SOUS-TITRE =====

    private void OnTailleSousTitreChange(int index)
    {
        if (enChargement) return;
        PlayerPrefs.SetInt("tailleSousTitre", index);
        MarquerModification();

        gestionSousTitre sousTitre =
            FindFirstObjectByType<gestionSousTitre>();
        if (sousTitre != null)
            sousTitre.RafraichirOptions();
    }

    // ===== COULEUR SOUS-TITRE =====

    private void OnCouleurSousTitreChange(int index)
    {
        if (enChargement) return;
        PlayerPrefs.SetInt("couleurSousTitre", index);
        MarquerModification();

        gestionSousTitre sousTitre =
            FindFirstObjectByType<gestionSousTitre>();
        if (sousTitre != null)
            sousTitre.RafraichirOptions();
    }

    // ===== INDICATEUR CORROSION =====

    private void OnIndicateurCorrosionChange(bool actif)
    {
        if (enChargement) return;
        PlayerPrefs.SetInt("indicateurCorrosion", actif ? 1 : 0);
        AppliquerIndicateurCorrosion(actif);
        MarquerModification();
    }

    private void CacherElementsCorrosion()
    {
        elementsCorrosionCache.Clear();

        GameObject[] elements =
            GameObject.FindGameObjectsWithTag("corrosion_ui");

        foreach (GameObject element in elements)
        {
            elementsCorrosionCache.Add(element);
            element.SetActive(false);
        }
    }

    private void AppliquerIndicateurCorrosion(bool actif)
    {
        if (indicateurCorrosionSimplifie != null)
            indicateurCorrosionSimplifie.SetActive(actif);

        if (actif)
        {
            CacherElementsCorrosion();
        }
        else
        {
            foreach (GameObject element in elementsCorrosionCache)
            {
                if (element != null)
                    element.SetActive(true);
            }
            elementsCorrosionCache.Clear();
        }
    }

    // ===== VITESSE CORROSION =====

    private void OnVitesseCorrosionChange(int index)
    {
        if (enChargement) return;
        PlayerPrefs.SetInt("vitesseCorrosion", index);
        MarquerModification();
    }

    public float ObtenirVitesseCorrosion()
    {
        int index = PlayerPrefs.GetInt("vitesseCorrosion", 1);
        if (index >= 0 && index < vitessesCorrosion.Length)
            return vitessesCorrosion[index];
        return 1f;
    }

    // ===== DESCRIPTION AUDIO =====

    private void OnDescriptionAudioChange(bool actif)
    {
        if (enChargement) return;
        PlayerPrefs.SetInt("descriptionAudio", actif ? 1 : 0);
        MarquerModification();
    }

    public bool DescriptionAudioActive()
    {
        return PlayerPrefs.GetInt("descriptionAudio", 0) == 1;
    }

    // ===== CHARGER PREFERENCES =====

    private void ChargerPreferences()
    {
        enChargement = true;

        toggleGlitchTexte.isOn =
            PlayerPrefs.GetInt("glitchTexte", 0) == 1;

        dropdownTailleTexteUI.value =
            PlayerPrefs.GetInt("tailleTexteUI", 1);
        dropdownTailleTexteUI.RefreshShownValue();

        dropdownTailleTexteBouton.value =
            PlayerPrefs.GetInt("tailleTexteBouton", 1);
        dropdownTailleTexteBouton.RefreshShownValue();

        dropdownTailleSousTitre.value =
            PlayerPrefs.GetInt("tailleSousTitre", 1);
        dropdownTailleSousTitre.RefreshShownValue();

        dropdownCouleurSousTitre.value =
            PlayerPrefs.GetInt("couleurSousTitre", 0);
        dropdownCouleurSousTitre.RefreshShownValue();

        toggleIndicateurCorrosion.isOn =
            PlayerPrefs.GetInt("indicateurCorrosion", 0) == 1;

        dropdownVitesseCorrosion.value =
            PlayerPrefs.GetInt("vitesseCorrosion", 1);
        dropdownVitesseCorrosion.RefreshShownValue();

        toggleDescriptionAudio.isOn =
            PlayerPrefs.GetInt("descriptionAudio", 0) == 1;

        enChargement = false;
    }

    public void Reinitialiser()
    {
        enChargement = true;

        toggleGlitchTexte.isOn = false;
        dropdownTailleTexteUI.value = 1;
        dropdownTailleTexteBouton.value = 1;
        dropdownTailleSousTitre.value = 1;
        dropdownCouleurSousTitre.value = 0;
        toggleIndicateurCorrosion.isOn = false;
        dropdownVitesseCorrosion.value = 1;
        toggleDescriptionAudio.isOn = false;

        enChargement = false;

        PlayerPrefs.SetInt("glitchTexte", 0);
        PlayerPrefs.SetInt("tailleTexteUI", 1);
        PlayerPrefs.SetInt("tailleTexteBouton", 1);
        PlayerPrefs.SetInt("tailleSousTitre", 1);
        PlayerPrefs.SetInt("couleurSousTitre", 0);
        PlayerPrefs.SetInt("indicateurCorrosion", 0);
        PlayerPrefs.SetInt("vitesseCorrosion", 1);
        PlayerPrefs.SetInt("descriptionAudio", 0);

        dropdownTailleTexteUI.RefreshShownValue();
        dropdownTailleTexteBouton.RefreshShownValue();
        dropdownTailleSousTitre.RefreshShownValue();
        dropdownCouleurSousTitre.RefreshShownValue();
        dropdownVitesseCorrosion.RefreshShownValue();

        AppliquerToutesLesOptions();

        Debug.Log("Options accessibilite reinitialisees");
    }

    public void RechargerPreferences()
    {
        ChargerPreferences();
        AppliquerToutesLesOptions();
    }

    private void AppliquerToutesLesOptions()
    {
        AppliquerGlitch(toggleGlitchTexte.isOn);
        AppliquerTailleTexteUI(dropdownTailleTexteUI.value);
        AppliquerTailleTexteBouton(dropdownTailleTexteBouton.value);
        AppliquerIndicateurCorrosion(toggleIndicateurCorrosion.isOn);
    }

    public bool GlitchActif()
    {
        return PlayerPrefs.GetInt("glitchTexte", 0) == 1;
    }

    public bool IndicateurCorrosionSimplifie()
    {
        return PlayerPrefs.GetInt("indicateurCorrosion", 0) == 1;
    }
}