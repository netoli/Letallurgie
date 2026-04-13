using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class gestionOptionsControle : MonoBehaviour
{
    [Header("Sensibilite Camera")]
    [SerializeField] private Slider sliderSensibilite;
    [SerializeField] private TMP_Text pourcentageSensibilite;
    [SerializeField] private float sensibiliteMin;
    [SerializeField] private float sensibiliteMax;

    [Header("Inverser Axes")]
    [SerializeField] private Toggle toggleInverserVertical;
    [SerializeField] private Toggle toggleInverserHorizontal;

    [Header("Champ de Vision")]
    [SerializeField] private Slider sliderChampVision;
    [SerializeField] private TMP_Text pourcentageChampVision;
    [SerializeField] private float fovMin;
    [SerializeField] private float fovMax;

    private const string CLE_SENSIBILITE = "sensibiliteCamera";
    private const string CLE_INVERSER_V = "inverserAxeVertical";
    private const string CLE_INVERSER_H = "inverserAxeHorizontal";
    private const string CLE_FOV = "champDeVision";

    private const float SENSIBILITE_DEFAUT = 1f;
    private const float FOV_DEFAUT = 90f;

    private bool enChargement = false;

    void Start()
    {
        ChargerParametres();
        ConfigurerListeners();
    }

    void OnEnable()
    {
        ChargerParametres();
    }

    private void MarquerModification()
    {
        if (enChargement) return;

        gestionConfirmationOptions confirmation =
            FindFirstObjectByType<gestionConfirmationOptions>();
        if (confirmation != null)
            confirmation.MarquerModification();
    }

    private void ChargerParametres()
    {
        enChargement = true;

        float sensibilite = PlayerPrefs.GetFloat(
            CLE_SENSIBILITE, SENSIBILITE_DEFAUT);
        sliderSensibilite.minValue = sensibiliteMin;
        sliderSensibilite.maxValue = sensibiliteMax;
        sliderSensibilite.value = sensibilite;
        MettreAJourPourcentage(sliderSensibilite,
            pourcentageSensibilite, sensibiliteMin, sensibiliteMax);

        toggleInverserVertical.isOn =
            PlayerPrefs.GetInt(CLE_INVERSER_V, 0) == 1;
        toggleInverserHorizontal.isOn =
            PlayerPrefs.GetInt(CLE_INVERSER_H, 0) == 1;

        float fov = PlayerPrefs.GetFloat(CLE_FOV, FOV_DEFAUT);
        sliderChampVision.minValue = fovMin;
        sliderChampVision.maxValue = fovMax;
        sliderChampVision.value = fov;
        MettreAJourPourcentage(sliderChampVision,
            pourcentageChampVision, fovMin, fovMax);

        enChargement = false;
    }

    private void ConfigurerListeners()
    {
        sliderSensibilite.onValueChanged.AddListener((valeur) =>
        {
            if (enChargement) return;
            PlayerPrefs.SetFloat(CLE_SENSIBILITE, valeur);
            MettreAJourPourcentage(sliderSensibilite,
                pourcentageSensibilite, sensibiliteMin, sensibiliteMax);
            MarquerModification();
        });

        toggleInverserVertical.onValueChanged.AddListener((actif) =>
        {
            if (enChargement) return;
            PlayerPrefs.SetInt(CLE_INVERSER_V, actif ? 1 : 0);
            MarquerModification();
        });

        toggleInverserHorizontal.onValueChanged.AddListener((actif) =>
        {
            if (enChargement) return;
            PlayerPrefs.SetInt(CLE_INVERSER_H, actif ? 1 : 0);
            MarquerModification();
        });

        sliderChampVision.onValueChanged.AddListener((valeur) =>
        {
            if (enChargement) return;
            PlayerPrefs.SetFloat(CLE_FOV, valeur);
            MettreAJourPourcentage(sliderChampVision,
                pourcentageChampVision, fovMin, fovMax);
            MarquerModification();
        });
    }

    private void MettreAJourPourcentage(
        Slider slider, TMP_Text texte, float min, float max)
    {
        float pourcentage = ((slider.value - min) / (max - min)) * 100f;
        texte.text = Mathf.RoundToInt(pourcentage) + "%";
    }

    public void Reinitialiser()
    {
        enChargement = true;

        sliderSensibilite.value = SENSIBILITE_DEFAUT;
        toggleInverserVertical.isOn = false;
        toggleInverserHorizontal.isOn = false;
        sliderChampVision.value = FOV_DEFAUT;

        enChargement = false;

        PlayerPrefs.SetFloat(CLE_SENSIBILITE, SENSIBILITE_DEFAUT);
        PlayerPrefs.SetInt(CLE_INVERSER_V, 0);
        PlayerPrefs.SetInt(CLE_INVERSER_H, 0);
        PlayerPrefs.SetFloat(CLE_FOV, FOV_DEFAUT);

        MettreAJourPourcentage(sliderSensibilite,
            pourcentageSensibilite, sensibiliteMin, sensibiliteMax);
        MettreAJourPourcentage(sliderChampVision,
            pourcentageChampVision, fovMin, fovMax);
        AppliquerParametres();

        Debug.Log("Options controle reinitialisees");
    }

    public void RechargerPreferences()
    {
        ChargerParametres();
        AppliquerParametres();
    }

    private void AppliquerParametres()
    {
        Camera cam = Camera.main;
        if (cam != null)
            cam.fieldOfView = sliderChampVision.value;
    }

    public static float ObtenirSensibilite()
    {
        return PlayerPrefs.GetFloat(CLE_SENSIBILITE, SENSIBILITE_DEFAUT);
    }

    public static bool ObtenirInverserVertical()
    {
        return PlayerPrefs.GetInt(CLE_INVERSER_V, 0) == 1;
    }

    public static bool ObtenirInverserHorizontal()
    {
        return PlayerPrefs.GetInt(CLE_INVERSER_H, 0) == 1;
    }

    public static float ObtenirChampDeVision()
    {
        return PlayerPrefs.GetFloat(CLE_FOV, FOV_DEFAUT);
    }
}