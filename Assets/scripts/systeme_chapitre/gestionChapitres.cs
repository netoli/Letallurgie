// ============================================================
// gestionChapitres.cs
// ------------------------------------------------------------
// Auteur      : Olivier Vernet
// Date créé   : 
// Dernière modification : 25/04/2026 - Fanny Fortier
// ------------------------------------------------------------
// Description :
//   Gestion centralisée des chapitres et tutoriels.
//   Adaptation : prise en charge de deux comportements distincts
//   pour la progression UI :
//     - idActionRequise == "jdb_ouvert"  => valider quand le journal a été ouvert ET fermé au moins une fois.
//     - idActionRequise == "utiliser_objet" => (remplacer par drag and drop) valider quand l'inventaire a été ouvert ET fermé au moins une fois
// ------------------------------------------------------------
// Dépendances :
//   - gestionTutoriel, gestionBanniere, DonneesChapitre, DonneesTutoriel
//   - gestionInventaire / UI doivent appeler NotifierInventaireOuvert/Ferme et NotifierJournalOuvert/Ferme
// ============================================================

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

    // Booléens pour suivre les actions UI (ouverture/fermeture)
    private bool inventaireOuvertAuMoinsUneFois = false;
    private bool inventaireFermeAuMoinsUneFois = false;
    private bool journalOuvertAuMoinsUneFois = false;
    private bool journalFermeAuMoinsUneFois = false;

    // Flag pour signaler qu'une action "utiliser_objet" a été effectuée.
    private bool objetUtiliseSignale = false;

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

    private IEnumerator SequenceDemarrageChapitre(DonneesChapitre chapitre)
    {
        Debug.Log("[Chapitre] Demarrage: " + chapitre.idChapitre);

        yield return new WaitForSecondsRealtime(chapitre.delaiApparitionBanniere);

        Debug.Log("[Chapitre] Lancement banniere");

        yield return StartCoroutine(gestionBanniere.AfficherBanniere(
            chapitre.nomAffiche,
            chapitre.dureeAffichageBanniere));

        Debug.Log("[Chapitre] Banniere terminee");

        yield return new WaitForSecondsRealtime(chapitre.delaiAvantPremierTuto);

        Debug.Log("[Chapitre] Nb tutoriels: " + chapitre.tutoriels.Length);

        if (chapitre.tutoriels.Length > 0)
        {
            DonneesTutoriel premier = chapitre.tutoriels[0];
            Debug.Log("[Chapitre] Premier tuto: " + premier.idDeclencheur + " | Deja vu: " + tutosVus.Contains(premier.idDeclencheur));

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

        DonneesTutoriel tuto = TrouverTutoDansChapitre(chapitreActuel, idDeclencheur);

        if (tuto == null)
        {
            Debug.LogWarning("Tuto introuvable dans le chapitre actuel: " + idDeclencheur);
            return;
        }

        if (tutosVus.Contains(idDeclencheur)) return;

        AfficherTuto(tuto);
    }


    // Méthode appelée par les objets du Tuto pour signaler qu'une action a été effectuée
    public void SignalerAction(string idAction)
    {
        if (tutoActuel == null) return;
        if (string.IsNullOrEmpty(idAction)) return;

        // si l'action correspond à l'attente du tuto, fermer le tuto.
        if (!string.IsNullOrEmpty(tutoActuel.idActionRequise) && tutoActuel.idActionRequise == idAction)
        {
            FermerTutoActuel(true);
        }

        // Désactiver l'interaction DialogueTuto après l'avoir signalée 1 fois
        var dialogues = FindObjectsOfType<DialogueTuto>(true);
        foreach (var d in dialogues)
        {
            if (d.IdAction == idAction)
            {
                d.DesactiverInteraction(); // utilise le flag interactionActive existant
                                           // Désactiver le highlight
                var highlight = d.GetComponentInParent<gestionHighlightHover>();
                if (highlight != null) highlight.Highlighter(false);
            }
        }

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

        // Activation du detecteur correspondant : on désactive tous les detecteurs, puis on
        // active celui qui correspond à l'idActionRequise (s'il existe).
        var tous = FindObjectsOfType<detecteurTuto>(true);
        foreach (var d in tous) d.gameObject.SetActive(false);

        if (!string.IsNullOrEmpty(tuto.idActionRequise))
        {
            foreach (var d in tous)
            {
                if (d.IdAction == tuto.idActionRequise)
                {
                    d.gameObject.SetActive(true);
                    Debug.Log($"[Chapitre] Detecteur activé pour idActionRequise={tuto.idActionRequise} (obj={d.name})");
                    break;
                }
            }
        }
        else
        {
            Debug.Log("[Chapitre] Aucun idActionRequise pour ce tuto — pas de detecteur activé");
        }
    }

    private void FermerTutoActuel(bool marquerVu)
    {
        if (tutoActuel == null) return;

        if (marquerVu)
        {
            tutosVus.Add(tutoActuel.idDeclencheur);
            SauvegarderTutosVus();
        }

        // Récupérer le id de l'action complétée
        string actionFerme = tutoActuel.idActionRequise;

        gestionTutoriel.FermerTuto();
        tutoActuel = null;

        // S'il reste un tuto a afficher, l'afficher
        if (chapitreActuel == null) return;

        // Trouver le prochain tuto non vu dans le chapitre
        DonneesTutoriel prochain = null;
        foreach (var t in chapitreActuel.tutoriels)
        {
            if (!tutosVus.Contains(t.idDeclencheur))
            {
                prochain = t;
                break;
            }
        }

        // Délai pour laisser du temps au fade out
        if (prochain != null)
        {
            Debug.Log($"[Chapitre] Passage à la prochaine étape: {prochain.idDeclencheur}");
            // Petite attente pour laisser le fade out se faire
            StartCoroutine(AfficherProchainTutoApresDelai(prochain, 0.25f));
        }
        else
        {
            Debug.Log("[Chapitre] Aucune étape suivante non vue dans ce chapitre.");
        }
    }

    private DonneesChapitre TrouverChapitre(string id)
    {
        foreach (DonneesChapitre c in chapitres)
        {
            if (c.idChapitre == id) return c;
        }
        return null;
    }

    private DonneesTutoriel TrouverTutoDansChapitre(DonneesChapitre chapitre, string idDeclencheur)
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
        PlayerPrefs.SetString(CLE_PLAYERPREFS, string.Join("|", array));
        PlayerPrefs.Save();
    }

    public void ReinitialiserTutosVus()
    {
        tutosVus.Clear();
        PlayerPrefs.DeleteKey(CLE_PLAYERPREFS);
        PlayerPrefs.Save();
    }

    // Affiche le prochain tuto après un délai
    private System.Collections.IEnumerator AfficherProchainTutoApresDelai(DonneesTutoriel tuto, float delai)
    {
        yield return new WaitForSecondsRealtime(delai);
        AfficherTuto(tuto);
    }



    
}
