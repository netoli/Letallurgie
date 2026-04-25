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
        foreach (pointAncrageTuyau point in pointsAncrage)
        {
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
}