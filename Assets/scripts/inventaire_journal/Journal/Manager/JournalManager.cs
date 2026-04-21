// ============================================================
// JournalManager.cs
// ------------------------------------------------------------
// Auteur      : Fanny Fortier
// Date        : 09/04/2026
// ------------------------------------------------------------
// Description :
//   G�re la cr�ation des entr�es dans le journal.
//   Attach� sur canvas_journal.
// ------------------------------------------------------------
// D�pendances :
//   - JournalSlotUI.cs  : appelle InitialiserSlot() sur chaque slot cr��
//   - RamasserObjet.cs  : appelle AjouterEntreeJournal() pour ajouter un indice
// ============================================================
using System.Collections.Generic;
using UnityEngine;

public class JournalManager : MonoBehaviour
{
    public static JournalManager Instance;

    public List<EntreeJournal> entrees = new List<EntreeJournal>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void AjouterEntreeJournal(Sprite icone,
        string titre, string description, string insight)
    {
        entrees.Add(new EntreeJournal(
            icone, titre, description, insight));
    }

    [System.Serializable]
    public class EntreeJournal
    {
        public Sprite icone;
        public string titre;
        public string description;
        public string insight;

        public EntreeJournal(Sprite i, string t,
            string d, string ins)
        {
            icone = i;
            titre = t;
            description = d;
            insight = ins;
        }
    }
}