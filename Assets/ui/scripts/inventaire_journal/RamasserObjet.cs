// ============================================================
// RamasserObjet.cs
// ------------------------------------------------------------
// Projet      : Létallurgie
// Auteur      : Fanny Fortier
// Date        : 09/04/2026
// ------------------------------------------------------------
// Description :
//   Attaché sur chaque objet indice dans la scène. Détecte
//   quand le joueur entre en collision avec l'objet, envoie
//   ses données au JournalManager et détruit l'objet.
// ------------------------------------------------------------
// Dépendances :
//   - JournalManager.cs : appelle AjouterEntreeJournal() pour
//     créer le slot dans le journal
// ============================================================
using UnityEngine;

public class RamasserObjet : MonoBehaviour
{
    [Header("Données de l'indice")]
    public Sprite img;
    [SerializeField] public string titre;
    [SerializeField] public string description;
    [SerializeField] public string insight;

    private void OnTriggerEnter(Collider autre)
    {
        // Si l'objet touché a le tag "Player"
        if (autre.CompareTag("Player")) 
        {
            // Créer une instance du prefab de slot dans le journal avec les données rentrées dans l'inspecteur
            JournalManager.Instance.AjouterEntreeJournal(img, titre, description, insight);
            // Si l'objet sur lequel ce script est attaché ne s'appelle pas "NPC", le détruire
            if (!gameObject.name.Contains("NPC"))
            {
                Destroy(gameObject);
            }               
        }
    }
}