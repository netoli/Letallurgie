using UnityEngine;
// ============================================================
// JournalUIDebug.cs
// ------------------------------------------------------------
// Auteur      : Fanny Fortier
// Date        : 13/04/2026
// ------------------------------------------------------------
// Description :
//   Génère les slots UI du journal lorsque le canvas du journal
//   devient actif. Script debug pour régler le problème de slots
//   qui ne s'instancient pas pendant que le UI est désactivé. 
//   Attaché sur canvas_journal.
// ------------------------------------------------------------
// Dépendances :
//   - JournalManager.cs : fournit les données
//   - JournalSlotUI.cs  : initialise chaque slot
// ============================================================
public class JournalUIDebug : MonoBehaviour
{
    [Header("Références UI")]
    public Transform contenuParent; //Où générer
    public GameObject slotPrefab; //Quoi générer

    // Par défaut, les slots ne sont pas déja générés
    private bool dejaGenere = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnEnable()
    {
        Debug.Log("JournalUIRenderer ACTIVÉ");

        // Générer seulement la première fois
        if (!dejaGenere)
        {
            dejaGenere = true;
            FindObjectOfType<TestJournal>()?.TestJDB();
            GenererUI();
        }
    }

    void GenererUI()
    {
        Debug.Log("Génération UI : " + JournalManager.Instance.entrees.Count + " entrées");

        foreach (var entree in JournalManager.Instance.entrees)
        {
            GameObject slot = Instantiate(slotPrefab, contenuParent);

            slot.GetComponent<JournalSlotUI>()
                .InitialiserSlot(entree.icone, entree.titre, entree.description, entree.insight);
        }
    }
}
