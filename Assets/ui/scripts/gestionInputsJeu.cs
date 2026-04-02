using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;


public class gestionInputsJeu : MonoBehaviour
{
    [Header("Canvas")]
    [SerializeField] private GameObject canvasMenuPause;
    [SerializeField] private CanvasGroup groupeMenuPause;
    [SerializeField] private GameObject canvasHud;
    [SerializeField] private GameObject canvasMenu;
    [SerializeField] private CanvasGroup groupeMenu;

    [Header("Cameras")]
    [SerializeField] private CinemachineCamera vcamMenu;
    [SerializeField] private CinemachineCamera vcamJeu;

    [Header("Parametres")]
    [SerializeField] private float vitesseFade;

    private bool estEnPause = false;
    private bool jeuActif = false;

    void Start()
    {
        canvasMenuPause.SetActive(false);
    }

    public void ActiverInputs()
    {
        jeuActif = true;
        estEnPause = false;
        Time.timeScale = 1f;
        canvasMenuPause.SetActive(false);
        StopAllCoroutines();
    }

    public void DesactiverInputs()
    {
        jeuActif = false;
    }

    void Update()
    {
        if (!jeuActif) return;

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            if (estEnPause)
                Reprendre();
            else
                MettreEnPause();
        }

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (estEnPause)
                Reprendre();
            else
                RetourMenuPrincipal();
        }
    }

    public void MettreEnPause()
    {
        estEnPause = true;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        canvasMenuPause.SetActive(true);
        groupeMenuPause.alpha = 0f;
        groupeMenuPause.interactable = true;
        groupeMenuPause.blocksRaycasts = true;

        StartCoroutine(FadeIn());
    }

    public void Reprendre()
    {
        estEnPause = false;
        Time.timeScale = 1f;

        groupeMenuPause.interactable = false;
        groupeMenuPause.blocksRaycasts = false;

        StartCoroutine(FadeOutEtDesactiver());
    }

    public void RetourMenuPrincipal()
    {
        StopAllCoroutines();
        estEnPause = false;
        jeuActif = false;
        Time.timeScale = 1f;

        canvasMenuPause.SetActive(false);
        canvasHud.SetActive(false);

        canvasMenu.SetActive(true);
        groupeMenu.alpha = 0f;
        groupeMenu.interactable = true;
        groupeMenu.blocksRaycasts = true;

        vcamMenu.Priority = 30;
        vcamJeu.Priority = 10;

        StartCoroutine(FadeInMenu());
    }

    private System.Collections.IEnumerator FadeIn()
    {
        while (groupeMenuPause.alpha < 0.99f)
        {
            groupeMenuPause.alpha = Mathf.Lerp(
                groupeMenuPause.alpha, 1f,
                Time.unscaledDeltaTime * vitesseFade);
            yield return null;
        }
        groupeMenuPause.alpha = 1f;
    }

    private System.Collections.IEnumerator FadeOutEtDesactiver()
    {
        while (groupeMenuPause.alpha > 0.01f)
        {
            groupeMenuPause.alpha = Mathf.Lerp(
                groupeMenuPause.alpha, 0f,
                Time.unscaledDeltaTime * vitesseFade);
            yield return null;
        }
        groupeMenuPause.alpha = 0f;
        canvasMenuPause.SetActive(false);
    }

    private System.Collections.IEnumerator FadeInMenu()
    {
        while (groupeMenu.alpha < 0.99f)
        {
            groupeMenu.alpha = Mathf.Lerp(
                groupeMenu.alpha, 1f,
                Time.unscaledDeltaTime * vitesseFade);
            yield return null;
        }
        groupeMenu.alpha = 1f;
    }
}