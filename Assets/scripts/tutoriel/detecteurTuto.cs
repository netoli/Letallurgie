using UnityEngine;

public class detecteurTuto : MonoBehaviour
{

    [Header("Identification")]
    [Tooltip("ID de l'action attendue (doit correspondre à DonneesTutoriel.idActionRequise)")]
    [SerializeField] private string idActionRequise;

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

            //désactiver ce détecteur pour éviter retriggers
            gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("[DetecteurTuto] gestionChapitres introuvable (singleton non initialisé?)");
        }
    }


}
