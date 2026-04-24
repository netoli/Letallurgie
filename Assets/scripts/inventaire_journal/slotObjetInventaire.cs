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
        {

            iconeObjet.sprite = objet.icone;

            Debug.Log($"Configurer: objet={objet.nomObjet} sprite={(objet.icone != null ? objet.icone.name : "NULL")}");
            iconeObjet.SetNativeSize();

            MettreAJourQuantite(quantite);

        }
        else
        {
            Debug.LogWarning("Configurer: iconeObjet est null");
        }

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