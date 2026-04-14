using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class gestionFlou : MonoBehaviour
{
    [Header("References")]
    public Volume volumeGlobal;

    [Header("Configurations par contexte")]
    public ConfigFlou configMenuPrincipal;
    public ConfigFlou configOptions;
    public ConfigFlou configCredits;
    public ConfigFlou configPause;
    public ConfigFlou configFinJeu;
    public ConfigFlou configConfirmation;
    public ConfigFlou configJeu;

    DepthOfField dof;

    [Serializable]
    public class ConfigFlou
    {
        public bool actif;
        [Range(0.1f, 300f)] public float distanceFocus;
        [Range(1f, 300f)] public float longueurFocale;
        [Range(1f, 32f)] public float ouverture;
    }

    void Awake()
    {
        if (volumeGlobal.profile.TryGet(out DepthOfField d))
            dof = d;
        else
            dof = volumeGlobal.profile.Add<DepthOfField>(true);

        dof.mode.Override(DepthOfFieldMode.Bokeh);
    }

    void AppliquerConfig(ConfigFlou config)
    {
        if (dof == null) return;

        dof.active = true;
        dof.mode.Override(DepthOfFieldMode.Bokeh);

        if (config.actif)
        {
            dof.focusDistance.Override(config.distanceFocus);
            dof.focalLength.Override(config.longueurFocale);
            dof.aperture.Override(config.ouverture);
        }
        else
        {
            dof.focusDistance.Override(100f);
            dof.focalLength.Override(1f);
            dof.aperture.Override(32f);
        }
    }

    public void FlouMenuPrincipal()
    {
        AppliquerConfig(configMenuPrincipal);
    }

    public void FlouOptions()
    {
        AppliquerConfig(configOptions);
    }

    public void FlouCredits()
    {
        AppliquerConfig(configCredits);
    }

    public void FlouPause()
    {
        AppliquerConfig(configPause);
    }

    public void FlouFinJeu()
    {
        AppliquerConfig(configFinJeu);
    }

    public void FlouConfirmation()
    {
        AppliquerConfig(configConfirmation);
    }

    public void FlouJeu()
    {
        AppliquerConfig(configJeu);
    }
}