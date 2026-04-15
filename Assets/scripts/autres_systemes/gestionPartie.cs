using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

public class gestionPartie : MonoBehaviour
{
    public static gestionPartie Instance;

    [Header("Parametres")]
    [SerializeField] private int nombreMaxSauvegardes;
    [SerializeField] private Camera cameraPrincipale;
    [SerializeField] private Camera cameraCapture;

    private string cheminDossierSauvegardes;

    [System.Serializable]
    public class DonneesSauvegarde
    {
        // Metadonnees
        public int indexSlot;
        public string nomSauvegarde;
        public string heure;
        public string date;
        public long poids;
        public string nomScene;
        public float tempsDeJeu;
        public string cheminCapture;

        // Progression
        public int chapitre;
        public float[] positionJoueur;
        public float[] rotationJoueur;

        // Corrosion
        public int niveauCorrosion;

        // Inventaire
        public List<string> inventaire;

        // Journal des indices
        public List<string> indices;

        // Enigmes
        public List<string> enigmesResolues;

        // Morceaux de tuyaux collectes
        public List<string> morceauxTuyaux;

        // Etats narratifs
        public bool personnageSecondaireKidnappe;
        public bool personnageSecondaireSauve;
        public bool personnageSecondaireContamine;
    }

    private DonneesSauvegarde partieEnCours;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        cheminDossierSauvegardes = Path.Combine(
            Application.persistentDataPath, "sauvegardes");

        if (!Directory.Exists(cheminDossierSauvegardes))
            Directory.CreateDirectory(cheminDossierSauvegardes);
    }

    public void InitialiserNouvellePartie()
    {
        partieEnCours = new DonneesSauvegarde
        {
            chapitre = 1,
            positionJoueur = new float[3],
            rotationJoueur = new float[3],
            niveauCorrosion = 0,
            inventaire = new List<string>(),
            indices = new List<string>(),
            enigmesResolues = new List<string>(),
            morceauxTuyaux = new List<string>(),
            personnageSecondaireKidnappe = false,
            personnageSecondaireSauve = false,
            personnageSecondaireContamine = false
        };
    }

    public DonneesSauvegarde ObtenirPartieEnCours()
    {
        return partieEnCours;
    }

    public void ChargerPartieEnCours(DonneesSauvegarde donnees)
    {
        partieEnCours = donnees;
    }

    // ===== MISE A JOUR PARTIE EN COURS =====

    public void MettreAJourPosition(Vector3 position, Vector3 rotation)
    {
        if (partieEnCours == null) return;

        partieEnCours.positionJoueur = new float[]
            { position.x, position.y, position.z };
        partieEnCours.rotationJoueur = new float[]
            { rotation.x, rotation.y, rotation.z };
    }

    public void MettreAJourChapitre(int chapitre)
    {
        if (partieEnCours == null) return;
        partieEnCours.chapitre = chapitre;
    }

    public void MettreAJourCorrosion(int niveau)
    {
        if (partieEnCours == null) return;
        partieEnCours.niveauCorrosion = niveau;
    }

    public void AjouterIndice(string indice)
    {
        if (partieEnCours == null) return;
        if (!partieEnCours.indices.Contains(indice))
            partieEnCours.indices.Add(indice);
    }

    public void AjouterObjetInventaire(string objet)
    {
        if (partieEnCours == null) return;
        if (!partieEnCours.inventaire.Contains(objet))
            partieEnCours.inventaire.Add(objet);
    }

    public void RetirerObjetInventaire(string objet)
    {
        if (partieEnCours == null) return;
        partieEnCours.inventaire.Remove(objet);
    }

    public void AjouterMorceauTuyau(string morceau)
    {
        if (partieEnCours == null) return;
        if (!partieEnCours.morceauxTuyaux.Contains(morceau))
            partieEnCours.morceauxTuyaux.Add(morceau);
    }

    public void MarquerEnigmeResolue(string enigme)
    {
        if (partieEnCours == null) return;
        if (!partieEnCours.enigmesResolues.Contains(enigme))
            partieEnCours.enigmesResolues.Add(enigme);
    }

    public void MarquerKidnapping()
    {
        if (partieEnCours == null) return;
        partieEnCours.personnageSecondaireKidnappe = true;
    }

    public void MarquerSauvetage()
    {
        if (partieEnCours == null) return;
        partieEnCours.personnageSecondaireSauve = true;
    }

    public void MarquerContamination()
    {
        if (partieEnCours == null) return;
        partieEnCours.personnageSecondaireContamine = true;
    }

    // ===== SAUVEGARDE / CHARGEMENT =====

    public bool SauvegardeExiste()
    {
        if (!Directory.Exists(cheminDossierSauvegardes))
            return false;

        string[] fichiers = Directory.GetFiles(
            cheminDossierSauvegardes, "sauvegarde_*.json");

        return fichiers.Length > 0;
    }

    public void Sauvegarder(int indexSlot = -1)
    {
        if (partieEnCours == null)
        {
            Debug.LogWarning("Aucune partie en cours a sauvegarder");
            return;
        }

        if (indexSlot < 0)
            indexSlot = TrouverProchainSlotLibre();

        GameObject joueur = GameObject.FindWithTag("Player");
        if (joueur != null)
        {
            MettreAJourPosition(
                joueur.transform.position,
                joueur.transform.eulerAngles);
        }

        string cheminCapture = Path.Combine(
            cheminDossierSauvegardes,
            "capture_" + indexSlot + ".png");

        CaptureScreenshot(cheminCapture);

        partieEnCours.indexSlot = indexSlot;
        partieEnCours.nomSauvegarde = "Sauvegarde " + (indexSlot + 1);
        partieEnCours.heure = DateTime.Now.ToString("HH:mm");
        partieEnCours.date = DateTime.Now.ToString("dd-MM-yyyy");
        partieEnCours.nomScene = UnityEngine.SceneManagement.SceneManager
            .GetActiveScene().name;
        partieEnCours.tempsDeJeu += Time.time;
        partieEnCours.cheminCapture = cheminCapture;

        string json = JsonUtility.ToJson(partieEnCours, true);
        string cheminFichier = Path.Combine(
            cheminDossierSauvegardes,
            "sauvegarde_" + indexSlot + ".json");

        File.WriteAllText(cheminFichier, json);

        FileInfo info = new FileInfo(cheminFichier);
        partieEnCours.poids = info.Length;
        json = JsonUtility.ToJson(partieEnCours, true);
        File.WriteAllText(cheminFichier, json);

        Debug.Log("Partie sauvegardee slot " + indexSlot
            + " | Chapitre " + partieEnCours.chapitre
            + " | Inventaire: " + partieEnCours.inventaire.Count + " objets"
            + " | Indices: " + partieEnCours.indices.Count);
    }

    public DonneesSauvegarde Charger(int indexSlot)
    {
        string cheminFichier = Path.Combine(
            cheminDossierSauvegardes,
            "sauvegarde_" + indexSlot + ".json");

        if (!File.Exists(cheminFichier))
            return null;

        string json = File.ReadAllText(cheminFichier);
        return JsonUtility.FromJson<DonneesSauvegarde>(json);
    }

    public void ChargerEtAppliquer(int indexSlot)
    {
        DonneesSauvegarde donnees = Charger(indexSlot);

        if (donnees == null)
        {
            Debug.LogWarning("Aucune sauvegarde au slot " + indexSlot);
            return;
        }

        partieEnCours = donnees;

        GameObject joueur = GameObject.FindWithTag("Player");
        if (joueur != null && donnees.positionJoueur != null
            && donnees.positionJoueur.Length == 3)
        {
            joueur.transform.position = new Vector3(
                donnees.positionJoueur[0],
                donnees.positionJoueur[1],
                donnees.positionJoueur[2]);

            joueur.transform.eulerAngles = new Vector3(
                donnees.rotationJoueur[0],
                donnees.rotationJoueur[1],
                donnees.rotationJoueur[2]);
        }

        Debug.Log("Partie chargee slot " + indexSlot
            + " | Chapitre " + donnees.chapitre);
    }

    public DonneesSauvegarde ChargerDerniereSauvegarde()
    {
        if (!Directory.Exists(cheminDossierSauvegardes))
            return null;

        string[] fichiers = Directory.GetFiles(
            cheminDossierSauvegardes, "sauvegarde_*.json");

        if (fichiers.Length == 0)
            return null;

        string fichierRecent = null;
        DateTime dateRecente = DateTime.MinValue;

        foreach (string fichier in fichiers)
        {
            DateTime dateModification = File.GetLastWriteTime(fichier);
            if (dateModification > dateRecente)
            {
                dateRecente = dateModification;
                fichierRecent = fichier;
            }
        }

        if (fichierRecent == null)
            return null;

        string json = File.ReadAllText(fichierRecent);
        return JsonUtility.FromJson<DonneesSauvegarde>(json);
    }

    public List<DonneesSauvegarde> ObtenirToutesSauvegardes()
    {
        List<DonneesSauvegarde> liste = new List<DonneesSauvegarde>();

        if (!Directory.Exists(cheminDossierSauvegardes))
            return liste;

        for (int i = 0; i < nombreMaxSauvegardes; i++)
        {
            string chemin = Path.Combine(
                cheminDossierSauvegardes,
                "sauvegarde_" + i + ".json");

            if (File.Exists(chemin))
            {
                string json = File.ReadAllText(chemin);
                DonneesSauvegarde donnees =
                    JsonUtility.FromJson<DonneesSauvegarde>(json);
                liste.Add(donnees);
            }
        }

        return liste;
    }

    public Texture2D ChargerCapture(string chemin)
    {
        if (string.IsNullOrEmpty(chemin) || !File.Exists(chemin))
            return null;

        byte[] octets = File.ReadAllBytes(chemin);
        Texture2D texture = new Texture2D(2, 2);
        texture.LoadImage(octets);
        return texture;
    }

    public void SupprimerSauvegarde(int indexSlot)
    {
        string cheminFichier = Path.Combine(
            cheminDossierSauvegardes,
            "sauvegarde_" + indexSlot + ".json");

        string cheminCapture = Path.Combine(
            cheminDossierSauvegardes,
            "capture_" + indexSlot + ".png");

        if (File.Exists(cheminFichier))
            File.Delete(cheminFichier);

        if (File.Exists(cheminCapture))
            File.Delete(cheminCapture);
    }

    public void SupprimerToutesSauvegardes()
    {
        if (!Directory.Exists(cheminDossierSauvegardes))
            return;

        string[] fichiers = Directory.GetFiles(cheminDossierSauvegardes);

        foreach (string fichier in fichiers)
            File.Delete(fichier);
    }

    private void CaptureScreenshot(string chemin)
    {
        // Utiliser camera_capture si disponible
        // Sinon fallback sur camera_principale
        Camera cam = cameraCapture;
        if (cam == null)
            cam = cameraPrincipale;
        if (cam == null)
            cam = Camera.main;

        if (cam == null)
        {
            Debug.LogWarning("Aucune camera pour la capture");
            return;
        }

        // Activer temporairement si desactivee
        bool etaitActive = cam.enabled;
        cam.enabled = true;

        int largeur = 384;
        int hauteur = 216;

        RenderTexture rt = new RenderTexture(largeur, hauteur, 24);
        cam.targetTexture = rt;
        cam.Render();

        RenderTexture.active = rt;
        Texture2D capture = new Texture2D(
            largeur, hauteur, TextureFormat.RGB24, false);
        capture.ReadPixels(new Rect(0, 0, largeur, hauteur), 0, 0);
        capture.Apply();

        cam.targetTexture = null;
        RenderTexture.active = null;

        // Remettre dans l'etat original
        cam.enabled = etaitActive;

        Destroy(rt);

        byte[] octets = capture.EncodeToPNG();
        Destroy(capture);

        File.WriteAllBytes(chemin, octets);
    }

    private int TrouverProchainSlotLibre()
    {
        for (int i = 0; i < nombreMaxSauvegardes; i++)
        {
            string chemin = Path.Combine(
                cheminDossierSauvegardes,
                "sauvegarde_" + i + ".json");

            if (!File.Exists(chemin))
                return i;
        }

        return 0;
    }
}