using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class gestionEnigmeTuyauterie : MonoBehaviour
{
    [Header("Points d'ancrage dans la scène")]
    [SerializeField] private List<pointAncrageTuyau> pointsAncrage;

    [Header("Seuil erreurs avant reset")]
    [SerializeField] private int nombreErreursMax;

    [Header("Intégration reset visuel")]
    [SerializeField] private gestionEcranReprise ecranReprise;

    [Header("Événements")]
    public UnityEvent onVictoire;
    public UnityEvent onReset;
    public UnityEvent<int, int> onProgression;
    public UnityEvent<int, int> onErreurs;
    public UnityEvent<resultatPlacement> onFeedback;

    private int nombreRemplis;
    private int nombreErreurs;

    void Start()
    {
        // AUTO-FIND : si pointsAncrage est vide ou contient des null,
        // on cherche tous les pointAncrageTuyau dans la scene
        // (inclus inactifs). Evite au user le drag-drop manuel des 9
        // snap_points.
        if (pointsAncrage == null
            || pointsAncrage.Count == 0
            || pointsAncrage.TrueForAll(p => p == null))
        {
            var tous = FindObjectsByType<pointAncrageTuyau>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            pointsAncrage = new List<pointAncrageTuyau>(tous);
            Debug.Log($"[gestionEnigmeTuyauterie] AUTO-FIND : " +
                $"{pointsAncrage.Count} snap_points trouves " +
                "automatiquement dans la scene.");
        }

        // AUTO-FIND ecranReprise si pas assigne dans Inspector. Sinon
        // les canvas "Oops/Reprise" sur echec ne s'affichent pas. Cas
        // observe : ecranReprise champ vide → ReinitialiserPuzzle()
        // skippe l'appel a LancerErreur() silencieusement.
        if (ecranReprise == null)
        {
            ecranReprise = FindFirstObjectByType<gestionEcranReprise>(
                FindObjectsInactive.Include);
            if (ecranReprise != null)
            {
                Debug.Log("[gestionEnigmeTuyauterie] AUTO-FIND : " +
                    $"gestionEcranReprise trouve sur '{ecranReprise.name}'.");
            }
            else
            {
                Debug.LogWarning("[gestionEnigmeTuyauterie] AUTO-FIND " +
                    "ecranReprise : aucun gestionEcranReprise dans la " +
                    "scene. Le canvas reprise ne s'affichera pas en cas " +
                    "d'echec ou de reussite.");
            }
        }

        // CABLAGE AUTO du minuteur : si onTempsEcoule n'a pas de listener
        // pointant vers ForcerEchec, on l'ajoute. Sinon le minuteur peut
        // expirer sans declencher le canvas reprise. Le user n'a plus a
        // configurer ca dans l'Inspector.
        var minuteur = minuteurEnigmeTuyauterie.Instance;
        if (minuteur == null)
            minuteur = FindFirstObjectByType<minuteurEnigmeTuyauterie>(
                FindObjectsInactive.Include);
        if (minuteur != null)
        {
            // Removelistener avant Add pour eviter doublon si Start re-call.
            minuteur.onTempsEcoule.RemoveListener(ForcerEchec);
            minuteur.onTempsEcoule.AddListener(ForcerEchec);
            Debug.Log("[gestionEnigmeTuyauterie] onTempsEcoule du minuteur " +
                "cable a ForcerEchec → declenchera le canvas reprise.");
        }

        // CABLAGE AUTO de la victoire : ajoute LancerReprise sur l'event
        // onVictoire pour afficher le canvas (variante reprise sans "Oops").
        // Si l'utilisateur a un autre canvas de felicitations, ce listener
        // s'ajoute en plus et ne casse rien (les 2 peuvent cohabiter).
        if (ecranReprise != null)
        {
            onVictoire.RemoveListener(ecranReprise.LancerReprise);
            onVictoire.AddListener(ecranReprise.LancerReprise);
            Debug.Log("[gestionEnigmeTuyauterie] onVictoire cable a " +
                "ecranReprise.LancerReprise → affichera le canvas en " +
                "cas de reussite.");
        }

        foreach (pointAncrageTuyau point in pointsAncrage)
        {
            if (point == null) continue;
            point.onRempli.AddListener(SurRemplissage);
            point.onPlacementTente.AddListener(SurTentativePlacement);
        }

        // #7 : retirer le vieux bandeau one-shot "tu peux commencer
        // (4 tuyaux)" et brancher le bandeau dynamique "tuyaux restants".
        ConfigurerBandeauTuyaux();

        nombreRemplis = 0;
        nombreErreurs = 0;
        onProgression.Invoke(0, pointsAncrage.Count);
        onErreurs.Invoke(0, nombreErreursMax);
    }

    public pointAncrageTuyau TrouverPointAncragePlusProche(Vector3 position)
    {
        pointAncrageTuyau plusProche = null;
        float distanceMin = float.MaxValue;

        foreach (pointAncrageTuyau point in pointsAncrage)
        {
            if (point.EstRempli()) continue;

            float distance = Vector3.Distance(
                position, point.transform.position);

            if (distance < point.rayonDetection
                && distance < distanceMin)
            {
                distanceMin = distance;
                plusProche = point;
            }
        }
        return plusProche;
    }

    private void SurRemplissage()
    {
        nombreRemplis++;
        onProgression.Invoke(nombreRemplis, pointsAncrage.Count);

        if (nombreRemplis >= pointsAncrage.Count)
        {
            onVictoire.Invoke();
        }
    }

    private void SurTentativePlacement(resultatPlacement resultat)
    {
        onFeedback.Invoke(resultat);

        if (resultat == resultatPlacement.MauvaisePiece
            || resultat == resultatPlacement.MauvaiseOrientation)
        {
            nombreErreurs++;
            onErreurs.Invoke(nombreErreurs, nombreErreursMax);

            if (nombreErreurs >= nombreErreursMax)
            {
                ReinitialiserPuzzle();
            }
        }
    }

    private void ReinitialiserPuzzle()
    {
        if (ecranReprise != null)
        {
            ecranReprise.LancerErreur();
        }

        // Avant de vider les snap_points, on rend a l'inventaire les
        // tuyaux qui avaient ete places (pieceAttendue de chaque snap
        // rempli). Le joueur peut ainsi reessayer sans devoir refaire
        // toute la fouille.
        if (gestionInventaire.Instance != null)
        {
            foreach (pointAncrageTuyau point in pointsAncrage)
            {
                if (point.EstRempli() && point.pieceAttendue != null)
                {
                    gestionInventaire.Instance.AjouterObjet(
                        point.pieceAttendue);
                }
            }
        }

        foreach (pointAncrageTuyau point in pointsAncrage)
        {
            point.ReinitialiserPourReset();
        }
        nombreRemplis = 0;
        nombreErreurs = 0;
        onProgression.Invoke(0, pointsAncrage.Count);
        onErreurs.Invoke(0, nombreErreursMax);

        // #5 (choix Oli) : a chaque echec (3 erreurs OU temps ecoule via
        // ForcerEchec), le minuteur repart a 6:00 pour la nouvelle
        // tentative — sans que le joueur ait a ressortir de la zone.
        minuteurEnigmeTuyauterie.RedemarrerCompletTous();

        onReset.Invoke();
    }

    /// <summary>
    /// Force la reinitialisation publique (appelable depuis l'exterieur,
    /// par exemple par le minuteur qui detecte l'expiration du temps).
    /// </summary>
    public void ForcerEchec()
    {
        ReinitialiserPuzzle();
    }

    /// <summary>
    /// #7 (cablage en code, choix Oli) :
    /// 1. Desactive surveillanceInventaire — le declencheur du vieux
    ///    bandeau one-shot "Tu peux commencer a placer les tuyaux". Ce
    ///    composant n'existe que dans scene2_usine et ne sert qu'a ca,
    ///    donc le desactiver ne casse rien ailleurs.
    /// 2. Cree au runtime un bandeauTuyauxRestants s'il n'en existe pas
    ///    deja (le script avait ete ecrit mais jamais attache : aucun
    ///    .meta). Il affiche "Il reste N tuyau(x) a ramasser" a chaque
    ///    ramassage. Le total (9) est la valeur par defaut documentee du
    ///    script.
    /// </summary>
    private void ConfigurerBandeauTuyaux()
    {
        var surveillances = FindObjectsByType<surveillanceInventaire>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var s in surveillances)
        {
            s.gameObject.SetActive(false);
            Debug.Log("[gestionEnigmeTuyauterie] surveillanceInventaire " +
                $"'{s.name}' desactive (retrait du vieux bandeau " +
                "'4 tuyaux').");
        }

        if (FindFirstObjectByType<bandeauTuyauxRestants>(
                FindObjectsInactive.Include) == null)
        {
            var go = new GameObject("bandeau_tuyaux_restants_auto");
            go.AddComponent<bandeauTuyauxRestants>();
            Debug.Log("[gestionEnigmeTuyauterie] bandeauTuyauxRestants " +
                "auto-cree (aucun n'existait dans la scene).");
        }
    }
}