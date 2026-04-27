// ============================================================
// JournalManager.cs
// ------------------------------------------------------------
// Auteur      : Fanny Fortier
// Date créé   : 09/04/2026
// Dernière modification : 22/04/2026 - Fanny Fortier
// ------------------------------------------------------------
// Description :
//   Gère la création des entrées dans le journal.
//   Attaché sur gestion_journal (game object vide dans les managers).
// ------------------------------------------------------------
// Dépendances :
//   - JournalSlotUI.cs  : appelle InitialiserSlot() sur chaque slot créé
//   - RamasserObjet.cs  : appelle AjouterEntreeJournal() pour ajouter un indice
// ============================================================
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class JournalManager : MonoBehaviour
{
    public static JournalManager Instance;
    public List<EntreeJournal> entrees = new List<EntreeJournal>();

    [Header("HUD")]
    [SerializeField] private TMP_Text compteurHUD;

    private void Awake()
    {
        // Assure que le JournalManager est un singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void AjouterEntreeJournal(Sprite icone, string titre, string description, string insight)
    {
        // Ajouter une nouvelle entrée au JDB
        entrees.Add(new EntreeJournal(icone, titre, description, insight));

        // Mettre à jour le compteur HUD
        MettreAJourCompteur();
    }

    public void MettreAJourCompteur()
    {
        if (compteurHUD == null) return;
        compteurHUD.text = entrees.Count.ToString();
    }


    [System.Serializable]
    public class EntreeJournal
    {
        public Sprite icone;
        public string titre;
        public string description;
        public string insight;

        public EntreeJournal(Sprite i, string t, string d, string ins)
        {
            icone = i;
            titre = t;
            description = d;
            insight = ins;
        }
    }
}