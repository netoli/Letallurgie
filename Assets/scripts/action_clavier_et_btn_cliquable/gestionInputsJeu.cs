using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using System.Collections;
using UnityEngine.SceneManagement;



public class gestionInputsJeu : MonoBehaviour
{
    [Header("Canvas")]
    [SerializeField] private GameObject canvasMenuPause;
    [SerializeField] private CanvasGroup groupeMenuPause;
    [SerializeField] private GameObject canvasOptions;
    [SerializeField] private CanvasGroup groupeOptions;
    [SerializeField] private GameObject canvasJournal;
    [SerializeField] private CanvasGroup groupeJournal;
    [SerializeField] private GameObject canvasCredits;
    [SerializeField] private CanvasGroup groupeCredits;
    [SerializeField] private GameObject canvasHud;
    [SerializeField] private GameObject canvasMenu;
    [SerializeField] private CanvasGroup groupeMenu;
    [SerializeField] private GameObject canvasRetournerMenuPrincipal;
    [SerializeField] private CanvasGroup groupeRetournerMenuPrincipal;
    [SerializeField] private GameObject canvasQuitter;
    [SerializeField] private CanvasGroup groupeQuitter;

    [Header("Canvas Tuto")]
    [SerializeField] private GameObject ensembleTuileTutoEtBoutonRetour;
    [SerializeField] private CanvasGroup groupeTuto;

    [Header("Canvas Reinitialisation")]
    [SerializeField] private GameObject canvasConfirmerReinitialisation;
    [SerializeField] private CanvasGroup groupeConfirmerReinitialisation;

    [Header("Canvas Inventaire")]
    [SerializeField] private GameObject ensembleMenuInventaire;
    [SerializeField] private CanvasGroup groupeContenuHud;

    [Tooltip("GameObject du pointeur central (reticule de visee). " +
        "Sera cache automatiquement quand l'inventaire est ouvert " +
        "pour eviter d'avoir deux pointeurs a l'ecran (le pointeur " +
        "fixe au centre + le curseur libre pour cliquer).")]
    [SerializeField] private GameObject pointeurCentre;

    [Header("Effets HUD")]
    [SerializeField] private ParticleSystem[] fxHud;

    [Header("Cameras")]
    [SerializeField] private CinemachineCamera vcamMenu;
    [SerializeField] private CinemachineCamera vcamJeu;
    [SerializeField] private CinemachineBrain cinemachineBrain;

    [Header("Parametres")]
    [SerializeField] private float vitesseFade;
    [SerializeField] private float delaiEffets;

    [Header("Flou")]
    public gestionFlou gestionFlou;

    [Header("Onglets Options")]
    [SerializeField] private CanvasGroup groupeOngletsOptions;

    private enum EtatJeu
    {
        EnJeu,
        EnPause,
        DansOptionsPause,
        DansOptionsJeu,
        DansJournal,
        DansCredits,
        DansTuto,
        DansInventaire,
        ConfirmationRetourMenu,
        ConfirmationQuitter,
        ConfirmationReinitialisation
    }

    private EtatJeu etatActuel = EtatJeu.EnJeu;

    /// <summary>
    /// True si le joueur est en gameplay actif, sans aucun menu,
    /// journal, options ou cinématique ouvert. Utilisé par les scripts
    /// d'ambiance sonore (sfxAmbiancePnj) pour savoir quand atténuer.
    /// </summary>
    public bool JeuEnCoursActif => jeuActif && etatActuel == EtatJeu.EnJeu;
    private EtatJeu etatAvantConfirmation = EtatJeu.EnJeu;
    private EtatJeu etatAvantJournal = EtatJeu.EnJeu;
    private EtatJeu etatAvantInventaire = EtatJeu.EnJeu;
    private EtatJeu etatAvantReinitialisation = EtatJeu.EnJeu;
    private bool jeuActif = false;
    // Timestamp d'ouverture inventaire — sert au grace period pour que
    // la fermeture auto (curseur hors zone) ne se declenche pas
    // immediatement quand le curseur est encore au centre.
    private float inventaireOuvertA = -10f;
    private bool attenteAction = false;
    private bool sourisVerrouillee = false;

    // Tracking du press long sur ESC. Quand le joueur maintient ESC
    // au moins escDureeLongPress secondes, on ouvre le menu pause —
    // peu importe le contexte (dialogue, tuile tuto, etc.). Press
    // court (relachement avant ce delai) : action contextuelle
    // (skip dialogue, fermer tuile, back menu).
    private const float escDureeLongPress = 1f;
    private float escAppuyeDepuis = -1f;
    private bool escLongPressDeclenche = false;
    // True si le long press a effectivement ouvert le menu pause
    // (on etait EnJeu). Si on etait deja dans un autre etat (EnPause,
    // DansJournal, etc.), le long press ne fait rien et on doit
    // permettre au release de faire l'action contextuelle (sinon ESC
    // pour fermer le menu pause ne marche plus apres 1s d'appui).
    private bool escMenuOuvertParLongPress = false;

    void Start()
    {
        // Auto-resolve : si l'instance de gestionInputsJeu provient d'un
        // prefab ou d'une autre scene (cas scene2 en standalone), les
        // references serialisees peuvent etre cassees. On retrouve les
        // canvases par nom dans le canvas_hud de la scene actuelle.
        AutoResoudreReferences();

        // Auto-binder les boutons HUD (cas scene2_usine standalone : les
        // onClick des boutons pointent vers une instance gestionInputsJeu
        // d'une autre scene, donc rien ne se passe au clic). On rebind
        // dynamiquement au runtime.
        AutoBindBoutonsHud();

        // D�sactiver le menu principal si on n'est pas dans la sc�ne du menu
        if (SceneManager.GetActiveScene().name != "scene0_tuto")
        {
            if (canvasMenu != null)
                canvasMenu.SetActive(false);

            // HUD visible immediatement (avant transition Cinemachine et
            // banniere annonce-chapitre). Comportement uniforme avec scene1.
            ActiverInputs();

            // En scene2_usine : cacher le pointeur_centre pendant la
            // transition Cinemachine d'ouverture. Il sera reactive a la
            // fin de la banniere "A la rescousse" via OnBanniereChapitre-
            // Terminee (handler ci-dessous).
            if (SceneManager.GetActiveScene().name == "scene2_usine"
                && pointeurCentre != null)
            {
                pointeurCentre.SetActive(false);
                if (gestionChapitres.Instance != null)
                    gestionChapitres.Instance.OnBanniereChapitreTerminee
                        += AuFinBanniereSauvetage;
            }

            // Activer la cam�ra premi�re personne par d�faut
            if (vcamJeu != null)
                vcamJeu.Priority = 50;

            if (vcamMenu != null)
                vcamMenu.Priority = 10;
        }

        if (canvasMenuPause != null)
            canvasMenuPause.SetActive(false);
        if (canvasRetournerMenuPrincipal != null)
            canvasRetournerMenuPrincipal.SetActive(false);
        if (canvasQuitter != null)
            canvasQuitter.SetActive(false);
        if (canvasConfirmerReinitialisation != null)
            canvasConfirmerReinitialisation.SetActive(false);
        if (ensembleMenuInventaire != null)
            ensembleMenuInventaire.SetActive(false);

        // Abonnement aux changements de selection d'inventaire : quand
        // le joueur clique un objet pendant que l'inventaire est ouvert,
        // on verrouille le curseur Windows et on reaffiche le pointeur
        // central (le HUD de visee). L'inventaire reste OUVERT. Le
        // joueur peut alors voir le pointeur_centre + icone flottante
        // au centre, parfait pour aller placer l'objet dans la 3D.
        if (gestionSelectionInventaire.Instance != null)
            gestionSelectionInventaire.Instance.onSelectionChangee
                += SurSelectionInventaireChangee;
    }

    void OnDestroy()
    {
        if (gestionSelectionInventaire.Instance != null)
            gestionSelectionInventaire.Instance.onSelectionChangee
                -= SurSelectionInventaireChangee;
    }

    /// <summary>
    /// Callback abonne a gestionSelectionInventaire.onSelectionChangee.
    /// Quand le joueur clique un objet pendant l'inventaire ouvert, on
    /// verrouille le curseur et on reaffiche le pointeur_centre (le HUD
    /// reticule + l'icone flottante restent visibles). Quand la
    /// selection est annulee (Esc, ou re-clic sur le meme objet), on
    /// redeverrouille le curseur pour permettre de re-cliquer un autre
    /// objet. Si l'inventaire est ferme, on ne touche a rien (le state
    /// du curseur est gere normalement par FermerInventaire).
    /// </summary>
    private void SurSelectionInventaireChangee(objetInventaire nouvelle)
    {
        // Ne pas interferer si on n'est pas dans l'inventaire (la
        // selection peut etre annulee depuis d'autres contextes ex :
        // placement reussi qui appelle Deselectionner()).
        if (etatActuel != EtatJeu.DansInventaire) return;

        if (nouvelle != null)
        {
            // Selection faite : curseur cache, pointeur central visible.
            VerrouillerSouris();
            if (pointeurCentre != null) pointeurCentre.SetActive(true);
        }
        else
        {
            // Deselection : on redonne le curseur Windows pour pouvoir
            // re-cliquer un autre objet dans l'inventaire. Le pointeur
            // central est recache puisqu'on revient au mode "navigation
            // inventaire".
            DeverrouillerSouris();
            if (pointeurCentre != null) pointeurCentre.SetActive(false);
        }
    }

    /// <summary>
    /// Retrouve les references UI par nom dans la scene si elles sont
    /// null. Utile quand le composant tourne dans une scene differente
    /// de celle ou ses refs Inspector ont ete serialisees (ex : scene2
    /// en standalone, qui a un canvas_hud different).
    /// </summary>
    private void AutoResoudreReferences()
    {
        // canvasHud : trouver le GameObject root nomme "canvas_hud"
        if (canvasHud == null)
            canvasHud = TrouverParNom("canvas_hud", true);

        // Tous les canvases UI sont enfants de canvas_hud
        if (canvasMenuPause == null)
            canvasMenuPause = TrouverParNom("canvas_menu_pause", true);
        if (canvasOptions == null)
            canvasOptions = TrouverParNom("canvas_options", true);
        if (canvasJournal == null)
            canvasJournal = TrouverParNom("canvas_journal_final_20avril", true)
                ?? TrouverParNom("canvas_journal", true);
        if (canvasCredits == null)
            canvasCredits = TrouverParNom("canvas_credits", true);
        if (canvasRetournerMenuPrincipal == null)
            canvasRetournerMenuPrincipal =
                TrouverParNom("canvas_retourner_menu_principal", true)
                ?? TrouverParNom("canvas_confirmer_retourner_au_menu_principal", true);
        if (canvasQuitter == null)
            canvasQuitter = TrouverParNom("canvas_quitter", true)
                ?? TrouverParNom("canvas_confirmer_quitter", true);
        if (canvasConfirmerReinitialisation == null)
            canvasConfirmerReinitialisation =
                TrouverParNom("canvas_confirmer_reinitialisation", true);
        if (ensembleMenuInventaire == null)
            ensembleMenuInventaire =
                TrouverParNom("ensemble_menu_inventaire", true);
        if (ensembleTuileTutoEtBoutonRetour == null)
            ensembleTuileTutoEtBoutonRetour =
                TrouverParNom("ensemble_tuile_tuto_et_bouton_retour", true);
        // Note : le canvas s'appelle 'canvas_menu_principal' (le nom
        // 'canvas_menu' n'existe pas dans la hierarchie). On cherche
        // d'abord le bon nom, fallback sur l'ancien au cas oui.
        if (canvasMenu == null)
            canvasMenu = TrouverParNom("canvas_menu_principal", true)
                ?? TrouverParNom("canvas_menu", true);

        // CanvasGroups : recuperer depuis le canvas associe
        if (groupeMenuPause == null && canvasMenuPause != null)
            groupeMenuPause = canvasMenuPause.GetComponent<CanvasGroup>();
        if (groupeOptions == null && canvasOptions != null)
            groupeOptions = canvasOptions.GetComponent<CanvasGroup>();
        if (groupeJournal == null && canvasJournal != null)
            groupeJournal = canvasJournal.GetComponent<CanvasGroup>();
        if (groupeCredits == null && canvasCredits != null)
            groupeCredits = canvasCredits.GetComponent<CanvasGroup>();
        if (groupeMenu == null && canvasMenu != null)
            groupeMenu = canvasMenu.GetComponent<CanvasGroup>();
        if (groupeRetournerMenuPrincipal == null
            && canvasRetournerMenuPrincipal != null)
            groupeRetournerMenuPrincipal =
                canvasRetournerMenuPrincipal.GetComponent<CanvasGroup>();
        if (groupeQuitter == null && canvasQuitter != null)
            groupeQuitter = canvasQuitter.GetComponent<CanvasGroup>();
        if (groupeConfirmerReinitialisation == null
            && canvasConfirmerReinitialisation != null)
            groupeConfirmerReinitialisation =
                canvasConfirmerReinitialisation.GetComponent<CanvasGroup>();
        if (groupeTuto == null && ensembleTuileTutoEtBoutonRetour != null)
            groupeTuto =
                ensembleTuileTutoEtBoutonRetour.GetComponent<CanvasGroup>();
        // groupeContenuHud : il faut CIBLER le GameObject "contenu_hud"
        // precisement (qui contient cadre_personnage_principal,
        // ensemble_indicateur_hud, indices_jouabilite). GetComponentInChildren
        // retourne le PREMIER CanvasGroup trouve, qui peut etre n'importe
        // lequel (canvas_hud lui-meme ou un sous-canvas). On cherche par nom.
        if (groupeContenuHud == null)
        {
            var contenuHudGo = TrouverParNom("contenu_hud", true);
            if (contenuHudGo != null)
                groupeContenuHud = contenuHudGo.GetComponent<CanvasGroup>();
            // Fallback : ancien comportement si pas de contenu_hud nomme
            if (groupeContenuHud == null && canvasHud != null)
                groupeContenuHud =
                    canvasHud.GetComponentInChildren<CanvasGroup>(true);
        }
    }

    /// <summary>
    /// Callback abonne a OnBanniereChapitreTerminee en scene2_usine.
    /// Reactive le pointeur_centre apres la fin de la banniere
    /// "A la rescousse" (qui suit la transition Cinemachine d'ouverture).
    /// </summary>
    private void AuFinBanniereSauvetage(string idChapitre)
    {
        Debug.Log("[gestionInputsJeu] AuFinBanniereSauvetage recu pour "
            + "chapitre='" + idChapitre + "'");
        if (idChapitre != "le_sauvetage") return;
        if (pointeurCentre != null)
        {
            pointeurCentre.SetActive(true);
            Debug.Log("[gestionInputsJeu] pointeur_centre reactive "
                + "apres banniere 'A la rescousse'.");
        }
        else
        {
            Debug.LogWarning("[gestionInputsJeu] pointeurCentre est null "
                + "-> pas de reactivation.");
        }
        // Une seule fois : on se desabonne.
        if (gestionChapitres.Instance != null)
            gestionChapitres.Instance.OnBanniereChapitreTerminee
                -= AuFinBanniereSauvetage;
    }

    /// <summary>
    /// Auto-bind les boutons HUD vers leurs methodes attendues. Necessaire
    /// quand la scene est lancee en standalone : les onClick serialises
    /// pointent vers une instance gestionInputsJeu d'une autre scene
    /// (donc null au runtime), et les boutons ne reagissent pas au clic.
    /// </summary>
    private void AutoBindBoutonsHud()
    {
        Debug.Log("[gestionInputsJeu] AutoBindBoutonsHud sur scene "
            + SceneManager.GetActiveScene().name);
        // (nom_du_bouton, methode_a_appeler) — boutons aux noms UNIQUES
        // dans la scene (HUD principal + boutons du menu pause).
        var bindings = new (string nom, System.Action act)[]
        {
            ("bouton_journal",         BoutonJournal),
            ("bouton_inventaire",      BoutonInventaire),
            ("bouton_options",         BoutonOptions),
            ("bouton_sauvegarder",     SauvegarderPartie),
            ("bouton_sauvegarde",      SauvegarderPartie),
            ("bouton_esc",             MettreEnPause),
            ("bouton_menu_principal",  AfficherConfirmationRetourMenu),
            ("bouton_quitter",         AfficherConfirmationQuitter),
        };
        foreach (var (nom, act) in bindings)
            RebindBoutonHud(nom, act);

        // Boutons Oui/Non des canvas de confirmation : leurs noms sont
        // GENERIQUES (bouton_oui / bouton_non) et reutilises dans 3 canvas
        // differents. On les rebind via leur canvas parent. Cas observe
        // en scene2_usine : les 14 onClick gestionInputsJeu avaient
        // m_Target=0 (refs Inspector perdues), donc les clics Oui/Non
        // ne faisaient rien. Le rebind runtime contourne ca proprement
        // sans toucher au YAML de la scene.
        var bindingsParCanvas = new (string canvas, string bouton, System.Action act)[]
        {
            // Canvas "Vraiment quitter le jeu ?"
            ("canvas_confirmer_quitter",
                "bouton_oui", ConfirmerQuitter),
            ("canvas_confirmer_quitter",
                "bouton_non", FermerConfirmationQuitter),
            // Canvas "Retourner au menu principal ?" (ouvert par Q)
            ("canvas_confirmer_retourner_au_menu_principal",
                "bouton_oui", ConfirmerRetourMenuPrincipal),
            ("canvas_confirmer_retourner_au_menu_principal",
                "bouton_non", FermerConfirmationRetourMenu),
            // Canvas "Reinitialiser les options ?"
            ("canvas_confirmer_reinitialisation",
                "bouton_oui", ConfirmerReinitialisation),
            ("canvas_confirmer_reinitialisation",
                "bouton_non", FermerConfirmationReinitialisation),
        };
        foreach (var (canvas, bouton, act) in bindingsParCanvas)
            RebindBoutonDansCanvas(canvas, bouton, act);

        // Boutons du canvas_menu_principal : OnContinuer, OnNouvellePartie,
        // OnOptions, OnCredits, OnQuitter sont des methodes de
        // gestionsTransitions. En scene2 (canvas copie de scene0), les
        // m_Target des onClick sont 0 -> les boutons sont morts. On rebind
        // au runtime en pointant vers le gestionsTransitions local.
        var transitions = GetComponent<gestionsTransitions>();
        if (transitions == null)
        {
            transitions = FindFirstObjectByType<gestionsTransitions>(
                FindObjectsInactive.Include);
        }
        if (transitions != null)
        {
            var menuPrincipalBindings = new (string canvas, string bouton, System.Action act)[]
            {
                ("canvas_menu_principal", "btn_continuer",
                    transitions.OnContinuer),
                ("canvas_menu_principal", "btn_nouvelle_partie",
                    transitions.OnNouvellePartie),
                ("canvas_menu_principal", "btn_options",
                    transitions.OnOptions),
                ("canvas_menu_principal", "btn_credit",
                    transitions.OnCredits),
                ("canvas_menu_principal", "btn_credits",
                    transitions.OnCredits),
                // btn_quiter / btn_quitter du MENU principal : on affiche
                // d'abord le canvas_confirmer_quitter (Oui/Non), pas
                // Application.Quit direct. C'est le comportement attendu
                // par le user pour le menu principal.
                ("canvas_menu_principal", "btn_quiter",
                    AfficherConfirmationQuitter),
                ("canvas_menu_principal", "btn_quitter",
                    AfficherConfirmationQuitter),
            };
            foreach (var (canvas, bouton, act) in menuPrincipalBindings)
                RebindBoutonDansCanvas(canvas, bouton, act);
        }
        else
        {
            Debug.LogWarning("[gestionInputsJeu] gestionsTransitions " +
                "introuvable : boutons du menu principal pas rebindes.");
        }
    }

    /// <summary>
    /// Rebind un bouton aux noms generiques (ex : bouton_oui, bouton_non)
    /// en l'identifiant par son ancetre canvas. Necessaire dans 2 cas :
    /// 1. m_Target=0 (refs Inspector perdues, ex : scene2_usine apres
    ///    plusieurs setup/merge)
    /// 2. m_MethodName INCORRECT cable a la mauvaise methode (ex :
    ///    canvas_confirmer_retourner_au_menu_principal qui appelle
    ///    ConfirmerQuitter au lieu de ConfirmerRetourMenuPrincipal —
    ///    erreur de copy-paste presente dans toutes les scenes).
    ///
    /// On preserve LancerEffetClic (effet sonore/visuel du clic) en le
    /// re-ajoutant comme listener apres RemoveAllListeners, sinon le
    /// rebind aurait l'effet secondaire de casser le retour utilisateur.
    /// </summary>
    private void RebindBoutonDansCanvas(
        string nomCanvas, string nomBouton, System.Action act)
    {
        int trouves = 0;
        var tous = Resources.FindObjectsOfTypeAll<UnityEngine.UI.Button>();
        foreach (var btn in tous)
        {
            if (btn == null || btn.gameObject == null) continue;
            if (btn.gameObject.name != nomBouton) continue;
            if (btn.gameObject.hideFlags != HideFlags.None) continue;
            if (!btn.gameObject.scene.IsValid()) continue;

            // Verifier que ce bouton a un ancetre nomme nomCanvas.
            // Sans ce filtre, on rebinderait TOUS les bouton_oui/non
            // du jeu vers la meme methode, cassant les autres canvas
            // de confirmation.
            Transform t = btn.transform;
            bool dansBonCanvas = false;
            while (t != null)
            {
                if (t.gameObject.name == nomCanvas)
                {
                    dansBonCanvas = true;
                    break;
                }
                t = t.parent;
            }
            if (!dansBonCanvas) continue;

            // Preserver l'effet sonore/visuel du clic : on cherche le
            // composant gestionEffetsBoutonsCliques sur le bouton (ou
            // un enfant) AVANT de modifier les listeners, pour le
            // re-ajouter ensuite.
            var effets = btn.GetComponent<gestionEffetsBoutonsCliques>();
            if (effets == null)
                effets = btn.GetComponentInChildren<
                    gestionEffetsBoutonsCliques>(true);

            // IMPORTANT : RemoveAllListeners() ne supprime QUE les
            // listeners ajoutes via AddListener (runtime). Il ne touche
            // PAS les listeners PERSISTANTS serialises dans le YAML
            // (ceux configures via l'Inspector). On doit donc desactiver
            // chaque listener persistant individuellement via
            // SetPersistentListenerState(Off), sinon la mauvaise methode
            // serialisee (ex: ConfirmerQuitter sur le bouton Oui du
            // canvas RetourMenu) continue de s'executer en plus de
            // notre rebind. Bug observe : cliquer Oui appelait a la
            // fois ConfirmerRetourMenuPrincipal (notre rebind) ET
            // ConfirmerQuitter (le persistant), donc l'app quittait.
            int persistantCount = btn.onClick.GetPersistentEventCount();
            for (int i = 0; i < persistantCount; i++)
            {
                btn.onClick.SetPersistentListenerState(i,
                    UnityEngine.Events.UnityEventCallState.Off);
            }
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => act());
            if (effets != null)
                btn.onClick.AddListener(() => effets.LancerEffetClic());

            Debug.Log($"[gestionInputsJeu] Rebind '{nomCanvas}/" +
                $"{nomBouton}' (persistantsDesactives={persistantCount}, " +
                $"effetClic={(effets != null)}).");
            trouves++;
        }
        if (trouves == 0)
            Debug.LogWarning($"[gestionInputsJeu] Aucun bouton " +
                $"'{nomCanvas}/{nomBouton}' trouve pour rebind.");
    }

    private void RebindBoutonHud(string nom, System.Action act)
    {
        int trouves = 0;
        var tous = Resources.FindObjectsOfTypeAll<
            UnityEngine.UI.Button>();
        foreach (var btn in tous)
        {
            if (btn == null || btn.gameObject == null) continue;
            if (btn.gameObject.name != nom) continue;
            if (btn.gameObject.hideFlags != HideFlags.None) continue;
            if (!btn.gameObject.scene.IsValid()) continue;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => act());
            Debug.Log($"[gestionInputsJeu] Rebind '{nom}' " +
                $"({btn.transform.parent?.name}/{btn.name}).");
            trouves++;
        }
        if (trouves == 0)
            Debug.LogWarning($"[gestionInputsJeu] Aucun bouton " +
                $"'{nom}' trouve pour rebind.");
    }

    /// <summary>
    /// Trouve un GameObject dans la scene par nom, en incluant les
    /// objets desactives. Renvoie null si introuvable.
    /// </summary>
    private GameObject TrouverParNom(string nom, bool inclureInactifs)
    {
        var tous = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var go in tous)
        {
            if (go == null) continue;
            if (go.name != nom) continue;
            // Exclure les prefabs assets (pas dans la scene)
            if (go.hideFlags != HideFlags.None) continue;
            if (!go.scene.IsValid()) continue;
            return go;
        }
        return null;
    }

    // ===== GESTION SOURIS =====

    private void VerrouillerSouris()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        sourisVerrouillee = true;
    }

    private void DeverrouillerSouris()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        sourisVerrouillee = false;
    }

    public void ActiverInputs()
    {
        jeuActif = true;
        etatActuel = EtatJeu.EnJeu;
        Time.timeScale = 1f;
        canvasMenuPause.SetActive(false);
        canvasOptions.SetActive(false);
        canvasRetournerMenuPrincipal.SetActive(false);
        canvasQuitter.SetActive(false);
        canvasCredits.SetActive(false);
        canvasJournal.SetActive(false);
        canvasConfirmerReinitialisation.SetActive(false);
        ensembleMenuInventaire.SetActive(false);
        // Reactiver le canvas_hud principal (peut avoir ete desactive
        // par ModeCinematique(true) ou manuellement dans l'Inspector
        // pour les scenes post-cinematique).
        if (canvasHud != null) canvasHud.SetActive(true);
        attenteAction = false;
        VerrouillerSouris();
        MontrerContenuHud();
        StopAllCoroutines();
    }

    public void DesactiverInputs()
    {
        jeuActif = false;
    }

    public bool PeutInteragir()
    {
        if (!jeuActif) return true;
        return !attenteAction
            && etatActuel != EtatJeu.ConfirmationRetourMenu
            && etatActuel != EtatJeu.ConfirmationQuitter
            && etatActuel != EtatJeu.ConfirmationReinitialisation;
    }

    void OnApplicationFocus(bool focus)
    {
        if (!focus) return;

        if (!jeuActif) return;

        if (etatActuel == EtatJeu.EnJeu && sourisVerrouillee)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public bool PeutJouerEffets()
    {
        if (!jeuActif) return true;
        return etatActuel != EtatJeu.ConfirmationRetourMenu
            && etatActuel != EtatJeu.ConfirmationQuitter
            && etatActuel != EtatJeu.ConfirmationReinitialisation;
    }

    private void MontrerContenuHud()
    {
        // Auto-resolve si pas assigne dans Inspector (cas scene2 standalone)
        if (groupeContenuHud == null && canvasHud != null)
            groupeContenuHud = canvasHud.GetComponentInChildren<CanvasGroup>(true);
        if (groupeContenuHud == null)
        {
            Debug.LogWarning("[gestionInputsJeu] groupeContenuHud null — " +
                "skip MontrerContenuHud (le HUD reste tel quel).");
        }
        else
        {
            groupeContenuHud.alpha = 1f;
            groupeContenuHud.interactable = true;
            groupeContenuHud.blocksRaycasts = true;
        }

        foreach (ParticleSystem fx in fxHud)
        {
            if (fx != null)
                fx.Play();
        }

    }

    private void CacherContenuHud()
    {
        if (groupeContenuHud == null) return;
        groupeContenuHud.alpha = 0f;
        groupeContenuHud.interactable = false;
        groupeContenuHud.blocksRaycasts = false;

        foreach (ParticleSystem fx in fxHud)
        {
            if (fx != null)
                fx.Stop(true,
                    ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private IEnumerator MontrerContenuHudApresDelai(
        float delai)
    {
        yield return new WaitForSecondsRealtime(delai);
        MontrerContenuHud();
    }

    private void BloquerCanvasGroup(CanvasGroup groupe)
    {
        if (groupe == null) return;
        groupe.interactable = false;
        groupe.blocksRaycasts = false;
    }

    private void DebloquerCanvasGroup(CanvasGroup groupe)
    {
        if (groupe == null) return;
        groupe.interactable = true;
        groupe.blocksRaycasts = true;
    }

    private void BloquerOptions()
    {
        BloquerCanvasGroup(groupeOptions);
        if (groupeOngletsOptions != null)
            BloquerCanvasGroup(groupeOngletsOptions);
    }

    private void DebloquerOptions()
    {
        DebloquerCanvasGroup(groupeOptions);
        if (groupeOngletsOptions != null)
            DebloquerCanvasGroup(groupeOngletsOptions);
    }

    private void AnnulerOptionsNonConfirmees()
    {
        gestionConfirmationOptions confirmation =
            FindFirstObjectByType<gestionConfirmationOptions>();
        if (confirmation != null && confirmation.ADesModifications())
        {
            confirmation.AnnulerChangements();

            gestionOptionsAudio audio =
                FindFirstObjectByType<gestionOptionsAudio>();
            if (audio != null)
                audio.RechargerPreferences();

            gestionOptionsGraphiques graphiques =
                FindFirstObjectByType<gestionOptionsGraphiques>();
            if (graphiques != null)
                graphiques.RechargerPreferences();

            gestionOptionsAccessibilite accessibilite =
                FindFirstObjectByType<gestionOptionsAccessibilite>();
            if (accessibilite != null)
                accessibilite.RechargerPreferences();

            gestionOptionsControle controle =
                FindFirstObjectByType<gestionOptionsControle>();
            if (controle != null)
                controle.RechargerPreferences();
        }
    }

    private void SauvegarderEtatOptions()
    {
        gestionConfirmationOptions confirmation =
            FindFirstObjectByType<gestionConfirmationOptions>();
        if (confirmation != null)
            confirmation.SauvegarderEtatActuel();
    }

    void Update()
    {
        if (!jeuActif) return;
        if (Keyboard.current == null) return;
        if (attenteAction) return;

        if (Keyboard.current.kKey.wasPressedThisFrame)
        {
            if (etatActuel == EtatJeu.EnJeu
                || etatActuel == EtatJeu.EnPause)
            {
                gestionPartie.Instance.Sauvegarder();
                Debug.Log("Sauvegarde rapide K");

                if (gestionChapitres.Instance != null)
                    gestionChapitres.Instance.SignalerAction(
                        "sauvegarde_faite");
            }
            return;
        }

        if (Keyboard.current.oKey.wasPressedThisFrame)
        {
            if (etatActuel == EtatJeu.EnJeu)
            {
                OuvrirOptionsDepuisJeu();

                if (gestionChapitres.Instance != null)
                    gestionChapitres.Instance.SignalerAction(
                        "options_ouvertes");
            }
            else if (etatActuel == EtatJeu.DansOptionsJeu)
                LancerActionAvecDelai(nameof(FermerOptionsVersJeu));
            return;
        }

        if (Keyboard.current.jKey.wasPressedThisFrame)
        {
            if (etatActuel == EtatJeu.EnJeu
                || etatActuel == EtatJeu.EnPause)
            {
                if (etatActuel == EtatJeu.EnPause)
                {
                    canvasMenuPause.SetActive(false);
                    BloquerCanvasGroup(groupeMenuPause);
                }
                OuvrirJournal();
            }
            else if (etatActuel == EtatJeu.DansJournal)
                LancerActionAvecDelai(nameof(FermerJournal));
            return;
        }

        if (Keyboard.current.iKey.wasPressedThisFrame)
        {
            if (etatActuel == EtatJeu.EnJeu
                || etatActuel == EtatJeu.EnPause)
            {
                if (etatActuel == EtatJeu.EnPause)
                {
                    canvasMenuPause.SetActive(false);
                    BloquerCanvasGroup(groupeMenuPause);
                }
                OuvrirInventaire();

                if (gestionChapitres.Instance != null)
                    gestionChapitres.Instance.SignalerAction(
                        "inventaire_ouvert");
            }
            else if (etatActuel == EtatJeu.DansInventaire)
                LancerActionAvecDelai(nameof(FermerInventaire));
            return;
        }

        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            if (etatActuel == EtatJeu.DansOptionsPause
                || etatActuel == EtatJeu.DansOptionsJeu)
                LancerActionAvecDelai(
                    nameof(AfficherConfirmationReinitialisation));
            return;
        }

        if (Keyboard.current.enterKey.wasPressedThisFrame)
        {
            if (etatActuel == EtatJeu.DansOptionsPause
                || etatActuel == EtatJeu.DansOptionsJeu)
            {
                UnityEngine.EventSystems.EventSystem.current
                    .SetSelectedGameObject(null);

                gestionConfirmationOptions confirmation =
                    FindFirstObjectByType<gestionConfirmationOptions>();
                if (confirmation != null)
                    confirmation.ConfirmerChangements();
            }
            return;
        }

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            // ESPACE : toggle verrouillage du curseur OS.
            // Permet au joueur de "lacher" la souris pour cliquer sur
            // une UI hors-jeu (Editor, autre fenetre), ou de la
            // reverrouiller au centre pour continuer le gameplay.
            if (etatActuel == EtatJeu.EnJeu)
            {
                if (sourisVerrouillee)
                    DeverrouillerSouris();
                else
                    VerrouillerSouris();
            }
        }

        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            if (etatActuel == EtatJeu.EnJeu
                || etatActuel == EtatJeu.EnPause)
                AfficherConfirmationRetourMenu();
            return;
        }

        // === Gestion ESC : short press (contextuel) vs long press (menu pause) ===
        // Quand ESC est appuye, on enregistre l'instant. Si maintenu
        // pendant escDureeLongPress (1s), on ouvre le menu pause. Si
        // relache avant, on execute l'action contextuelle (skip
        // dialogue, fermer tuile, etc.).
        //
        // EXCEPTION : si une enigme est active (zoneLancementEnigme),
        // Esc est reserve a la sortie d'enigme. On ne touche pas au
        // tracking pour eviter d'ouvrir le menu pause ou de faire une
        // action contextuelle indesirable.
        if (zoneLancementEnigme.EnigmeActive) return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            escAppuyeDepuis = Time.unscaledTime;
            escLongPressDeclenche = false;
            escMenuOuvertParLongPress = false;
        }
        else if (Keyboard.current.escapeKey.isPressed
            && escAppuyeDepuis > 0
            && !escLongPressDeclenche)
        {
            // Press maintenu : si la duree depasse le seuil, on
            // declenche le menu pause (uniquement si on est en jeu).
            if (Time.unscaledTime - escAppuyeDepuis >= escDureeLongPress)
            {
                escLongPressDeclenche = true;
                if (etatActuel == EtatJeu.EnJeu)
                {
                    escMenuOuvertParLongPress = true;
                    MettreEnPause();
                }
                // Sinon : on etait deja dans un autre etat (EnPause,
                // DansJournal, etc.). Le long press ne fait rien, mais
                // on laisse escMenuOuvertParLongPress = false pour que
                // le release puisse declencher l'action contextuelle
                // (ex : ESC pour fermer le menu pause).
            }
        }
        else if (Keyboard.current.escapeKey.wasReleasedThisFrame
            && escAppuyeDepuis > 0)
        {
            // Press relache : si le long press a effectivement OUVERT
            // le menu pause, ne rien refaire. Sinon (short press OU
            // long press hors EnJeu), executer l'action contextuelle.
            if (!escMenuOuvertParLongPress)
            {
                FaireActionEscContextuelle();
            }
            escAppuyeDepuis = -1f;
            escLongPressDeclenche = false;
            escMenuOuvertParLongPress = false;
        }
    }

    /// <summary>
    /// Execute l'action contextuelle d'un ESC court (release avant 1s).
    /// Si on est dans un menu (hors EnJeu), priorite est de fermer ce
    /// menu — le dialogue/tuile sous-jacents sont deja figes par
    /// Time.timeScale=0 et n'ont pas besoin d'etre skippes. Sinon
    /// (EnJeu), priorite : skip dialogue > fermer tuile tuto > ouvrir
    /// menu pause.
    /// </summary>
    private void FaireActionEscContextuelle()
    {
        // Si on est deja dans un menu (EnPause, DansJournal, etc.),
        // ESC ferme ce menu. C'est la priorite absolue : sans ce
        // check, un ESC en EnPause irait skipper le dialogue en
        // arriere-plan (DialogueTuto.DialogueEnCours reste true meme
        // si Time.timeScale=0) et le menu pause ne se fermerait jamais.
        if (etatActuel != EtatJeu.EnJeu)
        {
            switch (etatActuel)
            {
                case EtatJeu.EnPause:
                    LancerActionAvecDelai(nameof(Reprendre));
                    break;
                case EtatJeu.DansOptionsPause:
                    LancerActionAvecDelai(nameof(RetourAuMenuPause));
                    break;
                case EtatJeu.DansOptionsJeu:
                    LancerActionAvecDelai(nameof(FermerOptionsVersJeu));
                    break;
                case EtatJeu.DansJournal:
                    LancerActionAvecDelai(nameof(FermerJournal));
                    break;
                case EtatJeu.DansCredits:
                    LancerActionAvecDelai(nameof(FermerCredits));
                    break;
                case EtatJeu.DansTuto:
                    LancerActionAvecDelai(nameof(FermerTuto));
                    break;
                case EtatJeu.DansInventaire:
                    LancerActionAvecDelai(nameof(FermerInventaire));
                    break;
                case EtatJeu.ConfirmationRetourMenu:
                    LancerActionAvecDelai(
                        nameof(FermerConfirmationRetourMenu));
                    break;
                case EtatJeu.ConfirmationQuitter:
                    LancerActionAvecDelai(
                        nameof(FermerConfirmationQuitter));
                    break;
                case EtatJeu.ConfirmationReinitialisation:
                    LancerActionAvecDelai(
                        nameof(FermerConfirmationReinitialisation));
                    break;
            }
            return;
        }

        // === EnJeu ===
        // Priorite #1 : si un dialogue est en train de defiler,
        // ESC saute la ligne courante (le joueur a deja lu).
        if (DialogueTuto.DialogueActif != null
            && DialogueTuto.DialogueActif.DialogueEnCours)
        {
            DialogueTuto.DialogueActif.SkipLigneCourante();
            return;
        }

        // Priorite #2 : si un tuto (tuile) est affiche, ESC le ferme.
        if (gestionChapitres.Instance != null
            && gestionChapitres.Instance.TutoEstAffiche())
        {
            gestionChapitres.Instance.FermerTutoParEsc();
            return;
        }

        // EnJeu sans rien d'autre a faire : ouvrir le menu pause.
        // (Le long press 1s a son propre chemin via MettreEnPause()
        // directe dans Update — ce chemin court est un fallback si
        // le joueur fait un press court sans dialogue ni tuile.)
        MettreEnPause();
    }

    void LateUpdate()
    {
        if (!jeuActif) return;

        if (etatActuel == EtatJeu.DansOptionsPause
            || etatActuel == EtatJeu.DansOptionsJeu)
        {
            GameObject selectionne =
                UnityEngine.EventSystems.EventSystem.current
                    .currentSelectedGameObject;

            if (selectionne == null) return;

            if (selectionne.GetComponent<Toggle>() != null
                || selectionne.GetComponent<TMPro.TMP_Dropdown>() != null
                || selectionne.GetComponent<Slider>() != null)
            {
                UnityEngine.EventSystems.EventSystem.current
                    .SetSelectedGameObject(null);
            }
        }
    }

    private void LancerActionAvecDelai(string methode)
    {
        attenteAction = true;
        StartCoroutine(ExecuterApresDelai(methode));
    }

    private IEnumerator ExecuterApresDelai(string methode)
    {
        yield return new WaitForSecondsRealtime(delaiEffets);
        Invoke(methode, 0f);
        attenteAction = false;
    }

    // ===== BOUTONS HUD =====

    public void BoutonJournal()
    {
        if (!jeuActif || attenteAction) return;

        if (etatActuel == EtatJeu.EnJeu
            || etatActuel == EtatJeu.EnPause)
        {
            if (etatActuel == EtatJeu.EnPause)
            {
                canvasMenuPause.SetActive(false);
                BloquerCanvasGroup(groupeMenuPause);
            }
            OuvrirJournal();
        }
        else if (etatActuel == EtatJeu.DansJournal)
            LancerActionAvecDelai(nameof(FermerJournal));
    }

    public void BoutonOptions()
    {
        if (!jeuActif || attenteAction) return;

        if (etatActuel == EtatJeu.EnJeu)
            OuvrirOptionsDepuisJeu();
        else if (etatActuel == EtatJeu.EnPause)
            OuvrirOptionsDePause();
        else if (etatActuel == EtatJeu.DansOptionsJeu
            || etatActuel == EtatJeu.DansOptionsPause)
            LancerActionAvecDelai(nameof(FermerOptionsVersJeu));
    }

    public void BoutonInventaire()
    {
        if (!jeuActif || attenteAction) return;

        if (etatActuel == EtatJeu.EnJeu
            || etatActuel == EtatJeu.EnPause)
        {
            if (etatActuel == EtatJeu.EnPause)
            {
                canvasMenuPause.SetActive(false);
                BloquerCanvasGroup(groupeMenuPause);
            }
            OuvrirInventaire();
        }
        else if (etatActuel == EtatJeu.DansInventaire)
            LancerActionAvecDelai(nameof(FermerInventaire));
    }

    // ===== PAUSE =====

    public void MettreEnPause()
    {
        etatActuel = EtatJeu.EnPause;
        Time.timeScale = 0f;
        DeverrouillerSouris();

        CacherContenuHud();

        canvasMenuPause.SetActive(true);
        groupeMenuPause.alpha = 1f;
        DebloquerCanvasGroup(groupeMenuPause);

        if (gestionFlou != null)
            gestionFlou.ActiverFlou();

        // Si un dialogue est en cours, le mettre en pause aussi.
        // Sinon il continuerait a defiler en arriere-plan (Time.unscaled)
        // et la voix continuerait a jouer (AudioSource n'est pas affecte
        // par Time.timeScale=0).
        if (DialogueTuto.DialogueActif != null)
            DialogueTuto.DialogueActif.MettreEnPauseExterne();

        // Pause aussi le bandeau info en cours d'affichage (il
        // reapparaitra avec le temps restant a la fermeture du menu).
        gestionBandeauInfo.MettreEnPauseExterne();

        // Signal pour le tuto "menu_pause" (idActionRequise =
        // "menu_pause_ouvert"). Ferme la tuile si elle est affichee.
        if (gestionChapitres.Instance != null)
            gestionChapitres.Instance.SignalerAction("menu_pause_ouvert");
    }

    public void Reprendre()
    {
        etatActuel = EtatJeu.EnJeu;
        Time.timeScale = 1f;
        VerrouillerSouris();

        BloquerCanvasGroup(groupeMenuPause);

        StartCoroutine(DesactiverApresDelai(
            canvasMenuPause, delaiEffets));
        StartCoroutine(MontrerContenuHudApresDelai(delaiEffets));

        if (gestionFlou != null)
            gestionFlou.DesactiverFlou();

        // Reprendre le dialogue si on l'avait mis en pause.
        if (DialogueTuto.DialogueActif != null)
            DialogueTuto.DialogueActif.ReprendreExterne();

        // Reprendre le bandeau info qu'on avait mis en pause externe.
        gestionBandeauInfo.ReprendreExterne();
    }

    // ===== OPTIONS DEPUIS PAUSE =====

    public void OuvrirOptionsDePause()
    {
        etatActuel = EtatJeu.DansOptionsPause;

        SauvegarderEtatOptions();

        groupeMenuPause.alpha = 0f;
        BloquerCanvasGroup(groupeMenuPause);
        canvasMenuPause.SetActive(false);

        canvasOptions.SetActive(true);
        groupeOptions.alpha = 1f;
        DebloquerOptions();
    }

    public void RetourAuMenuPause()
    {
        etatActuel = EtatJeu.EnPause;

        AnnulerOptionsNonConfirmees();

        groupeOptions.blocksRaycasts = false;
        if (groupeOngletsOptions != null)
            groupeOngletsOptions.blocksRaycasts = false;

        StartCoroutine(DesactiverApresDelai(
            canvasOptions, delaiEffets));
        StartCoroutine(AfficherMenuPauseApresDelai(
            delaiEffets + 0.05f));
    }

    private IEnumerator AfficherMenuPauseApresDelai(float delai)
    {
        yield return new WaitForSecondsRealtime(delai);

        canvasMenuPause.SetActive(true);
        groupeMenuPause.alpha = 1f;
        DebloquerCanvasGroup(groupeMenuPause);
    }

    // ===== OPTIONS DEPUIS JEU =====

    private void OuvrirOptionsDepuisJeu()
    {
        etatActuel = EtatJeu.DansOptionsJeu;
        Time.timeScale = 0f;
        DeverrouillerSouris();

        CacherContenuHud();
        SauvegarderEtatOptions();

        canvasOptions.SetActive(true);
        groupeOptions.alpha = 1f;
        DebloquerOptions();

        if (gestionFlou != null)
            gestionFlou.ActiverFlou();

        // Met en pause un dialogue eventuellement en cours (meme logique
        // que MettreEnPause). Sans ca, le dialogue continuerait pendant
        // que le menu options est affiche.
        if (DialogueTuto.DialogueActif != null)
            DialogueTuto.DialogueActif.MettreEnPauseExterne();

        // Pause aussi le bandeau info (temps gele, reapparait apres).
        gestionBandeauInfo.MettreEnPauseExterne();
    }

    private void FermerOptionsVersJeu()
    {
        etatActuel = EtatJeu.EnJeu;
        Time.timeScale = 1f;
        VerrouillerSouris();

        AnnulerOptionsNonConfirmees();

        groupeOptions.blocksRaycasts = false;
        if (groupeOngletsOptions != null)
            groupeOngletsOptions.blocksRaycasts = false;

        StartCoroutine(DesactiverApresDelai(
            canvasOptions, delaiEffets));
        StartCoroutine(MontrerContenuHudApresDelai(delaiEffets));

        if (gestionFlou != null)
            gestionFlou.DesactiverFlou();

        // Reprendre le dialogue si on l'avait mis en pause.
        if (DialogueTuto.DialogueActif != null)
            DialogueTuto.DialogueActif.ReprendreExterne();

        // Reprendre le bandeau info qu'on avait mis en pause externe.
        gestionBandeauInfo.ReprendreExterne();
    }

    // ===== TUTO =====

    public void AfficherTuto()
    {
        etatActuel = EtatJeu.DansTuto;

        ensembleTuileTutoEtBoutonRetour.SetActive(true);
        if (groupeTuto != null)
        {
            groupeTuto.alpha = 1f;
            DebloquerCanvasGroup(groupeTuto);
        }
    }

    public void FermerTuto()
    {
        etatActuel = EtatJeu.EnJeu;

        if (groupeTuto != null)
            BloquerCanvasGroup(groupeTuto);

        StartCoroutine(DesactiverApresDelai(
            ensembleTuileTutoEtBoutonRetour, delaiEffets));
    }

    // ===== JOURNAL =====

    public void OuvrirJournal()
    {
        etatAvantJournal = etatActuel;
        etatActuel = EtatJeu.DansJournal;
        Time.timeScale = 0f;
        DeverrouillerSouris();

        CacherContenuHud();

        canvasJournal.SetActive(true);
        groupeJournal.alpha = 1f;
        DebloquerCanvasGroup(groupeJournal);

        if (gestionFlou != null)
            gestionFlou.ActiverFlou();

        // Met en pause un dialogue en cours (le journal cache l'ecran).
        if (DialogueTuto.DialogueActif != null)
            DialogueTuto.DialogueActif.MettreEnPauseExterne();

        // Pause aussi le bandeau info (temps gele, reapparait apres).
        gestionBandeauInfo.MettreEnPauseExterne();
    }

    public void FermerJournal()
    {
        BloquerCanvasGroup(groupeJournal);

        StartCoroutine(DesactiverApresDelai(
            canvasJournal, delaiEffets));

        // D�tection d'action - Tutoriel (Fermer journal)
        if (gestionChapitres.Instance != null)
            gestionChapitres.Instance.SignalerAction("jdb_ouvert");

        if (etatAvantJournal == EtatJeu.EnPause)
        {
            etatActuel = EtatJeu.EnPause;
            StartCoroutine(
                AfficherMenuPauseApresDelai(delaiEffets + 0.05f));
            // On reste en EnPause donc dialogue reste en pause aussi.
            // ReprendreExterne sera appele dans Reprendre() plus tard.
        }
        else
        {
            etatActuel = EtatJeu.EnJeu;
            Time.timeScale = 1f;
            VerrouillerSouris();
            StartCoroutine(MontrerContenuHudApresDelai(delaiEffets));

            if (gestionFlou != null)
                gestionFlou.DesactiverFlou();

            // Reprendre le dialogue qu'on avait mis en pause.
            if (DialogueTuto.DialogueActif != null)
                DialogueTuto.DialogueActif.ReprendreExterne();

            // Reprendre le bandeau info qu'on avait mis en pause externe.
            gestionBandeauInfo.ReprendreExterne();
        }
    }

    // ===== INVENTAIRE =====

    private void OuvrirInventaire()
    {
        etatAvantInventaire = etatActuel;
        etatActuel = EtatJeu.DansInventaire;

        if (Time.timeScale == 0f)
            Time.timeScale = 1f;

        // Fermer toute tuile explicative d'enigme active : sans ca, elle
        // capture les raycasts UI au-dessus des slots d'inventaire et le
        // joueur ne peut pas selectionner ses objets.
        zoneLancementEnigme.FermerTuilesActives();

        DeverrouillerSouris();
        MontrerContenuHud();

        // Cacher le pointeur central : sans ca, le joueur voit deux
        // pointeurs simultanement (le reticule fixe au centre + le
        // curseur Windows qui bouge librement), ce qui est trompeur
        // surtout quand l'icone flottante suit le curseur Windows.
        if (pointeurCentre != null) pointeurCentre.SetActive(false);

        ensembleMenuInventaire.SetActive(true);

        if (gestionFlou != null)
            gestionFlou.DesactiverFlou();

    }

    public void FermerInventaire()
    {
        ensembleMenuInventaire.SetActive(false);

        // Reafficher le pointeur central (cache pendant l'inventaire).
        if (pointeurCentre != null) pointeurCentre.SetActive(true);

        // D�tection d'action - Tutoriel (Fermer inventaire)
        if (gestionChapitres.Instance != null)
            gestionChapitres.Instance.SignalerAction("objet_utilise");


        if (etatAvantInventaire == EtatJeu.EnPause)
        {
            etatActuel = EtatJeu.EnPause;
            Time.timeScale = 0f;
            CacherContenuHud();

            canvasMenuPause.SetActive(true);
            groupeMenuPause.alpha = 1f;
            DebloquerCanvasGroup(groupeMenuPause);

            if (gestionFlou != null)
                gestionFlou.ActiverFlou();
        }
        else
        {
            etatActuel = EtatJeu.EnJeu;
            VerrouillerSouris();
        }
    }

    // ===== CREDITS =====

    public void OuvrirCredits()
    {
        etatActuel = EtatJeu.DansCredits;

        BloquerCanvasGroup(groupeMenuPause);

        canvasCredits.SetActive(true);
        groupeCredits.alpha = 1f;
        DebloquerCanvasGroup(groupeCredits);
    }

    public void FermerCredits()
    {
        etatActuel = EtatJeu.EnPause;

        BloquerCanvasGroup(groupeCredits);

        StartCoroutine(DesactiverApresDelai(
            canvasCredits, delaiEffets));
        StartCoroutine(AfficherMenuPauseApresDelai(
            delaiEffets + 0.05f));
    }

    // ===== CONFIRMATION RETOUR MENU PRINCIPAL =====

    public void AfficherConfirmationRetourMenu()
    {
        etatAvantConfirmation = etatActuel;

        if (etatActuel == EtatJeu.EnJeu)
        {
            Time.timeScale = 0f;
            CacherContenuHud();
            DeverrouillerSouris();

            // Mettre en pause un dialogue eventuellement en cours
            // (si on vient de EnJeu, le dialogue n'est pas encore en
            // pause externe). Si on vient de EnPause, le dialogue est
            // deja en pause par MettreEnPause() precedent.
            if (DialogueTuto.DialogueActif != null)
                DialogueTuto.DialogueActif.MettreEnPauseExterne();

            // Pause aussi le bandeau info (temps gele).
            gestionBandeauInfo.MettreEnPauseExterne();
        }

        if (etatActuel == EtatJeu.EnPause)
        {
            BloquerCanvasGroup(groupeMenuPause);
            canvasMenuPause.SetActive(false);
        }

        etatActuel = EtatJeu.ConfirmationRetourMenu;

        canvasRetournerMenuPrincipal.SetActive(true);
        groupeRetournerMenuPrincipal.alpha = 1f;
        DebloquerCanvasGroup(groupeRetournerMenuPrincipal);

        if (gestionFlou != null)
            gestionFlou.ActiverFlou();
    }

    public void ConfirmerRetourMenuPrincipal()
    {
        Debug.Log("[RetourMenu] 1. Methode appelee");

        // === Logique overlay menu (Q -> Oui dans n'importe quelle scene) ===
        // On NE charge PAS scene0_tuto. On affiche le canvas_menu_principal
        // par-dessus le decor de la scene courante. Le joueur peut alors :
        //  - Continuer : on un-freeze la scene courante (pas de LoadScene
        //    si la sauvegarde correspond a la scene actuelle)
        //  - Nouvelle partie : LoadScene scene0_tuto (recommence du debut)
        //  - Quitter : Application.Quit

        // Etat machine : on passe en EnPause pour que Update sorte tot
        // (jeuActif=false suffit deja mais EtatJeu.EnPause est plus clair
        // semantiquement et coherent avec le comportement attendu).
        etatActuel = EtatJeu.EnPause;
        jeuActif = false;
        attenteAction = false;
        DeverrouillerSouris();

        // FREEZE de la scene : Time.timeScale = 0 gele animations, physique,
        // coroutines normales, AudioSource (sauf ceux configures ignoreList-
        // enerPause). Cela suffit a "arreter" visuellement la scene.
        Time.timeScale = 0f;
        Debug.Log("[RetourMenu] Time.timeScale = "
            + Time.timeScale + " (devrait etre 0)");

        // Bloque explicitement le mouvement du joueur via les composants.
        // Cherche dans toute la scene (inclus inactifs).
        var pm = FindFirstObjectByType<PlayerMovement>(
            FindObjectsInactive.Include);
        if (pm != null)
        {
            pm.enabled = false;
            Debug.Log("[RetourMenu] PlayerMovement desactive sur "
                + pm.gameObject.name);
        }
        else
        {
            Debug.LogWarning("[RetourMenu] PlayerMovement NON trouve");
        }
        var pbr = FindFirstObjectByType<PlayerBodyRotation>(
            FindObjectsInactive.Include);
        if (pbr != null)
        {
            pbr.enabled = false;
            Debug.Log("[RetourMenu] PlayerBodyRotation desactive sur "
                + pbr.gameObject.name);
        }
        else
        {
            Debug.LogWarning("[RetourMenu] PlayerBodyRotation NON trouve");
        }
        // Bloque la rotation cam via Cinemachine InputAxisController
        var inputAxis = FindFirstObjectByType<
            Unity.Cinemachine.CinemachineInputAxisController>(
            FindObjectsInactive.Include);
        if (inputAxis != null)
        {
            inputAxis.enabled = false;
            Debug.Log("[RetourMenu] InputAxisController desactive sur "
                + inputAxis.gameObject.name);
        }
        else
        {
            Debug.LogWarning("[RetourMenu] InputAxisController NON trouve");
        }

        // Mettre tous les AudioSource de la scene en pause (le gameplay).
        // L'AudioSource n'est PAS affecte par Time.timeScale, donc les
        // musiques et SFX continuent. On les pause individuellement.
        var tousAudios = FindObjectsByType<AudioSource>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var src in tousAudios)
        {
            if (src != null && src.isPlaying)
                src.Pause();
        }
        Debug.Log("[RetourMenu] " + tousAudios.Length
            + " AudioSources mises en pause.");

        // Fermer toutes les fenetres UI ouvertes (null-safe). On laisse
        // canvasHud actif pour que d'eventuels elements en arriere
        // restent neutralises ; on cache son contenu via CacherContenuHud.
        if (canvasRetournerMenuPrincipal != null)
            canvasRetournerMenuPrincipal.SetActive(false);
        if (canvasMenuPause != null) canvasMenuPause.SetActive(false);
        if (canvasOptions != null) canvasOptions.SetActive(false);
        if (canvasJournal != null) canvasJournal.SetActive(false);
        if (canvasCredits != null) canvasCredits.SetActive(false);
        if (canvasConfirmerReinitialisation != null)
            canvasConfirmerReinitialisation.SetActive(false);
        if (ensembleMenuInventaire != null)
            ensembleMenuInventaire.SetActive(false);
        CacherContenuHud();

        // Verification null-safe : si canvasMenu manque, on log et sort.
        if (canvasMenu == null)
        {
            Debug.LogError("[RetourMenu] canvasMenu introuvable dans " +
                "cette scene. Ajoute 'canvas_menu_principal' au " +
                "GameObject --UI et rattache la ref sur gestionInputsJeu.");
            return;
        }

        // Activer TOUS les parents inactifs jusqu'a la racine (sinon
        // SetActive(true) sur le canvas est inutile si un ancetre est
        // desactive — cas observe sur ensembles UI).
        Transform tr = canvasMenu.transform;
        while (tr != null)
        {
            if (!tr.gameObject.activeSelf)
                tr.gameObject.SetActive(true);
            tr = tr.parent;
        }
        canvasMenu.SetActive(true);

        // On NE touche PAS a la position, ni au scale, ni au RenderMode
        // du canvas_menu_principal. On l'active simplement. C'est la
        // camera courante qui doit le filmer (camera_virtuelle_menu_
        // principal en scene0_tuto). Si une scene n'a pas cette camera,
        // c'est a toi de la copier dans la scene pour que le canvas
        // soit visible.

        if (groupeMenu != null)
        {
            groupeMenu.alpha = 1f;
            DebloquerCanvasGroup(groupeMenu);
        }

        // Switcher la priorite Cinemachine vers vcamMenu pour que la
        // Main Camera prenne sa position et voie le canvas_menu_principal.
        StartCoroutine(CutInstantaneVersMenu());

        // CRUCIAL : le Brain a IgnoreTimeScale=false par defaut. Donc
        // avec Time.timeScale=0 (overlay menu), il ne fait plus aucun
        // blend entre vcam. Resultat : si le joueur clique Options
        // depuis le menu overlay, le switch vcamMenu->vcamOptionsCredits
        // ne produit aucune transition visible. On active IgnoreTimeScale
        // pour que les blends fonctionnent malgre le freeze de la scene.
        if (cinemachineBrain != null)
            cinemachineBrain.IgnoreTimeScale = true;

        Debug.Log("[RetourMenu] 2. Canvas menu active. ActiveSelf=" +
            canvasMenu.activeSelf + " ActiveInHierarchy=" +
            canvasMenu.activeInHierarchy);

        // Mettre a jour le bouton Continuer (active si sauvegarde existe)
        var transitions = GetComponent<gestionsTransitions>();
        if (transitions != null)
            transitions.MettreAJourBoutonContinuer();

        // Effet flou sur le decor 3D derriere le menu (comme menu pause)
        if (gestionFlou != null)
            gestionFlou.ActiverFlou();

        // Musique d'intro du menu (peut etre coupee par OnNouvellePartie
        // /OnContinuer si on switch de scene apres)
        if (gestionAudio.Instance != null)
            gestionAudio.Instance.JouerMusiquesIntro();

        Debug.Log("[RetourMenu] 3. Overlay menu actif, scene figee.");
    }

    /// <summary>
    /// Restaure le jeu apres avoir affiche le menu overlay via
    /// ConfirmerRetourMenuPrincipal. Appele par gestionsTransitions.
    /// OnContinuer quand la sauvegarde correspond a la scene actuelle :
    /// on un-freeze tout sans LoadScene. Si on a besoin de switcher de
    /// scene (OnNouvellePartie, ou Continuer vers une autre scene), le
    /// LoadScene s'en charge dans gestionsTransitions.
    /// </summary>
    public void ReprendreJeuApresMenuOverlay()
    {
        Debug.Log("[ReprendreOverlay] Un-freeze de la scene.");

        // Un-freeze
        Time.timeScale = 1f;

        // Restaurer IgnoreTimeScale du Brain a false (etat normal en
        // jeu). On avait mis a true dans ConfirmerRetourMenuPrincipal
        // pour que les blends marchent malgre timeScale=0.
        if (cinemachineBrain != null)
            cinemachineBrain.IgnoreTimeScale = false;

        // Reactiver les composants du joueur
        var pm = FindFirstObjectByType<PlayerMovement>(
            FindObjectsInactive.Include);
        if (pm != null) pm.enabled = true;
        var pbr = FindFirstObjectByType<PlayerBodyRotation>(
            FindObjectsInactive.Include);
        if (pbr != null) pbr.enabled = true;
        var inputAxis = FindFirstObjectByType<
            Unity.Cinemachine.CinemachineInputAxisController>(
            FindObjectsInactive.Include);
        if (inputAxis != null) inputAxis.enabled = true;

        // Relancer toutes les AudioSources qui etaient en pause
        var tousAudios = FindObjectsByType<AudioSource>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var src in tousAudios)
        {
            if (src != null) src.UnPause();
        }

        // Cacher le menu overlay
        if (canvasMenu != null) canvasMenu.SetActive(false);

        // CUT INSTANTANE vers la vcam Jeu (pas un blend de 2s du Brain).
        // Sans ca, on voit la camera glisser doucement de vcamMenu a
        // vcamJeu pendant 2 secondes, alors qu'on veut un retour immediat
        // au gameplay. Pattern identique a CutInstantaneVersJeu de
        // gestionsTransitions : disable Brain -> change priorities ->
        // reenable Brain (qui prend instantanement vcamJeu en position).
        StartCoroutine(CutInstantaneVersJeu());

        // Reactiver le HUD
        if (canvasHud != null) canvasHud.SetActive(true);
        MontrerContenuHud();

        // Desactiver le flou
        if (gestionFlou != null)
            gestionFlou.DesactiverFlou();

        // Reverrouiller la souris (mode gameplay)
        VerrouillerSouris();

        // Restaurer l'etat machine et reactiver les inputs gameplay
        etatActuel = EtatJeu.EnJeu;
        jeuActif = true;
        attenteAction = false;
    }

    /// <summary>
    /// Cut Cinemachine instantane vers vcamJeu (sans blend). Necessaire
    /// quand on reprend le jeu depuis le menu overlay : sinon le Brain
    /// fait un blend de DefaultBlend.Time (typiquement 2s) qui donne
    /// l'impression que le retour au jeu est lent.
    /// </summary>
    private System.Collections.IEnumerator CutInstantaneVersJeu()
    {
        if (cinemachineBrain == null)
        {
            // Fallback sans Brain : juste switcher les priorites.
            if (vcamJeu != null) vcamJeu.Priority = 50;
            if (vcamMenu != null) vcamMenu.Priority = 10;
            yield break;
        }
        cinemachineBrain.enabled = false;
        if (vcamJeu != null) vcamJeu.Priority = 50;
        if (vcamMenu != null) vcamMenu.Priority = 10;
        yield return null;  // attendre 1 frame pour que les priorites s'appliquent
        cinemachineBrain.enabled = true;
    }

    private IEnumerator CutInstantaneVersMenu()
    {
        // Null-safe : si vcamMenu ou vcamJeu manquent (cas scene2_usine
        // ou il n'y a pas de camera "menu principal"), on saute la
        // transition Cinemachine. Le canvas_menu_principal est en
        // ScreenSpace-Overlay donc il s'affiche par-dessus l'image de
        // la camera courante meme sans switch. Avant ce check, un NRE
        // sur vcamMenu.Priority bloquait toute la coroutine et le menu
        // ne s'affichait pas.
        if (vcamMenu == null || vcamJeu == null)
        {
            Debug.LogWarning("[CutInstantane] vcamMenu ou vcamJeu " +
                "introuvable -> on saute le switch Cinemachine. Le " +
                "canvas_menu_principal s'affichera par-dessus la " +
                "camera courante (ScreenSpace-Overlay).");
            yield break;
        }

        if (cinemachineBrain == null)
        {
            Debug.LogWarning(
                "[CutInstantane] Brain non assigne dans l'Inspector");
            vcamMenu.Priority = 30;
            vcamJeu.Priority = 10;
            yield break;
        }

        cinemachineBrain.enabled = false;

        vcamMenu.Priority = 30;
        vcamJeu.Priority = 10;

        yield return null;

        cinemachineBrain.enabled = true;
    }

    public void FermerConfirmationRetourMenu()
    {
        etatActuel = etatAvantConfirmation;

        BloquerCanvasGroup(groupeRetournerMenuPrincipal);

        if (etatActuel == EtatJeu.EnJeu)
        {
            Time.timeScale = 1f;
            VerrouillerSouris();
            StartCoroutine(DesactiverApresDelai(
                canvasRetournerMenuPrincipal, delaiEffets));
            MontrerContenuHud();

            if (gestionFlou != null)
                gestionFlou.DesactiverFlou();

            // Reprendre le dialogue si on l'avait mis en pause.
            if (DialogueTuto.DialogueActif != null)
                DialogueTuto.DialogueActif.ReprendreExterne();

            // Reprendre le bandeau info qu'on avait mis en pause externe.
            gestionBandeauInfo.ReprendreExterne();
        }
        else if (etatActuel == EtatJeu.EnPause)
        {
            StartCoroutine(DesactiverApresDelai(
                canvasRetournerMenuPrincipal, delaiEffets));
            StartCoroutine(ActiverMenuPauseApresDelai());

            if (gestionFlou != null)
                gestionFlou.ActiverFlou();
            // On reste en EnPause donc le dialogue reste en pause.
        }
    }

    private IEnumerator ActiverMenuPauseApresDelai()
    {
        yield return new WaitForSecondsRealtime(delaiEffets);

        canvasMenuPause.SetActive(true);
        groupeMenuPause.alpha = 1f;
        DebloquerCanvasGroup(groupeMenuPause);
    }

    // ===== CONFIRMATION QUITTER =====

    public void AfficherConfirmationQuitter()
    {
        etatAvantConfirmation = etatActuel;
        etatActuel = EtatJeu.ConfirmationQuitter;

        BloquerCanvasGroup(groupeMenu);

        canvasQuitter.SetActive(true);
        groupeQuitter.alpha = 1f;
        DebloquerCanvasGroup(groupeQuitter);

        if (gestionFlou != null)
            gestionFlou.ActiverFlou();
    }

    public void ConfirmerQuitter()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void FermerConfirmationQuitter()
    {
        etatActuel = etatAvantConfirmation;

        BloquerCanvasGroup(groupeQuitter);
        DebloquerCanvasGroup(groupeMenu);

        StartCoroutine(DesactiverApresDelai(
            canvasQuitter, delaiEffets));
    }

    // ===== CONFIRMATION REINITIALISATION =====

    public void AfficherConfirmationReinitialisation()
    {
        etatAvantReinitialisation = etatActuel;
        etatActuel = EtatJeu.ConfirmationReinitialisation;

        BloquerOptions();

        canvasConfirmerReinitialisation.SetActive(true);
        groupeConfirmerReinitialisation.alpha = 1f;
        DebloquerCanvasGroup(groupeConfirmerReinitialisation);

        if (gestionFlou != null)
            gestionFlou.ActiverFlou();
    }

    public void ConfirmerReinitialisation()
    {
        etatActuel = etatAvantReinitialisation;

        BloquerCanvasGroup(groupeConfirmerReinitialisation);
        DebloquerOptions();

        StartCoroutine(DesactiverApresDelai(
            canvasConfirmerReinitialisation, delaiEffets));

        ReinitialiserOngletActif();
        SauvegarderEtatOptions();
    }

    public void FermerConfirmationReinitialisation()
    {
        etatActuel = etatAvantReinitialisation;

        BloquerCanvasGroup(groupeConfirmerReinitialisation);
        DebloquerOptions();

        StartCoroutine(DesactiverApresDelai(
            canvasConfirmerReinitialisation, delaiEffets));
    }

    private void ReinitialiserOngletActif()
    {
        gestionOngletsOptions onglets =
            FindFirstObjectByType<gestionOngletsOptions>();
        if (onglets == null) return;

        string ongletActif = onglets.ObtenirOngletActif();

        switch (ongletActif)
        {
            case "sauvegarde":
                gestionPartie.Instance.SupprimerToutesSauvegardes();
                gestionTuileSauvegarde tuiles =
                    FindFirstObjectByType<gestionTuileSauvegarde>();
                if (tuiles != null)
                    tuiles.RafraichirTuiles();
                break;

            case "controle":
                gestionOptionsControle controle =
                    FindFirstObjectByType<gestionOptionsControle>();
                if (controle != null)
                    controle.Reinitialiser();
                break;

            case "audio":
                gestionOptionsAudio audio =
                    FindFirstObjectByType<gestionOptionsAudio>();
                if (audio != null)
                    audio.Reinitialiser();
                break;

            case "graphique":
                gestionOptionsGraphiques graphiques =
                    FindFirstObjectByType<gestionOptionsGraphiques>();
                if (graphiques != null)
                    graphiques.Reinitialiser();
                break;

            case "accessibilite":
                gestionOptionsAccessibilite accessibilite =
                    FindFirstObjectByType<gestionOptionsAccessibilite>();
                if (accessibilite != null)
                    accessibilite.Reinitialiser();
                break;
        }

        gestionConfirmationOptions confirmation =
            FindFirstObjectByType<gestionConfirmationOptions>();
        if (confirmation != null)
            confirmation.MarquerModification();
    }

    // ===== SAUVEGARDE =====

    public void SauvegarderPartie()
    {
        gestionPartie.Instance.Sauvegarder();
    }

    // ===== UTILITAIRES =====

    private IEnumerator DesactiverApresDelai(
        GameObject canvas, float delai)
    {
        yield return new WaitForSecondsRealtime(delai);
        canvas.SetActive(false);
    }

    private IEnumerator ActiverApresDelai(
        GameObject canvas, float delai)
    {
        yield return new WaitForSecondsRealtime(delai);
        canvas.SetActive(true);
    }

    private IEnumerator FadeCanvasGroup(
        CanvasGroup groupe, float cible)
    {
        while (Mathf.Abs(groupe.alpha - cible) > 0.01f)
        {
            groupe.alpha = Mathf.Lerp(
                groupe.alpha, cible,
                Time.unscaledDeltaTime * vitesseFade);
            yield return null;
        }
        groupe.alpha = cible;
    }

    private IEnumerator FadeCanvasGroupEtDesactiver(
        CanvasGroup groupe, GameObject canvas)
    {
        while (groupe.alpha > 0.01f)
        {
            groupe.alpha = Mathf.Lerp(
                groupe.alpha, 0f,
                Time.unscaledDeltaTime * vitesseFade);
            yield return null;
        }
        groupe.alpha = 0f;
        canvas.SetActive(false);
    }

    /// <summary>
    /// Ferme immédiatement toutes les fenêtres UI ouvertes (journal,
    /// inventaire, options, menu pause, crédits, confirmations) et
    /// remet le jeu dans un état neutre (timeScale = 1, état = EnJeu,
    /// flou désactivé). À appeler avant toute cinématique ou transition
    /// abrupte entre scènes.
    /// </summary>
    public void FermerToutesLesFenetres()
    {
        // Annuler les coroutines d'UI en cours (fades, désactivations
        // différées) pour éviter qu'un canvas se désactive/réactive
        // en plein milieu d'une cinématique.
        StopAllCoroutines();
        attenteAction = false;

        // Fermer tous les canvases
        if (canvasMenuPause != null)               canvasMenuPause.SetActive(false);
        if (canvasOptions != null)                 canvasOptions.SetActive(false);
        if (canvasJournal != null)                 canvasJournal.SetActive(false);
        if (canvasCredits != null)                 canvasCredits.SetActive(false);
        if (canvasRetournerMenuPrincipal != null)  canvasRetournerMenuPrincipal.SetActive(false);
        if (canvasQuitter != null)                 canvasQuitter.SetActive(false);
        if (canvasConfirmerReinitialisation != null) canvasConfirmerReinitialisation.SetActive(false);
        if (ensembleMenuInventaire != null)        ensembleMenuInventaire.SetActive(false);

        // Bloquer tous les CanvasGroups (empêche les clics résiduels)
        BloquerCanvasGroup(groupeMenuPause);
        BloquerCanvasGroup(groupeOptions);
        BloquerCanvasGroup(groupeJournal);
        BloquerCanvasGroup(groupeCredits);
        BloquerCanvasGroup(groupeRetournerMenuPrincipal);
        BloquerCanvasGroup(groupeQuitter);
        BloquerCanvasGroup(groupeConfirmerReinitialisation);

        // Restaurer le pointeur central si caché par l'inventaire
        if (pointeurCentre != null) pointeurCentre.SetActive(true);

        // Restaurer le timeScale (journal, options et pause le mettent à 0)
        Time.timeScale = 1f;

        // Désactiver le flou si actif
        if (gestionFlou != null)
            gestionFlou.DesactiverFlou();

        // Remettre l'état logique propre
        etatActuel = EtatJeu.EnJeu;

        Debug.Log("[gestionInputsJeu] FermerToutesLesFenetres — UI réinitialisée.");
    }

    public void ModeCinematique(bool actif)
    {
        if (actif)
        {
            // === Fermer toutes les fenetres UI ouvertes ===
            // (journal, inventaire, menu pause, options, confirmation
            // retour menu, etc.) avant d'entrer en mode cinematique.
            FermerToutesLesFenetres();

            // === Bloquer inputs gameplay, mais LIBERER le curseur ===
            // Curseur OS visible et libre durant la cinematique pour
            // permettre au joueur de cliquer un bouton "Passer" (skip).
            // Le reticule de raycast in-game (gestionPointeur) reste
            // desactive pour ne pas afficher la croix de visee.
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            var pointeur = FindObjectOfType<gestionPointeur>(true);
            if (pointeur != null)
                pointeur.gameObject.SetActive(false);
            var testP = FindObjectOfType<testPointeur>(true);
            if (testP != null)
                testP.enabled = false;

            jeuActif = false;
            attenteAction = false;

            // Remettre le temps a 1 si on etait en pause (sinon la
            // video player et certaines animations pourraient etre
            // figees pendant la cinematique).
            Time.timeScale = 1f;

            // === Nettoyer toute l'UI : la cinematique doit etre la
            // SEULE chose visible a l'ecran. ===

            // Fermer tous les menus eventuellement ouverts
            if (canvasMenuPause != null)
                canvasMenuPause.SetActive(false);
            if (canvasOptions != null)
                canvasOptions.SetActive(false);
            if (canvasJournal != null)
                canvasJournal.SetActive(false);
            if (canvasCredits != null)
                canvasCredits.SetActive(false);
            if (canvasRetournerMenuPrincipal != null)
                canvasRetournerMenuPrincipal.SetActive(false);
            if (canvasQuitter != null)
                canvasQuitter.SetActive(false);
            if (canvasConfirmerReinitialisation != null)
                canvasConfirmerReinitialisation.SetActive(false);
            if (ensembleMenuInventaire != null)
                ensembleMenuInventaire.SetActive(false);
            if (ensembleTuileTutoEtBoutonRetour != null)
                ensembleTuileTutoEtBoutonRetour.SetActive(false);

            // Cacher le HUD (alpha 0 + non-interactable, sans
            // desactiver le GameObject qui vit dans --DontDestroyOnLoad).
            CacherContenuHud();

            // Desactiver la vignette / flou eventuels
            if (gestionFlou != null)
                gestionFlou.DesactiverFlou();

            // Cacher les sous-titres si un dialogue les laissait
            // affiches (cas d'erreur ; normalement le dialogue est
            // fini avant la cinematique de fin).
            var sousTitre = FindFirstObjectByType<gestionSousTitre>(
                FindObjectsInactive.Include);
            if (sousTitre != null)
                sousTitre.MasquerSousTitre();

            // Mettre en pause un dialogue eventuellement encore en
            // cours (stoppe l'audio et fige la coroutine). Securite :
            // normalement aucun dialogue n'est actif a ce stade.
            if (DialogueTuto.DialogueActif != null)
                DialogueTuto.DialogueActif.MettreEnPauseExterne();

            // Effacer completement le bandeau info (vide la file et
            // masque l'UI). Pendant la cinematique, on ne veut aucun
            // bandeau en arriere-plan.
            gestionBandeauInfo.Effacer();

            // Reset etat : on est dans une "pseudo-EnJeu" sans inputs.
            etatActuel = EtatJeu.EnJeu;

            // Stopper toutes nos coroutines en attente (fade out de
            // menus, delais d'actions, etc.) pour eviter qu'elles
            // reactivent un canvas pendant la cinematique.
            StopAllCoroutines();
        }
        else
        {
            // Reverrouiller le curseur (etat gameplay normal apres
            // cinematique). Si le callback de fin charge une scene
            // avec menu, le menu de la nouvelle scene s'occupera de
            // delocker le curseur lui-meme.
            VerrouillerSouris();

            // Reactiver le reticule de raycast in-game
            var pointeur = FindObjectOfType<gestionPointeur>(true);
            if (pointeur != null)
                pointeur.gameObject.SetActive(true);
            var testP = FindObjectOfType<testPointeur>(true);
            if (testP != null)
                testP.enabled = true;

            // Reactiver inputs gameplay
            jeuActif = true;

            // Restaurer le HUD : CacherContenuHud() a mis alpha=0
            // pendant la cinematique. Sans cet appel, les sous-titres
            // et le reste du HUD restent invisibles après la cinématique
            // même si leurs GameObjects sont actifs (groupeContenuHud
            // est un CanvasGroup parent commun à tout le contenu HUD).
            MontrerContenuHud();
        }
    }

}