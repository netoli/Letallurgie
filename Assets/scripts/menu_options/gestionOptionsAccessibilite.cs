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

    private bool initialise = false;

    void Start()
    {
        InitialiserEtAppliquer();
    }

    /// <summary>
    /// Initialise une seule fois (capture des tailles ORIGINALES de
    /// texte, dropdowns, listeners) puis charge et applique les reglages
    /// d'accessibilite sauvegardes. Appele par Start, ET par
    /// appliqueOptionsAuChargement a chaque chargement de scene — pour
    /// que tailles de texte / glitch / corrosion s'appliquent partout
    /// SANS avoir a ouvrir le menu options. Le flag 'initialise' evite
    /// surtout de recapturer des tailles deja agrandies (sinon
    /// double-echelle a chaque application).
    /// </summary>
    public void InitialiserEtAppliquer()
    {
        if (!initialise)
        {
            SauvegarderTaillesOriginales();
            ConfigurerDropdowns();
            ConfigurerListeners();
            initialise = true;
        }
        ChargerPreferences();
        AppliquerToutesLesOptions();
    }

    void OnEnable()
    {
        ChargerPreferences();
        AppliquerToutesLesOptions();
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

    private void SauvegarderTaillesOriginales()
    {
        // Inclut les INACTIFS : avant, les textes des canvas fermes au
        // chargement (menu options, menu principal en scenes 1-4)
        // n'etaient jamais captures -> ContainsKey les sautait ensuite
        // et leur taille ne changeait jamais.
        TMP_Text[] tousLesTextes = FindObjectsByType<TMP_Text>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (TMP_Text texte in tousLesTextes)
        {
            if (texte != null && !taillesOriginales.ContainsKey(texte))
                taillesOriginales[texte] = texte.fontSize;
        }
    }

    // ── Classification des textes ─────────────────────────────
    // Les tags (texte_ui / texte_bouton / texte_glitch) n'existent que
    // sur ~13 objets par scene (libelles de menus, tous texte_bouton).
    // On garde le tag comme signal PRIORITAIRE quand il est pose, et on
    // classe tout le reste automatiquement : texte sous un Button =
    // texte de bouton, sinon texte d'UI. Les textes de sous-titres sont
    // exclus (ils ont leur propre option de taille via gestionSousTitre).

    private bool EstTexteBouton(TMP_Text texte)
    {
        if (texte.CompareTag("texte_bouton")) return true;
        if (texte.CompareTag("texte_ui")) return false;
        return texte.GetComponentInParent<Button>(true) != null;
    }

    private bool EstTexteSousTitre(TMP_Text texte)
    {
        return texte.GetComponentInParent<gestionSousTitre>(true) != null;
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

    // Null-guards partout : certaines scenes ont des references UI
    // non assignees sur ce composant (cas observe : NRE sur
    // dropdownVitesseCorrosion au chargement). Les reglages restent
    // appliques via les prefs meme si le widget est absent.
    private void RemplirDropdown(TMP_Dropdown dd, List<string> options)
    {
        if (dd == null) return;
        dd.ClearOptions();
        dd.AddOptions(options);
    }

    private void ConfigurerDropdowns()
    {
        List<string> tailles = new List<string>
        {
            "Petit",
            "Normal",
            "Grand"
        };

        RemplirDropdown(dropdownTailleTexteUI, tailles);
        RemplirDropdown(dropdownTailleTexteBouton,
            new List<string>(tailles));
        RemplirDropdown(dropdownTailleSousTitre,
            new List<string>(tailles));
        RemplirDropdown(dropdownCouleurSousTitre, new List<string>
        {
            "Blanc",
            "Jaune",
            "Cyan"
        });
        RemplirDropdown(dropdownVitesseCorrosion, new List<string>
        {
            "Lente",
            "Normale",
            "Rapide"
        });
    }

    private void ConfigurerListeners()
    {
        if (toggleGlitchTexte != null)
            toggleGlitchTexte.onValueChanged.AddListener(OnGlitchChange);

        if (dropdownTailleTexteUI != null)
            dropdownTailleTexteUI.onValueChanged.AddListener(
                OnTailleTexteUIChange);
        if (dropdownTailleTexteBouton != null)
            dropdownTailleTexteBouton.onValueChanged.AddListener(
                OnTailleTexteBoutonChange);
        if (dropdownTailleSousTitre != null)
            dropdownTailleSousTitre.onValueChanged.AddListener(
                OnTailleSousTitreChange);
        if (dropdownCouleurSousTitre != null)
            dropdownCouleurSousTitre.onValueChanged.AddListener(
                OnCouleurSousTitreChange);

        if (toggleIndicateurCorrosion != null)
            toggleIndicateurCorrosion.onValueChanged.AddListener(
                OnIndicateurCorrosionChange);
        if (dropdownVitesseCorrosion != null)
            dropdownVitesseCorrosion.onValueChanged.AddListener(
                OnVitesseCorrosionChange);

        if (toggleDescriptionAudio != null)
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
        // AVANT : cherchait des textes tagues "texte_glitch" (il n'y en
        // a AUCUN dans le projet) portant un composant *Glitch* (qui
        // n'existait pas non plus) -> le toggle ne faisait rien.
        // MAINTENANT : on pose notre effetGlitchTexte sur les textes du
        // jeu et on bascule son enabled. Sous-titres inclus (thematique
        // corrosion). On ne CREE les composants que si l'option est
        // activee; pour desactiver, on eteint ceux qui existent.
        TMP_Text[] tous = FindObjectsByType<TMP_Text>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (TMP_Text texte in tous)
        {
            if (texte == null) continue;

            effetGlitchTexte effet =
                texte.GetComponent<effetGlitchTexte>();

            if (effet == null)
            {
                if (!actif) continue;
                effet = texte.gameObject
                    .AddComponent<effetGlitchTexte>();
            }

            effet.enabled = actif;
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
        AppliquerTailles();
    }

    // Une seule passe pour les DEUX tailles (UI et bouton) : on lit les
    // deux dropdowns, on classe chaque texte, on applique le bon
    // multiplicateur. Capture PARESSEUSE des tailles originales : un
    // texte jamais vu (instancie apres le chargement, ex. contenu des
    // tuiles de sauvegarde) est capture a sa taille actuelle = sa taille
    // prefab, donc jamais de double-echelle.
    private void AppliquerTailles()
    {
        // Source de verite : les PREFS (les listeners des dropdowns les
        // ecrivent avant d'appeler ici). Fonctionne meme sans widgets.
        int indexUI = Mathf.Clamp(
            PlayerPrefs.GetInt("tailleTexteUI", 1),
            0, multiplicateursTaille.Length - 1);
        int indexBouton = Mathf.Clamp(
            PlayerPrefs.GetInt("tailleTexteBouton", 1),
            0, multiplicateursTaille.Length - 1);

        float multUI = multiplicateursTaille[indexUI];
        float multBouton = multiplicateursTaille[indexBouton];

        TMP_Text[] tous = FindObjectsByType<TMP_Text>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (TMP_Text texte in tous)
        {
            if (texte == null) continue;
            if (EstTexteSousTitre(texte)) continue;

            if (!taillesOriginales.ContainsKey(texte))
                taillesOriginales[texte] = texte.fontSize;

            float mult = EstTexteBouton(texte) ? multBouton : multUI;
            texte.fontSize = taillesOriginales[texte] * mult;
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
        AppliquerTailles();
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

    // SetValueWithoutNotify / SetIsOnWithoutNotify : ne declenche NI nos
    // listeners NI ceux des composants d'effets sonores poses sur ces
    // widgets (sinon, ouvrir l'onglet jouait des sons parasites).
    private void ChargerDropdown(TMP_Dropdown dd, string cle, int defaut)
    {
        if (dd == null) return;
        dd.SetValueWithoutNotify(PlayerPrefs.GetInt(cle, defaut));
        dd.RefreshShownValue();
    }

    private void ChargerToggle(Toggle t, string cle, int defaut)
    {
        if (t == null) return;
        t.SetIsOnWithoutNotify(PlayerPrefs.GetInt(cle, defaut) == 1);
    }

    private void ChargerPreferences()
    {
        enChargement = true;

        ChargerToggle(toggleGlitchTexte, "glitchTexte", 0);
        ChargerDropdown(dropdownTailleTexteUI, "tailleTexteUI", 1);
        ChargerDropdown(dropdownTailleTexteBouton,
            "tailleTexteBouton", 1);
        ChargerDropdown(dropdownTailleSousTitre, "tailleSousTitre", 1);
        ChargerDropdown(dropdownCouleurSousTitre, "couleurSousTitre", 0);
        ChargerToggle(toggleIndicateurCorrosion,
            "indicateurCorrosion", 0);
        ChargerDropdown(dropdownVitesseCorrosion, "vitesseCorrosion", 1);
        ChargerToggle(toggleDescriptionAudio, "descriptionAudio", 0);

        enChargement = false;
    }

    public void Reinitialiser()
    {
        PlayerPrefs.SetInt("glitchTexte", 0);
        PlayerPrefs.SetInt("tailleTexteUI", 1);
        PlayerPrefs.SetInt("tailleTexteBouton", 1);
        PlayerPrefs.SetInt("tailleSousTitre", 1);
        PlayerPrefs.SetInt("couleurSousTitre", 0);
        PlayerPrefs.SetInt("indicateurCorrosion", 0);
        PlayerPrefs.SetInt("vitesseCorrosion", 1);
        PlayerPrefs.SetInt("descriptionAudio", 0);

        // Recharge l'UI depuis les prefs (null-safe, sans notification)
        // puis applique.
        ChargerPreferences();
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
        // Pilote par les PREFS (pas par l'etat des widgets) : les
        // reglages s'appliquent meme dans une scene dont le panneau a
        // des references UI manquantes.
        AppliquerGlitch(PlayerPrefs.GetInt("glitchTexte", 0) == 1);
        AppliquerTailles();
        AppliquerIndicateurCorrosion(
            PlayerPrefs.GetInt("indicateurCorrosion", 0) == 1);
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