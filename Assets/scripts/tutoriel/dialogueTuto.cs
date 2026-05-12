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

    [Header("Demarrage")]
    [Tooltip("Si coche, le PNJ est interactif des le debut. Sinon, " +
        "gestionChapitres doit l'activer via ReactiverInteraction() " +
        "quand la tuile correspondante apparait.")]
    [SerializeField] private bool interactifAuDemarrage = false;

    // Pour gestionChapitres : IdAction de l'etape EN COURS.
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

    // Vrai pendant qu'un dialogue est en train de defiler.
    public bool DialogueEnCours => dialogueOuvert;

    public static DialogueTuto DialogueActif { get; private set; }

    private int etapeActuelle = 0;
    private bool dialogueOuvert = false;
    private bool interactionActive;
    private Coroutine coroutineDialogue;
    private bool skipLigneDemande = false;

    void Start()
    {
        interactionActive = interactifAuDemarrage;

        if (gestionSousTitreRef == null)
            gestionSousTitreRef = FindFirstObjectByType<gestionSousTitre>(
                FindObjectsInactive.Include);
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

            // Attente avec possibilite de skip par ESC
            skipLigneDemande = false;
            float t = 0f;
            while (t < rep.duree && !skipLigneDemande)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            if (gestionSousTitreRef != null)
                gestionSousTitreRef.MasquerSousTitre();

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

    [Tooltip("Duree en secondes pendant laquelle cette replique " +
        "reste a l'ecran avant de disparaitre automatiquement.")]
    public float duree = 4f;

    [Tooltip("Pause en secondes apres la disparition de cette replique, " +
        "avant que la suivante n'apparaisse.")]
    public float pauseApres = 0.4f;
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
