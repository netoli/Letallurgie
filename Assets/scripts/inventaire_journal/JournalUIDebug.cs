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
    [Header("Références UI")]
    public Transform contenuParent;
    public GameObject slotPrefab;

    // Par défaut, les slots ne sont pas déja générés
    private bool dejaGenere = false;

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