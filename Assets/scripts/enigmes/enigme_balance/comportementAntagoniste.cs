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
//   Dialogue : JouerDialogue(int) déclenche Talking/Yelling.
//   Défaite : dialogue cinématique → Angry → Brutal Assassination.
// ------------------------------------------------------------
// Paramètres Animator attendus :
//   Triggers : DeclencherEntree, DeclencherMagie,
//              DeclencherIdleSpecial, DeclencherDialogue,
//              DeclencherDefaite, DeclencherMort
//   Ints     : TypeMagie    (0=apparaître, 1=grossir, 2=rétrécir)
//              IdleType     (1=Bored … 9=Threatening)
//              DialogueType (1=Talking, 2=Talking1, 3=Yelling)
// ============================================================

using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

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
    [Tooltip("Son atmosphérique éerie joué au début de l'intro.")]
    [SerializeField] private AudioClip _sfxVoixIntro;
    [Tooltip("Sons du boss pendant les idles spéciaux (index = IdleType).")]
    [SerializeField] private AudioClip[] _sfxIdles = new AudioClip[10];
    [Tooltip("Son du boss pendant le dialogue (optionnel).")]
    [SerializeField] private AudioClip _sfxDialogue;

    [Header("Paramètres visuels")]
    [SerializeField] private float _delaiAvantParticules = 0.6f;
    [SerializeField] private float _echelleMax = 3f;
    [SerializeField] private float _echelleMin = 0.3f;

    [Header("Caméra intro")]
    [Tooltip("Nom exact du VideoClip de la cinématique d'ouverture de la scène manoir " +
             "(sans extension). Jouée en tout premier, avant le dialogue d'intro du boss. " +
             "Doit correspondre à un clip dans le tableau _clips du controleurCinematique.")]
    [SerializeField] private string _nomCinematique3 = "cinematique3";
    [Tooltip("Caméra Cinemachine pointée sur le boss pendant le dialogue d'intro.")]
    [SerializeField] private CinemachineCamera _vcamIntroManoir;
    [Tooltip("Caméra Cinemachine pointée sur la balance — activée au reveal et au début de chaque phase.")]
    [SerializeField] private CinemachineCamera _vcamBalance;
    [Tooltip("Caméra Cinemachine zoomée sur l'objet de la ZoneAntagoniste — activée pendant l'animation d'impact.")]
    [SerializeField] private CinemachineCamera _vcamZoneAntagoniste;
    [Tooltip("Secondes après l'activation de la caméra avant l'animation d'entrée.")]
    [SerializeField] private float _delaiAvantAnimation = 1f;
    [Tooltip("Beat configurable après la fin de l'animation d'entrée, avant la première réplique.")]
    [SerializeField] private float _delaiApresEntree = 0.3f;
    [Tooltip("Secondes après la fin des particules avant de rendre le contrôle au joueur.")]
    [SerializeField] private float _dureeApresRevealAvantJoueur = 1.5f;

    [Header("Sous-titres")]
    [Tooltip("Référence au gestionSousTitre de la scène.")]
    [SerializeField] private gestionSousTitre _sousTitre;
    [Tooltip("Durée d'affichage des répliques sans voice-over (lignes du Joueur).")]
    [SerializeField] private float _dureeRepliquesJoueur = 3f;
    [Tooltip("Secondes ajoutées à la longueur du clip pour laisser le sous-titre " +
             "affiché un peu après la fin de la voix.")]
    [SerializeField] private float _dureeSupplementaireAffichage = 0.5f;
    [Tooltip("Durée minimale d'affichage d'un sous-titre, même si le clip est très court.")]
    [SerializeField] private float _dureeMinimaleAffichage = 1.5f;

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

    [Header("Animation dialogue")]
    [Tooltip("Si coché, alterne Talking (1) ↔ Talking1 (2) à chaque réplique boss. " +
             "Le switch se fait pendant le délai entre les répliques pour éviter le " +
             "retour en idle. Décocher si les transitions restent trop brusques " +
             "malgré le Transition Duration dans l'Animator.")]
    [SerializeField] private bool _alternerAnimationsTalking = true;
    [Tooltip("IdleType utilisé entre les répliques du joueur et après le Yelling. " +
             "2 = Dizzy Idle (mouvement des bras moins intense). 0 = idle par défaut (bras flottants).")]
    [SerializeField] private int _idleTypeDialogue = 2;

    [Header("Délais entre répliques — intro")]
    [Tooltip("Secondes de silence APRÈS chaque réplique, avant la suivante.\n" +
             "C'est pendant ce délai que la prochaine animation démarre " +
             "pour blender directement sans passer par l'idle.\n" +
             "Index 0 = pause après réplique 0, etc.")]
    [SerializeField] private float[] _delaisIntro = new float[16];

    [Header("Délais entre répliques — défaite")]
    [Tooltip("Secondes de silence APRÈS chaque réplique de défaite.")]
    [SerializeField] private float[] _delaisDefaite = new float[6];

    [Header("Idles aléatoires")]
    [Tooltip("Secondes entre chaque idle spécial. 0 pour désactiver.")]
    [SerializeField] private float _intervalleIdleSpecial = 8f;
    [Tooltip("Durée minimale à attendre après un idle avant d'en jouer un autre.")]
    [SerializeField] private float _dureeMinEntreIdles = 3f;
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

    [Header("Regard vers le joueur")]
    [Tooltip("Transform du joueur (ou de sa caméra FP) vers lequel le boss pivote en permanence. " +
             "Glisser l'objet joueur ici dans l'Inspector.")]
    [SerializeField] private Transform _cibleRegard;
    [Tooltip("Vitesse de rotation smooth vers le joueur (Slerp par frame). " +
             "0.5 ≈ 2 secondes pour corriger un pivotement d'animation.")]
    [SerializeField] private float _vitesseRotation = 0.5f;

    [Header("Défaite")]
    [Tooltip("Nom exact du VideoClip de la cinématique finale (sans extension). " +
             "Doit correspondre à un clip dans le tableau _clips du controleurCinematique.")]
    [SerializeField] private string _nomCinematiqueFin = "cinematique4";
    [Tooltip("Secondes passées sur la caméra balance après le dernier équilibre, " +
             "avant de lancer la cinématique finale.")]
    [SerializeField] private float _delaiAvantCinematiqueFinale = 1.5f;

    [Header("Réactions de phase")]
    [Tooltip("Trigger Animator pour la réaction de Phase 1 (ex : DeclencherAngry).")]
    [SerializeField] private string _triggerPhase1 = "DeclencherAngry";
    [Tooltip("Trigger Animator pour la réaction de Phase 2. " +
             "Laissez vide pour utiliser l'idle Battlecry (IdleType=8 + DeclencherIdleSpecial).")]
    [SerializeField] private string _triggerPhase2 = "";
    [Tooltip("Son joué sur le boss au déclenchement de la réaction de Phase 1 (cri de colère, etc.).")]
    [SerializeField] private AudioClip _sfxReactionPhase1;
    [Tooltip("Son joué sur le boss au déclenchement de la réaction de Phase 2.")]
    [SerializeField] private AudioClip _sfxReactionPhase2;
    [Tooltip("Secondes passées sur la caméra balance après l'égalisation. Laisse voir la balance + particules.")]
    [SerializeField] private float _delaiVueBalance = 1.5f;
    [Tooltip("Secondes à attendre après le trigger de phase 1 avant de switcher sur la caméra zone antagoniste.")]
    [SerializeField] private float _dureeAnimationPhase1 = 2f;
    [Tooltip("Secondes à attendre après le trigger de phase 2.")]
    [SerializeField] private float _dureeAnimationPhase2 = 2f;
    [Tooltip("Secondes après le début de l'animation d'impact boss avant de switcher sur vcamZoneAntagoniste.")]
    [SerializeField] private float _delaiAvantCameraZone = 1f;
    [Tooltip("Secondes après la fin des particules d'impact de phase, avant de rendre le contrôle au joueur.")]
    [SerializeField] private float _dureeApresImpactPhase = 1f;
    [Tooltip("Particules d'équilibration sur la balance — déclenchées quand la caméra balance s'active.")]
    [SerializeField] private ParticleSystem _particulesEquilibre;
    [Tooltip("idChapitre du ScriptableObject DonneesChapitre à afficher après le reveal " +
             "(Étape 1/3). Doit correspondre à un chapitre enregistré dans gestionChapitres.")]
    [SerializeField] private string _idChapitreEtape1 = "etape_manoir_1";

    // ===================== ÉVÉNEMENTS =====================
    public event Action OnActionTerminee;

    // ===================== ÉTAT INTERNE =====================
    private Coroutine _coroutineIdlesCycle;
    private bool _jeuEnCours = false;

    // Alterne Talking (1) ↔ Talking1 (2). Remis à 1 au début de chaque séquence.
    private int _talkingAlterne = 1;

    // Vrai si l'animation de la prochaine réplique a déjà été pré-déclenchée
    // pendant le délai de la réplique précédente. JouerReplique saute alors
    // son propre déclenchement pour éviter le double-trigger.
    private bool _animDejaPreDeclenche = false;

    // Référence aux inputs, stockée au début de JouerIntroScene pour pouvoir
    // l'utiliser depuis SkipIntroDialogue() déclenché par Update().
    private gestionInputsJeu _inputs;

    // True pendant la séquence de dialogues de l'intro manoir.
    // Update() surveille ESC pour déclencher SkipIntroDialogue().
    private bool _introEnCours = false;

    // Rotation Y (eulerAngles.y) sauvegardée au début de l'intro.
    // Réimposée chaque frame pendant _bloquerRotationYIntro = true pour
    // neutraliser la dérive causée par le root motion des animations.
    private float _rotationYIntro;
    private bool _bloquerRotationYIntro = false;

    // True quand JouerReactionPhaseCoroutine est terminée.
    // Initialisé à true : pas de réaction en attente au démarrage.
    // Utilisé par gestionnaireEnigmeBalance (WaitUntil) pour attendre
    // la fin de la séquence avant d'afficher le bandeau.
    private bool _reactionPhaseTerminee = true;
    public bool ReactionPhaseTerminee => _reactionPhaseTerminee;

    // ===================== UNITY =====================

    void Start()
    {
        if (_objetSurBalance != null)
            _objetSurBalance.gameObject.SetActive(false);

        // Jouer la cinématique d'ouverture de la scène en tout premier.
        // Tout le reste (dialogue boss, énigme) démarre dans le callback,
        // une fois la vidéo terminée.
        // Si controleurCinematique n'est pas dans la scène ou que le clip
        // est introuvable, JouerIntroScene démarre quand même immédiatement
        // pour ne pas bloquer le jeu.
        if (gestionChapitres.Instance != null
            && !string.IsNullOrEmpty(_nomCinematique3))
        {
            gestionChapitres.Instance.LancerCinematiqueAvecDelai(
                _nomCinematique3,
                0f,
                () => StartCoroutine(JouerIntroScene()));
        }
        else
        {
            Debug.LogWarning("[ComportementAntagoniste] gestionChapitres introuvable " +
                             "ou _nomCinematique3 vide — intro lancée sans cinématique.");
            StartCoroutine(JouerIntroScene());
        }
    }

    void Update()
    {
        // ── Skip intro (ESC) ──────────────────────────────────────────
        // Pendant le dialogue d'intro uniquement. gestionInputsJeu ne capte
        // pas ESC ici (jeuActif = false pendant la cinématique).
        if (_introEnCours && Keyboard.current != null
            && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            SkipIntroDialogue();
        }

        // ── Verrouillage rotation Y pendant l'intro ───────────────────
        // Les animations d'intro ont du root motion qui dévie la rotation Y.
        // On réimpose l'angle de départ chaque frame pour que le boss reste
        // face à la caméra intro jusqu'au reveal de la balance.
        if (_bloquerRotationYIntro)
        {
            Vector3 euler = transform.eulerAngles;
            euler.y = _rotationYIntro;
            transform.eulerAngles = euler;
        }

        // ── Rotation smooth vers le joueur ────────────────────────────
        // Corrige les pivotements causés par les animations (même avec
        // les contraintes de position/rotation activées, certaines
        // root-motions peuvent dériver). Actif uniquement pendant le jeu
        // (pas pendant les cinématiques, l'intro ou les séquences de phase).
        if (_jeuEnCours && _cibleRegard != null)
        {
            Vector3 direction = _cibleRegard.position - transform.position;
            direction.y = 0f; // Rotation horizontale uniquement — pas d'inclinaison

            if (direction.sqrMagnitude > 0.01f)
            {
                Quaternion cibleRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    cibleRotation,
                    Time.deltaTime * _vitesseRotation);
            }
        }
    }

    // ===================== COROUTINES =====================

    private IEnumerator JouerIntroScene()
    {
        _talkingAlterne = 1;
        _animDejaPreDeclenche = false;

        // Sauvegarder et verrouiller la rotation Y dès le départ.
        // Le root motion des animations d'intro dévie le boss vers la gauche —
        // on neutralise ça frame par frame dans Update().
        _rotationYIntro = transform.eulerAngles.y;
        _bloquerRotationYIntro = true;

        _inputs = FindObjectOfType<gestionInputsJeu>();
        _inputs?.ModeCinematique(true);

        if (_vcamIntroManoir != null)
            _vcamIntroManoir.Priority = 60;

        if (_sourceAudio != null && _sfxVoixIntro != null)
            _sourceAudio.PlayOneShot(_sfxVoixIntro);

        yield return new WaitForSeconds(_delaiAvantAnimation);

        if (_animateur != null)
            _animateur.SetTrigger("DeclencherEntree");

        // Attendre la fin réelle de l'animation d'entrée avant le dialogue
        yield return null;
        yield return null;
        if (_animateur != null)
        {
            if (_animateur.IsInTransition(0))
                yield return new WaitUntil(() => !_animateur.IsInTransition(0));

            yield return new WaitUntil(() =>
                _animateur.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f
                || _animateur.IsInTransition(0));
        }
        yield return new WaitForSeconds(_delaiApresEntree);

        // Activer le flag : à partir d'ici ESC peut skipper le dialogue.
        _introEnCours = true;

        // ── Dialogue d'intro ──────────────────────────────────────────
        // prochainTypeAnim : type brut (1=Talking, 3=Yelling) de la prochaine
        // LIGNE BOSS. 0 si la prochaine ligne est du Joueur ou si c'est la dernière.
        // L'animation est déclenchée au début du délai pour blender directement
        // vers le prochain état sans passer par l'idle.

        // 0. Incommensurable — "HAHAHAHAH! Mais qui est-ce?" [Talking]
        //    Prochaine ligne boss : aucune (lignes 1-2 = Joueur) → 0
        yield return StartCoroutine(JouerReplique(
            "Incommensurable", "HAHAHAHAH! Mais qui est-ce?",
            1, Clip(_sfxVoixIntroDialogue, 0), Delai(_delaisIntro, 0),
            prochainTypeAnim: 0));

        // 1. Joueur — boss en idle (pas de pré-trigger, prochaine ligne = Joueur)
        yield return StartCoroutine(JouerReplique(
            "Joueur", "Toi! Tu es celui derrière les enlèvements",
            0, Clip(_sfxVoixIntroDialogue, 1), Delai(_delaisIntro, 1),
            prochainTypeAnim: 0));

        // 2. Joueur — prochaine ligne boss = Talking → pré-trigger pendant le délai
        yield return StartCoroutine(JouerReplique(
            "Joueur", "et le corps de métal de mon ami!",
            0, Clip(_sfxVoixIntroDialogue, 2), Delai(_delaisIntro, 2),
            prochainTypeAnim: 1));

        // 3. Incommensurable — "Ohohoh! Un petit détective!" [Talking]
        //    Prochaine = Talking → pré-trigger
        yield return StartCoroutine(JouerReplique(
            "Incommensurable", "Ohohoh! Un petit détective!",
            1, Clip(_sfxVoixIntroDialogue, 3), Delai(_delaisIntro, 3),
            prochainTypeAnim: 1));

        // 4. Incommensurable — "Oui, c'est bien moi…" [Talking]
        //    Prochaine = Yelling → pré-trigger
        yield return StartCoroutine(JouerReplique(
            "Incommensurable", "Oui, c'est bien moi…",
            1, Clip(_sfxVoixIntroDialogue, 4), Delai(_delaisIntro, 4),
            prochainTypeAnim: 3));

        // 5. Incommensurable — "L'INCOMMENSURABLE," [Yelling]
        //    Prochaine = Yelling → pré-trigger
        yield return StartCoroutine(JouerReplique(
            "Incommensurable", "L'INCOMMENSURABLE,",
            3, Clip(_sfxVoixIntroDialogue, 5), Delai(_delaisIntro, 5),
            prochainTypeAnim: 3));

        // 6. Incommensurable — "le démon du métal et de l'imbalance!" [Yelling]
        //    Prochaine = Talking → pré-trigger
        yield return StartCoroutine(JouerReplique(
            "Incommensurable", "le démon du métal et de l'imbalance!",
            3, Clip(_sfxVoixIntroDialogue, 6), Delai(_delaisIntro, 6),
            prochainTypeAnim: 1));

        // 7. Incommensurable — "Faisons un pacte…" [Talking]
        //    Prochaine = Talking → pré-trigger
        yield return StartCoroutine(JouerReplique(
            "Incommensurable", "Faisons un pacte…",
            1, Clip(_sfxVoixIntroDialogue, 7), Delai(_delaisIntro, 7),
            prochainTypeAnim: 1));

        // 8. Incommensurable — "Tu dois égaliser cette balance!" [Talking]
        //    Prochaine = Talking → pré-trigger
        yield return StartCoroutine(JouerReplique(
            "Incommensurable", "Tu dois égaliser cette balance!",
            1, Clip(_sfxVoixIntroDialogue, 8), Delai(_delaisIntro, 8),
            prochainTypeAnim: 1));

        // 9. Incommensurable — "Si tu réussis, je libérerai tous" [Talking]
        //    Prochaine = Talking → pré-trigger
        yield return StartCoroutine(JouerReplique(
            "Incommensurable", "Si tu réussis, je libérerai tous",
            1, Clip(_sfxVoixIntroDialogue, 9), Delai(_delaisIntro, 9),
            prochainTypeAnim: 1));

        // 10. Incommensurable — "que j'ai transformé en métal," [Talking]
        //     Prochaine = Talking → pré-trigger
        yield return StartCoroutine(JouerReplique(
            "Incommensurable", "que j'ai transformé en métal,",
            1, Clip(_sfxVoixIntroDialogue, 10), Delai(_delaisIntro, 10),
            prochainTypeAnim: 1));

        // 11. Incommensurable — "et je ne reviendrai plus jamais…" [Talking]
        //     Prochaine = Talking → pré-trigger
        yield return StartCoroutine(JouerReplique(
            "Incommensurable", "et je ne reviendrai plus jamais…",
            1, Clip(_sfxVoixIntroDialogue, 11), Delai(_delaisIntro, 11),
            prochainTypeAnim: 1));

        // 12. Incommensurable — "MAIS SI TU N'Y ARRIVES PAS," [Talking]
        //     Prochaine = Yelling → pré-trigger
        yield return StartCoroutine(JouerReplique(
            "Incommensurable", "MAIS SI TU N'Y ARRIVES PAS,",
            1, Clip(_sfxVoixIntroDialogue, 12), Delai(_delaisIntro, 12),
            prochainTypeAnim: 3));

        // 13. Incommensurable — "je ferai de toi une statue" [Yelling]
        //     Prochaine = Yelling → pré-trigger
        yield return StartCoroutine(JouerReplique(
            "Incommensurable", "je ferai de toi une statue",
            3, Clip(_sfxVoixIntroDialogue, 13), Delai(_delaisIntro, 13),
            prochainTypeAnim: 3));

        // 14. Incommensurable — "comme tous les autres! HAHAHAHAH!" [Yelling]
        //     Prochaine = Yelling (C'est parti) → pré-trigger
        yield return StartCoroutine(JouerReplique(
            "Incommensurable", "comme tous les autres! HAHAHAHAH!",
            3, Clip(_sfxVoixIntroDialogue, 14), Delai(_delaisIntro, 14),
            prochainTypeAnim: 3));

        // ── Reveal de la balance ──────────────────────────────────────
        // Plus de skip ESC à partir d'ici (dernière réplique courte).
        _introEnCours = false;

        if (_vcamBalance != null)
            _vcamBalance.Priority = 70;
        if (_vcamIntroManoir != null)
            _vcamIntroManoir.Priority = 0;

        // 15. Incommensurable — "C'est parti!" [Yelling] — dernière réplique
        yield return StartCoroutine(JouerReplique(
            "Incommensurable", "C'est parti!",
            3, Clip(_sfxVoixIntroDialogue, 15), Delai(_delaisIntro, 15),
            prochainTypeAnim: 0));

        yield return StartCoroutine(JouerRevealBalance());
    }

    /// <summary>
    /// Déclenché par ESC pendant le dialogue d'intro.
    /// Arrête tout le dialogue et saute directement au reveal de la balance.
    /// </summary>
    private void SkipIntroDialogue()
    {
        if (!_introEnCours) return;
        _introEnCours = false;

        StopAllCoroutines();

        // Arrêter l'audio et masquer les sous-titres immédiatement.
        if (_sourceAudio != null) _sourceAudio.Stop();
        if (_sousTitre != null)   _sousTitre.MasquerSousTitre();

        // Remettre le boss en idle (il était peut-être en Talking/Yelling).
        if (_animateur != null)
            _animateur.SetInteger("DialogueType", 0);

        StartCoroutine(JouerRevealBalance());
    }

    /// <summary>
    /// Gère le reveal de l'objet sur la balance, les particules et la
    /// reprise du contrôle joueur. Appelé en fin normale de JouerIntroScene
    /// OU directement par SkipIntroDialogue() si ESC est pressé pendant
    /// les dialogues.
    /// </summary>
    private IEnumerator JouerRevealBalance()
    {
        // Switch caméra (idempotent — safe même si déjà fait en chemin normal)
        if (_vcamBalance != null)     _vcamBalance.Priority = 70;
        if (_vcamIntroManoir != null) _vcamIntroManoir.Priority = 0;

        // La caméra est maintenant sur la balance — le boss n'est plus visible.
        // On libère le verrou de rotation Y pour que le smooth look-at puisse
        // reprendre librement dès le retour en jeu.
        _bloquerRotationYIntro = false;

        // Animation d'apparition de l'objet sur la balance
        if (_animateur != null)
        {
            _animateur.SetInteger("TypeMagie", 0);
            _animateur.SetTrigger("DeclencherMagie");
        }

        yield return new WaitForSeconds(_delaiAvantParticules);

        if (_objetSurBalance != null)
            _objetSurBalance.gameObject.SetActive(true);

        JouerParticulesEtSon();
        _zoneAdverse.DefinirObjet(_objetSurBalance);

        yield return new WaitUntil(() => ParticulesTerminees());
        yield return new WaitForSeconds(_dureeApresRevealAvantJoueur);

        if (_vcamIntroManoir != null) _vcamIntroManoir.Priority = 0;
        if (_vcamBalance != null)     _vcamBalance.Priority = 0;

        // Réactiver les scripts de pointeur désactivés par ModeCinematique(true).
        // ActiverInputs() ne le fait pas — seul ModeCinematique(false) le faisait,
        // mais celui-ci ne reverrouillait pas la souris ni le HUD.
        var pointeur = FindObjectOfType<gestionPointeur>(true);
        if (pointeur != null) pointeur.gameObject.SetActive(true);
        var testP = FindObjectOfType<testPointeur>(true);
        if (testP != null) testP.enabled = true;

        // ActiverInputs() :
        // - reverrouille le curseur pour le FPS ✓
        // - réaffiche le HUD (canvasHud + groupeContenuHud) ✓
        // - remet etatActuel = EnJeu + jeuActif = true ✓
        _inputs?.ActiverInputs();
        gestionChapitres.Instance?.DemarrerChapitre(_idChapitreEtape1);
        Debug.Log("[ComportementAntagoniste] Intro terminée — bannière Étape 1/3 déclenchée.");

        _jeuEnCours = true;
        if (_intervalleIdleSpecial > 0f && _idles != null && _idles.Length > 0)
            _coroutineIdlesCycle = StartCoroutine(CyclerIdlesAleatoires());
    }

    /// <summary>
    /// Séquence complète de réaction de phase :
    ///   1. Caméra balance → particules d'équilibre → délai
    ///   2. Caméra boss → son + animation de réaction
    ///   3. Déclencher l'animation d'impact boss (DeclencherMagie) → délai
    ///   4. Caméra zone antagoniste → animation objet + particules + poids
    ///   5. Retour caméra FP → réactiver pointeur + ActiverInputs()
    ///   6. Relancer les idles + ReactionPhaseTerminee = true
    /// </summary>
    private IEnumerator JouerReactionPhaseCoroutine(gestionnaireEnigmeBalance.PhaseEnigme phase)
    {
        _reactionPhaseTerminee = false;
        _jeuEnCours = false;
        if (_coroutineIdlesCycle != null)
        {
            StopCoroutine(_coroutineIdlesCycle);
            _coroutineIdlesCycle = null;
        }

        // ── 1. Caméra balance ────────────────────────────────────────
        // Activer en premier pour voir l'égalisation + particules.
        if (_vcamBalance != null)     _vcamBalance.Priority = 70;
        if (_vcamIntroManoir != null) _vcamIntroManoir.Priority = 0;
        if (_vcamZoneAntagoniste != null) _vcamZoneAntagoniste.Priority = 0;

        // Particules d'équilibre déclenchées maintenant qu'on voit la balance.
        if (_particulesEquilibre != null)
        {
            _particulesEquilibre.Stop(true,
                ParticleSystemStopBehavior.StopEmittingAndClear);
            _particulesEquilibre.Play(true);
        }

        yield return new WaitForSeconds(_delaiVueBalance);

        // ── 2. Caméra boss — réaction ────────────────────────────────
        _inputs?.ModeCinematique(true);
        if (_vcamIntroManoir != null) _vcamIntroManoir.Priority = 60;
        if (_vcamBalance != null)     _vcamBalance.Priority = 0;

        // Son de réaction selon la phase
        AudioClip sfxReaction = (phase == gestionnaireEnigmeBalance.PhaseEnigme.Phase1)
            ? _sfxReactionPhase1 : _sfxReactionPhase2;
        if (_sourceAudio != null && sfxReaction != null)
            _sourceAudio.PlayOneShot(sfxReaction);

        // Animation de réaction du boss
        if (_animateur != null)
        {
            if (phase == gestionnaireEnigmeBalance.PhaseEnigme.Phase1)
            {
                if (!string.IsNullOrEmpty(_triggerPhase1))
                    _animateur.SetTrigger(_triggerPhase1);
                yield return new WaitForSeconds(_dureeAnimationPhase1);
            }
            else
            {
                if (!string.IsNullOrEmpty(_triggerPhase2))
                    _animateur.SetTrigger(_triggerPhase2);
                else
                {
                    _animateur.SetInteger("IdleType", 8);
                    _animateur.SetTrigger("DeclencherIdleSpecial");
                }
                yield return new WaitForSeconds(_dureeAnimationPhase2);
            }
        }

        // ── 3. Déclencher l'animation d'impact boss + sélectionner l'impact ──
        impactAntagoniste[] impacts = (phase == gestionnaireEnigmeBalance.PhaseEnigme.Phase1)
            ? _listeImpacts?.impactsPhase1
            : _listeImpacts?.impactsPhase2;

        bool aImpact = impacts != null && impacts.Length > 0;
        impactAntagoniste impactChoisi = aImpact
            ? impacts[UnityEngine.Random.Range(0, impacts.Length)]
            : default;

        if (aImpact && _animateur != null)
        {
            _animateur.SetInteger("TypeMagie", impactChoisi.grossit ? 1 : 2);
            _animateur.SetTrigger("DeclencherMagie");
        }

        // Délai avant le switch sur la caméra zone antagoniste.
        // Laisse voir le début de l'animation d'impact sur la caméra boss.
        yield return new WaitForSeconds(_delaiAvantCameraZone);

        // ── 4. Caméra zone antagoniste — objet se transforme ─────────
        if (_vcamZoneAntagoniste != null) _vcamZoneAntagoniste.Priority = 80;
        if (_vcamIntroManoir != null)     _vcamIntroManoir.Priority = 0;

        if (aImpact)
        {
            if (_animateurObjet != null)
                _animateurObjet.SetTrigger(impactChoisi.grossit
                    ? "DeclencherGrossir" : "DeclencherRapetisser");

            AppliquerEchelleVisuelle(impactChoisi.multiplicateur);
            JouerParticulesEtSon();
            _objetSurBalance.ModifierPoids(impactChoisi.multiplicateur);
            _zoneAdverse.NotifierChangementPoids();

            Debug.Log($"[ComportementAntagoniste] Impact phase : " +
                      $"×{impactChoisi.multiplicateur} — " +
                      $"poids={_objetSurBalance.valeurPoids}");

            yield return new WaitUntil(() => ParticulesTerminees());
        }

        yield return new WaitForSeconds(_dureeApresImpactPhase);

        // ── 5. Retour caméra FP ──────────────────────────────────────
        if (_vcamZoneAntagoniste != null) _vcamZoneAntagoniste.Priority = 0;
        if (_vcamIntroManoir != null)     _vcamIntroManoir.Priority = 0;
        if (_vcamBalance != null)         _vcamBalance.Priority = 0;

        var pointeur = FindObjectOfType<gestionPointeur>(true);
        if (pointeur != null) pointeur.gameObject.SetActive(true);
        var testP = FindObjectOfType<testPointeur>(true);
        if (testP != null) testP.enabled = true;

        _inputs?.ActiverInputs();

        _jeuEnCours = true;
        if (_intervalleIdleSpecial > 0f && _idles != null && _idles.Length > 0)
            _coroutineIdlesCycle = StartCoroutine(CyclerIdlesAleatoires());

        Debug.Log($"[ComportementAntagoniste] Réaction phase {phase} terminée.");
        _reactionPhaseTerminee = true;
    }

    /// <summary>
    /// Même logique qu'AppliquerImpact mais SANS déclencher OnActionTerminee.
    /// Utilisé par JouerReactionPhaseCoroutine — la coroutine de phase appelante
    /// gère elle-même la suite de la séquence.
    /// </summary>
    private IEnumerator AppliquerImpactPhase(impactAntagoniste impact)
    {
        if (_animateur != null)
        {
            int typeMagie = impact.grossit ? 1 : 2;
            _animateur.SetInteger("TypeMagie", typeMagie);
            _animateur.SetTrigger("DeclencherMagie");
        }

        yield return new WaitForSeconds(_delaiAvantParticules);

        if (_animateurObjet != null)
            _animateurObjet.SetTrigger(impact.grossit
                ? "DeclencherGrossir" : "DeclencherRapetisser");

        AppliquerEchelleVisuelle(impact.multiplicateur);
        JouerParticulesEtSon();
        _objetSurBalance.ModifierPoids(impact.multiplicateur);
        _zoneAdverse.NotifierChangementPoids();

        Debug.Log($"[ComportementAntagoniste] Impact phase : ×{impact.multiplicateur} — " +
                  $"poids={_objetSurBalance.valeurPoids}");

        yield return new WaitUntil(() => ParticulesTerminees());
        // Pas de OnActionTerminee — géré par JouerReactionPhaseCoroutine
    }

    private IEnumerator AppliquerImpact(impactAntagoniste impact)
    {
        if (_animateur != null)
        {
            int typeMagie = impact.grossit ? 1 : 2;
            _animateur.SetInteger("TypeMagie", typeMagie);
            _animateur.SetTrigger("DeclencherMagie");
        }

        yield return new WaitForSeconds(_delaiAvantParticules);

        if (_animateurObjet != null)
            _animateurObjet.SetTrigger(impact.grossit
                ? "DeclencherGrossir" : "DeclencherRapetisser");

        AppliquerEchelleVisuelle(impact.multiplicateur);
        JouerParticulesEtSon();
        _objetSurBalance.ModifierPoids(impact.multiplicateur);
        _zoneAdverse.NotifierChangementPoids();

        Debug.Log($"[ComportementAntagoniste] Impact : ×{impact.multiplicateur} — " +
                  $"poids={_objetSurBalance.valeurPoids}");

        yield return new WaitUntil(() => ParticulesTerminees());
        OnActionTerminee?.Invoke();
    }

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

            Debug.Log($"[ComportementAntagoniste] Idle : {idleChoisi.nom}");

            yield return new WaitForSeconds(_dureeMinEntreIdles);

            if (_animateur != null)
                _animateur.SetInteger("IdleType", 0);
        }
    }

    private IEnumerator JouerDefaiteSequence()
    {
        _jeuEnCours = false;
        if (_coroutineIdlesCycle != null)
        {
            StopCoroutine(_coroutineIdlesCycle);
            _coroutineIdlesCycle = null;
        }

        // ── 1. Caméra balance — voir le dernier équilibre ────────────
        if (_vcamBalance != null)         _vcamBalance.Priority = 70;
        if (_vcamIntroManoir != null)     _vcamIntroManoir.Priority = 0;
        if (_vcamZoneAntagoniste != null) _vcamZoneAntagoniste.Priority = 0;

        yield return new WaitForSeconds(_delaiAvantCinematiqueFinale);

        // ── 2. Audio dialogue par-dessus la cinématique ──────────────
        // Les clips vocaux sont lancés en PlayOneShot sur _sourceAudio
        // (AudioSource du boss — distinct de gestionAudio) et continuent
        // de jouer pendant la vidéo. Pas de sous-titres, pas d'animations.
        // Si le son se coupe à cause de l'arrêt musique dans
        // controleurCinematique, déplacer ce bloc APRÈS Jouer().
        if (_sfxVoixDefaiteDialogue != null && _sfxVoixDefaiteDialogue.Length > 0)
            StartCoroutine(JouerAudioDefaiteSurCinematique());

        // ── 3. Cinématique finale ────────────────────────────────────
        // gestionChapitres gère : ModeCinematique(true), arrêt musique,
        // lecture vidéo, puis callback en fin de vidéo.
        // OnActionTerminee → gestionnaireEnigmeBalance.LibererAttente()
        // → DeclencherVictoire() → OnEnigmeTerminee
        if (gestionChapitres.Instance != null)
        {
            gestionChapitres.Instance.LancerCinematiqueAvecDelai(
                _nomCinematiqueFin,
                0f,
                () => OnActionTerminee?.Invoke());
        }
        else
        {
            Debug.LogWarning("[ComportementAntagoniste] gestionChapitres.Instance " +
                             "introuvable — fin d'énigme déclenchée sans cinématique.");
            OnActionTerminee?.Invoke();
        }

        Debug.Log("[ComportementAntagoniste] Cinématique finale déclenchée.");
    }

    /// <summary>
    /// Joue les clips audio de la séquence de défaite en séquence,
    /// par-dessus la cinématique. Pas de sous-titres ni d'animations —
    /// uniquement le voice-over.
    /// </summary>
    private IEnumerator JouerAudioDefaiteSurCinematique()
    {
        for (int i = 0; i < _sfxVoixDefaiteDialogue.Length; i++)
        {
            AudioClip clip = Clip(_sfxVoixDefaiteDialogue, i);
            if (clip != null && _sourceAudio != null)
                _sourceAudio.PlayOneShot(clip);

            // Attendre la durée du clip + le délai configuré entre répliques
            float dureeClip  = (clip != null) ? clip.length : 0f;
            float delaiApres = Delai(_delaisDefaite, i);
            float attente    = dureeClip + delaiApres;

            if (attente > 0f)
                yield return new WaitForSeconds(attente);
        }
    }

    // ===================== MÉTHODES PUBLIQUES =====================

    public void ExecuterAction(gestionnaireEnigmeBalance.PhaseEnigme phase)
    {
        impactAntagoniste[] impacts = phase ==
            gestionnaireEnigmeBalance.PhaseEnigme.Phase1
                ? _listeImpacts.impactsPhase1
                : _listeImpacts.impactsPhase2;

        if (impacts == null || impacts.Length == 0)
        {
            Debug.LogWarning("[ComportementAntagoniste] Aucun impact.");
            OnActionTerminee?.Invoke();
            return;
        }

        StartCoroutine(AppliquerImpact(
            impacts[UnityEngine.Random.Range(0, impacts.Length)]));
    }

    public void JouerDefaite() => StartCoroutine(JouerDefaiteSequence());

    /// <summary>
    /// Lance la séquence de réaction de phase :
    ///   caméra boss → animation (Angry / Battlecry) → caméra balance
    ///   → impact antagoniste → retour caméra FP → contrôle joueur.
    /// ReactionPhaseTerminee passe à true en fin de séquence.
    /// Appelé par gestionnaireEnigmeBalance.
    /// </summary>
    public void DemarrerReactionPhase(gestionnaireEnigmeBalance.PhaseEnigme phase)
    {
        StartCoroutine(JouerReactionPhaseCoroutine(phase));
    }

    /// <param name="dialogueType">1=Talking, 2=Talking1, 3=Yelling</param>
    public void JouerDialogue(int dialogueType)
    {
        if (_animateur == null) return;
        _animateur.SetInteger("DialogueType", dialogueType);
        _animateur.SetTrigger("DeclencherDialogue");

        if (_sourceAudio != null && _sfxDialogue != null)
            _sourceAudio.PlayOneShot(_sfxDialogue);
    }

    // ===================== MÉTHODES PRIVÉES =====================

    /// <summary>
    /// Affiche un sous-titre, joue l'animation et le voice-over,
    /// attend la fin de la réplique, puis pré-déclenche l'animation
    /// suivante AU DÉBUT du délai pour blender directement vers
    /// le prochain état sans passer par l'idle.
    /// </summary>
    /// <param name="prochainTypeAnim">
    ///   Type brut (1=Talking, 3=Yelling) de la prochaine LIGNE BOSS.
    ///   0 = prochaine ligne est Joueur ou c'est la dernière réplique
    ///   → pas de pré-déclenchement, le boss retourne à l'idle normalement.
    /// </param>
    private IEnumerator JouerReplique(string interlocuteur, string texte,
        int dialogueType, AudioClip voiceClip, float delaiApres = 0f,
        int prochainTypeAnim = 0)
    {
        float duree = (voiceClip != null)
            ? Mathf.Max(
                (float)voiceClip.length + _dureeSupplementaireAffichage,
                _dureeMinimaleAffichage)
            : _dureeRepliquesJoueur;

        // ── Déclencher l'animation du boss ───────────────────────────
        if (dialogueType > 0)
        {
            if (!_animDejaPreDeclenche)
            {
                // Pas encore pré-déclenché : déclencher normalement
                int typeEffectif = dialogueType;
                if (dialogueType == 1 && _alternerAnimationsTalking)
                {
                    typeEffectif = _talkingAlterne;
                    _talkingAlterne = (_talkingAlterne == 1) ? 2 : 1;
                }
                JouerDialogue(typeEffectif);
            }
            // Que l'animation ait été pré-déclenchée ou non, réinitialiser le flag
            _animDejaPreDeclenche = false;
        }
        else if (_animateur != null)
        {
            // Ligne du Joueur : remettre DialogueType à 0 et basculer sur
            // l'idle de dialogue (_idleTypeDialogue=2 = Dizzy) pour que les
            // bras du boss restent calmes pendant que le joueur parle.
            _animDejaPreDeclenche = false;
            _animateur.SetInteger("DialogueType", 0);
            if (_idleTypeDialogue > 0)
            {
                _animateur.SetInteger("IdleType", _idleTypeDialogue);
                _animateur.SetTrigger("DeclencherIdleSpecial");
            }
        }

        // ── Voice-over + sous-titre ───────────────────────────────────
        if (voiceClip != null && _sourceAudio != null)
            _sourceAudio.PlayOneShot(voiceClip);

        if (_sousTitre != null)
            _sousTitre.AfficherSousTitre(interlocuteur, texte, duree);

        yield return new WaitForSeconds(duree);

        // ── Couper le Yelling à la fin du sous-titre ──────────────────
        // L'animation Yelling est longue : sans reset elle continue de
        // jouer pendant le délai suivant, causant un décalage visuel
        // (ex : "Faisons un pacte" démarrait encore en Yelling).
        // On remet DialogueType=0 immédiatement + on bascule sur l'idle
        // de dialogue pour que la transition suivante parte d'un état neutre.
        if (dialogueType == 3 && _animateur != null)
        {
            _animateur.SetInteger("DialogueType", 0);
            if (_idleTypeDialogue > 0)
            {
                _animateur.SetInteger("IdleType", _idleTypeDialogue);
                _animateur.SetTrigger("DeclencherIdleSpecial");
            }
        }

        // ── Pré-déclencher l'animation suivante au début du délai ─────
        // On déclenche MAINTENANT, pendant le délai, pour que l'Animator
        // ait le temps de blender idle → Talking (ou → Yelling) AVANT
        // que la prochaine réplique commence. Le flag _animDejaPreDeclenche
        // empêche le double-trigger au début de la prochaine JouerReplique.
        if (prochainTypeAnim > 0 && delaiApres > 0f && _animateur != null)
        {
            int typeEffectif = prochainTypeAnim;
            if (prochainTypeAnim == 1 && _alternerAnimationsTalking)
            {
                typeEffectif = _talkingAlterne;
                _talkingAlterne = (_talkingAlterne == 1) ? 2 : 1;
            }
            JouerDialogue(typeEffectif);
            _animDejaPreDeclenche = true;
        }

        if (delaiApres > 0f)
            yield return new WaitForSeconds(delaiApres);
    }

    private AudioClip Clip(AudioClip[] tableau, int index)
    {
        if (tableau == null || index < 0 || index >= tableau.Length) return null;
        return tableau[index];
    }

    private float Delai(float[] tableau, int index)
    {
        if (tableau == null || index < 0 || index >= tableau.Length) return 0f;
        return tableau[index];
    }

    private IdleAleatoire ChoisirIdleAleatoire()
    {
        int poidsTotal = 0;
        foreach (var idle in _idles) poidsTotal += idle.poids;

        int tirage = UnityEngine.Random.Range(0, poidsTotal);
        int cumul = 0;
        foreach (var idle in _idles)
        {
            cumul += idle.poids;
            if (tirage < cumul) return idle;
        }
        return _idles[_idles.Length - 1];
    }

    private void JouerSfxIdle(int idleType)
    {
        if (_sourceAudio == null || _sfxIdles == null) return;
        if (idleType < 1 || idleType >= _sfxIdles.Length) return;
        AudioClip clip = _sfxIdles[idleType];
        if (clip != null) _sourceAudio.PlayOneShot(clip);
    }

    private void JouerParticulesEtSon()
    {
        if (_particulesPortail != null)   _particulesPortail.Play();
        if (_particulesExplosion != null) _particulesExplosion.Play();
        if (_sourceAudio != null && _sfxTransformation != null)
            _sourceAudio.PlayOneShot(_sfxTransformation);
    }

    private bool ParticulesTerminees()
    {
        return (_particulesPortail  == null || !_particulesPortail.isPlaying)
            && (_particulesExplosion == null || !_particulesExplosion.isPlaying);
    }

    private void AppliquerEchelleVisuelle(float multiplicateur)
    {
        if (_objetSurBalance == null) return;
        Vector3 nouvelleEchelle =
            _objetSurBalance.transform.localScale * multiplicateur;

        if (nouvelleEchelle.magnitude > _echelleMax)
            nouvelleEchelle = nouvelleEchelle.normalized * _echelleMax;

        _objetSurBalance.transform.localScale = Vector3.Max(
            nouvelleEchelle, Vector3.one * _echelleMin);
    }
}
