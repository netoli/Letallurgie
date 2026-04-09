using UnityEngine;
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

    [Header("Cameras")]
    [SerializeField] private CinemachineCamera vcamMenu;
    [SerializeField] private CinemachineCamera vcamJeu;

    [Header("Parametres")]
    [SerializeField] private float vitesseFade;
    [SerializeField] private float delaiEffets;

    private enum EtatJeu
    {
        EnJeu,
        EnPause,
        DansOptionsPause,
        DansOptionsJeu,
        DansJournal,
        DansCredits,
        DansTuto,
        ConfirmationRetourMenu,
        ConfirmationQuitter
    }

    private EtatJeu etatActuel = EtatJeu.EnJeu;
    private EtatJeu etatAvantConfirmation = EtatJeu.EnJeu;
    private bool jeuActif = false;
    private bool attenteAction = false;

    void Start()
    {
        canvasMenuPause.SetActive(false);
        canvasRetournerMenuPrincipal.SetActive(false);
        canvasQuitter.SetActive(false);
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
        attenteAction = false;
        canvasHud.SetActive(true);
        StopAllCoroutines();
    }

    public void DesactiverInputs()
    {
        jeuActif = false;
    }

    void Update()
    {
        if (!jeuActif) return;
        if (Keyboard.current == null) return;
        if (attenteAction) return;

        // Sauvegarde rapide avec K
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

        // Ouvrir/fermer options avec O
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

        // Pause avec Espace
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            if (etatActuel == EtatJeu.EnJeu)
                MettreEnPause();
        }

        // Retour / ESC
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
                case EtatJeu.ConfirmationRetourMenu:
                    LancerActionAvecDelai(
                        nameof(FermerConfirmationRetourMenu));
                    break;
                case EtatJeu.ConfirmationQuitter:
                    LancerActionAvecDelai(
                        nameof(FermerConfirmationQuitter));
                    break;
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

    // ===== PAUSE =====

    public void MettreEnPause()
    {
        etatActuel = EtatJeu.EnPause;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        canvasHud.SetActive(false);

        canvasMenuPause.SetActive(true);
        groupeMenuPause.alpha = 1f;
        groupeMenuPause.interactable = true;
        groupeMenuPause.blocksRaycasts = true;
    }

    public void Reprendre()
    {
        etatActuel = EtatJeu.EnJeu;
        Time.timeScale = 1f;

        groupeMenuPause.interactable = false;
        groupeMenuPause.blocksRaycasts = false;

        StartCoroutine(DesactiverApresDelai(canvasMenuPause, delaiEffets));
        StartCoroutine(ActiverApresDelai(canvasHud, delaiEffets));
    }

    // ===== OPTIONS DEPUIS PAUSE =====

    public void OuvrirOptionsDePause()
    {
        etatActuel = EtatJeu.DansOptionsPause;

        groupeMenuPause.alpha = 0f;
        groupeMenuPause.interactable = false;
        groupeMenuPause.blocksRaycasts = false;
        canvasMenuPause.SetActive(false);

        canvasOptions.SetActive(true);
        groupeOptions.alpha = 1f;
        groupeOptions.interactable = true;
        groupeOptions.blocksRaycasts = true;
    }

    public void RetourAuMenuPause()
    {
        etatActuel = EtatJeu.EnPause;

        groupeOptions.interactable = false;
        groupeOptions.blocksRaycasts = false;

        StartCoroutine(DesactiverApresDelai(canvasOptions, delaiEffets));
        StartCoroutine(AfficherMenuPauseApresDelai(delaiEffets + 0.05f));
    }

    private System.Collections.IEnumerator AfficherMenuPauseApresDelai(
        float delai)
    {
        yield return new WaitForSecondsRealtime(delai);

        canvasMenuPause.SetActive(true);
        groupeMenuPause.alpha = 1f;
        groupeMenuPause.interactable = true;
        groupeMenuPause.blocksRaycasts = true;
    }

    // ===== OPTIONS DEPUIS JEU =====

    private void OuvrirOptionsDepuisJeu()
    {
        etatActuel = EtatJeu.DansOptionsJeu;
        Time.timeScale = 0f;
        canvasHud.SetActive(false);

        canvasOptions.SetActive(true);
        groupeOptions.alpha = 1f;
        groupeOptions.interactable = true;
        groupeOptions.blocksRaycasts = true;
    }

    private void FermerOptionsVersJeu()
    {
        etatActuel = EtatJeu.EnJeu;
        Time.timeScale = 1f;

        groupeOptions.interactable = false;
        groupeOptions.blocksRaycasts = false;

        StartCoroutine(DesactiverApresDelai(canvasOptions, delaiEffets));
        StartCoroutine(ActiverApresDelai(canvasHud, delaiEffets));
    }

    // ===== TUTO =====

    public void AfficherTuto()
    {
        etatActuel = EtatJeu.DansTuto;

        ensembleTuileTutoEtBoutonRetour.SetActive(true);
        if (groupeTuto != null)
        {
            groupeTuto.alpha = 1f;
            groupeTuto.interactable = true;
            groupeTuto.blocksRaycasts = true;
        }
    }

    public void FermerTuto()
    {
        etatActuel = EtatJeu.EnJeu;

        if (groupeTuto != null)
        {
            groupeTuto.interactable = false;
            groupeTuto.blocksRaycasts = false;
        }

        StartCoroutine(DesactiverApresDelai(
            ensembleTuileTutoEtBoutonRetour, delaiEffets));
    }

    // ===== JOURNAL =====

    public void OuvrirJournal()
    {
        etatActuel = EtatJeu.DansJournal;
        Time.timeScale = 0f;

        canvasHud.SetActive(false);

        canvasJournal.SetActive(true);
        groupeJournal.alpha = 1f;
        groupeJournal.interactable = true;
        groupeJournal.blocksRaycasts = true;
    }

    public void FermerJournal()
    {
        etatActuel = EtatJeu.EnJeu;
        Time.timeScale = 1f;

        groupeJournal.interactable = false;
        groupeJournal.blocksRaycasts = false;

        StartCoroutine(DesactiverApresDelai(canvasJournal, delaiEffets));
        StartCoroutine(ActiverApresDelai(canvasHud, delaiEffets));
    }

    // ===== CREDITS =====

    public void OuvrirCredits()
    {
        etatActuel = EtatJeu.DansCredits;

        canvasCredits.SetActive(true);
        groupeCredits.alpha = 1f;
        groupeCredits.interactable = true;
        groupeCredits.blocksRaycasts = true;
    }

    public void FermerCredits()
    {
        etatActuel = EtatJeu.EnPause;

        groupeCredits.interactable = false;
        groupeCredits.blocksRaycasts = false;

        StartCoroutine(DesactiverApresDelai(canvasCredits, delaiEffets));
        StartCoroutine(AfficherMenuPauseApresDelai(delaiEffets + 0.05f));
    }

    // ===== CONFIRMATION RETOUR MENU PRINCIPAL =====

    public void AfficherConfirmationRetourMenu()
    {
        etatAvantConfirmation = etatActuel;

        if (etatActuel == EtatJeu.EnJeu)
        {
            Time.timeScale = 0f;
        }

        if (etatActuel == EtatJeu.EnPause)
        {
            canvasMenuPause.SetActive(false);
        }

        etatActuel = EtatJeu.ConfirmationRetourMenu;

        canvasRetournerMenuPrincipal.SetActive(true);
        groupeRetournerMenuPrincipal.alpha = 1f;
        groupeRetournerMenuPrincipal.interactable = true;
        groupeRetournerMenuPrincipal.blocksRaycasts = true;
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
        canvasHud.SetActive(false);

        canvasMenu.SetActive(true);
        groupeMenu.alpha = 0f;
        groupeMenu.interactable = true;
        groupeMenu.blocksRaycasts = true;

        vcamMenu.Priority = 30;
        vcamJeu.Priority = 10;

        GetComponent<gestionsTransitions>().MettreAJourBoutonContinuer();

        StartCoroutine(FadeCanvasGroup(groupeMenu, 1f));
    }

    public void FermerConfirmationRetourMenu()
    {
        etatActuel = etatAvantConfirmation;

        groupeRetournerMenuPrincipal.interactable = false;
        groupeRetournerMenuPrincipal.blocksRaycasts = false;

        if (etatActuel == EtatJeu.EnJeu)
        {
            Time.timeScale = 1f;
            StartCoroutine(DesactiverApresDelai(
                canvasRetournerMenuPrincipal, delaiEffets));
        }
        else if (etatActuel == EtatJeu.EnPause)
        {
            StartCoroutine(DesactiverApresDelai(
                canvasRetournerMenuPrincipal, delaiEffets));
            StartCoroutine(ActiverMenuPauseApresDelai());
        }
    }

    private System.Collections.IEnumerator ActiverMenuPauseApresDelai()
    {
        yield return new WaitForSecondsRealtime(delaiEffets);

        canvasMenuPause.SetActive(true);
        groupeMenuPause.alpha = 1f;
        groupeMenuPause.interactable = true;
        groupeMenuPause.blocksRaycasts = true;
    }

    // ===== CONFIRMATION QUITTER =====

    public void AfficherConfirmationQuitter()
    {
        etatAvantConfirmation = etatActuel;
        etatActuel = EtatJeu.ConfirmationQuitter;

        canvasQuitter.SetActive(true);
        groupeQuitter.alpha = 1f;
        groupeQuitter.interactable = true;
        groupeQuitter.blocksRaycasts = true;
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

        groupeQuitter.interactable = false;
        groupeQuitter.blocksRaycasts = false;

        StartCoroutine(DesactiverApresDelai(canvasQuitter, delaiEffets));
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