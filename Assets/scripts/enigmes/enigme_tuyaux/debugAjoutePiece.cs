using UnityEngine;

public class debugAjoutePiece : MonoBehaviour
{
    [SerializeField] private objetInventaire pieceAAjouter;
    [SerializeField] private int quantite = 1;

    void Start()
    {
        if (gestionInventaire.Instance != null && pieceAAjouter != null)
        {
            gestionInventaire.Instance.AjouterObjet(pieceAAjouter, quantite);
        }
    }
}