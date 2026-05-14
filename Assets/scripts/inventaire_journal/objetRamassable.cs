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
    [Tooltip("ID d'action � signaler � gestionChapitres quand l'objet est ramass�")]
    [SerializeField] private string idActionADeclencher;

    public void Ramasser()
    {
        // Gating tutoriel : si cet objet est associe a une etape de tuto
        // (idActionADeclencher non vide), refuser le ramassage tant que
        // la tuile en cours n'attend pas justement cette action. Sinon
        // le joueur peut "griller" un step (ramasser la bouteille avant
        // que la tuile "Ramasser une bouteille" s'affiche, ce qui rend
        // le tuto bloque ensuite).
        if (!string.IsNullOrEmpty(idActionADeclencher)
            && gestionChapitres.Instance != null)
        {
            string attendu = gestionChapitres.Instance.IdActionAttenduActuelle;
            // Si une tuile est active et qu'elle attend une autre action
            // que la notre, on bloque. Si aucune tuile active (tuto fini),
            // on autorise normalement.
            if (!string.IsNullOrEmpty(attendu)
                && attendu != idActionADeclencher)
            {
                Debug.Log($"[Pickup] Ramassage refuse : la tuile en cours " +
                    $"attend '{attendu}', cet objet declenche " +
                    $"'{idActionADeclencher}'. Joueur doit attendre la " +
                    $"bonne tuile de tuto.");
                return;
            }
        }

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

        // Jouer son si assign�
        if (sonRamasser != null)
            AudioSource.PlayClipAtPoint(sonRamasser, transform.position);

        // Signaler l'action au syst�me de chapitres (si un idAction est fourni)
        if (!string.IsNullOrEmpty(idActionADeclencher) && gestionChapitres.Instance != null)
        {
            gestionChapitres.Instance.SignalerAction(idActionADeclencher);
            Debug.Log($"[Pickup] SignalerAction appel�: {idActionADeclencher}");
        }

        Destroy(gameObject);
    }
}