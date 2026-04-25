using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DialogueTuto : MonoBehaviour
{
    [Header("Identification")]
    [Tooltip("ID d'action envoyé à gestionChapitres quand le dialogue est terminé")]
    [SerializeField] private string idAction = "dialogue_passe";
    // lecture publique de l'ID d'action pour gestionChapitres et gestionInteractionClic
    public string IdAction => idAction;
    private bool interactionActive = true;


    [Header("Contenu du dialogue")]
    [Tooltip("Nom affiché comme interlocuteur dans les sous-titres")]
    [SerializeField] private string interlocuteur = "Tavernier";

    [Tooltip("Texte affiché dans les sous-titres")]
    [TextArea(1, 4)]
    [SerializeField] private string propos = "Bienvenue, voyageur. Prends un verre.";

    [Header("Références")]
    [Tooltip("Référence au gestionnaire de sous-titres (si vide, sera trouvé automatiquement)")]
    [SerializeField] private gestionSousTitre gestionSousTitreRef;

    [Header("Timing")]
    [Tooltip("Durée en secondes avant fermeture automatique du dialogue (si <= 0, utilise la valeur par défaut du gestionSousTitre)")]
    [SerializeField] private float dureeDialogue = 4f;

    private Coroutine coroutineFermeture;
    private bool dialogueOuvert = false;

    void Start()
    {
        if (gestionSousTitreRef == null)
            gestionSousTitreRef = FindObjectOfType<gestionSousTitre>();
    }

    /// <summary>
    /// Méthode publique appelée par gestionInteractionClic quand on clique sur l'objet.
    /// </summary>
    public void Interagir()
    {
        if (!interactionActive)
        {
            Debug.Log($"[DialogueTuto] Interaction ignorée (désactivée) pour {name}");
            return;
        }

        // Toggle dialogue ouvert/fermé
        if (!dialogueOuvert)
            OuvrirDialogue();
        else
            FermerDialogue();
    }

    private void OuvrirDialogue()
    {
        if (gestionSousTitreRef != null)
        {
            // Affiche le sous-titre par le gestionnaire
            gestionSousTitreRef.AfficherSousTitre(interlocuteur, propos, dureeDialogue > 0f ? dureeDialogue : -1f);
        }
        else
        {
            Debug.LogWarning("[DialogueTuto] gestionSousTitre introuvable.");
        }

        dialogueOuvert = true;

        if (coroutineFermeture != null) StopCoroutine(coroutineFermeture);
        if (dureeDialogue > 0f)
            coroutineFermeture = StartCoroutine(FermerApresDelai(dureeDialogue));
        else
            coroutineFermeture = null;

        Debug.Log($"[DialogueTuto] Dialogue ouvert: {name} (idAction={idAction})");
    }

    private void FermerDialogue()
    {
        if (coroutineFermeture != null)
        {
            StopCoroutine(coroutineFermeture);
            coroutineFermeture = null;
        }

        // Masquer (gestionSousTitre)
        if (gestionSousTitreRef != null)
            gestionSousTitreRef.MasquerSousTitre();

        dialogueOuvert = false;

        // Notifier gestionChapitres que l'action est faite
        if (gestionChapitres.Instance != null)
        {
            gestionChapitres.Instance.SignalerAction(idAction);
            Debug.Log($"[DialogueTuto] Dialogue fermé et signal envoyé: {idAction}");
        }
        else
        {
            Debug.LogWarning("[DialogueTuto] gestionChapitres introuvable lors de la fermeture du dialogue.");
        }
    }

    private IEnumerator FermerApresDelai(float delai)
    {
        yield return new WaitForSeconds(delai);
        // Si le dialogue est toujours ouvert, on le ferme et on notifie
        if (dialogueOuvert)
            FermerDialogue();
    }

    public void DesactiverInteraction()
    {
        interactionActive = false;
        Debug.Log($"[DialogueTuto] Interaction désactivée pour {name} (id={idAction})");
    }

    private void OnDisable()
    {
        Debug.LogWarning($"[DEBUG] DialogueTuto {name} a été désactivé (OnDisable). StackTrace:\n{System.Environment.StackTrace}");
    }

}
