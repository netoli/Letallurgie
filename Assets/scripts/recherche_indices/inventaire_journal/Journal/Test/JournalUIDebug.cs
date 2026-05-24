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
    //private bool dejaGenere = false;

    /* private void OnEnable()
    {
        Debug.Log("JournalUIRenderer ACTIVÉ");

        // Générer seulement la première fois
        if (!dejaGenere)
        {
            dejaGenere = true;
            FindObjectOfType<TestJournal>()?.TestJDB();
            GenererUI();
        }

    } */

    private void OnEnable()
    {
        Debug.Log("JournalUIRenderer ACTIVÉ");
        // Toujours (re)générer à l'activation pour refléter l'état courant du manager
        GenererUI();
    }


    /* void GenererUI()
    {
        Debug.Log("Génération UI : " + JournalManager.Instance.entrees.Count + " entrées");

        foreach (var entree in JournalManager.Instance.entrees)
        {
            GameObject slot = Instantiate(slotPrefab, contenuParent);

            slot.GetComponent<JournalSlotUI>()
                .InitialiserSlot(entree.icone, entree.titre, entree.description, entree.insight);
        }
    } */

    void GenererUI()
    {
        if (JournalManager.Instance == null)
        {
            Debug.LogError("[Journal] JournalManager.Instance is NULL");
            return;
        }

        Debug.Log($"Génération UI : {JournalManager.Instance.entrees.Count} entrées");

        // Vider d'abord le contenu existant (sécurise contre doublons)
        for (int i = contenuParent.childCount - 1; i >= 0; i--)
            Destroy(contenuParent.GetChild(i).gameObject);

        // Instancier proprement sous le parent (false = keep local transform)
        foreach (var entree in JournalManager.Instance.entrees)
        {
            if (slotPrefab == null)
            {
                Debug.LogError("[Journal] slotPrefab is NULL");
                break;
            }

            GameObject slot = Instantiate(slotPrefab, contenuParent, false);
            slot.SetActive(true);

            var ui = slot.GetComponent<JournalSlotUI>();
            if (ui != null)
                ui.InitialiserSlot(entree.icone, entree.titre, entree.description, entree.insight);
            else
                Debug.LogWarning("[Journal] slotPrefab missing JournalSlotUI component");
        }

        // Forcer le recalcul du layout (résout la plupart des problèmes d'affichage)
        Canvas.ForceUpdateCanvases();
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(contenuParent.GetComponent<RectTransform>());

        Debug.Log($"[Journal] Après génération : content childCount={contenuParent.childCount}");
    }


}