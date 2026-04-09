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

    void OnEnable()
    {
        AfficherContenu(contenuSauvegarde);
    }

    public void OnSauvegarde()
    {
        AfficherContenu(contenuSauvegarde);
    }

    public void OnControle()
    {
        AfficherContenu(contenuControle);
    }

    public void OnAudio()
    {
        AfficherContenu(contenuAudio);
    }

    public void OnGraphique()
    {
        AfficherContenu(contenuGraphique);
    }

    public void OnAccessibilite()
    {
        AfficherContenu(contenuAccessibilite);
    }

    private void AfficherContenu(GameObject contenu)
    {
        if (contenuActif != null)
            contenuActif.SetActive(false);

        contenu.SetActive(true);
        contenuActif = contenu;
    }
}