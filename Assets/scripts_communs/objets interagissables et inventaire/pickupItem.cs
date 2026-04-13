using UnityEngine;

public class pickupItem : MonoBehaviour
{
    public itemData itemData; // glisse la fiche ici dans l'Inspector

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("player"))
        {
            inventaire.instance.AddItem(itemData);
            Destroy(gameObject);
        }
    }
}