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
        // Gating tutoriel STRICT : si cet objet est associe a une etape
        // de tuto (idActionADeclencher non vide), on EXIGE que la tuile
        // correspondante soit affichee. Sinon le joueur peut "griller"
        // un step (ramasser la bouteille avant meme que la tuile
        // "Ramasser une bouteille" s'affiche, ce qui rend le tuto
        // bloque ensuite ou cree un decalage visuel).
        //
        // Cas couverts :
        //   - tuile pas encore affichee (debut de jeu)          -> BLOQUE
        //   - tuile autre affichee                              -> BLOQUE
        //   - bonne tuile affichee                              -> AUTORISE
        //   - aucune tuile (entre 2 tuiles, ou tuto fini)       -> BLOQUE
        //
        // Pour autoriser un objet hors tuto, laisse simplement
        // idActionADeclencher vide.
        if (!string.IsNullOrEmpty(idActionADeclencher)
            && gestionChapitres.Instance != null)
        {
            string attendu = gestionChapitres.Instance.IdActionAttenduActuelle;
            if (attendu != idActionADeclencher)
            {
                Debug.Log($"[Pickup] Ramassage refuse : tuile attendue=" +
                    $"'{attendu}', cet objet requiert '{idActionADeclencher}'. " +
                    $"La tuile tuto correspondante doit etre affichee.");
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