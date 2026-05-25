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
        // D�sactiver le menu principal si on n'est pas dans la sc�ne du menu
        if (SceneManager.GetActiveScene().name != "scene0_tuto")
        {
            if (canvasMenu != null)
                canvasMenu.SetActive(false);

            ActiverInputs();

            // Activer la cam�ra premi�re personne par d�faut
            if (vcamJeu != null)
                vcamJeu.Priority = 50;

            if (vcamMenu != null)
                vcamMenu.Priority = 10;
        }

        canvasMenuPause.SetActive(false);
        canvasRetournerMenuPrincipal.SetActive(false);
        canvasQuitter.SetActive(false);
        canvasConfirmerReinitialisation.SetActive(false);
        ensembleMenuInventaire.SetActive(false);
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
        groupeContenuHud.alpha = 1f;
        groupeContenuHud.interactable = true;
        groupeContenuHud.blocksRaycasts = true;

        foreach (ParticleSystem fx in fxHud)
        {
            if (fx != null)
                fx.Play();
        }

    }

    private void CacherContenuHud()
    {
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

        StopAllCoroutines();
        etatActuel = EtatJeu.EnJeu;
        jeuActif = false;
        attenteAction = false;
        Time.timeScale = 1f;
        DeverrouillerSouris();

        canvasRetournerMenuPrincipal.SetActive(false);
        canvasMenuPause.SetActive(false);
        canvasOptions.SetActive(false);
        canvasJournal.SetActive(false);
        canvasCredits.SetActive(false);
        canvasConfirmerReinitialisation.SetActive(false);
        ensembleMenuInventaire.SetActive(false);
        CacherContenuHud();
        canvasHud.SetActive(false);

        canvasMenu.SetActive(true);
        groupeMenu.alpha = 0f;
        DebloquerCanvasGroup(groupeMenu);

        Debug.Log("[RetourMenu] 2. Avant StartCoroutine cut");
        StartCoroutine(CutInstantaneVersMenu());
        Debug.Log("[RetourMenu] 3. Apres StartCoroutine cut");

        GetComponent<gestionsTransitions>()
            .MettreAJourBoutonContinuer();

        if (gestionFlou != null)
            gestionFlou.ActiverFlou();

        if (gestionAudio.Instance != null)
            gestionAudio.Instance.JouerMusiquesIntro();

        StartCoroutine(FadeCanvasGroup(groupeMenu, 1f));
    }

    private IEnumerator CutInstantaneVersMenu()
    {
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