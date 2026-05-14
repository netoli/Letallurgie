// ============================================================
// comportementAntagoniste.cs
// ------------------------------------------------------------
// Auteur      : Fanny Fortier
// Date créée  : 2026
// ------------------------------------------------------------
// Description :
//   Gère les réactions de l'antagoniste entre les phases.
//   Intro : cache l'objet, joue l'animation d'entrée du boss,
//   puis révèle l'objet avec particules + sfx avant de rendre
//   le contrôle au joueur. Phases 1 et 2 : anime le boss,
//   déclenche grossit ou rapetisser sur la gold bar, particules,
//   sfx, modifie le poids.
// ------------------------------------------------------------
// Dépendances :
//   - listeImpacts            : ScriptableObject des impacts
//   - objetPesable            : objet sur le plateau antagoniste
//   - zoneDepotAntagoniste    : notifiée après modification du poids
//   - gestionnaireEnigmeBalance.PhaseEnigme : enum de phase
// ============================================================

using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class comportementAntagoniste : MonoBehaviour
{
    // ===================== INSPECTEUR =====================
    [Header("Références")]
    [SerializeField] private ListeImpacts _listeImpacts;
    [SerializeField] private Animator _animateur;
    [SerializeField] private objetPesable _objetSurBalance;
    [SerializeField] private Animator _animateurObjet;
    [SerializeField] private ZoneDepotAntagoniste _zoneAdverse;

    [Header("Particules")]
    [SerializeField] private ParticleSystem _particulesPortail;
    [SerializeField] private ParticleSystem _particulesExplosion;

    [Header("Audio")]
    [SerializeField] private AudioSource _sourceAudio;
    [SerializeField] private AudioClip _sfxTransformation;

    [Header("Paramètres visuels")]
    [SerializeField] private float _delaiAvantParticules = 0.6f;
    [SerializeField] private float _echelleMax = 3f;
    [SerializeField] private float _echelleMin = 0.3f;

    [Header("Caméra intro")]
    [SerializeField] private CinemachineCamera _vcamIntroManoir;
    [SerializeField] private float _delaiAvantAnimation = 1f;
    [SerializeField] private float _dureeAvantRevealObjet = 2f;
    [SerializeField] private float _dureeApresRevealAvantJoueur = 1.5f;

    // ===================== ÉVÉNEMENTS =====================
    public event Action OnActionTerminee;

    // ===================== UNITY =====================

    void Start()
    {
        // Cacher l'objet au départ — il apparaît pendant l'intro
        if (_objetSurBalance != null)
            _objetSurBalance.gameObject.SetActive(false);

        StartCoroutine(JouerIntroScene());
    }

    // ===================== COROUTINES =====================

    private IEnumerator JouerIntroScene()
    {
        // 1. Activer la caméra intro
        if (_vcamIntroManoir != null)
            _vcamIntroManoir.Priority = 60;

        // Attendre que Cinemachine finisse son lerp vers la caméra intro
        yield return new WaitForSeconds(_delaiAvantAnimation);

        if (_animateur != null)
            _animateur.SetTrigger("DeclencherEntree");

        // 2. Animation d'entrée du boss
        if (_animateur != null)
            _animateur.SetTrigger("DeclencherEntree");

        // 3. Attendre le moment stratégique avant le reveal
        yield return new WaitForSeconds(_dureeAvantRevealObjet);

        // 4. Boss fait sa magie pour déposer l'objet
        if (_animateur != null)
            _animateur.SetTrigger("DeclencherMagie");

        yield return new WaitForSeconds(_delaiAvantParticules);

        // 5. Révéler l'objet avec particules + sfx
        if (_objetSurBalance != null)
            _objetSurBalance.gameObject.SetActive(true);

        JouerParticulesEtSon();

        // 6. Notifier la zone que l'objet est en place
        _zoneAdverse.DefinirObjet(_objetSurBalance);

        // 7. Attendre la fin des particules
        yield return new WaitUntil(() => ParticulesTerminees());

        // 8. Attendre un beat avant de rendre le contrôle
        yield return new WaitForSeconds(_dureeApresRevealAvantJoueur);

        // 9. Rendre le contrôle au joueur
        Debug.Log("[Intro] Étape 9 atteinte — on baisse la priorité");

        if (_vcamIntroManoir != null)
        {
            _vcamIntroManoir.Priority = 0;
            Debug.Log($"[Intro] Priority vcamIntro={_vcamIntroManoir.Priority}");
        }
    }

    private IEnumerator AppliquerImpact(impactAntagoniste impact)
    {
        // 1. Animation boss
        if (_animateur != null)
            _animateur.SetTrigger("DeclencherMagie");

        // 2. Attendre avant les particules
        yield return new WaitForSeconds(_delaiAvantParticules);

        // 3. Animation objet (grossit ou rapetisser)
        if (_animateurObjet != null)
        {
            string triggerObjet = impact.grossit
                ? "DeclencherGrossir"
                : "DeclencherRapetisser";
            _animateurObjet.SetTrigger(triggerObjet);
        }

        // 4. Particules + sfx
        JouerParticulesEtSon();

        // 5. Modifier le poids
        _objetSurBalance.ModifierPoids(impact.multiplicateur);

        // 6. Notifier la zone
        _zoneAdverse.NotifierChangementPoids();

        Debug.Log($"[ComportementAntagoniste] Impact appliqué : " +
                  $"×{impact.multiplicateur} — " +
                  $"nouveau poids={_objetSurBalance.valeurPoids}");

        // 7. Attendre la fin des particules
        yield return new WaitUntil(() => ParticulesTerminees());

        // 8. Notifier le gestionnaire
        OnActionTerminee?.Invoke();
    }

    // ===================== MÉTHODES PUBLIQUES =====================

    public void ExecuterAction(
        gestionnaireEnigmeBalance.PhaseEnigme phase)
    {
        impactAntagoniste[] impacts = phase ==
            gestionnaireEnigmeBalance.PhaseEnigme.Phase1
                ? _listeImpacts.impactsPhase1
                : _listeImpacts.impactsPhase2;

        if (impacts == null || impacts.Length == 0)
        {
            Debug.LogWarning("[ComportementAntagoniste] " +
                             "Aucun impact défini pour cette phase.");
            OnActionTerminee?.Invoke();
            return;
        }

        impactAntagoniste impactChoisi =
            impacts[UnityEngine.Random.Range(0, impacts.Length)];

        StartCoroutine(AppliquerImpact(impactChoisi));
    }

    // ===================== MÉTHODES PRIVÉES =====================

    private void JouerParticulesEtSon()
    {
        if (_particulesPortail != null)
            _particulesPortail.Play();
        if (_particulesExplosion != null)
            _particulesExplosion.Play();
        if (_sourceAudio != null && _sfxTransformation != null)
            _sourceAudio.PlayOneShot(_sfxTransformation);
    }

    private bool ParticulesTerminees()
    {
        bool portailFini = _particulesPortail == null
            || !_particulesPortail.isPlaying;
        bool explosionFinie = _particulesExplosion == null
            || !_particulesExplosion.isPlaying;
        return portailFini && explosionFinie;
    }

    private void AppliquerEchelleVisuelle(float multiplicateur)
    {
        if (_objetSurBalance == null) return;

        Vector3 echelleActuelle =
            _objetSurBalance.transform.localScale;
        Vector3 nouvelleEchelle = echelleActuelle * multiplicateur;

        float magnitude = nouvelleEchelle.magnitude;
        if (magnitude > _echelleMax)
            nouvelleEchelle =
                nouvelleEchelle.normalized * _echelleMax;

        nouvelleEchelle = Vector3.Max(
            nouvelleEchelle, Vector3.one * _echelleMin);

        _objetSurBalance.transform.localScale = nouvelleEchelle;
    }
}