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
using UnityEngine;

public class RamasserIndice : MonoBehaviour
{
    [Header("Données de l'indice")]

    [SerializeField] public string titre;
    [SerializeField] public string description;
    [SerializeField] public string insight;
    public Sprite img;
    public AudioClip sonRamasser;

    public void Ramasser()
    {
        // Jouer l'effet sonore au grab
        gestionAudio.Instance.JouerSFX(sonRamasser);

        // Créer une instance du prefab de slot dans le journal avec les données rentrées dans l'inspecteur
        JournalManager.Instance.AjouterEntreeJournal(img, titre, description, insight);
        // Si l'objet sur lequel ce script est attaché ne s'appelle pas "NPC", le détruire
        if (!gameObject.name.Contains("NPC"))
        {
            Destroy(gameObject);
        }
    }

    // TEST !!! A SUPPRIMER quand on a le personnage et les collisions
    public void SimulerRamassage()
    {
        Debug.Log("SIMULATION RAMASSAGE : " + titre);
        JournalManager.Instance.AjouterEntreeJournal(img, titre, description, insight);
        if (!gameObject.name.Contains("NPC"))
            Destroy(gameObject);
    }
}