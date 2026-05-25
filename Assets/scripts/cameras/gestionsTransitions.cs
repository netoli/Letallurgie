using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;


public class gestionsTransitions : MonoBehaviour
{
    [Header("Cameras virtuelles")]
    [SerializeField] private CinemachineCamera vcamMenu;
    [SerializeField] private CinemachineCamera vcamOptionsCredits;
    [SerializeField] private CinemachineCamera vcamJeu;
    [SerializeField] private CinemachineBrain cinemachineBrain;

    [Header("Canvas")]
    [SerializeField] private GameObject canvasMenu;
    [SerializeField] private GameObject canvasOptions;
    [SerializeField] private GameObject canvasCredits;

    [Header("Canvas HUD")]
    [SerializeField] private GameObject canvasHud;
    [SerializeField] private CanvasGroup groupeHud;

    [Header("Canvas Onglets")]
    [SerializeField] private GameObject canvasOngletsOptions;
    [SerializeField] private CanvasGroup groupeOngletsOptions;
    [SerializeField] private float delaiApparitionOnglets;
    [SerializeField] private float vitesseFadeOnglets;

    [Header("Canvas Groups")]
    [SerializeField] private CanvasGroup groupeMenu;
    [SerializeField] private CanvasGroup groupeOptions;
    [SerializeField] private CanvasGroup groupeCredits;

    [Header("Bouton Continuer")]
    [SerializeField] private GameObject btnContinuer;

    [Header("Brouillard Menu")]
    [SerializeField] private Transform brouillard1;
    [SerializeField] private float positionYDepart1;
    [SerializeField] private float positionYFinale1;
    [SerializeField] private float vitesseMontee1;
    [SerializeField] private float delaiDebutBrouillard1;

    [Header("Brouillard Options/Credits")]
    [SerializeField] private Transform brouillard2;
    [SerializeField] private float positionYDepart2;
    [SerializeField] private float positionYFinale2;
    [SerializeField] private float vitesseMontee2;
    [SerializeField] private float delaiDebutBrouillard2;
    [SerializeField] private float delaiAccumulationBrouillard2;

    [Header("Parametres")]
    [SerializeField] private float vitesseFade;
    [SerializeField] private float delaiApparition;

    [Header("Flou")]
    public gestionFlou gestionFlou;

    [Header("Joueur")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerBodyRotation playerBodyRotation;
    [SerializeField] private CinemachineInputAxisController inputAxisController;

    private struct FadeInfo
    {
        public CanvasGroup groupe;
        public float vitesse;
    }

    private List<FadeInfo> groupesEnFadeIn = new List<FadeInfo>();
    private List<FadeInfo> groupesEnFadeOut = new List<FadeInfo>();
    private bool estEnTransition = false;
    private float positionYCible1;
    private float positionYCible2;
    private bool dansOptionsDepuisMenu = false;
    private bool dansCreditsDepuisMenu = false;
    private bool attenteRetour = false;

    void Start()
    {
        if (SceneManager.GetActiveScene().name != "scene0_tuto")
        {
            // D�sactiver le flou
            if (gestionFlou != null)
                gestionFlou.DesactiverFlou();

            // Activer la cam�ra de jeu
            if (vcamJeu != null)
                vcamJeu.Priority = 50;

            if (vcamMenu != null)
                vcamMenu.Priority = 10;

            // Activer le joueur
            ActiverJoueur();

            // Demarrer le chapitre approprie selon la scene chargee.
            // Pour scene1_taverne1 : on annonce la
            // nouvelle phase narrative via la banniere "Mener l'enquete".
            // Important : ce code DOIT etre dans Start() (pas dans
            // DesactiverMenu) car apres une cinematique + LoadScene,
            // on n'a pas besoin de "quitter le menu" — la scene se
            // charge directement.
            string sc = SceneManager.GetActiveScene().name;
            if (sc == "scene1_taverne1"
                && gestionChapitres.Instance != null)
            {
                gestionChapitres.Instance.DemarrerChapitre(
                    "mener_enquete");
            }
            // scene2_usine : annonce du chapitre "À la rescousse".
            // Demarrage avec un petit delai pour laisser le temps a la
            // transition camera cinemachine de se faire avant que la
            // banniere apparaisse.
            else if (sc == "scene2_usine"
                && gestionChapitres.Instance != null)
            {
                StartCoroutine(DemarrerChapitreApresDelai(
                    "le_sauvetage", 5f));
            }

            // D�sactiver compl�tement ce script pour �viter qu'il interf�re
            this.enabled = false;
            return;
        }

        vcamMenu.Priority = 30;
        vcamOptionsCredits.Priority = 20;
        vcamJeu.Priority = 10;

        canvasMenu.SetActive(true);
        groupeMenu.alpha = 1f;
        groupeMenu.interactable = true;
        groupeMenu.blocksRaycasts = true;

        canvasOptions.SetActive(false);
        canvasCredits.SetActive(false);
        canvasOngletsOptions.SetActive(false);
        canvasHud.SetActive(false);

        DesactiverJoueur();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        MettreAJourBoutonContinuer();

        if (gestionFlou != null)
            gestionFlou.ActiverFlou();

        if (brouillard1 != null)
        {
            Vector3 pos = brouillard1.position;
            pos.y = positionYDepart1;
            brouillard1.position = pos;
            positionYCible1 = positionYDepart1;

            Invoke(nameof(DemarrerMonteeBrouillard1),
                delaiDebutBrouillard1);
        }

        if (brouillard2 != null)
        {
            brouillard2.gameObject.SetActive(false);
            positionYCible2 = positionYDepart2;
        }
    }

    // Helper coroutine pour demarrer un chapitre apres un delai
    // (laisse le temps a la transition camera Cinemachine au chargement
    // de scene2_usine avant que la banniere n'apparaisse).
    private IEnumerator DemarrerChapitreApresDelai(string idChapitre, float delai)
    {
        yield return new WaitForSecondsRealtime(delai);
        if (gestionChapitres.Instance != null)
            gestionChapitres.Instance.DemarrerChapitre(idChapitre);
    }

    private void DesactiverJoueur()
    {
        if (playerMovement != null)
            playerMovement.enabled = false;

        if (playerBodyRotation != null)
            playerBodyRotation.enabled = false;

        if (inputAxisController != null)
            inputAxisController.enabled = false;
    }

    private void ActiverJoueur()
    {
        if (playerMovement != null)
            playerMovement.enabled = true;

        if (playerBodyRotation != null)
            playerBodyRotation.enabled = true;

        if (inputAxisController != null)
            inputAxisController.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void MettreAJourBoutonContinuer()
    {
        if (btnContinuer != null)
            btnContinuer.SetActive(
                gestionPartie.Instance.SauvegardeExiste());
    }

    private void DemarrerMonteeBrouillard1()
    {
        positionYCible1 = positionYFinale1;
    }

    private void ActiverBrouillard2()
    {
        if (brouillard2 != null)
        {
            Vector3 pos = brouillard2.position;
            pos.y = positionYDepart2;
            brouillard2.position = pos;
            brouillard2.gameObject.SetActive(true);

            Invoke(nameof(MonterBrouillard2),
                delaiAccumulationBrouillard2);
        }
    }

    private void MonterBrouillard2()
    {
        positionYCible2 = positionYFinale2;
    }

    private void AfficherOnglets()
    {
        canvasOngletsOptions.SetActive(true);
        groupeOngletsOptions.alpha = 0f;
        FadeIn(groupeOngletsOptions, vitesseFadeOnglets);
    }

    void Update()
    {
        if (Keyboard.current != null
            && Keyboard.current.escapeKey.wasPressedThisFrame
            && !estEnTransition
            && !attenteRetour
            && (dansOptionsDepuisMenu || dansCreditsDepuisMenu))
        {
            attenteRetour = true;
            Invoke(nameof(ExecuterRetour), 0.15f);
        }

        for (int i = groupesEnFadeIn.Count - 1; i >= 0; i--)
        {
            var info = groupesEnFadeIn[i];
            info.groupe.alpha = Mathf.Lerp(
                info.groupe.alpha, 1f,
                Time.unscaledDeltaTime * info.vitesse);

            if (info.groupe.alpha > 0.99f)
            {
                info.groupe.alpha = 1f;
                groupesEnFadeIn.RemoveAt(i);
            }
        }

        for (int i = groupesEnFadeOut.Count - 1; i >= 0; i--)
        {
            var info = groupesEnFadeOut[i];
            info.groupe.alpha = Mathf.Lerp(
                info.groupe.alpha, 0f,
                Time.unscaledDeltaTime * info.vitesse);

            if (info.groupe.alpha < 0.01f)
            {
                info.groupe.alpha = 0f;
                groupesEnFadeOut.RemoveAt(i);
            }
        }

        if (brouillard1 != null)
        {
            Vector3 pos = brouillard1.position;
            pos.y = Mathf.Lerp(pos.y, positionYCible1,
                Time.unscaledDeltaTime * vitesseMontee1);
            brouillard1.position = pos;
        }

        if (brouillard2 != null
            && brouillard2.gameObject.activeSelf)
        {
            Vector3 pos = brouillard2.position;
            pos.y = Mathf.Lerp(pos.y, positionYCible2,
                Time.unscaledDeltaTime * vitesseMontee2);
            brouillard2.position = pos;
        }
    }

    private void ExecuterRetour()
    {
        attenteRetour = false;
        OnRetour();
    }

    public void OnNouvellePartie()
    {
        Debug.Log("[Transitions] OnNouvellePartie CLIQUE");

        if (estEnTransition) return;
        estEnTransition = true;

        gestionPartie.Instance.InitialiserNouvellePartie();

        FadeOut(groupeMenu);
        vcamJeu.Priority = 50;
        positionYCible1 = positionYDepart1;

        if (gestionFlou != null)
            gestionFlou.DesactiverFlou();

        if (gestionAudio.Instance != null)
        {
            if (SceneManager.GetActiveScene().name == "scene0_tuto")
            {
                gestionAudio.Instance.JouerMusiquesTutoriel();
            }
        }


        // Transition progressive de 2.5s - personnage proche du menu
        Invoke(nameof(DesactiverMenu), 2.5f);
    }

    public void OnContinuer()
    {
        Debug.Log("[Transitions] OnContinuer CLIQUE");

        if (estEnTransition) return;
        estEnTransition = true;

        gestionPartie.DonneesSauvegarde donnees =
            gestionPartie.Instance.ChargerDerniereSauvegarde();

        if (donnees != null)
        {
            gestionPartie.Instance.ChargerPartieEnCours(donnees);
            Debug.Log("Chargement de: " + donnees.nomSauvegarde
                + " | Scene: " + donnees.nomScene);
        }

        FadeOut(groupeMenu);
        positionYCible1 = positionYDepart1;

        if (gestionFlou != null)
            gestionFlou.DesactiverFlou();

        if (gestionAudio.Instance != null)
        {
            if (SceneManager.GetActiveScene().name == "scene1_taverne1")
            {
                gestionAudio.Instance.JouerMusiquesTaverne();
            }
            else if (SceneManager.GetActiveScene().name == "scene2_usine")
            {
                gestionAudio.Instance.JouerMusiquesUsine();
            }
            else if (SceneManager.GetActiveScene().name == "scene4_manoir")
            {
                gestionAudio.Instance.JouerMusiquesManoir();
            }


            // Cut instantane - personnage potentiellement loin du menu
            StartCoroutine(CutInstantaneVersJeu());

            Invoke(nameof(DesactiverMenu), 0.1f);
        }
    }

    private IEnumerator CutInstantaneVersJeu()
    {
        if (cinemachineBrain == null)
        {
            Debug.LogWarning(
                "[Transitions] Brain non assigne dans l'Inspector");
            vcamMenu.Priority = 10;
            vcamOptionsCredits.Priority = 10;
            vcamJeu.Priority = 50;
            yield break;
        }

        cinemachineBrain.enabled = false;

        vcamMenu.Priority = 10;
        vcamOptionsCredits.Priority = 10;
        vcamJeu.Priority = 50;

        yield return null;

        cinemachineBrain.enabled = true;
    }

    public void OnOptions()
    {
        if (estEnTransition) return;
        estEnTransition = true;

        dansOptionsDepuisMenu = true;

        FadeOut(groupeMenu);
        vcamOptionsCredits.Priority = 40;

        if (gestionFlou != null)
            gestionFlou.ActiverFlou();

        Invoke(nameof(AfficherOnglets), delaiApparitionOnglets);
        Invoke(nameof(ActiverBrouillard2), delaiDebutBrouillard2);
        Invoke(nameof(AfficherOptions), delaiApparition);
        Invoke(nameof(FinTransition), 2.5f);
    }

    public void OnCredits()
    {
        if (estEnTransition) return;
        estEnTransition = true;

        dansCreditsDepuisMenu = true;

        FadeOut(groupeMenu);
        vcamOptionsCredits.Priority = 40;

        if (gestionFlou != null)
            gestionFlou.ActiverFlou();

        Invoke(nameof(ActiverBrouillard2), delaiDebutBrouillard2);
        Invoke(nameof(AfficherCredits), delaiApparition);
        Invoke(nameof(FinTransition), 2.5f);
    }

    public void OnRetour()
    {
        if (estEnTransition) return;
        estEnTransition = true;

        dansOptionsDepuisMenu = false;
        dansCreditsDepuisMenu = false;

        if (canvasOptions.activeSelf)
            FadeOut(groupeOptions);
        if (canvasCredits.activeSelf)
            FadeOut(groupeCredits);
        if (canvasOngletsOptions.activeSelf)
            FadeOut(groupeOngletsOptions, vitesseFadeOnglets);

        FadeIn(groupeMenu);

        vcamMenu.Priority = 30;
        vcamOptionsCredits.Priority = 20;
        vcamJeu.Priority = 10;

        positionYCible2 = positionYDepart2;

        if (gestionFlou != null)
            gestionFlou.ActiverFlou();

        if (gestionAudio.Instance != null)
            gestionAudio.Instance.JouerMusiquesIntro();

        Invoke(nameof(DesactiverOptionsCredits), 2.5f);
    }

    public void RetourEnJeuDepuisChargement()
    {
        canvasOptions.SetActive(false);
        canvasCredits.SetActive(false);
        canvasOngletsOptions.SetActive(false);
        canvasMenu.SetActive(false);

        vcamMenu.Priority = 10;
        vcamOptionsCredits.Priority = 10;
        vcamJeu.Priority = 50;

        dansOptionsDepuisMenu = false;
        dansCreditsDepuisMenu = false;
        estEnTransition = false;
        attenteRetour = false;

        if (brouillard2 != null)
        {
            positionYCible2 = positionYDepart2;
            brouillard2.gameObject.SetActive(false);
        }

        groupesEnFadeIn.Clear();
        groupesEnFadeOut.Clear();

        ActiverJoueur();

        if (gestionFlou != null)
            gestionFlou.DesactiverFlou();

        if (gestionAudio.Instance != null)
            gestionAudio.Instance.JouerMusiquesTaverne();
    }

    public void OnQuitter()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void AfficherOptions()
    {
        canvasOptions.SetActive(true);
        groupeOptions.alpha = 0f;
        FadeIn(groupeOptions);
    }

    private void AfficherCredits()
    {
        canvasCredits.SetActive(true);
        groupeCredits.alpha = 0f;
        FadeIn(groupeCredits);
    }

    private void FadeIn(CanvasGroup groupe,
        float vitesseCustom = -1f)
    {
        float v = vitesseCustom > 0 ?
            vitesseCustom : vitesseFade;

        for (int i = groupesEnFadeOut.Count - 1; i >= 0; i--)
        {
            if (groupesEnFadeOut[i].groupe == groupe)
                groupesEnFadeOut.RemoveAt(i);
        }

        bool dejaPresent = false;
        for (int i = 0; i < groupesEnFadeIn.Count; i++)
        {
            if (groupesEnFadeIn[i].groupe == groupe)
            {
                dejaPresent = true;
                break;
            }
        }

        if (!dejaPresent)
        {
            groupesEnFadeIn.Add(new FadeInfo
            {
                groupe = groupe,
                vitesse = v
            });
        }

        groupe.interactable = true;
        groupe.blocksRaycasts = true;
    }

    private void FadeOut(CanvasGroup groupe,
        float vitesseCustom = -1f)
    {
        float v = vitesseCustom > 0 ?
            vitesseCustom : vitesseFade;

        for (int i = groupesEnFadeIn.Count - 1; i >= 0; i--)
        {
            if (groupesEnFadeIn[i].groupe == groupe)
                groupesEnFadeIn.RemoveAt(i);
        }

        bool dejaPresent = false;
        for (int i = 0; i < groupesEnFadeOut.Count; i++)
        {
            if (groupesEnFadeOut[i].groupe == groupe)
            {
                dejaPresent = true;
                break;
            }
        }

        if (!dejaPresent)
        {
            groupesEnFadeOut.Add(new FadeInfo
            {
                groupe = groupe,
                vitesse = v
            });
        }

        groupe.interactable = false;
        groupe.blocksRaycasts = false;
    }

    private void DesactiverMenu()
    {
        Debug.Log("[Transitions] DesactiverMenu appele");

        canvasMenu.SetActive(false);

        canvasHud.SetActive(true);
        groupeHud.alpha = 0f;
        FadeIn(groupeHud);

        ActiverJoueur();

        GetComponent<gestionInputsJeu>().ActiverInputs();

        estEnTransition = false;

        Debug.Log("[Transitions] gestionChapitres.Instance = "
            + (gestionChapitres.Instance != null ? "OK" : "NULL"));

        // Demarre le chapitre initial UNIQUEMENT en SCENE0 (tuto).
        // En scene1_taverne1 (et autres scenes ulterieures), aucun
        // chapitre n'est demarre automatiquement ici : ce sera a
        // un autre declencheur (interaction PNJ, zone, etc.) de
        // declencher le chapitre approprie.
        if (gestionChapitres.Instance != null)
        {
            string sceneActuelle = UnityEngine.SceneManagement
                .SceneManager.GetActiveScene().name;
            if (sceneActuelle == "scene0_tuto")
            {
                gestionChapitres.Instance.DemarrerChapitre(
                    "premier_contact");
            }
            // Note : "mener_enquete" pour scene1_taverne1
            // est declenche depuis Start() (pas ici), car DesactiverMenu
            // n'est appele que depuis le bouton Continuer du menu, pas
            // apres un LoadScene de cinematique.
            else
            {
                Debug.Log($"[Transitions] Scene '{sceneActuelle}' " +
                    "non-tuto : aucun chapitre demarre " +
                    "automatiquement.");
            }
        }
    }

    private void DesactiverOptionsCredits()
    {
        canvasOptions.SetActive(false);
        canvasCredits.SetActive(false);
        canvasOngletsOptions.SetActive(false);
        if (brouillard2 != null)
        {
            positionYCible2 = positionYDepart2;
            brouillard2.gameObject.SetActive(false);
        }
        estEnTransition = false;
    }

    private void FinTransition()
    {
        estEnTransition = false;
    }
}