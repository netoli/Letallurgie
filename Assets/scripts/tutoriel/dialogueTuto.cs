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

    [Header("Skip (ESC)")]
    [Tooltip("Delai (s) apres le 1er ESC avant que la tuile " +
        "\"ESC pour passer un dialogue\" se ferme automatiquement. " +
        "Le joueur a compris la mecanique, on retire l'indice.")]
    [SerializeField] private float delaiFermetureTuileEsc = 3f;

    void Start()
    {
        interactionActive = interactifAuDemarrage;

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

            // Attente avec possibilite de skip par ESC
            skipLigneDemande = false;
            float t = 0f;
            while (t < dureeReplique && !skipLigneDemande)
            {
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

            // Pause entre les repliques (sautee aussi par ESC)
            skipLigneDemande = false;
            float p = 0f;
            while (p < rep.pauseApres && !skipLigneDemande)
            {
                p += Time.unscaledDeltaTime;
                yield return null;
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

    void OnDisable()
    {
        if (DialogueActif == this) DialogueActif = null;
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
