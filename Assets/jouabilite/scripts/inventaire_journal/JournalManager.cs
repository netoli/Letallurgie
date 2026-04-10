// ============================================================
// JournalManager.cs
// ------------------------------------------------------------
// Projet      : Létallurgie
// Auteur      : Fanny Fortier
// Date        : 09/04/2026
// ------------------------------------------------------------
// Description :
//   Gère la création des entrées dans le journal.
//   Attaché sur canvas_journal.
// ------------------------------------------------------------
// Dépendances :
//   - JournalSlotUI.cs  : appelle InitialiserSlot() sur chaque slot créé
//   - RamasserObjet.cs  : appelle AjouterEntreeJournal() pour ajouter un indice
// ============================================================
using UnityEngine;

public class JournalManager : MonoBehaviour
{
    // créer une instance pour facilement référer le JournalManager
    public static JournalManager Instance; 

    // Parent de tous les slots 
    public Transform contenuParent;

    // Prefab de slot
    public GameObject slotPrefab;

    private void Awake()
    {
        // DEBUG : éviter d'avoir plusieurs instances du JournalManager avec les changements de scène
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        // Enregistrer
        Instance = this;

        // Garde le JournalManager dans toutes les scènes
        DontDestroyOnLoad(gameObject);
    }

    // Fonction appelée chaque fois qu'on touche un objet 
    public void AjouterEntreeJournal(Sprite icone, string titre, string description, string insight)
    {
        // Créer un nouveau slot dans le journal
        GameObject nouveauSlot = Instantiate(slotPrefab, contenuParent);
        // Initialiser les données de l'indice
        JournalSlotUI ui = nouveauSlot.GetComponent<JournalSlotUI>();
        ui.InitialiserSlot(icone, titre, description, insight);
    }
}