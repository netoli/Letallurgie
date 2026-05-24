// ============================================================
// comportementAntagoniste.cs
// ------------------------------------------------------------
// Auteur      : Fanny Fortier
// Date créée  : 2026
// Dernière modification : 2026-05-23 - Fanny Fortier
// ------------------------------------------------------------
// Description :
//   Gère les réactions de l'antagoniste entre les phases.
//   Intro : bloque les inputs joueur (ModeCinematique), joue la
//   voix éerie du boss, son animation d'entrée, le dialogue
//   complet avec sous-titres + voice-over, switch de caméra
//   vers la balance, révèle l'objet avec particules + sfx,
//   puis rend le contrôle au joueur.
//   Phases 1 et 2 : anime le boss, déclenche grossir/rapetisser
//   sur la gold bar, particules, sfx, modifie le poids.
//   Idles aléatoires : cycle avec poids configurables dans
//   l'Inspector — le Taunt rare a un poids faible.
//   Dialogue : JouerDialogue(int) déclenche Talking/Yelling
//   depuis le système de sous-titres, séparé des idles.
//   Défaite : dialogue cinématique → Angry → Brutal Assassination.
// ------------------------------------------------------------
// Paramètres Animator attendus :
//   Triggers : DeclencherEntree, DeclencherMagie,
//              DeclencherIdleSpecial, DeclencherDialogue,
//              DeclencherDefaite, DeclencherMort
//   Ints     : TypeMagie    (0=apparaître, 1=grossir, 2=rétrécir)
//              IdleType     (1=Bored, 2=DizzyIdle, 3=DrunkIdle,
//                            4=FightIdle, 5=StandingIdle, 6=Taunt,
//                            7=Loser, 8=Battlecry, 9=Threatening)
//              DialogueType (1=Talking, 2=Talking1, 3=Yelling)
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
    [Tooltip("Son atmosphérique éerie joué AU DÉBUT de l'intro, " +
             "avant que le boss soit visible. Laisser vide pour ignorer.")]
    [SerializeField] private AudioClip _sfxVoixIntro;
    [Tooltip("Sons du boss pendant les idles spéciaux. " +
             "Index = IdleType (1=Bored … 9=Threatening). " +
             "Laisser vide si le SFX n'est pas prêt.")]
    [SerializeField] private AudioClip[] _sfxIdles = new AudioClip[10];
    [Tooltip("Son du boss pendant le dialogue (optionnel).")]
    [SerializeField] private AudioClip _sfxDialogue;

    [Header("Paramètres visuels")]
    [SerializeField] private float _delaiAvantParticules = 0.6f;
    [SerializeField] private float _echelleMax = 3f;
    [SerializeField] private float _echelleMin = 0.3f;

    [Header("Caméra intro")]
    [Tooltip("Caméra Cinemachine pointée sur le boss — active pendant " +
             "le dialogue d'intro.")]
    [SerializeField] private CinemachineCamera _vcamIntroManoir;
    [Tooltip("Caméra Cinemachine pointée sur la balance — activée au " +
             "moment du reveal de l'objet (ligne « C'est parti! »).")]
    [SerializeField] private CinemachineCamera _vcamBalance;
    [Tooltip("Secondes d'attente après l'activation de la caméra intro " +
             "avant de déclencher l'animation d'entrée du boss. " +
             "Laisse le temps au lerp Cinemachine de se terminer.")]
    [SerializeField] private float _delaiAvantAnimation = 1f;
    [Tooltip("Secondes d'attente entre l'animation d'entrée du boss " +
             "et la première réplique de dialogue.")]
    [SerializeField] private float _delaiApresEntree = 0.5f;
    [Tooltip("Secondes d'attente après la fin des particules de reveal " +
             "avant de rendre le contrôle au joueur.")]
    [SerializeField] private float _dureeApresRevealAvantJoueur = 1.5f;

    [Header("Sous-titres")]
    [Tooltip("Référence au gestionSousTitre de la scène. " +
             "Glisser le GameObject gestion_sous_titre ici.")]
    [SerializeField] private gestionSousTitre _sousTitre;
    [Tooltip("Durée d'affichage (secondes) des répliques sans voice-over " +
             "(ex : lignes du Joueur sans clip assigné).")]
    [SerializeField] private float _dureeRepliquesJoueur = 3f;

    [Header("Voice-over — dialogue intro (16 clips)")]
    [Tooltip("Un AudioClip par réplique d'intro, dans l'ordre du script.\n" +
             " 0 = HAHAHAHAH! Mais qui est-ce?\n" +
             " 1 = (Joueur) Toi! Tu es celui derrière…\n" +
             " 2 = (Joueur) et le corps de métal…\n" +
             " 3 = Ohohoh! Un petit détective!\n" +
             " 4 = Oui, c'est bien moi…\n" +
             " 5 = L'INCOMMENSURABLE,\n" +
             " 6 = le démon du métal et de l'imbalance!\n" +
             " 7 = Faisons un pacte…\n" +
             " 8 = Tu dois égaliser cette balance!\n" +
             " 9 = Si tu réussis, je libérerai tous\n" +
             "10 = que j'ai transformé en métal,\n" +
             "11 = et je ne reviendrai plus jamais…\n" +
             "12 = MAIS SI TU N'Y ARRIVES PAS,\n" +
             "13 = je ferai de toi une statue\n" +
             "14 = comme tous les autres! HAHAHAHAH!\n" +
             "15 = C'est parti!")]
    [SerializeField] private AudioClip[] _sfxVoixIntroDialogue = new AudioClip[16];

    [Header("Voice-over — dialogue défaite (6 clips)")]
    [Tooltip("Un AudioClip par réplique de défaite, dans l'ordre du script.\n" +
             " 0 = non, NON! Tu as réussi…\n" +
             " 1 = (soupire), eh bien, je tiens ma parole…\n" +
             " 2 = Tu as gagné!\n" +
             " 3 = J'ai levé le sort sur tous ceux\n" +
             " 4 = que j'ai transformé en métal.\n" +
             " 5 = Maintenant DÉGUERPIS!")]
    [SerializeField] private AudioClip[] _sfxVoixDefaiteDialogue = new AudioClip[6];

    [Header("Idles aléatoires")]
    [Tooltip("Secondes entre chaque idle spécial. Mettre 0 pour désactiver.")]
    [SerializeField] private float _intervalleIdleSpecial = 8f;
    [Tooltip("Durée minimale à attendre après un idle avant d'en jouer un autre.")]
    [SerializeField] private float _dureeMinEntreIdles = 3f;
    [Tooltip("Liste des idles spéciaux avec leur poids de probabilité.\n" +
             "Talking / Yelling sont gérés séparément via JouerDialogue() " +
             "— ne pas les inclure ici.")]
    [SerializeField] private IdleAleatoire[] _idles = new IdleAleatoire[]
    {
        new IdleAleatoire { idleType = 1, nom = "Bored",         poids = 15 },
        new IdleAleatoire { idleType = 2, nom = "Dizzy Idle",    poids = 10 },
        new IdleAleatoire { idleType = 3, nom = "Drunk Idle",    poids = 10 },
        new IdleAleatoire { idleType = 4, nom = "Fight Idle",    poids = 15 },
        new IdleAleatoire { idleType = 5, nom = "Standing Idle", poids = 15 },
        new IdleAleatoire { idleType = 6, nom = "Taunt (rare)",  poids = 3  },
        new IdleAleatoire { idleType = 7, nom = "Loser",         poids = 8  },
        new IdleAleatoire { idleType = 8, nom = "Battlecry",     poids = 8  },
        new IdleAleatoire { idleType = 9, nom = "Threatening",   poids = 12 },
    };

    [Header("Défaite")]
    [Tooltip("Secondes entre la fin du dialogue de défaite et la mort (DeclencherMort).")]
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
        // 1. Bloquer les inputs dès le début de l'intro
        //    (ModeCinematique ferme aussi toute UI ouverte)
        var inputs = FindObjectOfType<gestionInputsJeu>();
        inputs?.ModeCinematique(true);

        // 2. Activer la caméra intro (vue sur le boss)
        if (_vcamIntroManoir != null)
            _vcamIntroManoir.Priority = 60;

        // 3. Son atmosphérique éerie avant que le boss soit visible
        if (_sourceAudio != null && _sfxVoixIntro != null)
            _sourceAudio.PlayOneShot(_sfxVoixIntro);

        // 4. Attendre que Cinemachine finisse son lerp
        yield return new WaitForSeconds(_delaiAvantAnimation);

        // 5. Animation d'entrée du boss
        if (_animateur != null)
            _animateur.SetTrigger("DeclencherEntree");

        // 6. Beat avant la première réplique
        yield return new WaitForSeconds(_delaiApresEntree);

        // ── Dialogue d'intro ─────────────────────────────────────────

        // 0. Incommensurable — "HAHAHAHAH! Mais qui est-ce?" [Yelling]
        yield return StartCoroutine(JouerReplique(
            "Incommensurable", "HAHAHAHAH! Mais qui est-ce?",
            3, Clip(_sfxVoixIntroDialogue, 0)));

        // 1. Joueur — "Toi! Tu es celui derrière les enlèvements" [—]
        yield return StartCoroutine(JouerReplique(
            "Joueur", "Toi! Tu es celui derrière les enlèvements",
            0, Clip(_sfxVoixIntroDialogue, 1)));

        // 2. Joueur — "et le corps de métal de mon ami!" [—]
        yield return StartCoroutine(JouerReplique(
            "Joueur", "et le corps de métal de mon ami!",
            0, Clip(_sfxVoixIntroDialogue, 2)));

        // 3. Incommensurable — "Ohohoh! Un petit détective!" [Talking]
        yield return StartCoroutine(JouerReplique(
            "Incommensurable", "Ohohoh! Un petit détective!",
            1, Clip(_sfxVoixIntroDialogue, 3)));

        // 4. Incommensurable — "Oui, c'est bien moi…" [Talking]
        yield return StartCoroutine(JouerReplique(
            "Incommensurable", "Oui, c'est bien moi…",
            1, Clip(_sfxVoixIntroDialogue, 4)));

        // 5. Incommensurable — "L'INCOMMENSURABLE," [Yelling]
        yield return StartCoroutine(JouerReplique(
            "Incommensurable", "L'INCOMMENSURABLE,",
            3, Clip(_sfxVoixIntroDialogue, 5)));

        // 6. Incommensurable — "le démon du métal et de l'imbalance!" [Yelling]
        yield return StartCoroutine(JouerReplique(
            "Incommensurable", "le démon du métal et de l'imbalance!",
            3, Clip(_sfxVoixIntroDialogue, 6)));

        // 7. Incommensurable — "Faisons un pacte…" [Talking]
        yield return StartCoroutine(JouerReplique(
            "Incommensurable", "Faisons un pacte…",
            1, Clip(_sfxVoixIntroDialogue, 7)));

        // 8. Incommensurable — "Tu dois égaliser cette balance!" [Talking]
        yield return StartCoroutine(JouerReplique(
            "Incommensurable", "Tu dois égaliser cette balance!",
            1, Clip(_sfxVoixIntroDialogue, 8)));

        // 9. Incommensurable — "Si tu réussis, je libérerai tous" [Talking]
        yield return StartCoroutine(JouerReplique(
            "Incommensurable", "Si tu réussis, je libérerai tous",
            1, Clip(_sfxVoixIntroDialogue, 9)));

        // 10. Incommensurable — "que j'ai transformé en métal," [Talking]
        yield return StartCoroutine(JouerReplique(
            "Incommensurable", "que j'ai transformé en métal,",
            1, Clip(_sfxVoixIntroDialogue, 10)));

        // 11. Incommensurable — "et je ne reviendrai plus jamais…" [Talking]
        yield return StartCoroutine(JouerReplique(
            "Incommensurable", "et je ne reviendrai plus jamais…",
            1, Clip(_sfxVoixIntroDialogue, 11)));

        // 12. Incommensurable — "MAIS SI TU N'Y ARRIVES PAS," [Talking]
        yield return StartCoroutine(JouerReplique(
            "Incommensurable", "MAIS SI TU N'Y ARRIVES PAS,",
            1, Clip(_sfxVoixIntroDialogue, 12)));

        // 13. Incommensurable — "je ferai de toi une statue" [Yelling]
        yield return StartCoroutine(JouerReplique(
            "Incommensurable", "je ferai de toi une statue",
            3, Clip(_sfxVoixIntroDialogue, 13)));

        // 14. Incommensurable — "comme tous les autres! HAHAHAHAH!" [Yelling]
        yield return StartCoroutine(JouerReplique(
            "Incommensurable", "comme tous les autres! HAHAHAHAH!",
            3, Clip(_sfxVoixIntroDialogue, 14)));

        // ── Reveal de la balance ──────────────────────────────────────

        // Basculer la caméra vers la balance (plus haute priorité)
        if (_vcamBalance != null)
            _vcamBalance.Priority = 70;
        if (_vcamIntroManoir != null)
            _vcamIntroManoir.Priority = 0;

        // 15. Incommensurable — "C'est parti!" [Yelling]
        //     Joue pendant que la caméra bascule vers la balance
        yield return StartCoroutine(JouerReplique(
            "Incommensurable", "C'est parti!",
            3, Clip(_sfxVoixIntroDialogue, 15)));

        // Boss fait la magie : TypeMagie=0 = animation "apparaître"
        if (_animateur != null)
        {
            _animateur.SetInteger("TypeMagie", 0);
            _animateur.SetTrigger("DeclencherMagie");
        }

        yield return new WaitForSeconds(_delaiAvantParticules);

        // Révéler l'objet avec particules + sfx
        if (_objetSurBalance != null)
            _objetSurBalance.gameObject.SetActive(true);

        JouerParticulesEtSon();

        // Notifier la zone que l'objet est en place
        _zoneAdverse.DefinirObjet(_objetSurBalance);

        // Attendre la fin des particules
        yield return new WaitUntil(() => ParticulesTerminees());

        // Beat final avant de rendre le contrôle
        yield return new WaitForSeconds(_dureeApresRevealAvantJoueur);

        // Remettre les deux caméras à priorité nulle
        if (_vcamIntroManoir != null)
            _vcamIntroManoir.Priority = 0;
        if (_vcamBalance != null)
            _vcamBalance.Priority = 0;

        // Rendre le contrôle au joueur
        inputs?.ModeCinematique(false);

        Debug.Log("[ComportementAntagoniste] Intro terminée — contrôle rendu au joueur.");

        // Démarrer le cycle d'idles aléatoires
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

        // 3. Animation objet (grossir ou rapetisser)
        if (_animateurObjet != null)
        {
            string triggerObjet = impact.grossit
                ? "DeclencherGrossir"
                : "DeclencherRapetisser";
            _animateurObjet.SetTrigger(triggerObjet);
        }

        // 4. Échelle visuelle
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
    /// Déclenche périodiquement un idle spécial aléatoire pondéré.
    /// Ex : Taunt poids=3 sur total ~130 ≈ 2.3% de chances à chaque tirage.
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

            JouerSfxIdle(idleChoisi.idleType);

            Debug.Log($"[ComportementAntagoniste] Idle spécial : " +
                      $"{idleChoisi.nom} (IdleType={idleChoisi.idleType})");

            yield return new WaitForSeconds(_dureeMinEntreIdles);

            if (_animateur != null)
                _animateur.SetInteger("IdleType", 0);
        }
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

        // Bloquer les inputs — début de la cinématique de défaite
        var inputs = FindObjectOfType<gestionInputsJeu>();
        inputs?.ModeCinematique(true);

        // Animation Angry (début de la séquence défaite)
        if (_animateur != null)
            _animateur.SetTrigger("DeclencherDefaite");

        Debug.Log("[ComportementAntagoniste] Défaite — Angry déclenché.");

        // Petit délai pour que l'animation Angry s'enclence
        yield return new WaitForSeconds(1f);

        // ── Dialogue défaite ──────────────────────────────────────────

        // 0. Incommensurable — "non, NON! Tu as réussi…" [Talking]
        yield return StartCoroutine(JouerReplique(
            "Incommensurable", "non, NON! Tu as réussi…",
            1, Clip(_sfxVoixDefaiteDialogue, 0)));

        // 1. Incommensurable — "(soupire), eh bien, je tiens ma parole…" [Talking]
        yield return StartCoroutine(JouerReplique(
            "Incommensurable", "(soupire), eh bien, je tiens ma parole…",
            1, Clip(_sfxVoixDefaiteDialogue, 1)));

        // 2. Incommensurable — "Tu as gagné!" [Talking]
        yield return StartCoroutine(JouerReplique(
            "Incommensurable", "Tu as gagné!",
            1, Clip(_sfxVoixDefaiteDialogue, 2)));

        // 3. Incommensurable — "J'ai levé le sort sur tous ceux" [Talking]
        yield return StartCoroutine(JouerReplique(
            "Incommensurable", "J'ai levé le sort sur tous ceux",
            1, Clip(_sfxVoixDefaiteDialogue, 3)));

        // 4. Incommensurable — "que j'ai transformé en métal." [Talking]
        yield return StartCoroutine(JouerReplique(
            "Incommensurable", "que j'ai transformé en métal.",
            1, Clip(_sfxVoixDefaiteDialogue, 4)));

        // 5. Incommensurable — "Maintenant DÉGUERPIS!" [Yelling]
        yield return StartCoroutine(JouerReplique(
            "Incommensurable", "Maintenant DÉGUERPIS!",
            3, Clip(_sfxVoixDefaiteDialogue, 5)));

        // ── Séquence de mort ──────────────────────────────────────────

        // Délai entre la dernière réplique et l'animation de mort
        yield return new WaitForSeconds(_delaiAvantMort);

        // Brutal Assassination
        if (_animateur != null)
            _animateur.SetTrigger("DeclencherMort");

        Debug.Log("[ComportementAntagoniste] Mort déclenchée.");

        // Note : ModeCinematique(false) n'est PAS appelé ici.
        // La transition vers la scène suivante s'en chargera via
        // gestionEcranChargement.ChargerScene().
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
    /// Déclenche la séquence de défaite : dialogue cinématique →
    /// Angry → Defeat Idle → Brutal Assassination.
    /// À appeler depuis gestionnaireEnigmeBalance quand le joueur gagne.
    /// </summary>
    public void JouerDefaite()
    {
        StartCoroutine(JouerDefaiteSequence());
    }

    /// <summary>
    /// Déclenche une animation de dialogue (Talking, Talking1, Yelling).
    /// Le boss retourne automatiquement à Idle 0 via Has Exit Time.
    /// </summary>
    /// <param name="dialogueType">1=Talking, 2=Talking1, 3=Yelling</param>
    public void JouerDialogue(int dialogueType)
    {
        if (_animateur == null) return;

        _animateur.SetInteger("DialogueType", dialogueType);
        _animateur.SetTrigger("DeclencherDialogue");

        if (_sourceAudio != null && _sfxDialogue != null)
            _sourceAudio.PlayOneShot(_sfxDialogue);

        Debug.Log($"[ComportementAntagoniste] Dialogue : DialogueType={dialogueType}");
    }

    // ===================== MÉTHODES PRIVÉES =====================

    /// <summary>
    /// Affiche un sous-titre, déclenche l'animation de dialogue du boss
    /// et joue le voice-over, puis attend la fin de la réplique.
    /// </summary>
    /// <param name="interlocuteur">Nom affiché dans les sous-titres.</param>
    /// <param name="texte">Réplique à afficher.</param>
    /// <param name="dialogueType">0=pas d'anim boss ; 1=Talking, 2=Talking1, 3=Yelling.</param>
    /// <param name="voiceClip">Voice-over. Si null, durée = _dureeRepliquesJoueur.</param>
    private IEnumerator JouerReplique(string interlocuteur, string texte,
        int dialogueType, AudioClip voiceClip)
    {
        float duree = (voiceClip != null)
            ? (float)voiceClip.length
            : _dureeRepliquesJoueur;

        // Déclencher l'animation de dialogue du boss (si applicable)
        if (dialogueType > 0)
            JouerDialogue(dialogueType);

        // Jouer le voice-over
        if (voiceClip != null && _sourceAudio != null)
            _sourceAudio.PlayOneShot(voiceClip);

        // Afficher le sous-titre pour la durée du clip
        if (_sousTitre != null)
            _sousTitre.AfficherSousTitre(interlocuteur, texte, duree);

        yield return new WaitForSeconds(duree);
    }

    /// <summary>
    /// Retourne un clip depuis un tableau en vérifiant les bornes.
    /// Retourne null si le tableau est trop petit ou non assigné.
    /// </summary>
    private AudioClip Clip(AudioClip[] tableau, int index)
    {
        if (tableau == null || index < 0 || index >= tableau.Length)
            return null;
        return tableau[index];
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

        return _idles[_idles.Length - 1];
    }

    /// <summary>
    /// Joue le SFX correspondant à l'IdleType, si le clip est assigné.
    /// </summary>
    private void JouerSfxIdle(int idleType)
    {
        if (_sourceAudio == null || _sfxIdles == null) return;
        if (idleType < 1 || idleType >= _sfxIdles.Length) return;

        AudioClip clip = _sfxIdles[idleType];
        if (clip != null)
            _sourceAudio.PlayOneShot(clip);
    }

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
