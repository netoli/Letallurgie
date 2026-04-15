using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class slotObjetInventaire : MonoBehaviour
{
    [Header("References")]
    public Image iconeObjet;
    public TMP_Text texteNombreObjets;

    private objetInventaire objetAssocie;

    public void Configurer(objetInventaire objet, int quantite)
    {
        objetAssocie = objet;

        if (iconeObjet != null)
            iconeObjet.sprite = objet.icone;

        MettreAJourQuantite(quantite);
    }

    public void MettreAJourQuantite(int quantite)
    {
        if (texteNombreObjets != null)
            texteNombreObjets.text = quantite.ToString();
    }

    public objetInventaire ObtenirObjet()
    {
        return objetAssocie;
    }
}