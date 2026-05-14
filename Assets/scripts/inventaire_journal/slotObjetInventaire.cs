using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class slotObjetInventaire : MonoBehaviour, IPointerClickHandler
{
    [Header("Références visuelles")]
    [SerializeField] private Image slotImage;
    [SerializeField] private Image iconeObjet;
    [SerializeField] private TMP_Text texteNombre;

    [Header("Couleurs du slot")]
    [SerializeField] private Color couleurNormale;
    [SerializeField] private Color couleurSelectionnee;

    [Header("Scale")]
    [SerializeField] private float scaleNormal;
    [SerializeField] private float scaleSelectionne;

    private objetInventaire objetAffiche;

    public void Configurer(objetInventaire objet, int quantite)
    {
        objetAffiche = objet;
        iconeObjet.sprite = objet.icone;
        texteNombre.text = quantite.ToString();
        MettreAJourVisuelSelection();
    }

    void OnEnable()
    {
        if (gestionSelectionInventaire.Instance != null)
        {
            gestionSelectionInventaire.Instance.onSelectionChangee
                += SurSelectionChangee;
        }
    }

    void OnDisable()
    {
        if (gestionSelectionInventaire.Instance != null)
        {
            gestionSelectionInventaire.Instance.onSelectionChangee
                -= SurSelectionChangee;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"[Slot] Clic detecte sur " +
            $"{(objetAffiche != null ? objetAffiche.nomObjet : "(slot vide)")}");

        if (objetAffiche == null)
        {
            Debug.LogWarning("[Slot] objetAffiche est null, clic ignore.");
            return;
        }

        // Avec le nouveau singleton auto-cree, Instance ne devrait
        // jamais etre null. Mais on garde le check par securite.
        if (gestionSelectionInventaire.Instance == null)
        {
            Debug.LogWarning("[Slot] gestionSelectionInventaire.Instance " +
                "est null, clic ignore.");
            return;
        }

        Debug.Log($"[Slot] Selection de '{objetAffiche.nomObjet}'");
        gestionSelectionInventaire.Instance.Selectionner(objetAffiche);
    }

    private void SurSelectionChangee(objetInventaire nouvelle)
    {
        MettreAJourVisuelSelection();
    }

    private void MettreAJourVisuelSelection()
    {
        if (gestionSelectionInventaire.Instance == null) return;

        bool estSelectionne =
            gestionSelectionInventaire.Instance.ObtenirSelection() == objetAffiche
            && objetAffiche != null;

        if (slotImage != null)
        {
            slotImage.color = estSelectionne ? couleurSelectionnee : couleurNormale;
        }

        float scale = estSelectionne ? scaleSelectionne : scaleNormal;
        transform.localScale = Vector3.one * scale;
    }
}