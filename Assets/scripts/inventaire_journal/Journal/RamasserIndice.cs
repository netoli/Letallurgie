// ============================================================
// RamasserIndice.cs
// ------------------------------------------------------------
// Auteur      : Fanny Fortier
// Date        : 09/04/2026
// ------------------------------------------------------------
// Description :
//   Attaché sur chaque objet indice dans la scène. Détecte
//   quand le joueur entre en collision avec l'objet, joue un son,
//   envoie ses données au JournalManager et détruit l'objet (si 
//   c'est pas un NPC).
// ------------------------------------------------------------
// Dépendances :
//   - JournalManager.cs : appelle AjouterEntreeJournal() pour
//     créer le slot dans le journal
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

        Debug.Log("Ramasser() appelé sur : " + gameObject.name);

        bool estPnj = gameObject.name.Contains("pnj_mysterieux");

        // Jouer l'effet sonore au grab
        if (gestionAudio.Instance != null && sonRamasser != null)
            gestionAudio.Instance.JouerSFX(sonRamasser);

        Debug.Log("Avant AjouterEntreeJournal - entrees count : " + JournalManager.Instance.entrees.Count);
        // Créer une instance du prefab de slot dans le journal avec les données rentrées dans l'inspecteur
        JournalManager.Instance.AjouterEntreeJournal(img, titre, description, insight);
        Debug.Log("Après AjouterEntreeJournal - entrees count : " + JournalManager.Instance.entrees.Count);

        if (estPnj)
        {
            gestionSousTitre sousTitre =
                FindFirstObjectByType<gestionSousTitre>(FindObjectsInactive.Include);
            if (sousTitre != null)
                StartCoroutine(JouerDialoguePnj(sousTitre));
            else
                Debug.LogWarning("[RamasserIndice] gestionSousTitre introuvable!");

            _dejaInteragi = true;
            return;
        }else
        {
            // Si l'objet sur lequel ce script est attaché ne s'appelle pas "pnj_mysterieux", le détruire
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
            "Villageois mystérieux",
            "Je n'y crois pas! Le tavernier!",
            3f);
        yield return new WaitForSeconds(3.2f);

        sousTitre.AfficherSousTitre(
            "Villageois mystérieux",
            "Je le connais depuis qu'il est tout petit.",
            3f);
        yield return new WaitForSeconds(3.2f);

        sousTitre.AfficherSousTitre(
            "Villageois mystérieux",
            "JAMAIS je n'aurais pensé...",
            4f);
        yield return new WaitForSeconds(3.2f);

        sousTitre.AfficherSousTitre(
            "Villageois mystérieux",
            "Que le tavernier serait un criminel!",
            4f);
        yield return new WaitForSeconds(3.5f);

        sousTitre.gameObject.SetActive(false);
    }
}