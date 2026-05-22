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
//   Idles aléatoires : cycle avec poids configurables dans
//   l'Inspector — le Taunt rare a un poids faible.
//   Défaite : séquence Angry → Defeat Idle → Brutal Assassination.
// ------------------------------------------------------------
// Paramètres Animator attendus :
//   Triggers : DeclencherEntree, DeclencherMagie,
//              DeclencherIdleSpecial, DeclencherDefaite,
//              DeclencherMort
//   Ints     : TypeMagie (0=apparaître, 1=grossir, 2=rétrécir)
//              IdleType  (1=Bored, 2=DizzyIdle, 3=DrunkIdle,
//                         4=FightIdle, 5=StandingIdle, 6=Taunt,
//                         7=Talking, 8=Talking2, 9=Yelling,
//                         10=Loser, 11=Battlecry, 12=Threatening)
// ============================================================

using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

[Serializable]
public struct IdleAleatoire
{
    [Tooltip("Valeur de IdleType à envoyer à l'Animator.")]
    public int idleType;
    [Tooltip("Nom lisible pour l'Inspector.")]
    public string nom;
    [Range(1, 100)]
    [Tooltip("Poids de probabilité. Plus élevé = plus fréquent.")]
    public int poids;
}

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

    [Header("Idles aléatoires")]
    [Tooltip("Secondes entre chaque idle spécial. Mis à 0 pour désactiver.")]
    [SerializeField] private float _intervalleIdleSpecial = 8f;
    [Tooltip("Durée minimale à attendre après un idle spécial avant d'en déclencher un autre.")]
    [SerializeField] private float _dureeMinEntreIdles = 3f;
    [Tooltip("Liste des idles spéciaux avec leur poids de probabilité.")]
    [SerializeField] private IdleAleatoire[] _idles = new IdleAleatoire[]
    {
        new IdleAleatoire { idleType = 1,  nom = "Bored",             poids = 15 },
        new IdleAleatoire { idleType = 2,  nom = "Dizzy Idle",        poids = 10 },
        new IdleAleatoire { idleType = 3,  nom = "Drunk Idle",        poids = 10 },
        new IdleAleatoire { idleType = 4,  nom = "Fight Idle",        poids = 15 },
        new IdleAleatoire { idleType = 5,  nom = "Standing Idle",     poids = 15 },
        new IdleAleatoire { idleType = 6,  nom = "Taunt (rare)",      poids = 3  },
        new IdleAleatoire { idleType = 7,  nom = "Talking",           poids = 12 },
        new IdleAleatoire { idleType = 8,  nom = "Talking (1)",       poids = 12 },
        new IdleAleatoire { idleType = 9,  nom = "Yelling",           poids = 10 },
        new IdleAleatoire { idleType = 10, nom = "Loser",             poids = 8  },
        new IdleAleatoire { idleType = 11, nom = "Battlecry",         poids = 8  },
        new IdleAleatoire { idleType = 12, nom = "Threatening",       poids = 12 },
    };

    [Header("Défaite")]
    [Tooltip("Secondes entre l'anim Angry et le déclenchement de la mort.")]
    [SerializeField] private float _delaiAvantMort = 2f;

    // ===================== ÉVÉNEMENTS =====================
    public event Action OnActionTerminee;

    // ===================== ÉTAT INTERNE =====================
    private Coroutine _coroutineIdlesCycle;
    private bool _jeuEnCours = false;

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

        // 2. Animation d'entrée du boss
        if (_animateur != null)
            _animateur.SetTrigger("DeclencherEntree");

        // 3. Attendre le moment stratégique avant le reveal
        yield return new WaitForSeconds(_dureeAvantRevealObjet);

        // 4. Boss fait sa magie pour déposer l'objet
        //    TypeMagie = 0 → animation "apparaître"
        if (_animateur != null)
        {
            _animateur.SetInteger("TypeMagie", 0);
            _animateur.SetTrigger("DeclencherMagie");
        }

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
        if (_vcamIntroManoir != null)
        {
            _vcamIntroManoir.Priority = 0;
            Debug.Log($"[Intro] Priority vcamIntro={_vcamIntroManoir.Priority}");
        }

        // 10. Démarrer le cycle d'idles aléatoires
        _jeuEnCours = true;
        if (_intervalleIdleSpecial > 0f && _idles != null && _idles.Length > 0)
            _coroutineIdlesCycle = StartCoroutine(CyclerIdlesAleatoires());
    }

    private IEnumerator AppliquerImpact(impactAntagoniste impact)
    {
        // 1. Animation boss
        //    TypeMagie = 1 (grossir) ou 2 (rétrécir) selon l'impact
        if (_animateur != null)
        {
            int typeMagie = impact.grossit ? 1 : 2;
            _animateur.SetInteger("TypeMagie", typeMagie);
            _animateur.SetTrigger("DeclencherMagie");
        }

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

        // 4. Échelle visuelle (grossit ou rapetisse)
        AppliquerEchelleVisuelle(impact.multiplicateur);

        // 5. Particules + sfx
        JouerParticulesEtSon();

        // 6. Modifier le poids
        _objetSurBalance.ModifierPoids(impact.multiplicateur);

        // 7. Notifier la zone
        _zoneAdverse.NotifierChangementPoids();

        Debug.Log($"[ComportementAntagoniste] Impact appliqué : " +
                  $"×{impact.multiplicateur} — " +
                  $"nouveau poids={_objetSurBalance.valeurPoids}");

        // 8. Attendre la fin des particules
        yield return new WaitUntil(() => ParticulesTerminees());

        // 9. Notifier le gestionnaire
        OnActionTerminee?.Invoke();
    }

    /// <summary>
    /// Déclenche périodiquement un idle spécial aléatoire avec système de poids.
    /// Ex : Taunt poids=3 sur total ~130 = ~2.3% de chances à chaque tirage.
    /// </summary>
    private IEnumerator CyclerIdlesAleatoires()
    {
        while (_jeuEnCours)
        {
            yield return new WaitForSeconds(_intervalleIdleSpecial);

            if (_animateur == null || !_jeuEnCours) break;

            IdleAleatoire idleChoisi = ChoisirIdleAleatoire();
            _animateur.SetInteger("IdleType", idleChoisi.idleType);
            _animateur.SetTrigger("DeclencherIdleSpecial");

            Debug.Log($"[ComportementAntagoniste] Idle spécial : " +
                      $"{idleChoisi.nom} (IdleType={idleChoisi.idleType})");

            // Attendre la durée minimale avant le prochain tirage
            yield return new WaitForSeconds(_dureeMinEntreIdles);

            // Remettre IdleType à 0 pour que le retour au state idle soit propre
            if (_animateur != null)
                _animateur.SetInteger("IdleType", 0);
        }
    }

    /// <summary>
    /// Sélection aléatoire pondérée parmi les idles configurés dans l'Inspector.
    /// </summary>
    private IdleAleatoire ChoisirIdleAleatoire()
    {
        int poidsTotal = 0;
        foreach (var idle in _idles)
            poidsTotal += idle.poids;

        int tirage = UnityEngine.Random.Range(0, poidsTotal);
        int cumul = 0;

        foreach (var idle in _idles)
        {
            cumul += idle.poids;
            if (tirage < cumul)
                return idle;
        }

        // Fallback au dernier (ne devrait pas arriver)
        return _idles[_idles.Length - 1];
    }

    private IEnumerator JouerDefaiteSequence()
    {
        // Arrêter le cycle d'idles
        _jeuEnCours = false;
        if (_coroutineIdlesCycle != null)
        {
            StopCoroutine(_coroutineIdlesCycle);
            _coroutineIdlesCycle = null;
        }

        // Angry
        if (_animateur != null)
            _animateur.SetTrigger("DeclencherDefaite");

        Debug.Log("[ComportementAntagoniste] Défaite — Angry déclenché.");

        // Attendre avant la mort (temps pour que Angry joue + Defeat Idle)
        yield return new WaitForSeconds(_delaiAvantMort);

        // Brutal Assassination
        if (_animateur != null)
            _animateur.SetTrigger("DeclencherMort");

        Debug.Log("[ComportementAntagoniste] Mort déclenchée.");
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

    /// <summary>
    /// Déclenche la séquence de défaite : Angry → Defeat Idle → Brutal Assassination.
    /// À appeler depuis gestionnaireEnigmeBalance quand le joueur gagne.
    /// </summary>
    public void JouerDefaite()
    {
        StartCoroutine(JouerDefaiteSequence());
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
