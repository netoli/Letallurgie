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

    [Header("Tuto")]
    [Tooltip("ID d'action à signaler à gestionChapitres quand l'objet est ramassé")]
    [SerializeField] private string idActionADeclencher;

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

        // Jouer son si assigné
        if (sonRamasser != null)
            AudioSource.PlayClipAtPoint(sonRamasser, transform.position);

        // Signaler l'action au système de chapitres (si un idAction est fourni)
        if (!string.IsNullOrEmpty(idActionADeclencher) && gestionChapitres.Instance != null)
        {
            gestionChapitres.Instance.SignalerAction(idActionADeclencher);
            Debug.Log($"[Pickup] SignalerAction appelé: {idActionADeclencher}");
        }

        Destroy(gameObject);
    }
}