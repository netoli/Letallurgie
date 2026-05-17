// ============================================================
// gestionChapitres.cs
// ------------------------------------------------------------
// Auteur      : Olivier Vernet
// Date cr��   : 
// Derni�re modification : 28/04/2026 - Fanny Fortier
// ------------------------------------------------------------
// Description :
//   Gestion centralis�e des chapitres et tutoriels.
//   Adaptation : prise en charge de deux comportements distincts
//   pour la progression UI :
//     - idActionRequise == "jdb_ouvert"  => valider quand le journal a �t� ouvert ET ferm� au moins une fois.
//     - idActionRequise == "utiliser_objet" => (remplacer par drag and drop) valider quand l'inventaire a �t� ouvert ET ferm� au moins une fois
// ------------------------------------------------------------
// D�pendances :
//   - gestionTutoriel, gestionBanniere, DonneesChapitre, DonneesTutoriel
//   - gestionInventaire / UI doivent appeler NotifierInventaireOuvert/Ferme et NotifierJournalOuvert/Ferme
// ============================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class gestionChapitres : MonoBehaviour
{
    public static gestionChapitres Instance { get; private set; }

    // Vrai par defaut pour les scenes hors-tuto. Mis a false dans
    // DemarrerChapitre (SCENE0) et remis a true quand la premiere
    // tuile de tuto apparait. Lu par PlayerMovement pour bloquer
    // le deplacement WASD pendant la banniere/intro.
    public bool MouvementAutorise { get; private set; } = true;

    // idActionRequise de la tuile actuellement affichee, ou chaine vide
    // si aucune tuile n'est en cours. Utilise par objetRamassable et
    // autres scripts d'interaction pour bloquer une action prematuree
    // qui ne correspond pas a l'etape en cours du tuto.
    public string IdActionAttenduActuelle =>
        tutoActuel != null ? tutoActuel.idActionRequise : "";

    // Event broadcast a chaque appel a SignalerAction, MEME si aucune
    // tuile n'est active. Permet a des composants externes (ex:
    // declencheurAction) de reagir a des actions tutoriel sans avoir
    // a passer par une tuile DonneesTutoriel. Utile pour chainer des
    // sequences narratives (audio PNJ, activation de pointeurs, etc.)
    // qui se produisent ENTRE deux tuiles.
    public event System.Action<string> OnActionSignalee;

    [Header("References")]
    [SerializeField] private gestionBanniere gestionBanniere;
    [SerializeField] private gestionTutoriel gestionTutoriel;
    [SerializeField] private VideoPlayer playerCinematiques;

    [Header("Chapitres disponibles")]
    [SerializeField] private DonneesChapitre[] chapitres;

    private DonneesChapitre chapitreActuel;
    private DonneesTutoriel tutoActuel;
    private HashSet<string> tutosVus = new HashSet<string>();

    [Header("Cin�matiques")]
    [SerializeField] private VideoClip[] cinematique;

    [Header("HUD post-tutoriel")]
    [Tooltip("GameObject des indices de jouabilite (canvas_hud > " +
        "contenu_hud > indices_jouabilite). Reste desactive pendant " +
        "tout le tutoriel et s'active automatiquement quand la " +
        "cinematique de fin du dernier chapitre se termine.")]
    [SerializeField] private GameObject indicesJouabilite;

    // Bool�ens pour suivre les actions UI (ouverture/fermeture)
    private bool inventaireOuvertAuMoinsUneFois = false;
    private bool inventaireFermeAuMoinsUneFois = false;
    private bool journalOuvertAuMoinsUneFois = false;
    private bool journalFermeAuMoinsUneFois = false;

    // Flag pour signaler qu'une action "utiliser_objet" a �t� effectu�e.
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

        // Bloquer le deplacement WASD tant que la premiere tuile
        // de tuto n'est pas affichee (uniquement dans la scene tuto).
        // Le regard a la souris reste autorise.
        if (SceneManager.GetActiveScene().name == "SCENE0-Menu-Tuto")
            MouvementAutorise = false;

        StartCoroutine(SequenceDemarrageChapitre(chapitre));
    }

    private IEnumerator SequenceDemarrageChapitre(DonneesChapitre chapitre)
    {

        // Ne pas afficher de tutoriels si on n'est pas dans la sc�ne du menu
        if (SceneManager.GetActiveScene().name != "SCENE0-Menu-Tuto")
            yield break;

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


    // Methode appelee par les objets du Tuto pour signaler qu'une
    // action a ete effectuee (detecteurTuto qui declenche, DialogueTuto
    // qui termine son etape, objet ramasse, etc.).
    public void SignalerAction(string idAction)
    {
        if (string.IsNullOrEmpty(idAction)) return;

        // Notifier les abonnes (declencheurAction etc.) AVANT de
        // traiter la tuile. Comme ca l'event passe meme si aucune
        // tuile n'est active (cas des etapes silencieuses : audio
        // PNJ, activation de pointeur, etc.).
        OnActionSignalee?.Invoke(idAction);

        if (tutoActuel == null) return;

        // Si l'action correspond a l'attente de la tuile actuelle,
        // la fermer (ce qui declenchera AfficherTuto sur la suivante,
        // qui verrouillera tous les PNJ et activera le bon).
        if (!string.IsNullOrEmpty(tutoActuel.idActionRequise)
            && tutoActuel.idActionRequise == idAction)
        {
            FermerTutoActuel(true);
        }

        // NOTE : on NE desactive PLUS d'office les DialogueTuto ici.
        // Chaque DialogueTuto gere lui-meme son interactionActive
        // dans EtapeTerminee(). C'est AfficherTuto qui pilote le
        // verrouillage / deverrouillage selon l'etape attendue.
    }

    public void FermerTutoParEsc()
    {
        if (tutoActuel == null) return;
        // ESC sur une tuile : cache juste l'UI sans faire avancer
        // dans la sequence du chapitre. Le detecteur (active par
        // AfficherTuto) reste actif, et le joueur doit toujours
        // executer l'action attendue pour passer a la tuile suivante.
        MasquerUITutoSansAvancer();
    }

    /// <summary>
    /// Cache l'UI de la tuile actuelle SANS la marquer comme vue
    /// et SANS passer a la tuile suivante. L'etat logique (tutoActuel,
    /// detecteur actif, idActionRequise) reste inchange. Utilise par
    /// ESC et par l'auto-close de la tuile "Passer un dialogue" pour
    /// que le joueur n'ait pas besoin de la voir, mais doive quand
    /// meme executer l'action pour avancer.
    /// </summary>
    private void MasquerUITutoSansAvancer()
    {
        if (tutoActuel == null) return;
        if (gestionTutoriel != null)
        {
            gestionTutoriel.FermerTuto();
        }
        Debug.Log($"[Chapitre] UI tuile masquee : " +
            $"'{tutoActuel.idDeclencheur}'. Le detecteur reste actif. " +
            $"Action attendue: '{tutoActuel.idActionRequise}'.");
    }

    /// <summary>
    /// Ferme automatiquement la tuile actuelle apres un delai donne.
    /// Utilise notamment par DialogueTuto : quand le joueur fait son
    /// premier ESC pour passer une ligne, la tuile "ESC pour passer
    /// un dialogue" disparait apres 3s (le joueur a compris le geste,
    /// l'info devient inutile).
    /// Si une autre tuile s'affiche entre-temps, le close est annule.
    /// </summary>
    public void FermerTuileActuelleApresDelai(float delai)
    {
        if (tutoActuel == null) return;
        StartCoroutine(FermerTuileActuelleCoroutine(tutoActuel, delai));
    }

    private IEnumerator FermerTuileActuelleCoroutine(
        DonneesTutoriel tuileCible, float delai)
    {
        yield return new WaitForSecondsRealtime(delai);

        // Securite : on agit uniquement si la tuile est toujours
        // la meme (le joueur peut avoir avance d'une etape entre-temps)
        if (tutoActuel != tuileCible) yield break;

        Debug.Log($"[Chapitre] Auto-masquage UI de " +
            $"'{tuileCible.idDeclencheur}' apres delai de {delai}s.");
        // On cache juste l'UI sans avancer dans la sequence : le
        // joueur doit toujours signaler l'action attendue (ex:
        // 'demande_aide_faite' pour la tuile "Passer un dialogue"
        // qui sera signalee par la derniere replique R7). Le
        // detecteur de la tuile reste actif.
        MasquerUITutoSansAvancer();
    }

    public bool TutoEstAffiche()
    {
        return tutoActuel != null;
    }

    /// <summary>
    /// Active un GameObject et remonte la hierarchie pour activer
    /// tous ses ancetres inactifs. Utilise pour les detecteurs et
    /// snap points dont le parent peut etre desactive par defaut
    /// (pour qu'ils n'apparaissent qu'au bon moment du tutoriel).
    /// </summary>
    private void ActiverAvecAncetres(GameObject go)
    {
        if (go == null) return;
        Transform t = go.transform;
        while (t != null)
        {
            if (!t.gameObject.activeSelf)
                t.gameObject.SetActive(true);
            t = t.parent;
        }
    }

    private void AfficherTuto(DonneesTutoriel tuto)
    {
        // Bloquer le tutoriel quand on n'est pas dans la sc�ne du menu
        if (SceneManager.GetActiveScene().name != "SCENE0-Menu-Tuto")
            return;

        // Des qu'une tuile de tuto s'affiche, autoriser le deplacement
        // WASD du joueur (le regard a la souris etait deja autorise).
        MouvementAutorise = true;

        tutoActuel = tuto;
        gestionTutoriel.AfficherTuto(tuto);

        // ----- Detecteurs (triggers de zone) -----
        // On desactive tous les detecteurs, puis on active celui qui
        // correspond a l'idActionRequise (s'il existe).
        var detecteurs = FindObjectsByType<detecteurTuto>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var d in detecteurs) d.gameObject.SetActive(false);

        if (!string.IsNullOrEmpty(tuto.idActionRequise))
        {
            foreach (var d in detecteurs)
            {
                if (d.IdAction == tuto.idActionRequise)
                {
                    // Active le detecteur ET tous ses ancetres dans
                    // la hierarchie. Sans ca, si l'utilisateur a
                    // desactive le GameObject parent (ex: snap_table)
                    // pour qu'il n'apparaisse pas trop tot, l'enfant
                    // ne pourrait jamais devenir visible.
                    ActiverAvecAncetres(d.gameObject);
                    Debug.Log($"[Chapitre] Detecteur active pour idActionRequise={tuto.idActionRequise} (obj={d.name})");
                    break;
                }
            }
        }

        // ----- DialogueTuto (PNJ avec dialogue multi-etapes) -----
        // Meme logique : tous les PNJ sont verrouilles, puis on
        // deverrouille celui dont l'IdAction courante matche la tuile.
        var dialogues = FindObjectsByType<DialogueTuto>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var d in dialogues) d.DesactiverInteraction();

        if (!string.IsNullOrEmpty(tuto.idActionRequise))
        {
            foreach (var d in dialogues)
            {
                // PeutSignaler regarde aussi les repliques (en plus de
                // idActionAuDebut / idActionAFin), donc un PNJ est
                // deverrouille meme si la tuile est fermee par un
                // idActionADeclencher d'une replique du milieu.
                if (d.PeutSignaler(tuto.idActionRequise))
                {
                    d.ReactiverInteraction();
                    Debug.Log($"[Chapitre] DialogueTuto deverrouille pour idActionRequise={tuto.idActionRequise} (obj={d.name})");
                    break;
                }
            }
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

        // R�cup�rer le id de l'action compl�t�e
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

        // D�lai pour laisser du temps au fade out
        if (prochain != null)
        {
            Debug.Log($"[Chapitre] Passage � la prochaine �tape: {prochain.idDeclencheur}");
            // Petite attente pour laisser le fade out se faire
            StartCoroutine(AfficherProchainTutoApresDelai(prochain, 0.25f));
        }
        else
        {
            Debug.Log("[Chapitre] Aucune etape suivante non vue dans ce chapitre.");

            // S'il y a un prochain chapitre, l'enchainer (sa banniere
            // s'affichera). Sinon, jouer la cinematique de fin.
            if (chapitreActuel.prochainChapitre != null)
            {
                Debug.Log($"[Chapitre] Enchainement vers: {chapitreActuel.prochainChapitre.idChapitre}");
                StartCoroutine(EnchainerChapitreApresDelai(
                    chapitreActuel.prochainChapitre,
                    chapitreActuel.delaiAvantProchainChapitre));
            }
            else
            {
                string nomCine = string.IsNullOrEmpty(chapitreActuel.nomCinematiqueAuFin)
                    ? "cinematique1"
                    : chapitreActuel.nomCinematiqueAuFin;
                Debug.Log($"[Chapitre] Lancement cinematique de fin: {nomCine}");
                StartCoroutine(JouerCinematique(nomCine, 3f));
            }
        }
    }

    // Enchaine un nouveau chapitre apres un delai, en passant par la
    // sequence normale (banniere + delai + premier tuto).
    private IEnumerator EnchainerChapitreApresDelai(DonneesChapitre suivant, float delai)
    {
        yield return new WaitForSecondsRealtime(delai);

        chapitreActuel = suivant;
        StartCoroutine(SequenceDemarrageChapitre(suivant));
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

    // Affiche le prochain tuto apr�s un d�lai
    private System.Collections.IEnumerator AfficherProchainTutoApresDelai(DonneesTutoriel tuto, float delai)
    {
        yield return new WaitForSecondsRealtime(delai);
        AfficherTuto(tuto);
    }

    public Coroutine LancerCinematiqueAvecDelai(string nomCinematique, float delaiAvantCinematique)
    {
        return StartCoroutine(JouerCinematique(nomCinematique, delaiAvantCinematique));
    }

    public System.Collections.IEnumerator JouerCinematique(string nomCinematique, float delaiAvantCinematique)
    {
        yield return new WaitForSecondsRealtime(delaiAvantCinematique);

        // Faire jouer le video player en lui assignant la vid�o correspondante au nomCinematique
        Debug.Log($"[Chapitre] Lancement cin�matique: {nomCinematique}");
        VideoClip clip = System.Array.Find(cinematique, c => c.name == nomCinematique);
        if (clip != null)
        {
            FindObjectOfType<gestionInputsJeu>()?.ModeCinematique(true);

            // Arr�ter la musique de fond si elle est encore en train de jouer
            var musique = FindObjectOfType<gestionAudio>();
                if (musique != null)
                    musique.ArreterMusique();

            playerCinematiques.clip = clip;
            playerCinematiques.loopPointReached += OnCinematiqueFinie;
            playerCinematiques.Play();
        }
        else
        {
            Debug.LogWarning($"[Chapitre] Cin�matique introuvable: {nomCinematique}");
        }
    }

    private void OnCinematiqueFinie(VideoPlayer vp)
    {
        Debug.Log("[Chapitre] Cinematique terminee, retour au jeu");

        FindObjectOfType<gestionInputsJeu>()?.ModeCinematique(false);

        // Reprendre la musique de fond apres la cinematique
        var musique = FindObjectOfType<gestionAudio>();
        if (musique != null)
            musique.ReprendreMusique();

        // Le tutoriel est complete : on active les indices de
        // jouabilite (UI persistante du HUD) pour le vrai gameplay.
        // Comme le GameObject vit dans --DONTDESTROYONLOAD, il
        // reste actif apres le LoadScene.
        if (indicesJouabilite != null)
        {
            indicesJouabilite.SetActive(true);
            Debug.Log("[Chapitre] indices_jouabilite active.");
        }

        SceneManager.LoadScene("SCENE1-Taverne1");
        gestionAudio.Instance.JouerMusiquesTaverne();
    }

}
