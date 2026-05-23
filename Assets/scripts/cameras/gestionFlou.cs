using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public class gestionFlou : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference au Global Volume de la scene. Si laisse vide, " +
        "sera trouve automatiquement au runtime (FindFirstObjectByType). " +
        "Cette tolerance permet d'utiliser ce script dans un PREFAB qui " +
        "serait instancie dans plusieurs scenes (la reference perdrait " +
        "sinon sa cible specifique a chaque chargement de scene).")]
    public Volume volumeGlobal;

    [Header("Parametres flou")]
    [Range(0.1f, 300f)] public float distanceFocus;
    [Range(1f, 300f)] public float longueurFocale;
    [Range(1f, 32f)] public float ouverture;

    private DepthOfField dof;

    void Awake()
    {
        // Si volumeGlobal n'est pas assigne (cas du prefab instancie
        // dans une nouvelle scene), on cherche le Volume principal de
        // la scene automatiquement.
        if (volumeGlobal == null)
        {
            volumeGlobal = FindFirstObjectByType<Volume>(
                FindObjectsInactive.Include);
            if (volumeGlobal == null)
            {
                Debug.LogWarning("[gestionFlou] Aucun Volume trouve " +
                    "dans la scene. Le flou ne pourra pas fonctionner.");
                return;
            }
        }

        // Securite supplementaire : si le Volume n'a pas de profile
        // assigne, on ne peut rien faire. Sort proprement.
        if (volumeGlobal.profile == null)
        {
            Debug.LogWarning("[gestionFlou] Le Volume trouve n'a pas " +
                "de profile assigne. Le flou ne pourra pas fonctionner.");
            return;
        }

        if (volumeGlobal.profile.TryGet(out DepthOfField d))
            dof = d;
        else
            dof = volumeGlobal.profile.Add<DepthOfField>(true);

        dof.mode.Override(DepthOfFieldMode.Bokeh);

        // D�sactiver par d�faut, sauf dans la sc�ne principale
        if (SceneManager.GetActiveScene().name != "scene_taverne_tutoriel")
        {
            DesactiverFlou();
        }
    }

    public void ActiverFlou()
    {
        if (dof == null) return;

        dof.active = true;
        dof.mode.Override(DepthOfFieldMode.Bokeh);
        dof.focusDistance.Override(distanceFocus);
        dof.focalLength.Override(longueurFocale);
        dof.aperture.Override(ouverture);
    }

    public void DesactiverFlou()
    {
        if (dof == null) return;

        dof.active = true;
        dof.mode.Override(DepthOfFieldMode.Bokeh);
        dof.focusDistance.Override(100f);
        dof.focalLength.Override(1f);
        dof.aperture.Override(32f);
    }
}