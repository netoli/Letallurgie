// ============================================================
// appliqueOptionsAuChargement.cs
// ------------------------------------------------------------
// Applique TOUTES les options sauvegardees a CHAQUE chargement de
// scene, sans que le joueur ait a rouvrir le menu options.
//
// PROBLEME RESOLU : les valeurs sont bien persistantes (PlayerPrefs),
// mais avant, la plupart ne s'appliquaient que quand le panneau
// d'options s'ouvrait dans la scene. Changer la luminosite dans
// scene1 n'avait donc aucun effet en scene2 tant qu'on n'ouvrait
// pas les options la-bas.
//
// FONCTIONNEMENT : singleton auto-cree au lancement du jeu
// (RuntimeInitializeOnLoadMethod), persistant (DontDestroyOnLoad),
// AUCUNE manip Inspector requise. A chaque sceneLoaded (+2 frames
// pour laisser les Start() de la scene s'executer), il fait :
//   - graphique     : gestionOptionsGraphiques.InitialiserEtAppliquer()
//                     (luminosite, vignette, brouillard)
//   - accessibilite : gestionOptionsAccessibilite.InitialiserEtAppliquer()
//                     (tailles de texte, glitch, corrosion)
//   - controle      : appliqueOptionsControleCamera.AppliquerDepuisPrefs()
//                     (sensibilite, inversion, FOV)
//   - sous-titres   : gestionSousTitre.RafraichirOptions()
//   - resolution    : appliquee une fois au demarrage du jeu
// L'audio (bande son, ambiance) s'applique deja tout seul via
// gestionAudio qui lit les prefs en continu.
// ============================================================

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class appliqueOptionsAuChargement : MonoBehaviour
{
    private static appliqueOptionsAuChargement instance;
    private static bool resolutionAppliquee = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;
        var go = new GameObject("applique_options_au_chargement");
        instance = go.AddComponent<appliqueOptionsAuChargement>();
        DontDestroyOnLoad(go);
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += AuChargementScene;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= AuChargementScene;
    }

    void Start()
    {
        // Premiere scene : sceneLoaded est deja passe au moment du
        // bootstrap, on applique donc manuellement.
        StartCoroutine(AppliquerApresDelai());
    }

    private void AuChargementScene(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(AppliquerApresDelai());
    }

    private IEnumerator AppliquerApresDelai()
    {
        // Laisser les Start() / auto-finds de la scene s'executer.
        yield return null;
        yield return null;
        AppliquerToutesLesOptions();
    }

    public void AppliquerToutesLesOptions()
    {
        // Resolution / mode d'affichage : une fois au demarrage du jeu
        // (les reglages Screen persistent ensuite pour la session).
        if (!resolutionAppliquee)
        {
            AppliquerResolutionSauvegardee();
            resolutionAppliquee = true;
        }

        // Graphique : luminosite, vignette, brouillard.
        var graphique = FindFirstObjectByType<gestionOptionsGraphiques>(
            FindObjectsInactive.Include);
        if (graphique != null)
            graphique.InitialiserEtAppliquer();

        // Accessibilite : tailles de texte, glitch, corrosion.
        var accessibilite =
            FindFirstObjectByType<gestionOptionsAccessibilite>(
                FindObjectsInactive.Include);
        if (accessibilite != null)
            accessibilite.InitialiserEtAppliquer();

        // Controle : sensibilite, inversion, FOV (rig Cinemachine).
        var controle =
            FindFirstObjectByType<appliqueOptionsControleCamera>(
                FindObjectsInactive.Include);
        if (controle == null)
        {
            // AUTO-INSTALLATION : le composant n'est pose nulle part
            // (l'etape manuelle "attacher au prefab joueur" sautait).
            // On le cree directement sur le GameObject du
            // CinemachineInputAxisController de la scene — c'est lui
            // qui lit la souris, donc le bon hote. Scene sans rig
            // (menu pur) : rien a controler, on saute.
            var axis = FindFirstObjectByType<
                Unity.Cinemachine.CinemachineInputAxisController>(
                    FindObjectsInactive.Include);
            if (axis != null)
                controle = axis.gameObject
                    .AddComponent<appliqueOptionsControleCamera>();
        }
        if (controle != null)
            controle.AppliquerDepuisPrefs();

        // Sous-titres : taille / couleur.
        var sousTitre = FindFirstObjectByType<gestionSousTitre>(
            FindObjectsInactive.Include);
        if (sousTitre != null)
            sousTitre.RafraichirOptions();

        // Flou d'arriere-plan : pose flouArrierePlanPopup sur les
        // canvas de confirmation (inactifs au chargement, d'ou le
        // scan complet). Aucune manip Inspector requise.
        InstallerFlouPopups();

        Debug.Log("[appliqueOptionsAuChargement] Options appliquees " +
            $"dans '{SceneManager.GetActiveScene().name}'.");
    }

    // Canvas qui doivent flouter l'arriere-plan quand ils s'ouvrent.
    private static readonly string[] CANVAS_A_FLOUTER =
    {
        "canvas_reprise_enigme",
        "canvas_confirmer_quitter",
        "canvas_confirmer_retourner_au_menu_principal",
        "canvas_confirmer_reinitialisation"
    };

    private void InstallerFlouPopups()
    {
        // Resources.FindObjectsOfTypeAll voit aussi les objets INACTIFS
        // (ces canvas le sont presque toujours au chargement). On
        // filtre les assets de prefab (hideFlags / scene invalide).
        var tous = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (var t in tous)
        {
            if (t == null) continue;
            if (t.hideFlags != HideFlags.None) continue;
            if (!t.gameObject.scene.IsValid()) continue;

            for (int i = 0; i < CANVAS_A_FLOUTER.Length; i++)
            {
                if (t.name != CANVAS_A_FLOUTER[i]) continue;
                if (t.GetComponent<flouArrierePlanPopup>() == null)
                    t.gameObject.AddComponent<flouArrierePlanPopup>();
                break;
            }
        }
    }

    // Applique la resolution et le mode d'affichage sauvegardes, mais
    // SEULEMENT si le joueur les a deja modifies au moins une fois
    // (HasKey) — sinon on respecte le defaut du build.
    private void AppliquerResolutionSauvegardee()
    {
        if (!PlayerPrefs.HasKey("resolution")
            && !PlayerPrefs.HasKey("modeAffichage"))
            return;

        int index = PlayerPrefs.GetInt("resolution", 2);
        int mode = PlayerPrefs.GetInt("modeAffichage", 0);

        FullScreenMode fsMode;
        if (mode == 1) fsMode = FullScreenMode.Windowed;
        else if (mode == 2) fsMode = FullScreenMode.FullScreenWindow;
        else fsMode = FullScreenMode.ExclusiveFullScreen;

        if (index == 0) Screen.SetResolution(1280, 720, fsMode);
        else if (index == 1) Screen.SetResolution(1600, 900, fsMode);
        else Screen.SetResolution(1920, 1080, fsMode);
    }
}
