// ============================================================
// comportementAntagoniste.cs
// ------------------------------------------------------------
// Auteur      : Fanny Fortier
// Date créée  : 2026
// ------------------------------------------------------------
// Description :
//   Gère les réactions de l'antagoniste entre les phases.
//   Pioche un impact aléatoire dans ListeImpacts selon la
//   phase, applique l'animation, les particules, modifie
//   le poids de son objet et notifie la zone adverse.
// ------------------------------------------------------------
// Dépendances :
//   - ListeImpacts            : ScriptableObject des impacts
//   - ObjetPesable            : objet sur le plateau antagoniste
//   - ZoneDepotAntagoniste    : notifiée après modification du poids
//   - GestionnaireEnigmeBalance.PhaseEnigme : enum de phase
// ============================================================

using System;
using System.Collections;
using UnityEngine;

public class comportementAntagoniste : MonoBehaviour
{
    // ===================== INSPECTEUR =====================
    [Header("Références")]
    [SerializeField] private ListeImpacts _listeImpacts;
    [SerializeField] private Animator _animateur;
    [SerializeField] private ParticleSystem _particulesTransformation;
    [SerializeField] private objetPesable _objetSurBalance;
    [SerializeField] private ZoneDepotAntagoniste _zoneAdverse;

    [Header("Paramètres visuels")]
    [SerializeField] private float _delaiAvantParticules = 0.6f;
    [SerializeField] private float _echelleMax = 3f;
    [SerializeField] private float _echelleMin = 0.3f;

    // ===================== ÉVÉNEMENTS =====================
    public event Action OnActionTerminee;

    // ===================== MÉTHODES PUBLIQUES =====================

    public void ExecuterAction(GestionnaireEnigmeBalance.PhaseEnigme phase)
    {
        ImpactAntagoniste[] impacts = phase ==
            GestionnaireEnigmeBalance.PhaseEnigme.Phase1
                ? _listeImpacts.impactsPhase1
                : _listeImpacts.impactsPhase2;

        if (impacts == null || impacts.Length == 0)
        {
            Debug.LogWarning("[ComportementAntagoniste] " +
                             "Aucun impact défini pour cette phase.");
            OnActionTerminee?.Invoke();
            return;
        }

        ImpactAntagoniste impactChoisi =
            impacts[UnityEngine.Random.Range(0, impacts.Length)];

        StartCoroutine(AppliquerImpact(impactChoisi));
    }

    // ===================== COROUTINES =====================

    private IEnumerator AppliquerImpact(ImpactAntagoniste impact)
    {
        // 1. Animation antagoniste
        if (_animateur != null && !string.IsNullOrEmpty(impact.nomAnimation))
            _animateur.SetTrigger(impact.nomAnimation);

        // 2. Attendre avant les particules
        yield return new WaitForSeconds(_delaiAvantParticules);

        // 3. Particules
        if (_particulesTransformation != null)
            _particulesTransformation.Play();

        // 4. Modifier le poids et l'échelle visuelle
        _objetSurBalance.ModifierPoids(impact.multiplicateur);
        AppliquerEchelleVisuelle(impact.multiplicateur);

        // 5. Notifier la zone que le poids a changé
        _zoneAdverse.NotifierChangementPoids();

        Debug.Log($"[ComportementAntagoniste] Impact appliqué : " +
                  $"×{impact.multiplicateur} — " +
                  $"nouveau poids={_objetSurBalance.valeurPoids}");

        // 6. Attendre la fin des particules
        if (_particulesTransformation != null)
            yield return new WaitUntil(
                () => !_particulesTransformation.isPlaying);

        // 7. Notifier le gestionnaire
        OnActionTerminee?.Invoke();
    }

    // ===================== MÉTHODES PRIVÉES =====================

    private void AppliquerEchelleVisuelle(float multiplicateur)
    {
        if (_objetSurBalance == null) return;

        Vector3 echelleActuelle =
            _objetSurBalance.transform.localScale;
        Vector3 nouvelleEchelle = echelleActuelle * multiplicateur;

        // Plafonner pour rester lisible en scène
        float magnitude = nouvelleEchelle.magnitude;
        if (magnitude > _echelleMax)
            nouvelleEchelle = nouvelleEchelle.normalized * _echelleMax;

        nouvelleEchelle = Vector3.Max(
            nouvelleEchelle, Vector3.one * _echelleMin);

        _objetSurBalance.transform.localScale = nouvelleEchelle;
    }
}