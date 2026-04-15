using UnityEngine;

public class objetRamassable : MonoBehaviour
{
    [Header("Definition")]
    public objetInventaire objetInventaire;

    public void Ramasser()
    {
        gestionInventaire.Instance.AjouterObjet(objetInventaire);
        Destroy(gameObject);
    }
}