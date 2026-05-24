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
    [Tooltip("Caméra Cinemachine pointée sur le boss pendant le dialogue d'intro.")]
    [SerializeField] private CinemachineCamera _vcamIntroManoir;
    [Tooltip("Caméra Cinemachine pointée sur la balance — activée au reveal.")]
    [SerializeField] private CinemachineCamera _vcamBalance;
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
    [Tooltip("Secondes entre la fin du dialogue de défaite et la mort.")]
    [SerializeField] private float _delaiAvantMort = 2f;

    [Header("Réactions de phase")]
    [Tooltip("Trigger Animator pour la réaction de Phase 1 (ex : DeclencherAngry). " +
             "À ajouter dans l'Animator si l'animation n'existe pas encore.")]
    [SerializeField] private string _triggerPhase1 = "DeclencherAngry";
    [Tooltip("Trigger Animator pour la réaction de Phase 2. " +
             "Laissez vide pour utiliser l'idle Battlecry (IdleType=8 + DeclencherIdleSpecial).")]
    [SerializeField] private string _triggerPhase2 = "";
    [Tooltip("Secondes à attendre après le trigger de phase 1, avant de switcher sur la caméra balance.")]
    [SerializeField] private float _dureeAnimationPhase1 = 2f;
    [Tooltip("Secondes à attendre après le trigger de phase 2.")]
    [SerializeField] private float _dureeAnimationPhase2 = 2f;
    [Tooltip("Secondes après la fin des particules d'impact de phase, avant de rendre le contrôle au joueur.")]
    [SerializeField] private float _dureeApresImpactPhase = 1f;
    [Tooltip("Secondes passées sur la caméra balance après une égalisation, avant de switcher sur la caméra boss pour sa réaction. Permet de voir la balance s'équilibrer visuellement.")]
    [SerializeField] private float _delaiVueBalance = 1.5f;
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

        StartCoroutine(JouerIntroScene());
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
    ///   1. Stop idles + ModeCinematique(true)
    ///   2. Caméra boss → animation de réaction
    ///   3. Caméra balance → AppliquerImpactPhase (sans OnActionTerminee)
    ///   4. Retour caméra FP → réactiver pointeur + ActiverInputs()
    ///   5. Relancer les idles + mettre ReactionPhaseTerminee à true
    /// </summary>
    private IEnumerator JouerReactionPhaseCoroutine(gestionnaireEnigmeBalance.PhaseEnigme phase)
    {
        _reactionPhaseTerminee = false;

        // Stopper les idles pendant la cinématique de phase
        _jeuEnCours = false;
        if (_coroutineIdlesCycle != null)
        {
            StopCoroutine(_coroutineIdlesCycle);
            _coroutineIdlesCycle = null;
        }

        // Caméra balance en premier — laisser voir l'égalisation visuellement
        if (_vcamBalance != null)     _vcamBalance.Priority = 70;
        if (_vcamIntroManoir != null) _vcamIntroManoir.Priority = 0;

        yield return new WaitForSeconds(_delaiVueBalance);

        // Bloquer les inputs et switcher sur la caméra boss pour la réaction
        _inputs?.ModeCinematique(true);
        if (_vcamIntroManoir != null) _vcamIntroManoir.Priority = 60;
        if (_vcamBalance != null)     _vcamBalance.Priority = 0;

        // Animation de réaction selon la phase
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
                {
                    _animateur.SetTrigger(_triggerPhase2);
                }
                else
                {
                    // Fallback : idle Battlecry (IdleType=8)
                    _animateur.SetInteger("IdleType", 8);
                    _animateur.SetTrigger("DeclencherIdleSpecial");
                }
                yield return new WaitForSeconds(_dureeAnimationPhase2);
            }
        }

        // Switch caméra balance pour l'impact
        if (_vcamBalance != null)     _vcamBalance.Priority = 70;
        if (_vcamIntroManoir != null) _vcamIntroManoir.Priority = 0;

        // Impact de l'antagoniste (sans OnActionTerminee)
        if (_listeImpacts != null)
        {
            impactAntagoniste[] impacts = phase == gestionnaireEnigmeBalance.PhaseEnigme.Phase1
                ? _listeImpacts.impactsPhase1
                : _listeImpacts.impactsPhase2;

            if (impacts != null && impacts.Length > 0)
            {
                impactAntagoniste impact = impacts[UnityEngine.Random.Range(0, impacts.Length)];
                yield return StartCoroutine(AppliquerImpactPhase(impact));
            }
        }

        yield return new WaitForSeconds(_dureeApresImpactPhase);

        // Retour caméra first-person
        if (_vcamIntroManoir != null) _vcamIntroManoir.Priority = 0;
        if (_vcamBalance != null)     _vcamBalance.Priority = 0;

        // Réactiver le pointeur et les inputs joueur
        var pointeur = FindObjectOfType<gestionPointeur>(true);
        if (pointeur != null) pointeur.gameObject.SetActive(true);
        var testP = FindObjectOfType<testPointeur>(true);
        if (testP != null) testP.enabled = true;

        _inputs?.ActiverInputs();

        // Relancer les idles
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
        _talkingAlterne = 1;
        _animDejaPreDeclenche = false;

        _jeuEnCours = false;
        if (_coroutineIdlesCycle != null)
        {
            StopCoroutine(_coroutineIdlesCycle);
            _coroutineIdlesCycle = null;
        }

        _inputs?.ModeCinematique(true);

        // Caméra boss pour la cinématique de défaite
        if (_vcamIntroManoir != null) _vcamIntroManoir.Priority = 60;

        if (_animateur != null)
            _animateur.SetTrigger("DeclencherDefaite");

        yield return new WaitForSeconds(1f);

        // ── Dialogue défaite ──────────────────────────────────────────

        // 0. "non, NON! Tu as réussi…" — idle defeat (DeclencherDefaite déjà actif)
        //    On bloque le re-déclenchement d'une animation de dialogue pour laisser
        //    l'animation de défaite continuer de jouer pendant cette première réplique.
        _animDejaPreDeclenche = true;
        yield return StartCoroutine(JouerReplique(
            "Incommensurable", "non, NON! Tu as réussi…",
            1, Clip(_sfxVoixDefaiteDialogue, 0), Delai(_delaisDefaite, 0),
            prochainTypeAnim: 1));

        // 1. "(soupire), eh bien, je tiens ma parole…" — prochaine = Talking
        yield return StartCoroutine(JouerReplique(
            "Incommensurable", "(soupire), eh bien, je tiens ma parole…",
            1, Clip(_sfxVoixDefaiteDialogue, 1), Delai(_delaisDefaite, 1),
            prochainTypeAnim: 1));

        // 2. "Tu as gagné!" — prochaine = Talking
        yield return StartCoroutine(JouerReplique(
            "Incommensurable", "Tu as gagné!",
            1, Clip(_sfxVoixDefaiteDialogue, 2), Delai(_delaisDefaite, 2),
            prochainTypeAnim: 1));

        // 3. "J'ai levé le sort sur tous ceux" — prochaine = Talking
        yield return StartCoroutine(JouerReplique(
            "Incommensurable", "J'ai levé le sort sur tous ceux",
            1, Clip(_sfxVoixDefaiteDialogue, 3), Delai(_delaisDefaite, 3),
            prochainTypeAnim: 1));

        // 4. "que j'ai transformé en métal." — prochaine = Yelling
        yield return StartCoroutine(JouerReplique(
            "Incommensurable", "que j'ai transformé en métal.",
            1, Clip(_sfxVoixDefaiteDialogue, 4), Delai(_delaisDefaite, 4),
            prochainTypeAnim: 3));

        // 5. "Maintenant DÉGUERPIS!" — dernière réplique
        yield return StartCoroutine(JouerReplique(
            "Incommensurable", "Maintenant DÉGUERPIS!",
            3, Clip(_sfxVoixDefaiteDialogue, 5), Delai(_delaisDefaite, 5),
            prochainTypeAnim: 0));

        yield return new WaitForSeconds(_delaiAvantMort);

        if (_animateur != null)
            _animateur.SetTrigger("DeclencherMort");

        Debug.Log("[ComportementAntagoniste] Mort déclenchée.");

        // Signaler la fin de la séquence de défaite à gestionnaireEnigmeBalance
        // → LibererAttente() vérifie PhaseEnigme.Terminee → DeclencherVictoire()
        OnActionTerminee?.Invoke();
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
