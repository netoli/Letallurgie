using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class gestionFlou : MonoBehaviour
{
    [Header("References")]
    public Volume volumeGlobal;

    [Header("Parametres flou")]
    [Range(0.1f, 300f)] public float distanceFocus;
    [Range(1f, 300f)] public float longueurFocale;
    [Range(1f, 32f)] public float ouverture;

    private DepthOfField dof;

    void Awake()
    {
        if (volumeGlobal.profile.TryGet(out DepthOfField d))
            dof = d;
        else
            dof = volumeGlobal.profile.Add<DepthOfField>(true);

        dof.mode.Override(DepthOfFieldMode.Bokeh);
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