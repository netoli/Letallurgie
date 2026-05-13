using UnityEngine;
using TMPro;

public class compteurInventaireHud : MonoBehaviour
{
    [SerializeField] private TMP_Text texteNombreObjetsHud;

    void OnEnable()
    {
        if (gestionInventaire.Instance != null)
            gestionInventaire.Instance.onInventaireModifieHud += MettreAJour;

        // initialisation immédiate (au cas où des objets ont été ramassés avant activation)
        if (texteNombreObjetsHud != null && gestionInventaire.Instance != null)
            texteNombreObjetsHud.text = gestionInventaire.Instance.ObtenirTotalObjets().ToString();
    }

    void OnDisable()
    {
        if (gestionInventaire.Instance != null)
            gestionInventaire.Instance.onInventaireModifieHud -= MettreAJour;
    }

    private void MettreAJour(int total)
    {
        if (texteNombreObjetsHud != null)
            texteNombreObjetsHud.text = total.ToString();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        MettreAJour(gestionInventaire.Instance != null ? gestionInventaire.Instance.ObtenirTotalObjets() : 0);
    }
}
