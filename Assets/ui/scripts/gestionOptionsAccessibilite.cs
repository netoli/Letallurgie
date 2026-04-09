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
    [SerializeField] private GameObject jaugeCorrosionNormale;

    [Header("Corrosion - Vitesse")]
    [SerializeField] private TMP_Dropdown dropdownVitesseCorrosion;

    [Header("Audio - Description")]
    [SerializeField] private Toggle toggleDescriptionAudio;

    private readonly float[] multiplicateursTaille =
        { 0.75f, 1f, 1.35f };

    private readonly Color[] couleursSousTitre =
    {
        Color.white,
        Color.yellow,
        Color.cyan
    };

    private readonly float[] vitessesCorrosion = { 0.5f, 1f, 1.5f };

    private Dictionary<TMP_Text, float> taillesOriginales =
        new Dictionary<TMP_Text, float>();

    void Start()
    {
        SauvegarderTaillesOriginales();
        ConfigurerDropdowns();
        ChargerPreferences();
        ConfigurerListeners();
        AppliquerToutesLesOptions();
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
        PlayerPrefs.SetInt("glitchTexte", actif ? 1 : 0);
        AppliquerGlitch(actif);
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
        PlayerPrefs.SetInt("tailleTexteUI", index);
        AppliquerTailleTexteUI(index);
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
        PlayerPrefs.SetInt("tailleTexteBouton", index);
        AppliquerTailleTexteBouton(index);
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
        PlayerPrefs.SetInt("tailleSousTitre", index);

        gestionSousTitre sousTitre =
            FindFirstObjectByType<gestionSousTitre>();
        if (sousTitre != null)
            sousTitre.RafraichirOptions();
    }

    // ===== COULEUR SOUS-TITRE =====

    private void OnCouleurSousTitreChange(int index)
    {
        PlayerPrefs.SetInt("couleurSousTitre", index);

        gestionSousTitre sousTitre =
            FindFirstObjectByType<gestionSousTitre>();
        if (sousTitre != null)
            sousTitre.RafraichirOptions();
    }

    // ===== INDICATEUR CORROSION =====

    private void OnIndicateurCorrosionChange(bool actif)
    {
        PlayerPrefs.SetInt("indicateurCorrosion", actif ? 1 : 0);
        AppliquerIndicateurCorrosion(actif);
    }

    private void AppliquerIndicateurCorrosion(bool actif)
    {
        if (indicateurCorrosionSimplifie != null)
            indicateurCorrosionSimplifie.SetActive(actif);

        if (jaugeCorrosionNormale != null)
            jaugeCorrosionNormale.SetActive(!actif);
    }

    // ===== VITESSE CORROSION =====

    private void OnVitesseCorrosionChange(int index)
    {
        PlayerPrefs.SetInt("vitesseCorrosion", index);
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
        PlayerPrefs.SetInt("descriptionAudio", actif ? 1 : 0);
    }

    public bool DescriptionAudioActive()
    {
        return PlayerPrefs.GetInt("descriptionAudio", 0) == 1;
    }

    // ===== CHARGER PREFERENCES =====

    private void ChargerPreferences()
    {
        toggleGlitchTexte.isOn =
            PlayerPrefs.GetInt("glitchTexte", 1) == 1;

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
    }

    private void AppliquerToutesLesOptions()
    {
        AppliquerGlitch(toggleGlitchTexte.isOn);
        AppliquerTailleTexteUI(dropdownTailleTexteUI.value);
        AppliquerTailleTexteBouton(dropdownTailleTexteBouton.value);
        AppliquerIndicateurCorrosion(toggleIndicateurCorrosion.isOn);
    }

    // ===== METHODES PUBLIQUES =====

    public bool GlitchActif()
    {
        return PlayerPrefs.GetInt("glitchTexte", 1) == 1;
    }

    public bool IndicateurCorrosionSimplifie()
    {
        return PlayerPrefs.GetInt("indicateurCorrosion", 0) == 1;
    }
}