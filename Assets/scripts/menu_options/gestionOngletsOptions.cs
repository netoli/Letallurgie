using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

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

    void Update()
    {
        if (boutonActif != null)
            EventSystem.current.SetSelectedGameObject(
                boutonActif.gameObject);
    }

    void Start()
    {
        AfficherContenu(contenuSauvegarde, "sauvegarde",
            boutonSauvegarde);
    }

    void OnEnable()
    {
        AfficherContenu(contenuSauvegarde, "sauvegarde",
            boutonSauvegarde);
    }

    void OnDisable()
    {
        EventSystem.current.SetSelectedGameObject(null);
        ResetTousLesBoutons();
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

        // Désélectionne l'ancien onglet
        if (boutonActif != null && boutonActif != bouton)
        {
            gestionEffetsBoutonsCliques effetAncien =
                boutonActif.GetComponent<gestionEffetsBoutonsCliques>();
            if (effetAncien != null)
                effetAncien.AppliquerCouleurNormale();
        }

        boutonActif = bouton;

        // Sélectionne le nouvel onglet
        EventSystem.current.SetSelectedGameObject(bouton.gameObject);

        MettreAJourCouleursTexte(bouton);
    }

    private void ResetTousLesBoutons()
    {
        ResetBouton(boutonSauvegarde);
        ResetBouton(boutonControle);
        ResetBouton(boutonAudio);
        ResetBouton(boutonGraphique);
        ResetBouton(boutonAccessibilite);
    }

    private void ResetBouton(Button bouton)
    {
        if (bouton == null) return;
        bouton.interactable = true;
        gestionEffetsBoutonsCliques effet =
            bouton.GetComponent<gestionEffetsBoutonsCliques>();
        if (effet != null)
            effet.AppliquerCouleurNormale();
    }

    private void MettreAJourCouleursTexte(Button boutonSelectionne)
    {
        gestionEffetsBoutonsCliques effet =
            boutonSelectionne
                .GetComponent<gestionEffetsBoutonsCliques>();
        if (effet != null)
            effet.AppliquerCouleurSelectionne();
    }
}