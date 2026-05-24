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

    // Event broadcast quand la banniere annonce-chapitre d'un chapitre
    // se termine (apres AfficherBanniere). Le parametre est idChapitre.
    // Utilise notamment par gestionGlowIndices pour activer les effets
    // glow sur les indices apres que la banniere "Mener l'enquete" se
    // soit affichee.
    public event System.Action<string> OnBanniereChapitreTerminee;

    [Header("References")]
    [SerializeField] private gestionBanniere gestionBanniere;
    [SerializeField] private gestionTutoriel gestionTutoriel;
    [SerializeField] private VideoPlayer playerCinematiques;

    [Header("Chapitres disponibles")]
    [SerializeField] private DonneesChapitre[] chapitres;

    private DonneesChapitre chapitreActuel;
    private DonneesTutoriel tutoActuel;
    private HashSet<string> tutosVus = new HashSet<string>();
    // Toutes les actions signalees depuis le debut de la session.
    // Permet a une tuile de verifier si son idActionAnnulation a deja
    // ete signalee AVANT son affichage (auquel cas la tuile est sautee).
    private HashSet<string> actionsSignalees = new HashSet<string>();

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
        if (SceneManager.GetActiveScene().name == "scene_taverne_tutoriel")
            MouvementAutorise = false;

        StartCoroutine(SequenceDemarrageChapitre(chapitre));
    }

    private IEnumerator SequenceDemarrageChapitre(DonneesChapitre chapitre)
    {

        // Comportement par scene :
        // - SCENE0-Menu-Tuto : sequence complete (banniere +
        //   tutoriels). C'est l'usage premier du systeme.
        // - autres scenes (ex : SCENE1-Taverne1) :
        //   on autorise la banniere annonce-chapitre, mais on
        //   n'affiche pas les tutoriels (la sequence s'arrete apres
        //   la banniere). Permet d'annoncer un nouveau chapitre
        //   narratif (genre "Mener l'enquete") sans devoir y
        //   accrocher des tuiles tuto.
        string sceneActuelle = SceneManager.GetActiveScene().name;
        bool sceneEstTutoriel = sceneActuelle == "scene_taverne_tutoriel";

        Debug.Log("[Chapitre] Demarrage: " + chapitre.idChapitre);

        // Bloquer les mouvements du personnage pendant l'affichage
        // de la banniere annonce-chapitre, SAUF pour le tout premier
        // chapitre (premier_contact) qui a deja sa propre logique de
        // blocage (DemarrerChapitre met deja MouvementAutorise=false
        // pour ce chapitre, et le reactive a la 1ere tuile de tuto).
        // Pour les chapitres suivants (aide_precieuse, mener_enquete,
        // et les bannières "Reparler/Ecouter au tavernier" affichées
        // comme bannières), on fige le joueur pendant la bannière et
        // on re-autorise les mouvements une fois la bannière terminée.
        bool bloquerMouvementsPourBanniere =
            chapitre.idChapitre != "premier_contact";

        // IMPORTANT : on NE bloque PAS le mouvement au demarrage du
        // chapitre. Le joueur doit pouvoir bouger librement pendant le
        // delaiApparitionBanniere (ex : 6s pour mener_enquete apres la
        // cinematique). Le blocage ne s'applique QU'AU MOMENT ou la
        // banniere apparait, pour figer le joueur pendant qu'il lit le
        // titre du chapitre. La reprise se fait 1s plus tard.
        // Exception : premier_contact a sa propre logique de blocage
        // dans DemarrerChapitre (le joueur est fige des le tout debut
        // du jeu, jusqu'a la 1ere tuile de tuto qui le libere).

        yield return new WaitForSecondsRealtime(chapitre.delaiApparitionBanniere);

        // Maintenant que la banniere va apparaitre : on bloque le
        // mouvement et on programme sa reprise 1s plus tard. Comme ca
        // le joueur ne peut pas bouger pendant qu'il decouvre le titre.
        if (bloquerMouvementsPourBanniere)
        {
            MouvementAutorise = false;
            StartCoroutine(ReautoriserMouvementApresDelai(1f));
        }

        Debug.Log("[Chapitre] Lancement banniere");

        yield return StartCoroutine(gestionBanniere.AfficherBanniere(
            chapitre.nomAffiche,
            chapitre.dureeAffichageBanniere,
            chapitre.tailleTitre));

        Debug.Log("[Chapitre] Banniere terminee");

        // Notifier les abonnes (ex: gestionGlowIndices) que la
        // banniere annonce-chapitre de ce chapitre vient de se terminer.
        OnBanniereChapitreTerminee?.Invoke(chapitre.idChapitre);

        // Signaler une action standardisee pour pouvoir declencher
        // des bandeaux info, activer des indicateurs, etc. depuis
        // l'editeur sans ecrire de code dedie. Convention :
        // "chapitre_<idChapitre>_banniere_terminee".
        // Ex : "chapitre_mener_enquete_banniere_terminee" pour
        // declencher le bandeau "Va voir le client" + activer le
        // pointeur visuel devant la table du npc.
        SignalerAction(
            $"chapitre_{chapitre.idChapitre}_banniere_terminee");

        // Note : plus besoin de restaurer MouvementAutorise ici. La
        // coroutine ReautoriserMouvementApresDelai s'en occupe apres 1s.

        // Si on n'est pas dans la scene tutoriel, la sequence s'arrete
        // ici : on ne deroule pas les tuiles de tuto (le chapitre sert
        // juste a annoncer une nouvelle phase narrative).
        if (!sceneEstTutoriel)
            yield break;

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

        // Memoriser l'action pour les futures verifications
        // d'idActionAnnulation (une tuile pas encore affichee peut
        // etre sautee si son action d'annulation a deja eu lieu).
        actionsSignalees.Add(idAction);

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
            return;
        }

        // Si l'action correspond a l'idActionAnnulation de la tuile
        // actuellement affichee, on la ferme aussi (et on la marque
        // comme vue pour ne pas la reafficher). Cas typique : tuile
        // menu_pause affichee, mais le joueur laisse le dialogue
        // progresser sans ouvrir le menu pause -> tuile sautee.
        if (!string.IsNullOrEmpty(tutoActuel.idActionAnnulation)
            && tutoActuel.idActionAnnulation == idAction)
        {
            Debug.Log($"[Chapitre] Tuile '{tutoActuel.idDeclencheur}' " +
                $"annulee par action '{idAction}'.");
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

    /// <summary>
    /// Coroutine qui affiche une banniere annonce-chapitre tout en
    /// bloquant les mouvements WASD du joueur pendant 1 seconde.
    /// Utilise pour les tuiles tuto marquees "afficherCommeBanniere"
    /// (typiquement des indications narratives comme "Reparler au
    /// tavernier"). Le mouvement est reautorise apres 1s (pas a la fin
    /// de la banniere) pour que le joueur puisse commencer a bouger
    /// vers le tavernier des qu'il voit le titre s'afficher.
    /// </summary>
    private IEnumerator AfficherBanniereEtBloquerMouvements(
        string titre, float duree)
    {
        MouvementAutorise = false;
        // Reautoriser le mouvement apres 1s, en parallele de la banniere.
        StartCoroutine(ReautoriserMouvementApresDelai(1f));
        yield return StartCoroutine(
            gestionBanniere.AfficherBanniere(titre, duree));
    }

    /// <summary>
    /// Surcharge avec signalement d'action a la fin de la banniere.
    /// Permet a d'autres systemes (bandeaux info) de se synchroniser sur
    /// la fin precise de la banniere d'une tuile (ex: bandeau menu_pause
    /// apparait 2s apres que la banniere "Les retrouvailles" disparaisse).
    /// </summary>
    private IEnumerator AfficherBanniereEtBloquerMouvements(
        string titre, float duree, string idActionApres)
    {
        MouvementAutorise = false;
        StartCoroutine(ReautoriserMouvementApresDelai(1f));
        yield return StartCoroutine(
            gestionBanniere.AfficherBanniere(titre, duree));

        if (!string.IsNullOrEmpty(idActionApres))
        {
            Debug.Log($"[Chapitre] Banniere terminee, signal action " +
                $"'{idActionApres}'.");
            SignalerAction(idActionApres);
        }
    }

    /// <summary>
    /// Reautorise le mouvement du joueur apres un delai donne. Utilise
    /// pour libérer le joueur 1s apres le debut d'une banniere, plutot
    /// que d'attendre la fin de la banniere (3-4s typiquement).
    /// </summary>
    private IEnumerator ReautoriserMouvementApresDelai(float delai)
    {
        yield return new WaitForSecondsRealtime(delai);
        MouvementAutorise = true;
    }

    private void AfficherTuto(DonneesTutoriel tuto)
    {
        // Bloquer le tutoriel quand on n'est pas dans la sc�ne du menu
        if (SceneManager.GetActiveScene().name != "scene_taverne_tutoriel")
            return;

        // SKIP : si l'idActionAnnulation de cette tuile a deja ete
        // signalee avant son affichage, on la saute (et on passe a la
        // prochaine du chapitre). Ex : tuile menu_pause/options avec
        // idActionAnnulation="demande_aide_faite" ne s'affiche pas si
        // la 1ere etape du dialogue est deja terminee.
        if (!string.IsNullOrEmpty(tuto.idActionAnnulation)
            && actionsSignalees.Contains(tuto.idActionAnnulation))
        {
            Debug.Log($"[Chapitre] Tuile '{tuto.idDeclencheur}' " +
                $"sautee : action d'annulation '{tuto.idActionAnnulation}' " +
                $"deja signalee.");
            tutosVus.Add(tuto.idDeclencheur);
            SauvegarderTutosVus();
            // Avancer immediatement a la tuile suivante du chapitre
            AvancerVersProchaineTuile();
            return;
        }

        // BANDEAU INFO : si la tuile est marquee afficherCommeBandeau,
        // on l'envoie au systeme gestionBandeauInfo (non-bloquant) au
        // lieu d'afficher une tuile tutoriel. La sequence continue
        // immediatement vers la prochaine tuile.
        if (tuto.afficherCommeBandeau)
        {
            float dureeBandeau = tuto.dureeAuto > 0f ? tuto.dureeAuto : 5f;
            Debug.Log($"[Chapitre] Tuile '{tuto.idDeclencheur}' " +
                $"affichee comme BANDEAU INFO ({dureeBandeau}s).");
            gestionBandeauInfo.Afficher(tuto.explication, dureeBandeau);
            tutosVus.Add(tuto.idDeclencheur);
            SauvegarderTutosVus();
            AvancerVersProchaineTuile();
            return;
        }

        tutoActuel = tuto;

        // Branche : si l'etape est marquee comme "afficher comme
        // banniere" (typiquement les etapes narratives qui ne sont
        // pas du tutoriel a proprement parler, ex : "Reparler au
        // tavernier"), on utilise la banniere annonce-chapitre au
        // lieu d'une tuile tutoriel. L'idActionRequise continue de
        // fonctionner normalement pour fermer l'etape.
        // Sous cette branche, on BLOQUE les mouvements pendant la
        // banniere (et on les laisse bloques jusqu'a la prochaine
        // tuile tuto qui les reautorisera).
        if (tuto.afficherCommeBanniere && gestionBanniere != null)
        {
            MouvementAutorise = false;
            StartCoroutine(AfficherBanniereEtBloquerMouvements(
                tuto.titre, tuto.dureeBanniere, tuto.idActionApresBanniere));
        }
        else
        {
            // Tuile de tuto classique : on autorise les mouvements
            // (premier_contact bloque jusqu'a cette ligne via la
            // logique de DemarrerChapitre).
            MouvementAutorise = true;
            gestionTutoriel.AfficherTuto(tuto);
        }

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

        // ----- Auto-fermeture des tuiles informatives -----
        // Si dureeAuto > 0, on demarre un timer qui fermera la tuile
        // automatiquement et passera a l'etape suivante du chapitre.
        // Cela permet aux tuiles "informatives" (ex : menu_pause,
        // options) de ne pas bloquer le tutoriel : le joueur les voit
        // pendant quelques secondes puis le tuto continue meme s'il
        // n'a pas effectue l'action.
        if (tuto.dureeAuto > 0f && !tuto.afficherCommeBanniere)
        {
            StartCoroutine(AutoFermerTuileApresDelai(
                tuto, tuto.dureeAuto));
        }
    }

    /// <summary>
    /// Ferme automatiquement la tuile tutoriel apres un delai donne,
    /// si elle est toujours affichee. Ne ferme pas si l'utilisateur a
    /// deja avance (autre tuto, idActionRequise signalee, etc.).
    /// </summary>
    private IEnumerator AutoFermerTuileApresDelai(
        DonneesTutoriel cible, float delai)
    {
        yield return new WaitForSecondsRealtime(delai);

        // Verifier qu'on est toujours sur la meme tuile (le joueur n'a
        // pas avance entre-temps en accomplissant l'action ou en
        // appuyant sur ESC).
        if (tutoActuel == cible)
        {
            Debug.Log($"[Chapitre] Auto-fermeture tuile informative " +
                $"'{cible.idDeclencheur}' apres {delai}s.");
            // Ferme la tuile et marque-la comme vue pour passer a la
            // suivante du chapitre.
            FermerTutoActuel(true);
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

        AvancerVersProchaineTuile();
    }

    /// <summary>
    /// Cherche la prochaine tuile non vue du chapitre courant et
    /// l'affiche apres un petit delai. Si aucune tuile n'est en
    /// attente, enchaine sur le prochain chapitre ou la cinematique.
    /// </summary>
    private void AvancerVersProchaineTuile()
    {
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
            Debug.Log($"[Chapitre] Passage � la prochaine �tape: {prochain.idDeclencheur} (delai {prochain.delaiAvantApparition}s)");
            // Le delai est configurable par tuile (defaut 0.25s) pour
            // permettre des respirations narratives plus longues sur
            // certaines tuiles informatives.
            StartCoroutine(AfficherProchainTutoApresDelai(prochain, prochain.delaiAvantApparition));
        }
        else
        {
            Debug.Log("[Chapitre] Aucune etape suivante non vue dans ce chapitre.");

            // Verifier s'il faut attendre une action de fin de chapitre
            // avant d'enchainer (ex: fin de la 1ere etape du dialogue
            // tav_1 dans premier_contact). Sans cette attente, le
            // dureeAuto des tuiles ferait demarrer le prochain chapitre
            // pendant que le dialogue est encore en cours.
            string idAttenteFin = chapitreActuel.idActionRequiseFinChapitre;
            if (!string.IsNullOrEmpty(idAttenteFin)
                && !actionsSignalees.Contains(idAttenteFin))
            {
                Debug.Log($"[Chapitre] Tuiles fermees mais attente de " +
                    $"'{idAttenteFin}' avant d'enchainer le prochain " +
                    $"chapitre.");
                StartCoroutine(AttendreActionPuisEnchainer(idAttenteFin));
            }
            else
            {
                EnchainerOuLancerCinematique();
            }
        }
    }

    /// <summary>
    /// Attend qu'une action specifique soit signalee avant d'enchainer
    /// sur le prochain chapitre (ou la cinematique). Utilise quand une
    /// tuile s'est fermee via dureeAuto mais qu'un evenement narratif
    /// (ex: fin d'une etape de dialogue) doit encore se produire avant
    /// que la suite s'enchaine.
    /// </summary>
    private IEnumerator AttendreActionPuisEnchainer(string idAction)
    {
        while (!actionsSignalees.Contains(idAction))
        {
            yield return null;
        }
        Debug.Log($"[Chapitre] Action '{idAction}' signalee, " +
            $"enchainement du prochain chapitre.");
        EnchainerOuLancerCinematique();
    }

    /// <summary>
    /// Demarre le prochainChapitre s'il existe, sinon lance la
    /// cinematique de fin. Centralise la logique de transition pour
    /// pouvoir l'appeler depuis plusieurs endroits (fin normale ou
    /// fin avec attente d'action).
    /// </summary>
    private void EnchainerOuLancerCinematique()
    {
        if (chapitreActuel == null) return;

        if (chapitreActuel.prochainChapitre != null)
        {
            Debug.Log($"[Chapitre] Enchainement vers: " +
                $"{chapitreActuel.prochainChapitre.idChapitre}");
            StartCoroutine(EnchainerChapitreApresDelai(
                chapitreActuel.prochainChapitre,
                chapitreActuel.delaiAvantProchainChapitre));
        }
        else
        {
            string nomCine = string.IsNullOrEmpty(
                chapitreActuel.nomCinematiqueAuFin)
                ? "cinematique1"
                : chapitreActuel.nomCinematiqueAuFin;
            Debug.Log($"[Chapitre] Lancement cinematique de fin: " +
                $"{nomCine}");
            // Delai 1s apres la derniere replique du tavernier pour
            // un petit temps de respiration narratif avant la video.
            StartCoroutine(JouerCinematique(nomCine, 1f));
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
        // Geler les mouvements du personnage des le debut de la
        // cinematique (et meme pendant le delai d'attente). Le
        // PlayerMovement consulte MouvementAutorise pour decider
        // s'il accepte les inputs WASD. On le remet a true au
        // chargement de la prochaine scene si necessaire.
        MouvementAutorise = false;

        // Activer le mode cinematique IMMEDIATEMENT (avant le delai)
        // pour bloquer les inputs clavier et fermer toutes les fenetres
        // UI ouvertes (menu pause, journal, inventaire, options). Sans
        // ca, pendant le delai d'attente avant la video, le joueur
        // pourrait encore ouvrir un menu (ESC long press, K, J, I, O)
        // et l'UI resterait visible au demarrage de la video.
        // Note : ModeCinematique(true) appelle FermerToutesLesFenetres
        // en interne (merge collegue), donc on a le double effet.
        FindObjectOfType<gestionInputsJeu>()?.ModeCinematique(true);

        yield return new WaitForSecondsRealtime(delaiAvantCinematique);

        // Faire jouer le video player en lui assignant la vid�o correspondante au nomCinematique
        Debug.Log($"[Chapitre] Lancement cin�matique: {nomCinematique}");
        VideoClip clip = System.Array.Find(cinematique, c => c.name == nomCinematique);
        if (clip != null)
        {
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
        Debug.Log("[Chapitre] Cinematique terminee, chargement SCENE1");

        // Sortir du mode cinématique (réactive curseur, etc.)
        // Le gestionInputsJeu de SCENE0 sera détruit au LoadScene —
        // c'est sans conséquence, SCENE1 repart de son propre Start().
        FindObjectOfType<gestionInputsJeu>()?.ModeCinematique(false);

        // NE PAS reprendre la musique ici : on change de scène
        // immédiatement, la musique de SCENE1 démarrera via le callback.

        // Le tutoriel est complete : on active les indices de
        // jouabilite (UI persistante du HUD) pour le vrai gameplay.
        // Comme le GameObject vit dans --DONTDESTROYONLOAD, il
        // reste actif apres le LoadScene.
        if (indicesJouabilite != null)
        {
            indicesJouabilite.SetActive(true);
            Debug.Log("[Chapitre] indices_jouabilite active.");
        }

        // Capture la position et rotation actuelles du Player avant
        // le changement de scene, afin de les restaurer dans
        // SCENE1-Taverne1 (le joueur garde sa derniere
        // position au lieu de respawner au point initial du tutoriel).
        var playerGo = GameObject.FindGameObjectWithTag("Player");
        if (playerGo == null)
        {
            // Fallback : chercher par script si pas de tag "Player"
            var pm = FindFirstObjectByType<PlayerMovement>(
                FindObjectsInactive.Include);
            if (pm != null) playerGo = pm.gameObject;
        }
        if (playerGo != null)
            PositionPlayerEntreScenes.Capturer(playerGo.transform);
        else
            Debug.LogWarning(
                "[Chapitre] Player introuvable avant LoadScene - "
                + "la position ne sera pas preservee.");

        // Passer par l'écran de chargement si disponible.
        // La musique de taverne est démarrée via le callback, une frame
        // APRÈS que la scène soit chargée — pas pendant la cinématique.
        if (gestionEcranChargement.Instance != null)
            gestionEcranChargement.Instance.ChargerScene(
                "scene_taverne_recherche_indices",
                () => gestionAudio.Instance?.JouerMusiquesTaverne());
        else
        {
            SceneManager.LoadScene("scene_taverne_recherche_indices");
            gestionAudio.Instance?.JouerMusiquesTaverne();
        }
    }

}
