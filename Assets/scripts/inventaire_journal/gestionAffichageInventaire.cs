using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class gestionAffichageInventaire : MonoBehaviour
{
    [Header("Conteneur de slots")]
    [SerializeField] private Transform conteneurSlots;
    [SerializeField] private GameObject prefabSlot;

    [Header("Onglets")]
    [Tooltip("Onglet tutoriel (visible uniquement pendant la sequence " +
        "tuto si le joueur a un objet de categorie Tuto dans son inventaire).")]
    [SerializeField] private Button ongletTuto;
    [SerializeField] private Button ongletTuyaux;
    [SerializeField] private Button ongletAlchimie;

    [Header("Scroll")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Button flecheGauche;
    [SerializeField] private Button flecheDroite;
    [SerializeField] private float defilementParClic;

    [Header("Indicateur HUD")]
    [SerializeField] private TMPro.TMP_Text texteNombreObjetsHud;

    private CategorieObjet categorieActuelle = CategorieObjet.Tuto;
    private List<GameObject> slotsInstancies = new List<GameObject>();

    void Start()
    {
        if (ongletTuto != null)
            ongletTuto.onClick.AddListener(
                () => ChangerCategorie(CategorieObjet.Tuto));
        if (ongletTuyaux != null)
            ongletTuyaux.onClick.AddListener(
                () => ChangerCategorie(CategorieObjet.Tuyaux));
        if (ongletAlchimie != null)
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
        // 1. Mettre a jour la visibilite des onglets selon leur contenu
        MettreAJourVisibiliteOnglets();

        // 2. Si la categorie actuellement selectionnee est devenue vide
        //    (ex: dernier objet tuto vient d'etre utilise), basculer sur
        //    le premier onglet visible.
        if (gestionInventaire.Instance != null
            && gestionInventaire.Instance.ObtenirParCategorie(
                categorieActuelle).Count == 0)
        {
            CategorieObjet? premiereVisible = TrouverPremiereCategorieAvecObjets();
            if (premiereVisible.HasValue)
                categorieActuelle = premiereVisible.Value;
        }

        // 3. Reconstruire les slots de la categorie courante
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

    // Affiche un onglet uniquement si sa categorie contient au moins
    // un objet. Quand un onglet est cache, son GameObject est desactive
    // (donc pas cliquable, pas visible).
    private void MettreAJourVisibiliteOnglets()
    {
        if (gestionInventaire.Instance == null) return;

        AfficherOngletSiContenu(ongletTuto, CategorieObjet.Tuto);
        AfficherOngletSiContenu(ongletTuyaux, CategorieObjet.Tuyaux);
        AfficherOngletSiContenu(ongletAlchimie, CategorieObjet.Alchimie);
    }

    private void AfficherOngletSiContenu(Button onglet, CategorieObjet categorie)
    {
        if (onglet == null) return;
        bool aDuContenu = gestionInventaire.Instance
            .ObtenirParCategorie(categorie).Count > 0;
        onglet.gameObject.SetActive(aDuContenu);
    }

    private CategorieObjet? TrouverPremiereCategorieAvecObjets()
    {
        if (gestionInventaire.Instance == null) return null;

        // Ordre de priorite (premier non vide gagne)
        CategorieObjet[] ordre = new CategorieObjet[]
        {
            CategorieObjet.Tuto,
            CategorieObjet.Tuyaux,
            CategorieObjet.Alchimie
        };

        foreach (var cat in ordre)
        {
            if (gestionInventaire.Instance.ObtenirParCategorie(cat).Count > 0)
                return cat;
        }
        return null;
    }

    private void MettreAJourIndicateurHud()
    {
        if (texteNombreObjetsHud != null)
            texteNombreObjetsHud.text =
                gestionInventaire.Instance.ObtenirTotalObjets()
                    .ToString();
    }
}