// ============================================================
// appliqueOptionsControleCamera.cs
// ------------------------------------------------------------
// Le MAILLON MANQUANT de l'onglet Controle : il lit les prefs
// (sensibilite, inversion des axes, FOV) et les applique au rig
// camera Cinemachine en jeu.
//
// Avant, ces options sauvegardaient une valeur que rien
// n'appliquait : mouseLook forcait 300, et le FOV passait par
// Camera.main (ecrase chaque frame par Cinemachine).
//
// COMMENT CA MARCHE :
//   - Sensibilite : multiplie le Gain de chaque axe de regard du
//     CinemachineInputAxisController (le composant qui lit la souris).
//   - Inversion V/H : inverse le signe du Gain de l'axe concerne.
//   - FOV : ecrit Lens.FieldOfView sur la vcam de jeu.
//
// SETUP : poser ce composant sur le rig camera du joueur (ou sur le
// prefab joueur pour qu'il soit present dans toutes les scenes). Les
// references s'auto-resolvent si laissees vides. L'onglet Controle
// appelle AppliquerDepuisPrefs() a chaque changement de slider/toggle.
// ============================================================

using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

public class appliqueOptionsControleCamera : MonoBehaviour
{
    [Tooltip("Le CinemachineInputAxisController qui lit la souris. " +
        "Auto-trouve dans la scene si laisse vide.")]
    [SerializeField] private CinemachineInputAxisController inputAxisController;

    [Tooltip("La vcam de jeu (pour le FOV). Auto-trouvee par nom si vide.")]
    [SerializeField] private CinemachineCamera vcam;

    [Tooltip("Nom de la vcam de jeu a trouver si 'vcam' est vide.")]
    [SerializeField] private string nomVcamJeu =
        "camera_virtuelle_premiere_personne";

    private const string CLE_SENSIBILITE = "sensibiliteCamera";
    private const string CLE_INVERSER_V = "inverserAxeVertical";
    private const string CLE_INVERSER_H = "inverserAxeHorizontal";
    private const string CLE_FOV = "champDeVision";
    private const float SENSIBILITE_DEFAUT = 1f;
    private const float FOV_DEFAUT = 90f;

    // Gains "de base" (valeurs designer) captures une seule fois, avant
    // toute modification, pour que sensibilite = base * facteur.
    private float[] gainsBase;
    private bool basesCapturees = false;

    void Start()
    {
        StartCoroutine(AppliquerQuandPret());
    }

    // Le CinemachineInputAxisController cree ses Controllers au demarrage
    // (timing CM). On attend qu'ils existent avant de capturer/appliquer.
    private IEnumerator AppliquerQuandPret()
    {
        ResoudreReferences();

        float t = 0f;
        while (inputAxisController != null
            && (inputAxisController.Controllers == null
                || inputAxisController.Controllers.Count == 0)
            && t < 3f)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        AppliquerDepuisPrefs();
    }

    private bool referencesLoggees = false;

    private void ResoudreReferences()
    {
        if (inputAxisController == null)
            inputAxisController =
                FindFirstObjectByType<CinemachineInputAxisController>(
                    FindObjectsInactive.Include);

        if (vcam == null)
        {
            var cams = FindObjectsByType<CinemachineCamera>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            // 1) Par nom (camera_virtuelle_premiere_personne).
            if (!string.IsNullOrEmpty(nomVcamJeu))
            {
                foreach (var c in cams)
                {
                    if (c != null && c.name == nomVcamJeu)
                    {
                        vcam = c;
                        break;
                    }
                }
            }

            // 2) Fallback : la vcam ACTIVE la plus prioritaire (si la
            //    scene nomme sa camera de jeu autrement).
            if (vcam == null)
            {
                int meilleure = int.MinValue;
                foreach (var c in cams)
                {
                    if (c == null || !c.isActiveAndEnabled) continue;
                    if (c.Priority > meilleure)
                    {
                        meilleure = c.Priority;
                        vcam = c;
                    }
                }
            }
        }

        // Log unique pour diagnostiquer "rien ne se passe" : dit
        // exactement ce qui a ete trouve (ou pas) dans cette scene.
        if (!referencesLoggees)
        {
            referencesLoggees = true;
            Debug.Log("[appliqueOptionsControleCamera] axis="
                + (inputAxisController != null
                    ? inputAxisController.name : "INTROUVABLE")
                + " | vcam="
                + (vcam != null ? vcam.name : "INTROUVABLE")
                + " (FOV/inversion sans effet si INTROUVABLE -> "
                + "donne-moi ce log).");
        }
    }

    /// <summary>
    /// Lit les prefs de l'onglet Controle et les applique au rig.
    /// Appelable depuis l'exterieur (l'onglet Controle l'appelle a chaque
    /// changement pour un effet immediat).
    /// </summary>
    public void AppliquerDepuisPrefs()
    {
        if (inputAxisController == null || vcam == null)
            ResoudreReferences();

        float sensibilite =
            PlayerPrefs.GetFloat(CLE_SENSIBILITE, SENSIBILITE_DEFAUT);
        bool inverserV = PlayerPrefs.GetInt(CLE_INVERSER_V, 0) == 1;
        bool inverserH = PlayerPrefs.GetInt(CLE_INVERSER_H, 0) == 1;
        float fov = PlayerPrefs.GetFloat(CLE_FOV, FOV_DEFAUT);

        // --- Sensibilite + inversion sur les axes de regard ---
        if (inputAxisController != null
            && inputAxisController.Controllers != null
            && inputAxisController.Controllers.Count > 0)
        {
            var ctrls = inputAxisController.Controllers;

            if (!basesCapturees)
            {
                gainsBase = new float[ctrls.Count];
                string noms = "";
                for (int i = 0; i < ctrls.Count; i++)
                {
                    gainsBase[i] =
                        (ctrls[i] != null && ctrls[i].Input != null)
                            ? Mathf.Abs(ctrls[i].Input.Gain) : 1f;
                    noms += (ctrls[i] != null ? ctrls[i].Name : "?") + " ";
                }
                basesCapturees = true;
                Debug.Log("[appliqueOptionsControleCamera] Axes de regard " +
                    "detectes : " + noms.Trim() + " (si l'inversion vise " +
                    "le mauvais axe, dis-moi ces noms).");
            }

            for (int i = 0; i < ctrls.Count && i < gainsBase.Length; i++)
            {
                var ctrl = ctrls[i];
                if (ctrl == null || ctrl.Input == null) continue;

                float gain = gainsBase[i] * sensibilite;

                // Devine l'axe par son nom (Pan/X = horizontal, Tilt/Y =
                // vertical) pour appliquer la bonne inversion.
                string n = (ctrl.Name ?? "").ToLowerInvariant();
                bool axeVertical = n.Contains("tilt") || n.Contains("vert")
                    || (n.Contains("y") && !n.Contains("x"));
                bool axeHorizontal = n.Contains("pan") || n.Contains("horiz")
                    || (n.Contains("x") && !n.Contains("y"));

                if ((axeVertical && inverserV)
                    || (axeHorizontal && inverserH))
                    gain = -gain;

                ctrl.Input.Gain = gain;
            }
        }

        // --- FOV sur la vcam de jeu ---
        if (vcam != null)
        {
            LensSettings lens = vcam.Lens;
            lens.FieldOfView = fov;
            vcam.Lens = lens;
        }
    }
}
