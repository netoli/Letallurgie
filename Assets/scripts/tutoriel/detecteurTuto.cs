using UnityEngine;

public class detecteurTuto : MonoBehaviour
{

    [Header("Identification")]
    [Tooltip("ID de l'action attendue (doit correspondre à DonneesTutoriel.idActionRequise)")]
    [SerializeField] private string idActionRequise;

    [Header("Comportement")]
    [Tooltip("Si coche (defaut), le GameObject se desactive (SetActive " +
        "false) apres le premier contact, pour eviter les retriggers. " +
        "Decoche pour les pointeurs qui doivent rester visibles meme " +
        "apres contact (ex : prefab_pointeur_enigme qui doit rester " +
        "tant que l'enigme n'est pas completee).")]
    [SerializeField] private bool desactiverApresContact = true;

    // lecture publique, écriture privée
    public string IdAction => idActionRequise;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        Debug.Log($"[DetecteurTuto] Triggered idAction={IdAction} by {other.name}");

        if (gestionChapitres.Instance != null)
        {
            // Notifie le gestionnaire de chapitres que l'action a été réalisée
            gestionChapitres.Instance.SignalerAction(IdAction);
        }
        else
        {
            Debug.LogWarning("[DetecteurTuto] gestionChapitres introuvable (singleton non initialisé?)");
        }

        // Desactivation du pointeur APRES contact, independamment du
        // singleton gestionChapitres. Avant, ce SetActive(false) etait
        // dans le if (Instance != null), donc si le singleton manquait
        // (timing de chargement, scene sans gestionChapitres), le pointeur
        // restait visible meme apres avoir ete touche. On le sort du if
        // pour que le pointeur disparaisse toujours apres le premier contact.
        // Decoche dans l'Inspector pour les pointeurs persistants
        // (ex : prefab_pointeur_enigme qui doit rester tant que l'enigme
        // n'est pas completee).
        if (desactiverApresContact)
            gameObject.SetActive(false);
    }


}
