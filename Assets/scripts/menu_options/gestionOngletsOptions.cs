using UnityEngine;
using UnityEngine.UI;

public class gestionOngletsOptions : MonoBehaviour
{
    [Header("Contenus")]
    [SerializeField] private GameObject contenuSauvegarde;
    [SerializeField] private GameObject contenuControle;
    [SerializeField] private GameObject contenuAudio;
    [SerializeField] private GameObject contenuGraphique;
    [SerializeField] private GameObject contenuAccessibilite;

    [Header("Boutons Onglets")]
    [SerializeField] private Button boutonSauvegarde;
    [SerializeField] private Button boutonControle;
    [SerializeField] private Button boutonAudio;
    [SerializeField] private Button boutonGraphique;
    [SerializeField] private Button boutonAccessibilite;

    private GameObject contenuActif;
    private string nomOngletActif = "sauvegarde";
    private Button boutonActif;

    void Start()
    {
        AfficherContenu(contenuSauvegarde, "sauvegarde",
            boutonSauvegarde);
    }

    void OnEnable()
    {
        if (boutonActif != null)
            AfficherContenu(contenuSauvegarde, "sauvegarde",
                boutonSauvegarde);
    }

    public void OnSauvegarde()
    {
        AfficherContenu(contenuSauvegarde, "sauvegarde",
            boutonSauvegarde);
    }

    public void OnControle()
    {
        AfficherContenu(contenuControle, "controle",
            boutonControle);
    }

    public void OnAudio()
    {
        AfficherContenu(contenuAudio, "audio",
            boutonAudio);
    }

    public void OnGraphique()
    {
        AfficherContenu(contenuGraphique, "graphique",
            boutonGraphique);
    }

    public void OnAccessibilite()
    {
        AfficherContenu(contenuAccessibilite, "accessibilite",
            boutonAccessibilite);
    }

    public string ObtenirOngletActif()
    {
        return nomOngletActif;
    }

    private void AfficherContenu(GameObject contenu, string nom,
        Button bouton)
    {
        if (contenuActif != null)
            contenuActif.SetActive(false);

        contenu.SetActive(true);
        contenuActif = contenu;
        nomOngletActif = nom;

        DesactiverTousLesBoutons();
        bouton.interactable = false;
        boutonActif = bouton;

        MettreAJourCouleursTexte(bouton);
    }

    private void DesactiverTousLesBoutons()
    {
        boutonSauvegarde.interactable = true;
        boutonControle.interactable = true;
        boutonAudio.interactable = true;
        boutonGraphique.interactable = true;
        boutonAccessibilite.interactable = true;
    }

    private void MettreAJourCouleursTexte(Button boutonSelectionne)
    {
        ResetCouleurTexte(boutonSauvegarde);
        ResetCouleurTexte(boutonControle);
        ResetCouleurTexte(boutonAudio);
        ResetCouleurTexte(boutonGraphique);
        ResetCouleurTexte(boutonAccessibilite);

        gestionEffetsBoutonsCliques effet =
            boutonSelectionne
                .GetComponent<gestionEffetsBoutonsCliques>();
        if (effet != null)
            effet.AppliquerCouleurSelectionne();
    }

    private void ResetCouleurTexte(Button bouton)
    {
        if (bouton == null) return;

        gestionEffetsBoutonsCliques effet =
            bouton.GetComponent<gestionEffetsBoutonsCliques>();
        if (effet != null)
            effet.AppliquerCouleurNormale();
    }
}