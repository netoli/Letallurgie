using TMPro;
using UnityEngine;

public class JournalCompteurHUD : MonoBehaviour
{

    public TMP_Text compteurTexte;

    private void OnEnable()
    {
        MettreAJourCompteur();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        MettreAJourCompteur();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void MettreAJourCompteur()
    {
        if (JournalManager.Instance == null) return;

        int count = JournalManager.Instance.entrees.Count;
        compteurTexte.text = count.ToString();
    }
}
