using System.Collections;
using UnityEngine;

// IMPORTANT : la classe DialogueTuto (MonoBehaviour) DOIT etre la
// premiere classe declaree dans ce fichier. Sinon, Unity, en cherchant
// le composant a charger d'apres le nom du fichier (dialogueTuto.cs),
// tombe sur la classe au-dessus et lance l'erreur
// "X is missing the class attribute 'ExtensionOfNativeClass'".
// Les classes serialisables (RepliqueDialogue, EtapeDialogue) sont
// declarees plus bas dans ce meme fichier, apres la MonoBehaviour.

[RequireComponent(typeof(Collider))]
public class DialogueTuto : MonoBehaviour
{
    [Header("Etapes")]
    [Tooltip("Chaque entree = une interaction complete distincte. " +
        "La 1re fois que le joueur parle au PNJ, joue etapes[0]. " +
        "La 2e fois, joue etapes[1]. Etc.")]
    [SerializeField] private EtapeDialogue[] etapes;

    [Header("References")]
    [Tooltip("Si vide, sera trouve automatiquement via FindFirstObjectByType.")]
    [SerializeField] private gestionSousTitre gestionSousTitreRef;

    [Tooltip("AudioSource utilise pour jouer les clipAudio des repliques. " +
        "Si vide, un AudioSource sera ajoute automatiquement sur ce " +
        "GameObject au runtime.")]
    [SerializeField] private AudioSource audioSourceVoix;

    [Tooltip("Buffer (s) ajoute apres la fin du clip audio avant de " +
        "passer a la replique suivante. Donne un peu de respiration " +
        "naturelle. 0 = pas de buffer.")]
    [SerializeField] private float bufferApresClip = 0.3f;

    [Header("Demarrage")]
    [Tooltip("Si coche, le PNJ est interactif des le debut. Sinon, " +
        "gestionChapitres doit l'activer via ReactiverInteraction() " +
        "quand la tuile correspondante apparait.")]
    [SerializeField] private bool interactifAuDemarrage = false;

    // Pour gestionChapitres : IdAction de l'etape EN COURS.
    // Renvoie en priorite l'idActionAuDebut, sinon l'idActionAFin.
    // (Conserve pour compat ; pour le deverrouillage, preferer
    // PeutSignaler(idAction) qui considere aussi les repliques.)
    public string IdAction
    {
        get
        {
            if (etapes == null
                || etapeActuelle < 0
                || etapeActuelle >= etapes.Length) return "";

            var etape = etapes[etapeActuelle];
            return !string.IsNullOrEmpty(etape.idActionAuDebut)
                ? etape.idActionAuDebut
                : etape.idActionAFin;
        }
    }

    /// <summary>
    /// Vrai si l'etape courante peut signaler cette idAction, soit
    /// au debut (idActionAuDebut), soit a la fin (idActionAFin), soit
    /// en fin d'une de ses repliques (idActionADeclencher). Utilise
    /// par gestionChapitres pour decider si ce PNJ doit etre
    /// deverrouille quand une tuile attend cette idAction.
    /// </summary>
    public bool PeutSignaler(string idAction)
    {
        if (string.IsNullOrEmpty(idAction)) return false;
        if (etapes == null
            || etapeActuelle < 0
            || etapeActuelle >= etapes.Length) return false;

        var etape = etapes[etapeActuelle];
        if (etape.idActionAuDebut == idAction) return true;
        if (etape.idActionAFin == idAction) return true;
        if (etape.repliques != null)
        {
            foreach (var r in etape.repliques)
            {
                if (!string.IsNullOrEmpty(r.idActionADeclencher)
                    && r.idActionADeclencher == idAction)
                    return true;
            }
        }
        return false;
    }

    // Vrai pendant qu'un dialogue est en train de defiler.
    public bool DialogueEnCours => dialogueOuvert;

    public static DialogueTuto DialogueActif { get; private set; }

    private int etapeActuelle = 0;
    private bool dialogueOuvert = false;
    private bool interactionActive;
    private Coroutine coroutineDialogue;
    private bool skipLigneDemande = false;
    private bool premierSkipFait = false;
    // Action que le dialogue attend AVANT d'avancer a la replique
    // suivante. Mise a jour pour chaque replique qui a un
    // idActionRequiseAvancement. Si elle est signalee par
    // gestionChapitres.SignalerAction, le flag attendActionEffectue
    // devient true et la boucle d'attente debloque la replique.
    private string idActionAvancementEnAttente = "";
    private bool attentActionEffectue = false;
    // Replique actuellement affichee a l'ecran (sous-titre). Sert a
    // re-afficher le sous-titre apres une pause externe (menu pause,
    // options, journal) sans avoir a relancer toute la replique.
    private string interlocuteurCourant = "";
    private string texteCourant = "";
    // Vrai quand un menu externe (pause, options) suspend le dialogue.
    // Les boucles d'attente verifient ce flag a chaque frame et
    // n'incrementent leur compteur de temps que s'il est false. L'audio
    // est aussi mis en pause / repris via AudioSource.Pause()/UnPause().
    private bool dialoguePauseExterne = false;
    // Vrai quand le joueur est trop loin et que le dialogue est en
    // pause distance. Distinct de dialoguePauseExterne pour pouvoir
    // les combiner (ex: menu pause + trop loin = double pause).
    private bool dialoguePauseDistance = false;
    // Distance enregistree au moment ou la pause distance s'est
    // declenchee. Sert a calculer le seuil de reprise (* ratio).
    private float distanceLorsDePauseDistance = 0f;
    // True si gestion distance est desactivee pour cette session de
    // dialogue (le joueur a fait au moins 1 ESC pour skipper).
    private bool gestionDistanceDesactiveeParSkip = false;
    // True si le joueur a ete au moins une fois dans la zone audible
    // (distance <= distanceVolumeMin) pendant ce dialogue. Necessaire
    // pour eviter de declencher la pause distance immediatement si le
    // joueur a clique sur le PNJ de loin (raycast longue distance).
    // La pause distance ne se declenche que APRES que le joueur ait
    // ete proche au moins une fois.
    private bool joueurDejaProche = false;
    // Vrai si le dialogue est effectivement en pause (externe OU
    // distance). Utilise par les boucles d'attente.
    private bool DialogueEffectivementEnPause =>
        dialoguePauseExterne || dialoguePauseDistance;

    public bool EstEnPauseDistance => dialoguePauseDistance;

    /// <summary>
    /// Vrai si le dialogue est actuellement bloque entre 2 repliques
    /// en attente d'une action joueur (idActionRequiseAvancement). Dans
    /// cet etat, audio et sous-titre sont deja arretes : il ne faut pas
    /// appliquer la pause externe par-dessus (sinon la reprise
    /// reafficherait le sous-titre de la replique deja terminee).
    /// </summary>
    public bool EstEnAttenteAction =>
        !string.IsNullOrEmpty(idActionAvancementEnAttente);

    [Header("Skip (ESC)")]
    [Tooltip("Delai (s) apres le 1er ESC avant que la tuile " +
        "\"ESC pour passer un dialogue\" se ferme automatiquement. " +
        "Le joueur a compris la mecanique, on retire l'indice.")]
    [SerializeField] private float delaiFermetureTuileEsc = 3f;

    [Header("Reprise apres action joueur")]
    [Tooltip("Delai (s) entre le signal de l'action attendue " +
        "(idActionRequiseAvancement) et la reprise effective du " +
        "dialogue. Laisse un temps de respiration apres que le joueur " +
        "ferme le menu/inventaire/journal. Default 2s.")]
    [SerializeField] private float delaiAvantRepriseApresAction = 2f;

    [Header("Volume voix selon distance")]
    [Tooltip("Si coche, le volume de la voix diminue avec la distance " +
        "entre joueur et PNJ, et le dialogue se met en pause si le " +
        "joueur s'eloigne trop. Decoche pour un comportement classique " +
        "(volume constant, pas de pause distance). Default: false pour " +
        "ne pas casser les dialogues si le clic est fait de loin.")]
    [SerializeField] private bool gestionDistanceActive = false;

    [Tooltip("Reference au Transform du Player. Si null, sera trouve " +
        "automatiquement via tag 'Player' au Start.")]
    [SerializeField] private Transform refPlayer;

    [Tooltip("Distance (m) en deca de laquelle le volume reste a 100%.")]
    [SerializeField] private float distanceVolumeMax = 3f;

    [Tooltip("Distance (m) au-dela de laquelle le volume tombe a 0%. " +
        "Entre distanceVolumeMax et cette valeur, le volume decroit " +
        "lineairement.")]
    [SerializeField] private float distanceVolumeMin = 8f;

    [Tooltip("Distance (m) au-dela de laquelle le dialogue se met " +
        "automatiquement en PAUSE (le joueur est trop loin pour " +
        "entendre). Doit etre >= distanceVolumeMin.")]
    [SerializeField] private float distancePauseAuto = 10f;

    [Tooltip("Pour reprendre le dialogue apres une pause distance, le " +
        "joueur doit se rapprocher a ce ratio de la distance qui a " +
        "declenche la pause. Ex: 0.3 = 30% de distancePauseAuto.")]
    [Range(0.05f, 1f)]
    [SerializeField] private float ratioReprisePause = 0.3f;

    [Tooltip("(Optionnel) AudioClip joue 1 fois quand le dialogue se " +
        "met en pause distance (ex: voix du PNJ qui appelle 'Reviens !'). " +
        "Joue via audioSourceRappel, non spatial pour etre audible " +
        "meme loin.")]
    [SerializeField] private AudioClip clipRappelTropLoin;

    [Tooltip("(Optionnel) AudioSource utilise pour jouer clipRappelTropLoin. " +
        "Doit etre configure non-spatial (spatialBlend=0) pour etre " +
        "entendu partout. Si vide, sera cree automatiquement.")]
    [SerializeField] private AudioSource audioSourceRappel;

    [Tooltip("(Optionnel) Message a afficher dans le bandeau info " +
        "quand le dialogue se met en pause distance. Ex: 'Reviens " +
        "vers le tavernier'. Laisse vide pour ne rien afficher.")]
    [SerializeField] private string messageBandeauTropLoin;

    void Awake()
    {
        // Init dans Awake pour eviter race condition avec ReactiverInteraction
        interactionActive = interactifAuDemarrage;
    }

    void Start()
    {
        // S'abonner aux actions signalees pour pouvoir debloquer une
        // replique qui attend une action specifique (idActionRequiseAvancement).
        if (gestionChapitres.Instance != null)
            gestionChapitres.Instance.OnActionSignalee += AuActionSignalee;

        if (gestionSousTitreRef == null)
            gestionSousTitreRef = FindFirstObjectByType<gestionSousTitre>(
                FindObjectsInactive.Include);

        // Si aucun AudioSource n'est assigne, on en cree un automatiquement
        // sur ce GameObject. Pratique : l'utilisateur n'a rien a configurer
        // s'il ne veut pas de reglages specifiques (3D, spatial, etc.).
        if (audioSourceVoix == null)
        {
            audioSourceVoix = GetComponent<AudioSource>();
            if (audioSourceVoix == null)
            {
                audioSourceVoix = gameObject.AddComponent<AudioSource>();
                audioSourceVoix.playOnAwake = false;
                audioSourceVoix.spatialBlend = 0f; // 2D par defaut
            }
        }

        // Trouver le Player si refPlayer n'est pas assigne
        if (gestionDistanceActive && refPlayer == null)
        {
            var playerGo = GameObject.FindGameObjectWithTag("Player");
            if (playerGo != null)
                refPlayer = playerGo.transform;
            else
                Debug.LogWarning($"[DialogueTuto] {name} : Player " +
                    "introuvable (tag 'Player'). gestionDistanceActive " +
                    "ne fonctionnera pas.");
        }

        // Creer un AudioSource non-spatial pour le son de rappel
        // "trop loin" si l'utilisateur n'en a pas fourni un.
        if (clipRappelTropLoin != null && audioSourceRappel == null)
        {
            audioSourceRappel = gameObject.AddComponent<AudioSource>();
            audioSourceRappel.playOnAwake = false;
            audioSourceRappel.spatialBlend = 0f; // 2D pour entendre partout
        }
    }

    void Update()
    {
        if (!gestionDistanceActive) return;
        if (gestionDistanceDesactiveeParSkip) return;
        if (!dialogueOuvert) return;
        if (refPlayer == null) return;

        float distance = Vector3.Distance(
            refPlayer.position, transform.position);

        // Tant que le joueur n'a pas ete proche au moins une fois, on
        // ne fait rien : ni reduction de volume ni pause distance. Ca
        // evite de casser le debut du dialogue quand le clic est fait
        // de loin (raycast). Le joueur doit d'abord "venir voir" le
        // PNJ pour que le systeme s'enclenche.
        if (!joueurDejaProche)
        {
            if (distance <= distanceVolumeMin)
            {
                joueurDejaProche = true;
                Debug.Log($"[DialogueTuto] {name} : joueur entre dans " +
                    "zone audible, gestion distance activee.");
            }
            else
            {
                // Encore loin du PNJ : on garde le volume max et on
                // attend que le joueur s'approche.
                if (audioSourceVoix != null) audioSourceVoix.volume = 1f;
                return;
            }
        }

        // 1) Ajuster le volume de la voix selon la distance.
        //    - distance <= distanceVolumeMax : volume 100%
        //    - distance >= distanceVolumeMin : volume 0%
        //    - entre les deux : decroissance lineaire
        if (audioSourceVoix != null)
        {
            float volume;
            if (distance <= distanceVolumeMax)
                volume = 1f;
            else if (distance >= distanceVolumeMin)
                volume = 0f;
            else
            {
                float t = (distance - distanceVolumeMax)
                    / (distanceVolumeMin - distanceVolumeMax);
                volume = 1f - t;
            }
            audioSourceVoix.volume = volume;
        }

        // 2) Pause distance : declenchement et reprise automatique
        if (!dialoguePauseDistance && distance > distancePauseAuto)
        {
            DeclencherPauseDistance(distance);
        }
        else if (dialoguePauseDistance)
        {
            float seuilReprise =
                distanceLorsDePauseDistance * ratioReprisePause;
            if (distance <= seuilReprise)
            {
                SortiePauseDistance();
            }
        }
    }

    private void DeclencherPauseDistance(float distance)
    {
        dialoguePauseDistance = true;
        distanceLorsDePauseDistance = distance;
        if (audioSourceVoix != null && audioSourceVoix.isPlaying)
            audioSourceVoix.Pause();

        // Jouer le son de rappel non-spatial si configure
        if (clipRappelTropLoin != null && audioSourceRappel != null)
        {
            audioSourceRappel.Stop();
            audioSourceRappel.clip = clipRappelTropLoin;
            audioSourceRappel.Play();
        }

        // Afficher message dans le bandeau info si configure
        if (!string.IsNullOrEmpty(messageBandeauTropLoin))
        {
            gestionBandeauInfo.Afficher(messageBandeauTropLoin, 5f);
        }

        Debug.Log($"[DialogueTuto] {name} : pause distance declenchee " +
            $"(distance={distance:F1}). Seuil reprise : " +
            $"{distanceLorsDePauseDistance * ratioReprisePause:F1}m.");
    }

    /// <summary>
    /// Sortir de la pause distance. Appelee automatiquement quand le
    /// joueur revient assez pres, ou manuellement par
    /// declencheurDialogue quand le joueur re-entre dans le trigger.
    /// </summary>
    public void SortiePauseDistance()
    {
        if (!dialoguePauseDistance) return;
        dialoguePauseDistance = false;
        if (audioSourceVoix != null && !dialoguePauseExterne)
            audioSourceVoix.UnPause();
        Debug.Log($"[DialogueTuto] {name} : sortie de pause distance.");
    }

    /// <summary>
    /// Appele par gestionInteractionClic au clic gauche sur le PNJ.
    /// </summary>
    public void Interagir()
    {
        if (!interactionActive)
        {
            Debug.Log($"[DialogueTuto] {name} : clic ignore " +
                "(interactionActive=false). Le systeme de tutoriel " +
                "n'a pas encore deverrouille cette etape.");
            return;
        }

        DemarrerEtapeCourante();
    }

    /// <summary>
    /// Demarre automatiquement l'etape courante du dialogue, SANS
    /// passer par le verrou interactionActive. Utilise par
    /// declencheurAction pour que le PNJ "parle tout seul" quand une
    /// action tutoriel est signalee (ex: client qui dit "prends mon
    /// verre" apres que le joueur depose la bouteille). N'attend pas
    /// un clic du joueur.
    /// </summary>
    public void DemarrerAuto()
    {
        Debug.Log($"[DialogueTuto] {name} : DemarrerAuto() appele " +
            $"(etape={etapeActuelle}).");
        DemarrerEtapeCourante();
    }

    private void DemarrerEtapeCourante()
    {
        if (dialogueOuvert) return;
        if (etapes == null || etapes.Length == 0) return;
        if (etapeActuelle >= etapes.Length) return;

        EtapeDialogue etape = etapes[etapeActuelle];
        if (etape.repliques == null || etape.repliques.Length == 0)
        {
            Debug.LogWarning($"[DialogueTuto] {name} etape {etapeActuelle} " +
                "n'a aucune replique.");
            EtapeTerminee(etape);
            return;
        }

        if (coroutineDialogue != null) StopCoroutine(coroutineDialogue);
        coroutineDialogue = StartCoroutine(JouerDialogue(etape));
    }

    private IEnumerator JouerDialogue(EtapeDialogue etape)
    {
        dialogueOuvert = true;
        DialogueActif = this;
        interactionActive = false;
        premierSkipFait = false;
        // Reset des flags lies a cette session de dialogue. Si une
        // etape precedente avait desactive la gestion distance via
        // skip, on la reactive pour cette nouvelle etape.
        gestionDistanceDesactiveeParSkip = false;
        dialoguePauseDistance = false;
        distanceLorsDePauseDistance = 0f;
        joueurDejaProche = false;
        if (audioSourceVoix != null) audioSourceVoix.volume = 1f;

        // Signal du debut : ferme la tuile "Parler avec le tavernier".
        if (!string.IsNullOrEmpty(etape.idActionAuDebut)
            && gestionChapitres.Instance != null)
        {
            Debug.Log($"[DialogueTuto] Signal debut: {etape.idActionAuDebut}");
            gestionChapitres.Instance.SignalerAction(etape.idActionAuDebut);
        }

        // Defile les repliques
        for (int i = 0; i < etape.repliques.Length; i++)
        {
            RepliqueDialogue rep = etape.repliques[i];

            // Memoriser la replique courante pour pouvoir la re-afficher
            // apres une pause externe (menu ouvert -> sous-titre cache).
            interlocuteurCourant = rep.interlocuteur;
            texteCourant = rep.texte;

            if (gestionSousTitreRef != null)
            {
                gestionSousTitreRef.AfficherSousTitre(
                    rep.interlocuteur, rep.texte, -1f);
            }

            // Joue le clip audio s'il y en a un. La duree d'affichage
            // devient alors la duree du clip + bufferApresClip.
            float dureeReplique;
            if (rep.clipAudio != null && audioSourceVoix != null)
            {
                audioSourceVoix.Stop();
                audioSourceVoix.clip = rep.clipAudio;
                audioSourceVoix.Play();
                dureeReplique = rep.clipAudio.length + bufferApresClip;
            }
            else
            {
                // Pas de clip : on utilise la duree manuelle.
                dureeReplique = rep.duree;
            }

            // Attente avec possibilite de skip par ESC.
            // Si DialogueEffectivementEnPause (menu pause/options OU
            // pause distance), on suspend le compteur : t n'avance pas,
            // donc la replique reste affichee et l'audio est en pause.
            skipLigneDemande = false;
            float t = 0f;
            while (t < dureeReplique && !skipLigneDemande)
            {
                if (!DialogueEffectivementEnPause)
                    t += Time.unscaledDeltaTime;
                yield return null;
            }

            // Stop l'audio si le joueur a skippe avant la fin
            if (audioSourceVoix != null && audioSourceVoix.isPlaying)
                audioSourceVoix.Stop();

            if (gestionSousTitreRef != null)
                gestionSousTitreRef.MasquerSousTitre();

            // Signal d'action en fin de cette replique (avant la pause).
            // Permet d'enchainer une tuile/banniere/chapitre pendant le
            // dialogue, sans attendre la fin de l'etape entiere.
            if (!string.IsNullOrEmpty(rep.idActionADeclencher)
                && gestionChapitres.Instance != null)
            {
                Debug.Log($"[DialogueTuto] Replique {i} signale: " +
                    $"{rep.idActionADeclencher}");
                gestionChapitres.Instance.SignalerAction(
                    rep.idActionADeclencher);
            }

            // Pause entre les repliques (sautee aussi par ESC).
            // Meme logique pour DialogueEffectivementEnPause.
            skipLigneDemande = false;
            float p = 0f;
            while (p < rep.pauseApres && !skipLigneDemande)
            {
                if (!DialogueEffectivementEnPause)
                    p += Time.unscaledDeltaTime;
                yield return null;
            }

            // Attente d'une action joueur AVANT de passer a la replique
            // suivante (ex: le tavernier dit "Ouvre ton journal" puis le
            // dialogue attend que le joueur appuie sur J avant de
            // continuer). Le skip ESC ne court-circuite PAS cette attente :
            // c'est une etape pedagogique obligatoire. Sous-titre cache et
            // texte du bandeau "attente" affiche si configure.
            if (!string.IsNullOrEmpty(rep.idActionRequiseAvancement))
            {
                idActionAvancementEnAttente = rep.idActionRequiseAvancement;
                attentActionEffectue = false;
                Debug.Log($"[DialogueTuto] {name} : attente action " +
                    $"'{rep.idActionRequiseAvancement}' avant de passer " +
                    "a la replique suivante.");

                while (!attentActionEffectue)
                {
                    yield return null;
                }

                idActionAvancementEnAttente = "";
                Debug.Log($"[DialogueTuto] {name} : reprise du dialogue " +
                    "apres action joueur.");
            }
        }

        // Fin du dialogue
        dialogueOuvert = false;
        if (DialogueActif == this) DialogueActif = null;

        int etapeQuiSeFerme = etapeActuelle;
        etapeActuelle++;

        Debug.Log($"[DialogueTuto] Fin dialogue etape {etapeQuiSeFerme}, " +
            $"signal fin: {etape.idActionAFin}");

        if (!string.IsNullOrEmpty(etape.idActionAFin)
            && gestionChapitres.Instance != null)
        {
            gestionChapitres.Instance.SignalerAction(etape.idActionAFin);
        }

        coroutineDialogue = null;
    }

    private void EtapeTerminee(EtapeDialogue etape)
    {
        if (!string.IsNullOrEmpty(etape.idActionAFin)
            && gestionChapitres.Instance != null)
        {
            gestionChapitres.Instance.SignalerAction(etape.idActionAFin);
        }
        etapeActuelle++;
    }

    public void SkipLigneCourante()
    {
        if (!dialogueOuvert) return;
        skipLigneDemande = true;
        Debug.Log("[DialogueTuto] Skip ligne demande par ESC.");

        // Le joueur veut skipper : on desactive la gestion distance
        // pour ne pas mettre le dialogue en pause s'il s'eloigne, et
        // pour ne pas interferer avec son envie d'avancer vite.
        // Aussi : si une pause distance etait deja active, on en sort
        // (sinon le skip ne ferait rien tant qu'il est encore loin).
        if (gestionDistanceActive && !gestionDistanceDesactiveeParSkip)
        {
            gestionDistanceDesactiveeParSkip = true;
            if (dialoguePauseDistance)
                SortiePauseDistance();
            // Remettre le volume a 100% pour que le joueur entende
            // la fin du dialogue meme s'il s'eloigne.
            if (audioSourceVoix != null) audioSourceVoix.volume = 1f;
            Debug.Log("[DialogueTuto] gestion distance desactivee " +
                "pour le reste de la session (joueur a skippe).");
        }

        // Au 1er ESC du dialogue : le joueur a compris la mecanique,
        // on ferme la tuile "ESC pour passer un dialogue" apres un
        // court delai (lui laisse le temps de lire encore un instant).
        if (!premierSkipFait)
        {
            premierSkipFait = true;
            if (gestionChapitres.Instance != null)
            {
                Debug.Log("[DialogueTuto] 1er ESC -> fermeture tuile " +
                    $"ESC dans {delaiFermetureTuileEsc}s.");
                gestionChapitres.Instance.FermerTuileActuelleApresDelai(
                    delaiFermetureTuileEsc);
            }
        }
    }

    public void ReactiverInteraction()
    {
        interactionActive = true;
        Debug.Log($"[DialogueTuto] {name} reactive (etape={etapeActuelle}).");
    }

    public void DesactiverInteraction()
    {
        interactionActive = false;
    }

    /// <summary>
    /// Met le dialogue en pause depuis un contexte externe (ouverture
    /// du menu pause, options, journal, etc.). Suspend l'avancement
    /// des repliques, met en pause l'audio de la voix, et MASQUE le
    /// sous-titre pour ne pas etre distrayant pendant que le menu est
    /// affiche par dessus.
    /// </summary>
    public void MettreEnPauseExterne()
    {
        if (!dialogueOuvert) return;
        if (dialoguePauseExterne) return;
        // Cas special : si le dialogue est deja en attente d'une action
        // joueur (ex: idActionRequiseAvancement = 'jdb_ouvert' qui se
        // declenche en ouvrant le journal), on N'APPLIQUE PAS la pause
        // externe. Audio et sous-titre sont deja arretes. Sinon, la
        // reprise reafficherait le sous-titre de la replique deja
        // terminee, ce qui n'a aucun sens.
        if (EstEnAttenteAction)
        {
            Debug.Log($"[DialogueTuto] {name} : pause externe IGNOREE " +
                $"(dialogue en attente action " +
                $"'{idActionAvancementEnAttente}').");
            return;
        }
        dialoguePauseExterne = true;
        if (audioSourceVoix != null && audioSourceVoix.isPlaying)
            audioSourceVoix.Pause();
        // Cacher le sous-titre pendant que le menu est ouvert. On le
        // re-affichera dans ReprendreExterne avec les memes textes.
        if (gestionSousTitreRef != null)
            gestionSousTitreRef.MasquerSousTitre();
        Debug.Log($"[DialogueTuto] {name} : pause externe activee.");
    }

    /// <summary>
    /// Reprend le dialogue suspendu par MettreEnPauseExterne. Reprend
    /// l'audio la ou il avait ete mis en pause et re-affiche le
    /// sous-titre de la replique qui etait en cours.
    /// Si une pause distance est encore active, l'audio reste en pause.
    /// </summary>
    public void ReprendreExterne()
    {
        if (!dialoguePauseExterne) return;
        dialoguePauseExterne = false;
        // Ne relancer l'audio que si l'autre source de pause (distance)
        // n'est pas active. Sinon l'audio reste en pause jusqu'a ce que
        // le joueur revienne assez pres.
        if (audioSourceVoix != null && !dialoguePauseDistance)
            audioSourceVoix.UnPause();
        // Re-afficher le sous-titre de la replique en cours (cache lors
        // de la pause). On ne le fait que si l'autre source de pause
        // (distance) n'est pas active, sinon ce serait incoherent.
        if (gestionSousTitreRef != null
            && !dialoguePauseDistance
            && !string.IsNullOrEmpty(texteCourant))
        {
            gestionSousTitreRef.AfficherSousTitre(
                interlocuteurCourant, texteCourant, -1f);
        }
        Debug.Log($"[DialogueTuto] {name} : reprise apres pause externe " +
            $"(pauseDistance encore active : {dialoguePauseDistance}).");
    }

    void OnDisable()
    {
        if (DialogueActif == this) DialogueActif = null;
    }

    void OnDestroy()
    {
        if (gestionChapitres.Instance != null)
            gestionChapitres.Instance.OnActionSignalee -= AuActionSignalee;
    }

    /// <summary>
    /// Appele a chaque action signalee dans gestionChapitres. Sert a
    /// debloquer une replique qui attend une action specifique
    /// (idActionRequiseAvancement). Le flag attentActionEffectue est
    /// consomme par la boucle d'attente dans JouerDialogue.
    /// </summary>
    private void AuActionSignalee(string idAction)
    {
        if (string.IsNullOrEmpty(idActionAvancementEnAttente)) return;
        if (idAction == idActionAvancementEnAttente)
        {
            Debug.Log($"[DialogueTuto] {name} : action attendue " +
                $"'{idAction}' signalee, reprise dans " +
                $"{delaiAvantRepriseApresAction}s.");
            StartCoroutine(RepriseApresDelai());
        }
    }

    private IEnumerator RepriseApresDelai()
    {
        if (delaiAvantRepriseApresAction > 0f)
            yield return new WaitForSecondsRealtime(
                delaiAvantRepriseApresAction);
        attentActionEffectue = true;
    }
}


// ============================================================
// Classes de donnees serialisables utilisees par DialogueTuto.
// Elles sont declarees APRES la MonoBehaviour pour respecter la
// convention Unity (la 1re classe du fichier porte le nom du fichier).
// ============================================================

/// <summary>
/// Une replique = UNE bulle de sous-titre affichee a l'ecran.
/// </summary>
[System.Serializable]
public class RepliqueDialogue
{
    [Tooltip("Qui parle (ex: Tavernier, Personnage, PNJ).")]
    public string interlocuteur = "Tavernier";

    [Tooltip("Texte affiche dans la bulle.")]
    [TextArea(2, 5)]
    public string texte;

    [Tooltip("(Optionnel) Clip audio de la voix qui dit cette replique. " +
        "Si renseigne, sa duree controle automatiquement combien de temps " +
        "la bulle reste affichee (le champ 'duree' est ignore quand un " +
        "clip est present).")]
    public AudioClip clipAudio;

    [Tooltip("Duree en secondes pendant laquelle cette replique " +
        "reste a l'ecran avant de disparaitre automatiquement. " +
        "Ignoree si un clipAudio est renseigne (on utilise alors la " +
        "duree du clip + un petit buffer).")]
    public float duree = 4f;

    [Tooltip("Pause en secondes apres la disparition de cette replique, " +
        "avant que la suivante n'apparaisse.")]
    public float pauseApres = 0.4f;

    [Tooltip("(Optionnel) ID d'action signalee A LA FIN de cette replique " +
        "(juste apres que la bulle disparaisse, avant la pause). Permet " +
        "de declencher une tuile/banniere/chapitre PENDANT une etape de " +
        "dialogue, sans attendre la fin de l'etape entiere.")]
    public string idActionADeclencher;

    [Tooltip("(Optionnel) ID d'action que le joueur DOIT effectuer " +
        "AVANT que le dialogue puisse passer a la replique suivante. " +
        "Le dialogue reste bloque (audio/voix arretes, sous-titre " +
        "masque) jusqu'a ce que cette action soit signalee. Permet " +
        "d'imposer une etape pedagogique au milieu d'un dialogue " +
        "(ex: 'jdb_ouvert' = le tavernier dit 'Ouvre ton journal' et " +
        "le dialogue attend que le joueur appuie sur J avant de " +
        "continuer). Laisse vide pour ne pas bloquer.")]
    public string idActionRequiseAvancement;
}

/// <summary>
/// Une etape = UN echange complet (par ex. la 1re visite au tavernier),
/// compose de plusieurs repliques alternees entre les interlocuteurs.
/// </summary>
[System.Serializable]
public class EtapeDialogue
{
    [Tooltip("Repliques jouees dans l'ordre, automatiquement.")]
    public RepliqueDialogue[] repliques;

    [Tooltip("(Optionnel) ID d'action signalee DES LE DEBUT du " +
        "dialogue (au clic sur le PNJ). Sert a fermer la tuile " +
        "\"Parler au tavernier\" et faire apparaitre la suivante.")]
    public string idActionAuDebut;

    [Tooltip("ID d'action signalee A LA FIN du dialogue (quand la " +
        "derniere replique a disparu). Doit correspondre au " +
        "idActionRequise de la tuile qui veut se fermer ici.")]
    public string idActionAFin;
}
