using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class gestionChapitres : MonoBehaviour
{
    public static gestionChapitres Instance { get; private set; }

    [Header("References")]
    [SerializeField] private gestionBanniere gestionBanniere;
    [SerializeField] private gestionTutoriel gestionTutoriel;

    [Header("Chapitres disponibles")]
    [SerializeField] private DonneesChapitre[] chapitres;

    private DonneesChapitre chapitreActuel;
    private DonneesTutoriel tutoActuel;
    private HashSet<string> tutosVus = new HashSet<string>();

    private const string CLE_PLAYERPREFS = "tutosVus_";

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        Debug.Log("[Chapitre] Awake - Instance initialisee");

        // TEMPORAIRE DEV : efface les tutos vus a chaque lancement
        PlayerPrefs.DeleteKey("tutosVus_");
        PlayerPrefs.Save();

        ChargerTutosVus();
    }

    public void DemarrerChapitre(string idChapitre)
    {
        DonneesChapitre chapitre = TrouverChapitre(idChapitre);
        if (chapitre == null)
        {
            Debug.LogWarning("Chapitre introuvable: " + idChapitre);
            return;
        }

        chapitreActuel = chapitre;
        StartCoroutine(SequenceDemarrageChapitre(chapitre));
    }

    private IEnumerator SequenceDemarrageChapitre(
    DonneesChapitre chapitre)
    {
        Debug.Log("[Chapitre] Demarrage: " + chapitre.idChapitre);

        yield return new WaitForSecondsRealtime(
            chapitre.delaiApparitionBanniere);

        Debug.Log("[Chapitre] Lancement banniere");

        yield return StartCoroutine(
            gestionBanniere.AfficherBanniere(
                chapitre.nomAffiche,
                chapitre.dureeAffichageBanniere));

        Debug.Log("[Chapitre] Banniere terminee");

        yield return new WaitForSecondsRealtime(
            chapitre.delaiAvantPremierTuto);

        Debug.Log("[Chapitre] Nb tutoriels: "
            + chapitre.tutoriels.Length);

        if (chapitre.tutoriels.Length > 0)
        {
            DonneesTutoriel premier = chapitre.tutoriels[0];
            Debug.Log("[Chapitre] Premier tuto: "
                + premier.idDeclencheur
                + " | Deja vu: "
                + tutosVus.Contains(premier.idDeclencheur));

            if (!tutosVus.Contains(premier.idDeclencheur))
            {
                Debug.Log("[Chapitre] AfficherTuto appele");
                AfficherTuto(premier);
            }
        }
    }

    public void DeclencherTuto(string idDeclencheur)
    {
        if (chapitreActuel == null) return;

        DonneesTutoriel tuto = TrouverTutoDansChapitre(
            chapitreActuel, idDeclencheur);

        if (tuto == null)
        {
            Debug.LogWarning(
                "Tuto introuvable dans le chapitre actuel: "
                + idDeclencheur);
            return;
        }

        if (tutosVus.Contains(idDeclencheur)) return;

        AfficherTuto(tuto);
    }

    public void SignalerAction(string idAction)
    {
        if (tutoActuel == null) return;
        if (string.IsNullOrEmpty(tutoActuel.idActionRequise)) return;

        if (tutoActuel.idActionRequise == idAction)
            FermerTutoActuel(true);
    }

    public void FermerTutoParEsc()
    {
        if (tutoActuel == null) return;
        FermerTutoActuel(true);
    }

    public bool TutoEstAffiche()
    {
        return tutoActuel != null;
    }

    private void AfficherTuto(DonneesTutoriel tuto)
    {
        tutoActuel = tuto;
        gestionTutoriel.AfficherTuto(tuto);
    }

    private void FermerTutoActuel(bool marquerVu)
    {
        if (tutoActuel == null) return;

        if (marquerVu)
        {
            tutosVus.Add(tutoActuel.idDeclencheur);
            SauvegarderTutosVus();
        }

        gestionTutoriel.FermerTuto();
        tutoActuel = null;
    }

    private DonneesChapitre TrouverChapitre(string id)
    {
        foreach (DonneesChapitre c in chapitres)
        {
            if (c.idChapitre == id) return c;
        }
        return null;
    }

    private DonneesTutoriel TrouverTutoDansChapitre(
        DonneesChapitre chapitre, string idDeclencheur)
    {
        foreach (DonneesTutoriel t in chapitre.tutoriels)
        {
            if (t.idDeclencheur == idDeclencheur) return t;
        }
        return null;
    }

    private void ChargerTutosVus()
    {
        tutosVus.Clear();
        string joined = PlayerPrefs.GetString(CLE_PLAYERPREFS, "");
        if (string.IsNullOrEmpty(joined)) return;

        string[] ids = joined.Split('|');
        foreach (string id in ids)
        {
            if (!string.IsNullOrEmpty(id)) tutosVus.Add(id);
        }
    }

    private void SauvegarderTutosVus()
    {
        string[] array = new string[tutosVus.Count];
        tutosVus.CopyTo(array);
        PlayerPrefs.SetString(CLE_PLAYERPREFS,
            string.Join("|", array));
        PlayerPrefs.Save();
    }

    public void ReinitialiserTutosVus()
    {
        tutosVus.Clear();
        PlayerPrefs.DeleteKey(CLE_PLAYERPREFS);
        PlayerPrefs.Save();
    }
}