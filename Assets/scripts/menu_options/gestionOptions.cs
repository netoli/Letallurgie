using UnityEngine;
using Unity.Cinemachine;

public class gestionOptions : MonoBehaviour
{
    public static gestionOptions Instance;

    [Header("References")]
    [SerializeField] private CinemachineCamera cameraVirtuellePremierePersonneJoueur;

    // Valeurs par defaut
    private float sensibilite = 1f;
    private float champDeVision = 75f;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        // Charger les preferences sauvegardees
        sensibilite = PlayerPrefs.GetFloat("sensibilite", 1f);
        champDeVision = PlayerPrefs.GetFloat("champDeVision", 75f);
    }

    public void AppliquerSensibilite(float valeur)
    {
        sensibilite = valeur;
        PlayerPrefs.SetFloat("sensibilite", valeur);
        // Appliquer sur le script de mouvement camera
    }

    public void AppliquerChampDeVision(float valeur)
    {
        champDeVision = valeur;
        PlayerPrefs.SetFloat("champDeVision", valeur);

        if (cameraVirtuellePremierePersonneJoueur != null)
            cameraVirtuellePremierePersonneJoueur.Lens = new LensSettings
            {
                FieldOfView = valeur,
                NearClipPlane = cameraVirtuellePremierePersonneJoueur.Lens.NearClipPlane,
                FarClipPlane = cameraVirtuellePremierePersonneJoueur.Lens.FarClipPlane
            };
    }

    public float ObtenirSensibilite() { return sensibilite; }
    public float ObtenirChampDeVision() { return champDeVision; }
}