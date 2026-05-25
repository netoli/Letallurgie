// ============================================================
// JournalSlotUI.cs
// ------------------------------------------------------------
// Auteur      : Fanny Fortier
// Date        : 28/03/2026
// ------------------------------------------------------------
// Description :
//   G�re l'affichage du slot d'indice dans le journal. Cr�e un
//   template pour les indices, avec une image, un titre, une
//   description et un insight. G�re les interactions de hover
//   et de toggle clic pour afficher le panneau de d�tail de chaque
//   indice
// ------------------------------------------------------------
// D�pendances :
//   - JournalDetailPanneau.cs : appelle OuvrirPanneauDetail() et FermerPanneauDetail()
//   - JournalManager.cs : appelle InitialiserSlot()
// ============================================================



using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class JournalSlotUI : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler

{
    [Header("Éléments du UI - Liste")]
    public Image imageIndice;
    public TMP_Text textTitreIndice;
    public TMP_Text textDescriptionIndice;
    public TMP_Text textInsightIndice;

    [Header("Audio")]
    [SerializeField] AudioSource sourceAudio;
    [SerializeField] AudioClip sonHover;
    [SerializeField] AudioClip sonClic;

    Sprite _sprite;
    string _description;
    string _insight;
    bool _estEpingle = false;

    static JournalSlotUI slotActif;

    public void InitialiserSlot(Sprite img, string titre,
        string description, string insight)
    {
        imageIndice.sprite = img;
        textTitreIndice.text = titre;
        textDescriptionIndice.text = description;
        textInsightIndice.text = insight;
        _sprite = img;
        _description = description;
        _insight = insight;
        _estEpingle = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (sonHover != null && sourceAudio != null && !sourceAudio.isPlaying)
            sourceAudio.PlayOneShot(sonHover);

        // Aperçu seulement si aucun slot épinglé, ou si c'est ce slot
        if (slotActif == null || slotActif == this)
            JournalDetailPanneau.Instance.OuvrirPanneauDetail(
                _sprite, textTitreIndice.text, _description, _insight);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Fermer seulement si ce slot n'est pas épinglé
        if (!_estEpingle)
            JournalDetailPanneau.Instance.FermerPanneauDetail();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (sonClic != null && sourceAudio != null && !sourceAudio.isPlaying)
            sourceAudio.PlayOneShot(sonClic);

        // Désépingler l'ancien slot actif si c'est un autre
        if (slotActif != null && slotActif != this)
            slotActif._estEpingle = false;

        _estEpingle = !_estEpingle;
        slotActif = _estEpingle ? this : null;

        if (_estEpingle)
            JournalDetailPanneau.Instance.OuvrirPanneauDetail(
                _sprite, textTitreIndice.text, _description, _insight);
        else
            JournalDetailPanneau.Instance.FermerPanneauDetail();
    }
}