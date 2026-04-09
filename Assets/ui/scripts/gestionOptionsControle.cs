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

    // Cles PlayerPrefs
    private const string CLE_SENSIBILITE = "sensibiliteCamera";
    private const string CLE_INVERSER_V = "inverserAxeVertical";
    private const string CLE_INVERSER_H = "inverserAxeHorizontal";
    private const string CLE_FOV = "champDeVision";

    // Valeurs par defaut
    private const float SENSIBILITE_DEFAUT = 1f;
    private const float FOV_DEFAUT = 90f;

    void Start()
    {
        ChargerParametres();
        ConfigurerListeners();
    }

    private void ChargerParametres()
    {
        // Sensibilite
        float sensibilite = PlayerPrefs.GetFloat(
            CLE_SENSIBILITE, SENSIBILITE_DEFAUT);
        sliderSensibilite.minValue = sensibiliteMin;
        sliderSensibilite.maxValue = sensibiliteMax;
        sliderSensibilite.value = sensibilite;
        MettreAJourPourcentage(sliderSensibilite,
            pourcentageSensibilite, sensibiliteMin, sensibiliteMax);

        // Inverser axes
        toggleInverserVertical.isOn =
            PlayerPrefs.GetInt(CLE_INVERSER_V, 0) == 1;
        toggleInverserHorizontal.isOn =
            PlayerPrefs.GetInt(CLE_INVERSER_H, 0) == 1;

        // Champ de vision
        float fov = PlayerPrefs.GetFloat(CLE_FOV, FOV_DEFAUT);
        sliderChampVision.minValue = fovMin;
        sliderChampVision.maxValue = fovMax;
        sliderChampVision.value = fov;
        MettreAJourPourcentage(sliderChampVision,
            pourcentageChampVision, fovMin, fovMax);
    }

    private void ConfigurerListeners()
    {
        sliderSensibilite.onValueChanged.AddListener((valeur) =>
        {
            PlayerPrefs.SetFloat(CLE_SENSIBILITE, valeur);
            MettreAJourPourcentage(sliderSensibilite,
                pourcentageSensibilite, sensibiliteMin, sensibiliteMax);
        });

        toggleInverserVertical.onValueChanged.AddListener((actif) =>
        {
            PlayerPrefs.SetInt(CLE_INVERSER_V, actif ? 1 : 0);
        });

        toggleInverserHorizontal.onValueChanged.AddListener((actif) =>
        {
            PlayerPrefs.SetInt(CLE_INVERSER_H, actif ? 1 : 0);
        });

        sliderChampVision.onValueChanged.AddListener((valeur) =>
        {
            PlayerPrefs.SetFloat(CLE_FOV, valeur);
            MettreAJourPourcentage(sliderChampVision,
                pourcentageChampVision, fovMin, fovMax);
        });
    }

    private void MettreAJourPourcentage(
        Slider slider, TMP_Text texte, float min, float max)
    {
        float pourcentage = ((slider.value - min) / (max - min)) * 100f;
        texte.text = Mathf.RoundToInt(pourcentage) + "%";
    }

    public void Confirmer()
    {
        PlayerPrefs.Save();
        AppliquerParametres();
    }

    public void Reinitialiser()
    {
        sliderSensibilite.value = SENSIBILITE_DEFAUT;
        toggleInverserVertical.isOn = false;
        toggleInverserHorizontal.isOn = false;
        sliderChampVision.value = FOV_DEFAUT;

        Confirmer();
    }

    private void AppliquerParametres()
    {
        // Appliquer le FOV a la camera
        Camera cam = Camera.main;
        if (cam != null)
            cam.fieldOfView = sliderChampVision.value;
    }

    // Methodes statiques pour lire les valeurs depuis d'autres scripts
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