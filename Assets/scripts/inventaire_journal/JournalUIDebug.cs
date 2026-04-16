using UnityEngine;
// ============================================================
// JournalUIDebug.cs
// ------------------------------------------------------------
// Auteur      : Fanny Fortier
// Date        : 13/04/2026
// ------------------------------------------------------------
// Description :
//   G�n�re les slots UI du journal lorsque le canvas du journal
//   devient actif. Script debug pour r�gler le probl�me de slots
//   qui ne s'instancient pas pendant que le UI est d�sactiv�. 
//   Attach� sur canvas_journal.
// ------------------------------------------------------------
// D�pendances :
//   - JournalManager.cs : fournit les donn�es
//   - JournalSlotUI.cs  : initialise chaque slot
// ============================================================
public class JournalUIDebug : MonoBehaviour
{
    [Header("References UI")]
    public Transform contenuParent;
    public GameObject slotPrefab;

    private void OnEnable()
    {
        if (JournalManager.Instance != null
            && JournalManager.Instance.entrees.Count == 0)
        {
            JournalManager.Instance.AjouterEntreeJournal(
                null, "Cle rouille",
                "Une vieille cle trouvee dans la taverne.",
                "Elle pourrait ouvrir le coffre du sous-sol.");
            JournalManager.Instance.AjouterEntreeJournal(
                null, "Note du barman",
                "Un message griffonne sur un bout de papier.",
                "Le barman semble cacher quelque chose.");
        }

        GenererUI();
    }

    void GenererUI()
    {
        if (JournalManager.Instance == null) return;

        foreach (Transform enfant in contenuParent)
            Destroy(enfant.gameObject);

        Debug.Log("Journal: " +
            JournalManager.Instance.entrees.Count + " entrees");

        foreach (var entree in JournalManager.Instance.entrees)
        {
            GameObject slot = Instantiate(slotPrefab, contenuParent);
            JournalSlotUI ui = slot.GetComponent<JournalSlotUI>();
            if (ui != null)
                ui.InitialiserSlot(entree.icone, entree.titre,
                    entree.description, entree.insight);
        }
    }
}