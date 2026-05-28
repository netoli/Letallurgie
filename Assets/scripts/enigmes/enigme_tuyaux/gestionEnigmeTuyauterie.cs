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

        foreach (pointAncrageTuyau point in pointsAncrage)
        {
            if (point == null) continue;
            point.onRempli.AddListener(SurRemplissage);
            point.onPlacementTente.AddListener(SurTentativePlacement);
        }
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
}