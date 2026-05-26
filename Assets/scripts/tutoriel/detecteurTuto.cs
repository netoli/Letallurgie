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

            // Optionnel : desactiver ce detecteur pour eviter retriggers.
            // Decoche pour les pointeurs persistants (ex : pointeur_enigme
            // qui doit rester visible jusqu'a la fin de l'enigme).
            if (desactiverApresContact)
                gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("[DetecteurTuto] gestionChapitres introuvable (singleton non initialisé?)");
        }
    }


}
