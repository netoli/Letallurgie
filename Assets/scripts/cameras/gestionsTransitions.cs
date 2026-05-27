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
        // Auto-resolve des refs par nom dans la scene si elles sont
        // vides dans l'Inspector. Necessaire pour les scenes lancees
        // en standalone (scene2_usine, scene1_taverne1, scene3) ou
        // les references serialisees pointent vers des objets d'une
        // autre scene (donc null au runtime). Sans ca, les onClick des
        // boutons Menu (Nouvelle partie, Continuer, Options, etc.)
        // crashent quand on retourne au menu principal depuis le jeu
        // (ex: scene2 -> Q -> Oui).
        AutoResoudreReferences();

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
            // scene2_usine : le chapitre "le_sauvetage" est demarre par
            // le GameObject 'starter_chapitre_le_sauvetage' (composant
            // demarreurChapitreScene) directement dans la scene. PAS
            // ici, sinon double declenchement et banniere "A la
            // rescousse" qui s'affiche deux fois.

            // ATTENTION : on NE desactive PAS ce script en scene2/3/etc.
            // Si on faisait `this.enabled = false`, Update() ne tournerait
            // plus et les LISTES DE FADE (groupesEnFadeIn / groupesEnFadeOut)
            // ne seraient pas traitees. Resultat : les FadeOut(groupeMenu)
            // / FadeIn(groupeMenu) dans OnOptions / OnCredits / OnRetour
            // ne progresseraient pas visuellement -> les boutons "ne
            // font rien". On laisse Update tourner ; il est null-safe sur
            // brouillard1/2 et ne fait rien quand aucune fade n'est en
            // cours et que dansOptionsDepuisMenu/dansCreditsDepuisMenu
            // sont a false.
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

    /// <summary>
    /// Retrouve les references vides par nom dans la scene actuelle.
    /// Conçu pour les scenes standalone (scene2_usine etc.) ou les
    /// refs Inspector pointent vers des GameObjects d'une autre scene
    /// (donc null au runtime). Liste exhaustive : canvas, CanvasGroups,
    /// cameras virtuelles, brain Cinemachine, et bouton Continuer.
    /// Ne touche RIEN si les refs sont deja remplies dans l'Inspector.
    /// </summary>
    private void AutoResoudreReferences()
    {
        // Cameras virtuelles : on cherche par nom precis observe en scene
        if (vcamMenu == null)
            vcamMenu = TrouverCinemachine(
                "camera_virtuelle_menu_principal");
        if (vcamJeu == null)
            vcamJeu = TrouverCinemachine(
                "camera_virtuelle_premiere_personne");
        if (vcamOptionsCredits == null)
            vcamOptionsCredits = TrouverCinemachine(
                "camera_virtuelle_options_credits");

        // CinemachineBrain : un seul dans la scene generalement
        if (cinemachineBrain == null)
            cinemachineBrain =
                FindFirstObjectByType<CinemachineBrain>(
                    FindObjectsInactive.Include);

        // Canvas principaux du menu
        if (canvasMenu == null)
            canvasMenu = TrouverGameObject("canvas_menu_principal", true);
        if (canvasOptions == null)
            canvasOptions = TrouverGameObject("canvas_options", true);
        if (canvasCredits == null)
            canvasCredits = TrouverGameObject("canvas_credits", true);

        // HUD
        if (canvasHud == null)
            canvasHud = TrouverGameObject("canvas_hud", true);
        if (groupeHud == null)
        {
            var go = TrouverGameObject("contenu_hud", true);
            if (go != null)
                groupeHud = go.GetComponent<CanvasGroup>();
            if (groupeHud == null && canvasHud != null)
                groupeHud =
                    canvasHud.GetComponentInChildren<CanvasGroup>(true);
        }

        // Onglets options
        if (canvasOngletsOptions == null)
            canvasOngletsOptions =
                TrouverGameObject("canvas_onglets_options", true);
        if (groupeOngletsOptions == null && canvasOngletsOptions != null)
            groupeOngletsOptions =
                canvasOngletsOptions.GetComponent<CanvasGroup>();

        // CanvasGroups des canvas resolus ci-dessus
        if (groupeMenu == null && canvasMenu != null)
            groupeMenu = canvasMenu.GetComponent<CanvasGroup>();
        if (groupeOptions == null && canvasOptions != null)
            groupeOptions = canvasOptions.GetComponent<CanvasGroup>();
        if (groupeCredits == null && canvasCredits != null)
            groupeCredits = canvasCredits.GetComponent<CanvasGroup>();

        // Bouton Continuer (peut s'appeler bouton_continuer ou btn_continuer)
        if (btnContinuer == null)
            btnContinuer = TrouverGameObject("bouton_continuer", true)
                ?? TrouverGameObject("btn_continuer", true);
    }

    /// <summary>
    /// Trouve un GameObject par nom dans la scene (inclut inactifs).
    /// Exclut les prefabs assets pour ne pas retourner un asset.
    /// </summary>
    private GameObject TrouverGameObject(string nom, bool inclureInactifs)
    {
        var tous = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var go in tous)
        {
            if (go == null) continue;
            if (go.name != nom) continue;
            if (go.hideFlags != HideFlags.None) continue;
            if (!go.scene.IsValid()) continue;
            return go;
        }
        return null;
    }

    private CinemachineCamera TrouverCinemachine(string nom)
    {
        var tous = Resources.FindObjectsOfTypeAll<CinemachineCamera>();
        foreach (var c in tous)
        {
            if (c == null || c.gameObject == null) continue;
            if (c.gameObject.name != nom) continue;
            if (c.gameObject.hideFlags != HideFlags.None) continue;
            if (!c.gameObject.scene.IsValid()) continue;
            return c;
        }
        return null;
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
        // Null-safe : canvasOngletsOptions n'existe pas en scene2/3
        // (pas copie depuis scene0). On skip silencieusement.
        if (canvasOngletsOptions == null) return;
        canvasOngletsOptions.SetActive(true);
        if (groupeOngletsOptions != null)
        {
            groupeOngletsOptions.alpha = 0f;
            FadeIn(groupeOngletsOptions, vitesseFadeOnglets);
        }
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
            // Coroutine unscaled-time pour fonctionner meme avec
            // Time.timeScale = 0 (overlay menu).
            StartCoroutine(InvokeNonScale(nameof(ExecuterRetour), 0.15f));
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

        // Si on n'est PAS deja dans scene0_tuto, on charge scene0_tuto
        // pour que la "nouvelle partie" recommence vraiment depuis le
        // debut du tuto. Sans ca, le joueur reste dans la scene
        // courante (ex: scene2_usine) apres "Nouvelle partie" — bug
        // signale par l'utilisateur. On lance la musique d'intro/tuto
        // AVANT le LoadScene pour qu'elle survive au changement.
        if (SceneManager.GetActiveScene().name != "scene0_tuto")
        {
            Debug.Log("[Transitions] Nouvelle partie depuis " +
                SceneManager.GetActiveScene().name +
                " -> chargement de scene0_tuto.");
            // CRUCIAL : remettre timeScale a 1 avant LoadScene, sinon
            // scene0_tuto demarre figee (cas overlay menu via Q->Oui).
            Time.timeScale = 1f;
            // Reset IgnoreTimeScale du Brain (mis a true dans
            // ConfirmerRetourMenuPrincipal). Le Brain de scene0 sera
            // recree au LoadScene mais on reset celui-ci au cas oui il
            // serait dontDestroyOnLoad.
            if (cinemachineBrain != null)
                cinemachineBrain.IgnoreTimeScale = false;
            if (gestionAudio.Instance != null)
                gestionAudio.Instance.JouerMusiquesTutoriel();
            SceneManager.LoadScene("scene0_tuto");
            return;
        }

        FadeOut(groupeMenu);
        if (vcamJeu != null) vcamJeu.Priority = 50;
        positionYCible1 = positionYDepart1;

        if (gestionFlou != null)
            gestionFlou.DesactiverFlou();

        if (gestionAudio.Instance != null)
        {
            gestionAudio.Instance.JouerMusiquesTutoriel();
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

        string sceneActuelle = SceneManager.GetActiveScene().name;

        // CAS A : on est en overlay menu (Time.timeScale = 0 indique
        // qu'on est arrive ici via Q -> Oui dans une scene de jeu).
        // L'utilisateur veut REPRENDRE la scene actuelle telle quelle
        // (peu importe ce que dit la sauvegarde) : juste un un-freeze.
        // C'est plus robuste que de comparer donnees.nomScene car la
        // sauvegarde peut contenir un ancien nom obsolete.
        if (sceneActuelle != "scene0_tuto" && Time.timeScale == 0f)
        {
            Debug.Log("[Transitions] Continuer en overlay : un-freeze " +
                "de la scene actuelle (" + sceneActuelle + ").");
            if (gestionAudio.Instance != null)
            {
                if (sceneActuelle == "scene1_taverne1")
                    gestionAudio.Instance.JouerMusiquesTaverne();
                else if (sceneActuelle == "scene2_usine")
                    gestionAudio.Instance.JouerMusiquesUsine();
                else if (sceneActuelle == "scene4_manoir")
                    gestionAudio.Instance.JouerMusiquesManoir();
            }
            var inputs = FindFirstObjectByType<gestionInputsJeu>(
                FindObjectsInactive.Include);
            if (inputs != null)
                inputs.ReprendreJeuApresMenuOverlay();
            estEnTransition = false;
            return;
        }

        // CAS B : on est en scene0_tuto (vrai menu principal). Si la
        // sauvegarde existe et pointe vers une autre scene, on la charge.
        // Si la sauvegarde a un nomScene invalide ou inexistant, on
        // fallback sur le comportement original (CAS C) au lieu de
        // crasher avec un LoadScene impossible.
        if (donnees != null)
        {
            gestionPartie.Instance.ChargerPartieEnCours(donnees);
            Debug.Log("Chargement de: " + donnees.nomSauvegarde
                + " | Scene: " + donnees.nomScene);

            // Whitelist des scenes valides (build settings). Si le
            // nomScene de la sauvegarde n'est pas dans cette liste, on
            // l'ignore et on continue avec scene0_tuto.
            bool nomSceneValide =
                donnees.nomScene == "scene0_tuto"
                || donnees.nomScene == "scene1_taverne1"
                || donnees.nomScene == "scene2_usine"
                || donnees.nomScene == "scene3_taverne2"
                || donnees.nomScene == "scene4_manoir";

            if (nomSceneValide
                && !string.IsNullOrEmpty(donnees.nomScene)
                && sceneActuelle != donnees.nomScene)
            {
                Debug.Log("[Transitions] Sauvegarde en '" +
                    donnees.nomScene + "' mais on est en '" +
                    sceneActuelle + "' -> chargement de la bonne scene.");
                if (gestionAudio.Instance != null)
                {
                    if (donnees.nomScene == "scene1_taverne1")
                        gestionAudio.Instance.JouerMusiquesTaverne();
                    else if (donnees.nomScene == "scene2_usine")
                        gestionAudio.Instance.JouerMusiquesUsine();
                    else if (donnees.nomScene == "scene4_manoir")
                        gestionAudio.Instance.JouerMusiquesManoir();
                }
                Time.timeScale = 1f;
                SceneManager.LoadScene(donnees.nomScene);
                return;
            }
            else if (!nomSceneValide && !string.IsNullOrEmpty(donnees.nomScene))
            {
                Debug.LogWarning("[Transitions] Sauvegarde avec " +
                    "nomScene invalide '" + donnees.nomScene +
                    "' (ancienne sauvegarde ?). On continue dans la " +
                    "scene actuelle.");
            }
        }

        // CAS C : on est en scene0_tuto et la sauvegarde y correspond
        // aussi (ou il n'y a pas de sauvegarde). Comportement original
        // du menu principal : FadeOut + cut Cinemachine + activer HUD.
        FadeOut(groupeMenu);
        positionYCible1 = positionYDepart1;

        if (gestionFlou != null)
            gestionFlou.DesactiverFlou();

        if (gestionAudio.Instance != null)
        {
            if (sceneActuelle == "scene1_taverne1")
            {
                gestionAudio.Instance.JouerMusiquesTaverne();
            }
            else if (sceneActuelle == "scene2_usine")
            {
                gestionAudio.Instance.JouerMusiquesUsine();
            }
            else if (sceneActuelle == "scene4_manoir")
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
        Debug.Log("[Transitions] OnOptions CLIQUE");
        if (estEnTransition) return;
        estEnTransition = true;

        dansOptionsDepuisMenu = true;

        if (groupeMenu != null) FadeOut(groupeMenu);
        if (vcamOptionsCredits != null) vcamOptionsCredits.Priority = 40;

        if (gestionFlou != null)
            gestionFlou.ActiverFlou();

        // IMPORTANT : on utilise StartCoroutine + WaitForSecondsRealtime
        // au lieu de Invoke, parce que Invoke depend de Time.timeScale.
        // Quand on est en overlay menu (Q -> Oui depuis le jeu),
        // Time.timeScale = 0 -> les Invoke ne se declenchent JAMAIS.
        // Avec unscaled, les delais fonctionnent peu importe timeScale.
        StartCoroutine(InvokeNonScale(nameof(AfficherOnglets),
            delaiApparitionOnglets));
        StartCoroutine(InvokeNonScale(nameof(ActiverBrouillard2),
            delaiDebutBrouillard2));
        StartCoroutine(InvokeNonScale(nameof(AfficherOptions),
            delaiApparition));
        StartCoroutine(InvokeNonScale(nameof(FinTransition), 2.5f));
    }

    public void OnCredits()
    {
        Debug.Log("[Transitions] OnCredits CLIQUE");
        if (estEnTransition) return;
        estEnTransition = true;

        dansCreditsDepuisMenu = true;

        if (groupeMenu != null) FadeOut(groupeMenu);
        if (vcamOptionsCredits != null) vcamOptionsCredits.Priority = 40;

        if (gestionFlou != null)
            gestionFlou.ActiverFlou();

        StartCoroutine(InvokeNonScale(nameof(ActiverBrouillard2),
            delaiDebutBrouillard2));
        StartCoroutine(InvokeNonScale(nameof(AfficherCredits),
            delaiApparition));
        StartCoroutine(InvokeNonScale(nameof(FinTransition), 2.5f));
    }

    /// <summary>
    /// Equivalent de Invoke mais base sur Time.unscaledDeltaTime, donc
    /// fonctionne meme quand Time.timeScale = 0 (cas overlay menu).
    /// </summary>
    private IEnumerator InvokeNonScale(string methodName, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        Invoke(methodName, 0f);
    }

    public void OnRetour()
    {
        if (estEnTransition) return;
        estEnTransition = true;

        dansOptionsDepuisMenu = false;
        dansCreditsDepuisMenu = false;

        // Tous null-safe pour les scenes ou certains canvas peuvent
        // manquer (scene2/3 sans canvas_onglets_options par exemple).
        if (canvasOptions != null && canvasOptions.activeSelf
            && groupeOptions != null)
            FadeOut(groupeOptions);
        if (canvasCredits != null && canvasCredits.activeSelf
            && groupeCredits != null)
            FadeOut(groupeCredits);
        if (canvasOngletsOptions != null && canvasOngletsOptions.activeSelf
            && groupeOngletsOptions != null)
            FadeOut(groupeOngletsOptions, vitesseFadeOnglets);

        if (groupeMenu != null) FadeIn(groupeMenu);

        if (vcamMenu != null) vcamMenu.Priority = 30;
        if (vcamOptionsCredits != null) vcamOptionsCredits.Priority = 20;
        if (vcamJeu != null) vcamJeu.Priority = 10;

        positionYCible2 = positionYDepart2;

        if (gestionFlou != null)
            gestionFlou.ActiverFlou();

        if (gestionAudio.Instance != null)
            gestionAudio.Instance.JouerMusiquesIntro();

        // Coroutine unscaled-time (cf. OnOptions/OnCredits)
        StartCoroutine(InvokeNonScale(nameof(DesactiverOptionsCredits), 2.5f));
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
        if (canvasOptions == null) return;
        canvasOptions.SetActive(true);
        if (groupeOptions != null)
        {
            groupeOptions.alpha = 0f;
            FadeIn(groupeOptions);
        }
    }

    private void AfficherCredits()
    {
        if (canvasCredits == null) return;
        canvasCredits.SetActive(true);
        if (groupeCredits != null)
        {
            groupeCredits.alpha = 0f;
            FadeIn(groupeCredits);
        }
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
        if (canvasOptions != null) canvasOptions.SetActive(false);
        if (canvasCredits != null) canvasCredits.SetActive(false);
        if (canvasOngletsOptions != null)
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