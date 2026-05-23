// ============================================================
// RamasserIndice.cs
// ------------------------------------------------------------
// Auteur      : Fanny Fortier
// Date        : 09/04/2026
// ------------------------------------------------------------
// Description :
//   Attach� sur chaque objet indice dans la sc�ne. D�tecte
//   quand le joueur entre en collision avec l'objet, joue un son,
//   envoie ses donn�es au JournalManager et d�truit l'objet (si 
//   c'est pas un NPC).
// ------------------------------------------------------------
// D�pendances :
//   - JournalManager.cs : appelle AjouterEntreeJournal() pour
//     cr�er le slot dans le journal
// ============================================================
using System.Collections;
using UnityEngine;

public class RamasserIndice : MonoBehaviour
{
    [Header("Données de l'indice")]

    [SerializeField] public string titre;
    [SerializeField] public string description;
    [SerializeField] public string insight;
    public Sprite img;
    public AudioClip sonRamasser;

    private bool _dejaInteragi = false;

    public void Ramasser()
    {
        if (_dejaInteragi) return;

        Debug.Log("Ramasser() appel� sur : " + gameObject.name);

        // Le dialogue PNJ mysterieux se declenche si le GameObject porte
        // le nom "pnj_mysterieux" OU "npc" (selon la scene : la collegue
        // utilise "pnj_mysterieux" dans environnement_taverne1, et le
        // tutoriel utilise "npc" dans monde_assets). On accepte les deux
        // pour rester compatible avec les deux organisations de scene.
        bool estPnj = gameObject.name.Contains("npc");

        // Jouer l'effet sonore au grab
        if (gestionAudio.Instance != null && sonRamasser != null)
            gestionAudio.Instance.JouerSFX(sonRamasser);

        Debug.Log("Avant AjouterEntreeJournal - entrees count : " + JournalManager.Instance.entrees.Count);
        // Cr�er une instance du prefab de slot dans le journal avec les donn�es rentr�es dans l'inspecteur
        JournalManager.Instance.AjouterEntreeJournal(img, titre, description, insight);
        Debug.Log("Apr�s AjouterEntreeJournal - entrees count : " + JournalManager.Instance.entrees.Count);

        _dejaInteragi = true;

        // ARCHITECTURE REFACTOREE (option 1) :
        // Pour les PNJ, le dialogue n'est PLUS joue par ce script.
        // C'est le composant DialogueTuto attache au meme GameObject
        // qui joue le dialogue (systeme uniforme avec le tavernier).
        // Cette methode Ramasser() est appelee a la fin du dialogue
        // via un declencheurAction qui ecoute idActionAFin de l'etape.
        // Ramasser() ajoute donc juste l'entree au journal + SFX,
        // sans demarrer JouerDialoguePnj (devenue obsolete).
        if (estPnj)
        {
            // On ne detruit pas le PNJ - il reste present dans la
            // scene meme apres avoir donne son indice.
            return;
        }
        else
        {
            // Si l'objet sur lequel ce script est attach� ne s'appelle pas "pnj_mysterieux", le d�truire
            Destroy(gameObject);
        }


        
    }

    // TEST !!! A SUPPRIMER quand on a le personnage et les collisions
    /* public void SimulerRamassage()
    {
        Debug.Log("SIMULATION RAMASSAGE : " + titre);
        JournalManager.Instance.AjouterEntreeJournal(img, titre, description, insight);
        if (!gameObject.name.Contains("NPC"))
            Destroy(gameObject);
    }
    */


    private IEnumerator JouerDialoguePnj(gestionSousTitre sousTitre)
    {
        sousTitre.gameObject.SetActive(true);

        sousTitre.AfficherSousTitre(
            "Villageois myst�rieux",
            "Je n'y crois pas! Le tavernier!",
            3f);
        yield return new WaitForSeconds(3.2f);

        sousTitre.AfficherSousTitre(
            "Villageois myst�rieux",
            "Je le connais depuis qu'il est tout petit.",
            3f);
        yield return new WaitForSeconds(3.2f);

        sousTitre.AfficherSousTitre(
            "Villageois myst�rieux",
            "JAMAIS je n'aurais pens�...",
            4f);
        yield return new WaitForSeconds(3.2f);

        sousTitre.AfficherSousTitre(
            "Villageois myst�rieux",
            "Que le tavernier serait un criminel!",
            4f);
        yield return new WaitForSeconds(3.5f);

        sousTitre.gameObject.SetActive(false);
    }
}