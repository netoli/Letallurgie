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

        // Capture de l'etat INSPECTOR du DoF (la base configuree par
        // l'equipe dans le profil partage) AVANT toute modification.
        // Avant : on forcait Bokeh ici puis DesactiverFlou ecrasait
        // tout avec des valeurs en dur (100/1/32) -> les reglages DoF
        // du Global Volume "changeaient a chaque lancement".
        dofBaseActive = dof.active;
        dofBaseMode = dof.mode.value;
        dofBaseFocusDistance = dof.focusDistance.value;
        dofBaseFocalLength = dof.focalLength.value;
        dofBaseAperture = dof.aperture.value;

        // scene0 demarre sur le MENU PRINCIPAL : on active le flou de
        // menu explicitement (champs distanceFocus/longueurFocale/
        // ouverture du composant). Le look "menu floute" vient donc de
        // cet effet, PAS de la base du profil — la base doit etre
        // l'etat EN JEU. Hors scene0 : on part de la base.
        if (SceneManager.GetActiveScene().name == "scene0_tuto")
            ActiverFlou();
        else
            DesactiverFlou();
    }

    // Etat Inspector du DoF, restaure a chaque DesactiverFlou.
    private bool dofBaseActive;
    private DepthOfFieldMode dofBaseMode;
    private float dofBaseFocusDistance;
    private float dofBaseFocalLength;
    private float dofBaseAperture;

    public void ActiverFlou()
    {
        if (dof == null) return;

        // Flou de menu/pause : les valeurs viennent des champs de CE
        // composant (distanceFocus / longueurFocale / ouverture) —
        // comportement d'origine du jeu. Astuce reglage : pour un
        // panneau d'UI world-space NET sur fond flou, mettre
        // distanceFocus = distance camera->panneau.
        dof.active = true;
        dof.mode.Override(DepthOfFieldMode.Bokeh);
        dof.focusDistance.Override(distanceFocus);
        dof.focalLength.Override(longueurFocale);
        dof.aperture.Override(ouverture);
    }

    public void DesactiverFlou()
    {
        if (dof == null) return;

        // Restaure l'etat INSPECTOR du DoF (la base du profil partage)
        // au lieu des anciennes valeurs en dur qui ecrasaient le
        // reglage de l'equipe a chaque fermeture de menu.
        dof.active = dofBaseActive;
        dof.mode.Override(dofBaseMode);
        dof.focusDistance.Override(dofBaseFocusDistance);
        dof.focalLength.Override(dofBaseFocalLength);
        dof.aperture.Override(dofBaseAperture);
    }
}