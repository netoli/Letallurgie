using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Unity.Cinemachine;


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

    [Header("Effets HUD")]
    [SerializeField] private ParticleSystem[] fxHud;

    [Header("Cameras")]
    [SerializeField] private CinemachineCamera vcamMenu;
    [SerializeField] private CinemachineCamera vcamJeu;

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
    private EtatJeu etatAvantConfirmation = EtatJeu.EnJeu;
    private EtatJeu etatAvantJournal = EtatJeu.EnJeu;
    private EtatJeu etatAvantInventaire = EtatJeu.EnJeu;
    private EtatJeu etatAvantReinitialisation = EtatJeu.EnJeu;
    private bool jeuActif = false;
    private bool attenteAction = false;

    void Start()
    {
        canvasMenuPause.SetActive(false);
        canvasRetournerMenuPrincipal.SetActive(false);
        canvasQuitter.SetActive(false);
        canvasConfirmerReinitialisation.SetActive(false);
        ensembleMenuInventaire.SetActive(false);
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
        attenteAction = false;
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

    private System.Collections.IEnumerator MontrerContenuHudApresDelai(
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
            }
            return;
        }

        if (Keyboard.current.oKey.wasPressedThisFrame)
        {
            if (etatActuel == EtatJeu.EnJeu)
            {
                OuvrirOptionsDepuisJeu();
            }
            else if (etatActuel == EtatJeu.DansOptionsJeu)
            {
                LancerActionAvecDelai(nameof(FermerOptionsVersJeu));
            }
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
            {
                LancerActionAvecDelai(nameof(FermerJournal));
            }
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
            }
            else if (etatActuel == EtatJeu.DansInventaire)
            {
                LancerActionAvecDelai(nameof(FermerInventaire));
            }
            return;
        }

        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            if (etatActuel == EtatJeu.DansOptionsPause
                || etatActuel == EtatJeu.DansOptionsJeu)
            {
                LancerActionAvecDelai(
                    nameof(AfficherConfirmationReinitialisation));
            }
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
            if (etatActuel == EtatJeu.EnJeu)
                MettreEnPause();
        }

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            switch (etatActuel)
            {
                case EtatJeu.EnJeu:
                    AfficherConfirmationRetourMenu();
                    break;
                case EtatJeu.EnPause:
                    LancerActionAvecDelai(nameof(Reprendre));
                    break;
                case EtatJeu.DansOptionsPause:
                    LancerActionAvecDelai(
                        nameof(RetourAuMenuPause));
                    break;
                case EtatJeu.DansOptionsJeu:
                    LancerActionAvecDelai(
                        nameof(FermerOptionsVersJeu));
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
                    LancerActionAvecDelai(
                        nameof(FermerInventaire));
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
        }
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

    private System.Collections.IEnumerator ExecuterApresDelai(
        string methode)
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
        {
            LancerActionAvecDelai(nameof(FermerJournal));
        }
    }

    public void BoutonOptions()
    {
        if (!jeuActif || attenteAction) return;

        if (etatActuel == EtatJeu.EnJeu)
        {
            OuvrirOptionsDepuisJeu();
        }
        else if (etatActuel == EtatJeu.EnPause)
        {
            OuvrirOptionsDePause();
        }
        else if (etatActuel == EtatJeu.DansOptionsJeu
            || etatActuel == EtatJeu.DansOptionsPause)
        {
            LancerActionAvecDelai(nameof(FermerOptionsVersJeu));
        }
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
        {
            LancerActionAvecDelai(nameof(FermerInventaire));
        }
    }

    // ===== PAUSE =====

    public void MettreEnPause()
    {
        etatActuel = EtatJeu.EnPause;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        CacherContenuHud();

        canvasMenuPause.SetActive(true);
        groupeMenuPause.alpha = 1f;
        DebloquerCanvasGroup(groupeMenuPause);

        if (gestionFlou != null)
            gestionFlou.ActiverFlou();
    }

    public void Reprendre()
    {
        etatActuel = EtatJeu.EnJeu;
        Time.timeScale = 1f;

        BloquerCanvasGroup(groupeMenuPause);

        StartCoroutine(DesactiverApresDelai(
            canvasMenuPause, delaiEffets));
        StartCoroutine(MontrerContenuHudApresDelai(delaiEffets));

        if (gestionFlou != null)
            gestionFlou.DesactiverFlou();
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

    private System.Collections.IEnumerator
        AfficherMenuPauseApresDelai(float delai)
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

        CacherContenuHud();

        SauvegarderEtatOptions();

        canvasOptions.SetActive(true);
        groupeOptions.alpha = 1f;
        DebloquerOptions();

        if (gestionFlou != null)
            gestionFlou.ActiverFlou();
    }

    private void FermerOptionsVersJeu()
    {
        etatActuel = EtatJeu.EnJeu;
        Time.timeScale = 1f;

        AnnulerOptionsNonConfirmees();

        groupeOptions.blocksRaycasts = false;
        if (groupeOngletsOptions != null)
            groupeOngletsOptions.blocksRaycasts = false;

        StartCoroutine(DesactiverApresDelai(
            canvasOptions, delaiEffets));
        StartCoroutine(MontrerContenuHudApresDelai(delaiEffets));

        if (gestionFlou != null)
            gestionFlou.DesactiverFlou();
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
        // TEMPORAIRE : ACTIVER CURSEUR POUR TESTER LES INTERACTIONS AVEC LE JOURNAL
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        etatAvantJournal = etatActuel;
        etatActuel = EtatJeu.DansJournal;
        Time.timeScale = 0f;

        CacherContenuHud();

        canvasJournal.SetActive(true);
        groupeJournal.alpha = 1f;
        DebloquerCanvasGroup(groupeJournal);

        if (gestionFlou != null)
            gestionFlou.ActiverFlou();
    }

    public void FermerJournal()
    {
        BloquerCanvasGroup(groupeJournal);

        StartCoroutine(DesactiverApresDelai(
            canvasJournal, delaiEffets));

        if (etatAvantJournal == EtatJeu.EnPause)
        {
            etatActuel = EtatJeu.EnPause;
            StartCoroutine(
                AfficherMenuPauseApresDelai(delaiEffets + 0.05f));
        }
        else
        {
            etatActuel = EtatJeu.EnJeu;
            Time.timeScale = 1f;
            StartCoroutine(MontrerContenuHudApresDelai(delaiEffets));

            if (gestionFlou != null)
                gestionFlou.DesactiverFlou();

            // TEMPORAIRE : Désactiver curseur
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    // ===== INVENTAIRE =====

    private void OuvrirInventaire()
    {
        etatAvantInventaire = etatActuel;
        etatActuel = EtatJeu.DansInventaire;

        if (Time.timeScale == 0f)
            Time.timeScale = 1f;

        MontrerContenuHud();
        ensembleMenuInventaire.SetActive(true);

        if (gestionFlou != null)
            gestionFlou.DesactiverFlou();
    }

    private void FermerInventaire()
    {
        ensembleMenuInventaire.SetActive(false);

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
        StopAllCoroutines();
        etatActuel = EtatJeu.EnJeu;
        jeuActif = false;
        attenteAction = false;
        Time.timeScale = 1f;

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

        vcamMenu.Priority = 30;
        vcamJeu.Priority = 10;

        GetComponent<gestionsTransitions>()
            .MettreAJourBoutonContinuer();

        if (gestionFlou != null)
            gestionFlou.ActiverFlou();

        if (gestionAudio.Instance != null)
            gestionAudio.Instance.JouerMusiquesIntro();

        StartCoroutine(FadeCanvasGroup(groupeMenu, 1f));
    }

    public void FermerConfirmationRetourMenu()
    {
        etatActuel = etatAvantConfirmation;

        BloquerCanvasGroup(groupeRetournerMenuPrincipal);

        if (etatActuel == EtatJeu.EnJeu)
        {
            Time.timeScale = 1f;
            StartCoroutine(DesactiverApresDelai(
                canvasRetournerMenuPrincipal, delaiEffets));
            MontrerContenuHud();

            if (gestionFlou != null)
                gestionFlou.DesactiverFlou();
        }
        else if (etatActuel == EtatJeu.EnPause)
        {
            StartCoroutine(DesactiverApresDelai(
                canvasRetournerMenuPrincipal, delaiEffets));
            StartCoroutine(ActiverMenuPauseApresDelai());

            if (gestionFlou != null)
                gestionFlou.ActiverFlou();
        }
    }

    private System.Collections.IEnumerator
        ActiverMenuPauseApresDelai()
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
                Debug.Log("Toutes les sauvegardes supprimees");
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

    private System.Collections.IEnumerator DesactiverApresDelai(
        GameObject canvas, float delai)
    {
        yield return new WaitForSecondsRealtime(delai);
        canvas.SetActive(false);
    }

    private System.Collections.IEnumerator ActiverApresDelai(
        GameObject canvas, float delai)
    {
        yield return new WaitForSecondsRealtime(delai);
        canvas.SetActive(true);
    }

    private System.Collections.IEnumerator FadeCanvasGroup(
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

    private System.Collections.IEnumerator FadeCanvasGroupEtDesactiver(
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
}