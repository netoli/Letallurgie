using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class gestionTuileSauvegarde : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private GameObject prefabContenusSauvegarde;

    [Header("Tuiles")]
    [SerializeField] private List<TuileSauvegarde> tuiles;

    [System.Serializable]
    public class TuileSauvegarde
    {
        public Transform ensembleImageEtDonnees;
        public GameObject boutonCharger;
        public GameObject boutonSauvegarder;
        [HideInInspector] public int indexSlot;
        [HideInInspector] public bool contientSauvegarde;
        [HideInInspector] public GameObject contenuInstancie;
    }

    private int tuileSelectionnee = -1;

    void OnEnable()
    {
        StartCoroutine(RafraichirAvecDelai());
    }

    private System.Collections.IEnumerator RafraichirAvecDelai()
    {
        yield return null;
        if (gestionPartie.Instance != null)
            RafraichirTuiles();
    }

    public void RafraichirTuiles()
    {
        List<gestionPartie.DonneesSauvegarde> sauvegardes =
            gestionPartie.Instance.ObtenirToutesSauvegardes();

        tuileSelectionnee = -1;

        for (int i = 0; i < tuiles.Count; i++)
        {
            TuileSauvegarde tuile = tuiles[i];
            if (tuile == null || tuile.ensembleImageEtDonnees == null)
                continue;

            tuile.indexSlot = i;

            // Reset couleur via gestionEffetsBoutonsCliques
            gestionEffetsBoutonsCliques effet =
                tuile.ensembleImageEtDonnees
                    .GetComponent<gestionEffetsBoutonsCliques>();
            if (effet != null)
                effet.AppliquerCouleurNormale();

            if (tuile.contenuInstancie != null)
            {
                Destroy(tuile.contenuInstancie);
                tuile.contenuInstancie = null;
            }

            gestionPartie.DonneesSauvegarde donnees = null;
            foreach (var s in sauvegardes)
            {
                if (s.indexSlot == i)
                {
                    donnees = s;
                    break;
                }
            }

            if (donnees != null)
            {
                tuile.contientSauvegarde = true;

                GameObject contenu = Instantiate(
                    prefabContenusSauvegarde,
                    tuile.ensembleImageEtDonnees);
                tuile.contenuInstancie = contenu;

                RemplirContenu(contenu, donnees);
            }
            else
            {
                tuile.contientSauvegarde = false;
            }

            if (tuile.boutonCharger != null)
                tuile.boutonCharger.SetActive(false);
            if (tuile.boutonSauvegarder != null)
                tuile.boutonSauvegarder.SetActive(false);
        }
    }

    public void SelectionnerTuile(int index)
    {
        if (index < 0 || index >= tuiles.Count) return;

        // Désélectionne l'ancienne tuile
        if (tuileSelectionnee >= 0
            && tuileSelectionnee < tuiles.Count)
        {
            TuileSauvegarde ancienne = tuiles[tuileSelectionnee];

            if (ancienne.boutonCharger != null)
                ancienne.boutonCharger.SetActive(false);
            if (ancienne.boutonSauvegarder != null)
                ancienne.boutonSauvegarder.SetActive(false);

            gestionEffetsBoutonsCliques effetAncien =
                ancienne.ensembleImageEtDonnees
                    .GetComponent<gestionEffetsBoutonsCliques>();
            if (effetAncien != null)
                effetAncien.AppliquerCouleurNormale();
        }

        tuileSelectionnee = index;
        TuileSauvegarde tuile = tuiles[index];

        // Sélectionne la nouvelle tuile
        gestionEffetsBoutonsCliques effetNouvel =
            tuile.ensembleImageEtDonnees
                .GetComponent<gestionEffetsBoutonsCliques>();
        if (effetNouvel != null)
            effetNouvel.AppliquerCouleurSelectionne();

        if (tuile.contientSauvegarde)
        {
            if (tuile.boutonCharger != null)
            {
                tuile.boutonCharger.SetActive(true);

                int slot = index;
                Button btnCharger = tuile.boutonCharger
                    .GetComponent<Button>();
                btnCharger.onClick.RemoveAllListeners();
                btnCharger.onClick.AddListener(() =>
                {
                    gestionPartie.Instance.ChargerEtAppliquer(slot);

                    gestionInputsJeu inputs =
                        FindFirstObjectByType<gestionInputsJeu>();
                    if (inputs != null)
                    {
                        gestionsTransitions transitions =
                            inputs.GetComponent<gestionsTransitions>();
                        if (transitions != null)
                            transitions.RetourEnJeuDepuisChargement();

                        inputs.ActiverInputs();
                    }
                });
            }

            if (tuile.boutonSauvegarder != null)
            {
                tuile.boutonSauvegarder.SetActive(true);

                int slot = index;
                Button btnSauvegarder = tuile.boutonSauvegarder
                    .GetComponent<Button>();
                btnSauvegarder.onClick.RemoveAllListeners();
                btnSauvegarder.onClick.AddListener(() =>
                {
                    gestionPartie.Instance.Sauvegarder(slot);
                    RafraichirTuiles();
                });
            }
        }
        else
        {
            if (tuile.boutonCharger != null)
                tuile.boutonCharger.SetActive(false);

            if (tuile.boutonSauvegarder != null)
            {
                tuile.boutonSauvegarder.SetActive(true);

                int slot = index;
                Button btnSauvegarder = tuile.boutonSauvegarder
                    .GetComponent<Button>();
                btnSauvegarder.onClick.RemoveAllListeners();
                btnSauvegarder.onClick.AddListener(() =>
                {
                    gestionPartie.Instance.Sauvegarder(slot);
                    RafraichirTuiles();
                });
            }
        }
    }

    private void RemplirContenu(
        GameObject contenu,
        gestionPartie.DonneesSauvegarde donnees)
    {
        Transform contenus = contenu.transform
            .Find("contenus_sauvegarde");

        if (contenus == null)
            contenus = contenu.transform;

        RawImage imageUI = contenus
            .Find("image_capturee_dans_jeu")
            ?.GetComponent<RawImage>();

        if (imageUI != null)
        {
            Texture2D capture = gestionPartie.Instance
                .ChargerCapture(donnees.cheminCapture);

            if (capture != null)
                imageUI.texture = capture;
        }

        Transform donnesSur = contenus
            .Find("donnees_sur_sauvegarde");

        if (donnesSur != null)
        {
            TMP_Text titre = donnesSur.Find("titre_sauvegarde")
                ?.GetComponent<TMP_Text>();
            if (titre != null)
                titre.text = donnees.nomSauvegarde;

            TMP_Text nomScene = donnesSur.Find("nom_scene")
                ?.GetComponent<TMP_Text>();
            if (nomScene != null)
                nomScene.text = donnees.nomScene;
        }

        Transform ensembleDonnees = contenus
            .Find("donnees_sur_sauvegarde/ensemble_donnees_sauvegarde");

        if (ensembleDonnees == null)
            ensembleDonnees = contenus
                .Find("ensemble_donnees_sauvegarde");

        if (ensembleDonnees != null)
        {
            TMP_Text dateTexte = RechercheProfonde(
                ensembleDonnees, "date");
            if (dateTexte != null)
                dateTexte.text = donnees.date;

            TMP_Text heureTexte = RechercheProfonde(
                ensembleDonnees, "nombre_heure");
            if (heureTexte != null)
                heureTexte.text = donnees.heure;

            TMP_Text tempsTexte = RechercheProfonde(
                ensembleDonnees, "temps_ecoule");
            if (tempsTexte != null)
                tempsTexte.text = FormaterTemps(donnees.tempsDeJeu);

            TMP_Text poidsTexte = RechercheProfonde(
                ensembleDonnees, "poid_du_sauvegarde");
            if (poidsTexte != null)
                poidsTexte.text = FormaterTaille(donnees.poids);
        }
    }

    private TMP_Text RechercheProfonde(Transform parent, string nom)
    {
        foreach (Transform enfant in parent)
        {
            if (enfant.name == nom)
                return enfant.GetComponent<TMP_Text>();

            TMP_Text resultat = RechercheProfonde(enfant, nom);
            if (resultat != null)
                return resultat;
        }
        return null;
    }

    private string FormaterTaille(long octets)
    {
        if (octets < 1024)
            return octets + " o";
        if (octets < 1024 * 1024)
            return (octets / 1024f).ToString("F1") + " Ko";

        return (octets / (1024f * 1024f)).ToString("F1") + " Mo";
    }

    private string FormaterTemps(float secondes)
    {
        int heures = (int)(secondes / 3600);
        int minutes = (int)((secondes % 3600) / 60);
        int secs = (int)(secondes % 60);

        if (heures > 0)
            return heures + "h " + minutes.ToString("D2")
                + "m " + secs.ToString("D2") + "s";

        return minutes + "m " + secs.ToString("D2") + "s";
    }
}