// ============================================================
// JournalDetailPanneau.cs
// ------------------------------------------------------------
// Projet      : Létallurgie
// Auteur      : Fanny Fortier
// Date        : 28/03/2026
// ------------------------------------------------------------
// Description :
//   Gère le panneau de détail gauche du journal. Attaché sur
//   indices_agrandis. Affiche ou cache le panneau selon les
//   interactions du slot sélectionné.
// ------------------------------------------------------------
// Dépendances :
//   - JournalSlotUI.cs : appelle OuvrirPanneauDetail() et FermerPanneauDetail()
// ============================================================
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class JournalDetailPanneau : MonoBehaviour
{
    // Instance singleton pour référer facilement au panneau
    public static JournalDetailPanneau Instance;

    [Header("Éléments du UI - Détails")]
    public GameObject panneauDetails; // Le container du panneau détail à activer/désactiver
    public Image imageIndice; // L'image de l'indice
    public TMP_Text textTitre; // Le titre de l'indice
    public TMP_Text textDescription; // La description de l'indice
    public TMP_Text textInsight; // Le morceau d'histoire révélé par l'indice

    private void Awake()
    {
        // DEBUG : Si un autre panneau existe déjà, détruis ce panneau
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        // Enregistrer cette instance 
        Instance = this;

        // Désactivé par défaut
        panneauDetails.SetActive(false);
    }

    public void OuvrirPanneauDetail(Sprite img, string titre, string description, string insight) // Appelée par JournalSlotUI pour afficher le panneau avec les données de l'indice survolé ou cliqué
    {
        // Lier les éléments du panneau aux données de l'indice
        imageIndice.sprite = img;
        textTitre.text = titre;
        textDescription.text = description;
        textInsight.text = insight; 
        // Activer le panneau détails
        panneauDetails.SetActive(true);
    }

    public void FermerPanneauDetail()
    {
        // Désactiver le panneau détails
        panneauDetails.SetActive(false);
    }
}