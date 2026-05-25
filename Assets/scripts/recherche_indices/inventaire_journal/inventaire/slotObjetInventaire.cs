// ============================================================
// slotObjetInventaire.cs
// ------------------------------------------------------------
// Auteur      : Olivier Vernet
// Derniere modification : 14/05/2026 - merge integration_prototype_build_2
// ------------------------------------------------------------
// Description :
//   Composant attache a chaque slot d'inventaire. Gere :
//     - l'affichage (icone + quantite)
//     - la selection au clic (IPointerClickHandler) avec feedback
//       visuel (couleur + scale) via gestionSelectionInventaire
//     - l'envoi de l'objet sur la balance (scene4_manoir) si une
//       ZoneDepotJoueur est assignee
// ============================================================

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;

public class slotObjetInventaire : MonoBehaviour, IPointerClickHandler
{
    [Header("References visuelles")]
    [SerializeField] private Image slotImage;
    [SerializeField] private Image iconeObjet;
    [SerializeField] private TMP_Text texteNombre;

    [Header("Couleurs du slot (selection)")]
    [SerializeField] private Color couleurNormale;
    [SerializeField] private Color couleurSelectionnee;

    [Header("Scale (selection)")]
    [SerializeField] private float scaleNormal = 1f;
    [SerializeField] private float scaleSelectionne = 1.1f;

    private objetInventaire objetAffiche;

    // Reference optionnelle a la zone de depot pour l'enigme balance.
    // Assignee par AssignerZoneBalance() depuis la scene4_manoir.
    private static ZoneDepotJoueur _zoneBalance;

    public static void AssignerZoneBalance(ZoneDepotJoueur zone)
    {
        _zoneBalance = zone;
    }

    public void Configurer(objetInventaire objet, int quantite)
    {
        objetAffiche = objet;

        if (iconeObjet != null && objet != null)
        {
            iconeObjet.sprite = objet.icone;
            // On NE force PAS SetNativeSize() : sinon l'Image prend
            // la resolution pixel du sprite (ex: 512x512) au lieu de
            // la taille du prefab du slot. Pour garder les proportions,
            // coche "Preserve Aspect" sur le composant Image du prefab.
        }

        MettreAJourQuantite(quantite);
        MettreAJourVisuelSelection();

        // Listener balance : uniquement dans scene4_manoir.
        // Permet au clic sur le slot d'envoyer l'objet sur la balance
        // (en plus du comportement de selection standard).
        if (SceneManager.GetActiveScene().name == "scene4_manoir")
        {
            Button btn = GetComponent<Button>();
            if (btn != null && _zoneBalance != null)
            {
                btn.onClick.RemoveListener(UtiliserSurBalance);
                btn.onClick.AddListener(UtiliserSurBalance);
            }
        }
    }

    public void MettreAJourQuantite(int quantite)
    {
        if (texteNombre != null)
            texteNombre.text = quantite.ToString();
    }

    // Appele quand le joueur clique sur le slot en mode balance.
    private void UtiliserSurBalance()
    {
        if (objetAffiche == null || _zoneBalance == null) return;

        // Fallback intelligent : utilise prefab3D si defini, sinon
        // prefabModele3D. Permet aux assets de l'ancien systeme balance
        // (prefab3D) et du nouveau systeme tuyaux/tuto (prefabModele3D)
        // de cohabiter pendant la transition.
        GameObject prefabAUtiliser = objetAffiche.prefab3D != null
            ? objetAffiche.prefab3D
            : objetAffiche.prefabModele3D;

        if (prefabAUtiliser == null)
        {
            Debug.LogWarning($"[Slot] {objetAffiche.nomObjet} n'a " +
                "ni prefab3D ni prefabModele3D defini.");
            return;
        }

        GameObject instance = Instantiate(
            prefabAUtiliser,
            _zoneBalance.transform.position,
            Quaternion.identity);

        objetPesable composant = instance.GetComponent<objetPesable>();
        if (composant != null)
        {
            _zoneBalance.AjouterObjet(composant);
            if (gestionInventaire.Instance != null)
                gestionInventaire.Instance.RetirerObjet(objetAffiche);
            Debug.Log($"[Slot] {objetAffiche.nomObjet} envoye sur la balance.");
        }
        else
        {
            Debug.LogError($"[Slot] Le prefab de {objetAffiche.nomObjet} " +
                "n'a pas de composant objetPesable.");
            Destroy(instance);
        }
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

        if (gestionSelectionInventaire.Instance == null)
        {
            Debug.LogWarning("[Slot] gestionSelectionInventaire.Instance " +
                "est null, clic ignore.");
            return;
        }

        // Toggle : si cet objet est deja celui qui est selectionne,
        // un re-clic le deselectionne. Sinon on le selectionne.
        objetInventaire selectionActuelle =
            gestionSelectionInventaire.Instance.ObtenirSelection();

        if (selectionActuelle == objetAffiche)
        {
            Debug.Log($"[Slot] Deselection de '{objetAffiche.nomObjet}'.");
            gestionSelectionInventaire.Instance.Deselectionner();
        }
        else
        {
            Debug.Log($"[Slot] Selection de '{objetAffiche.nomObjet}'.");
            gestionSelectionInventaire.Instance.Selectionner(objetAffiche);
        }
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
            slotImage.color = estSelectionne
                ? couleurSelectionnee
                : couleurNormale;
        }

        float scale = estSelectionne ? scaleSelectionne : scaleNormal;
        transform.localScale = Vector3.one * scale;
    }
}
