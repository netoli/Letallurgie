using UnityEngine;

public class gestionOngletsOptions : MonoBehaviour
{
    [Header("Contenus")]
    [SerializeField] private GameObject contenuSauvegarde;
    [SerializeField] private GameObject contenuControle;
    [SerializeField] private GameObject contenuAudio;
    [SerializeField] private GameObject contenuGraphique;
    [SerializeField] private GameObject contenuAccessibilite;

    private GameObject contenuActif;
    private string nomOngletActif = "sauvegarde";

    void OnEnable()
    {
        AfficherContenu(contenuSauvegarde, "sauvegarde");
    }

    public void OnSauvegarde()
    {
        AfficherContenu(contenuSauvegarde, "sauvegarde");
    }

    public void OnControle()
    {
        AfficherContenu(contenuControle, "controle");
    }

    public void OnAudio()
    {
        AfficherContenu(contenuAudio, "audio");
    }

    public void OnGraphique()
    {
        AfficherContenu(contenuGraphique, "graphique");
    }

    public void OnAccessibilite()
    {
        AfficherContenu(contenuAccessibilite, "accessibilite");
    }

    public string ObtenirOngletActif()
    {
        return nomOngletActif;
    }

    private void AfficherContenu(GameObject contenu, string nom)
    {
        if (contenuActif != null)
            contenuActif.SetActive(false);

        contenu.SetActive(true);
        contenuActif = contenu;
        nomOngletActif = nom;
    }
}