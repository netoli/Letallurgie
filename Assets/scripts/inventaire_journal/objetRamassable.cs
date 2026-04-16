using UnityEngine;

public class objetRamassable : MonoBehaviour
{
    [Header("Inventaire")]
    [SerializeField] private bool ajouterInventaire;
    public objetInventaire objetInventaire;

    [Header("Journal")]
    [SerializeField] private bool ajouterJournal;
    [SerializeField] private string titreJournal;
    [SerializeField] private string descriptionJournal;
    [SerializeField] private string insightJournal;
    [SerializeField] private Sprite imageJournal;

    [Header("Audio")]
    [SerializeField] private AudioClip sonRamasser;

    public void Ramasser()
    {
        if (ajouterInventaire
            && objetInventaire != null
            && gestionInventaire.Instance != null)
            gestionInventaire.Instance.AjouterObjet(objetInventaire);

        if (ajouterJournal
            && JournalManager.Instance != null)
            JournalManager.Instance.AjouterEntreeJournal(
                imageJournal,
                titreJournal,
                descriptionJournal,
                insightJournal);

        Destroy(gameObject);
    }
}