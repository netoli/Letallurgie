using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class gestionAffichageInventaire : MonoBehaviour
{
    [Header("Conteneur de slots")]
    [SerializeField] private Transform conteneurSlots;
    [SerializeField] private GameObject prefabSlot;

    [Header("Onglets")]
    [SerializeField] private Button ongletTuyaux;
    [SerializeField] private Button ongletMorse;
    [SerializeField] private Button ongletCartographie;
    [SerializeField] private Button ongletAlchimie;

    [Header("Scroll")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Button flecheGauche;
    [SerializeField] private Button flecheDroite;
    [SerializeField] private float defilementParClic;

    [Header("Indicateur HUD")]
    [SerializeField] private TMPro.TMP_Text texteNombreObjetsHud;

    private CategorieObjet categorieActuelle = CategorieObjet.Tuyaux;
    private List<GameObject> slotsInstancies = new List<GameObject>();

    void Start()
    {
        ongletTuyaux.onClick.AddListener(
            () => ChangerCategorie(CategorieObjet.Tuyaux));
        ongletMorse.onClick.AddListener(
            () => ChangerCategorie(CategorieObjet.Morse));
        ongletCartographie.onClick.AddListener(
            () => ChangerCategorie(CategorieObjet.Cartographie));
        ongletAlchimie.onClick.AddListener(
            () => ChangerCategorie(CategorieObjet.Alchimie));

        flecheGauche.onClick.AddListener(DefilerGauche);
        flecheDroite.onClick.AddListener(DefilerDroite);
    }



    void OnEnable()
    {
        if (gestionInventaire.Instance != null)
            gestionInventaire.Instance.onInventaireModifie +=
                RafraichirAffichage;

        RafraichirAffichage();
    }

    void OnDisable()
    {
        if (gestionInventaire.Instance != null)
            gestionInventaire.Instance.onInventaireModifie -=
                RafraichirAffichage;
    }

    private void ChangerCategorie(CategorieObjet categorie)
    {
        categorieActuelle = categorie;

        if (scrollRect != null)
            scrollRect.horizontalNormalizedPosition = 0f;

        RafraichirAffichage();
    }

    private void DefilerGauche()
    {
        if (scrollRect == null) return;

        float nouvPos = scrollRect.horizontalNormalizedPosition
            - defilementParClic;
        scrollRect.horizontalNormalizedPosition =
            Mathf.Clamp01(nouvPos);

        MettreAJourFleches();
    }

    private void DefilerDroite()
    {
        if (scrollRect == null) return;

        float nouvPos = scrollRect.horizontalNormalizedPosition
            + defilementParClic;
        scrollRect.horizontalNormalizedPosition =
            Mathf.Clamp01(nouvPos);

        MettreAJourFleches();
    }

    private void MettreAJourFleches()
    {
        if (scrollRect == null) return;

        bool peutDefiler =
            conteneurSlots.GetComponent<RectTransform>().rect.width
            > scrollRect.GetComponent<RectTransform>().rect.width;

        flecheGauche.interactable = peutDefiler
            && scrollRect.horizontalNormalizedPosition > 0.01f;
        flecheDroite.interactable = peutDefiler
            && scrollRect.horizontalNormalizedPosition < 0.99f;
    }

    public void RafraichirAffichage()
    {
        foreach (GameObject slot in slotsInstancies)
            Destroy(slot);
        slotsInstancies.Clear();

        if (gestionInventaire.Instance == null) return;

        List<KeyValuePair<objetInventaire, int>> objets =
            gestionInventaire.Instance.ObtenirParCategorie(
                categorieActuelle);

        for (int i = 0; i < objets.Count; i++)
        {
            GameObject slotGO = Instantiate(
                prefabSlot, conteneurSlots);

            slotObjetInventaire slot = slotGO.GetComponent<slotObjetInventaire>();
            if (slot != null)
                slot.Configurer(objets[i].Key, objets[i].Value);

            slotsInstancies.Add(slotGO);
        }

        if (scrollRect != null)
            scrollRect.horizontalNormalizedPosition = 0f;

        MettreAJourFleches();
        MettreAJourIndicateurHud();
    }

    private void MettreAJourIndicateurHud()
    {
        if (texteNombreObjetsHud != null)
            texteNombreObjetsHud.text =
                gestionInventaire.Instance.ObtenirTotalObjets()
                    .ToString();
    }
}