// ============================================================
// JournalSlotUI.cs
// ------------------------------------------------------------
// Projet      : Létallurgie
// Auteur      : Fanny Fortier
// Date        : 28/03/2026
// ------------------------------------------------------------
// Description :
//   Gère l'affichage du slot d'indice dans le journal. Crée un
//   template pour les indices, avec une image, un titre, une
//   description et un insight. Gère les interactions de hover
//   et de toggle clic pour afficher le panneau de détail de chaque
//   indice
// ------------------------------------------------------------
// Dépendances :
//   - 
// ============================================================



using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class JournalSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{


    [Header("Éléments du UI - Liste")]
    public Image imageIndice;
    public TMP_Text textTitreIndice;
    public TMP_Text textDescriptionIndice;
    public TMP_Text textInsightIndice;

    [Header("Données de l'indice")]
    private string _description;
    private string _insight;


    private bool _panneauOuvert = false;

    // Assigner les données de l'indice au slot
    public void InitialiserSlot(Sprite img, string titre, string description, string insight)
    {
        imageIndice.sprite = img;
        textTitreIndice.text = titre;
        textDescriptionIndice.text = description;
        textInsightIndice.text = insight;
        _description = description;
        _insight = insight;
        _panneauOuvert = false;
    }

    // Détection du hover
    public void OnPointerEnter(PointerEventData eventData)
    {
        JournalDetailPanneau.Instance.OuvrirPanneauDetail(imageIndice.sprite, textTitreIndice.text, _description, _insight);
    }

    // Détection de la sortie du hover
    public void OnPointerExit(PointerEventData eventData)
    {
        if (!_panneauOuvert)
            JournalDetailPanneau.Instance.FermerPanneauDetail();
    }

    // Détection du clic pour toggle le panneau de détail
    public void OnPointerClick(PointerEventData eventData)
    {
        _panneauOuvert = !_panneauOuvert;

        if (_panneauOuvert)
            JournalDetailPanneau.Instance.OuvrirPanneauDetail(imageIndice.sprite, textTitreIndice.text, _description, _insight);
        else
            JournalDetailPanneau.Instance.FermerPanneauDetail();
    }
}