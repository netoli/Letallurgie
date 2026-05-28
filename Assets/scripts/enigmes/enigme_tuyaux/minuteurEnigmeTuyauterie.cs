// ============================================================
// minuteurEnigmeTuyauterie.cs
// ------------------------------------------------------------
// Minuteur d'enigme : decompte une duree configurable. A zero,
// declenche un signal d'echec (typiquement le reset force de
// l'enigme via gestionEnigmeTuyauterie).
//
// DEMARRAGE :
// - Sur signal d'action 'enigme_tuyauterie_lancee' (depuis
//   zoneLancementEnigme).
// - OU appel public DemarrerMinuteur() depuis un autre script.
//
// ARRET :
// - Sur victoire (event onVictoire de gestionEnigmeTuyauterie).
// - OU appel public ArreterMinuteur().
// - OU expiration : declenche onTempsEcoule.
//
// UI :
// - tempsRestant lisible en public pour binding UI (ex : Text
//   ou Slider). Format : float secondes.
// ============================================================

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class minuteurEnigmeTuyauterie : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Duree totale du minuteur en secondes.")]
    [SerializeField] private float dureeTotale = 120f;

    [Tooltip("Si coche, demarre automatiquement le minuteur quand " +
        "l'action 'enigme_tuyauterie_lancee' est signalee a " +
        "gestionChapitres.")]
    [SerializeField] private bool autoDemarrerSurAction = true;

    [Tooltip("ID action ecoutee pour demarrer le minuteur.")]
    [SerializeField] private string idActionDemarrage = "enigme_tuyauterie_lancee";

    [Header("UI (optionnel)")]
    [Tooltip("Texte TMP qui affiche le temps restant (format MM:SS).")]
    [SerializeField] private TMP_Text texteAffichage;

    [Tooltip("Slider qui montre la progression du temps " +
        "(value: 1 au depart, 0 a la fin).")]
    [SerializeField] private Slider sliderAffichage;

    [Header("Events")]
    public UnityEvent onTempsEcoule;
    public UnityEvent onMinuteurDemarre;

    private bool actif = false;
    private bool aDejaDemarre = false;
    private bool enPause = false;
    private float tempsRestant;
    private gestionEnigmeTuyauterie enigme;

    public float TempsRestant => tempsRestant;
    public bool EstActif => actif;
    public bool EstEnPause => enPause;
    public bool ADejaDemarre => aDejaDemarre;

    // Singleton statique simple pour permettre a zoneLancementEnigme
    // d'appeler MettreEnPause()/Reprendre() sans dependance Inspector.
    // Note : il ne peut y avoir qu'un seul minuteur d'enigme par scene
    // (le puzzle est unique). Si plusieurs, le dernier reveille gagne.
    public static minuteurEnigmeTuyauterie Instance { get; private set; }

    private bool estInscrit = false;

    void Awake()
    {
        // Inscription du singleton AVANT Start() : ainsi zoneLancement-
        // Enigme.Start() peut deja le voir si l'ordre d'execution joue.
        Instance = this;
    }

    // Auto-inscription apres chaque chargement de scene, robuste face au
    // cas ou le GameObject porteur est INACTIF au demarrage (UI souvent
    // cachee jusqu'au lancement de l'enigme). Sans ca, Awake/Start ne
    // tournent jamais et le minuteur n'ecoute pas 'enigme_tuyauterie_lancee'.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InitGlobalHook()
    {
        SceneManager.sceneLoaded -= OnSceneChargee;
        SceneManager.sceneLoaded += OnSceneChargee;
        InscrireToutes();
    }

    private static void OnSceneChargee(Scene s, LoadSceneMode m)
    {
        InscrireToutes();
    }

    public static void InscrireToutes()
    {
        var tous = Object.FindObjectsByType<minuteurEnigmeTuyauterie>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var inst in tous)
        {
            if (Instance == null) Instance = inst;
            inst.SInscrire();
        }
    }

    private void SInscrire()
    {
        if (estInscrit) return;
        if (!autoDemarrerSurAction) return;
        if (string.IsNullOrEmpty(idActionDemarrage)) return;
        if (gestionChapitres.Instance == null) return;
        gestionChapitres.Instance.OnActionSignalee += AuActionSignalee;
        estInscrit = true;
        Debug.Log($"[minuteurEnigme] {name} : inscrit a OnActionSignalee, " +
            $"ecoute '{idActionDemarrage}' (gameObject active={gameObject.activeInHierarchy}).");
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (gestionChapitres.Instance != null)
            gestionChapitres.Instance.OnActionSignalee -= AuActionSignalee;
        if (enigme != null)
            enigme.onVictoire.RemoveListener(ArreterMinuteur);
    }

    void Start()
    {
        // Auto-lien sur le composant gestionEnigmeTuyauterie (si present
        // sur le meme GameObject ou ailleurs dans la scene).
        enigme = GetComponent<gestionEnigmeTuyauterie>();
        if (enigme == null)
            enigme = FindFirstObjectByType<gestionEnigmeTuyauterie>();

        if (enigme != null)
        {
            enigme.onVictoire.AddListener(ArreterMinuteur);
        }

        // Garde-fou : inscription via Start si l'auto-inscription
        // RuntimeInitializeOnLoadMethod n'a pas marche (timing tardif).
        SInscrire();

        // Auto-find UI si pas assignee dans l'Inspector. Cherche un
        // TMP_Text enfant nomme "texte_minuteur" ou contenant "minuteur".
        // Permet au minuteur d'afficher l'UI meme si l'Inspector n'a
        // pas ete configure (cas signale par le user).
        if (texteAffichage == null)
        {
            var tousTextes = GetComponentsInChildren<TMP_Text>(true);
            foreach (var t in tousTextes)
            {
                if (t == null) continue;
                string n = t.gameObject.name.ToLowerInvariant();
                if (n.Contains("minuteur") || n.Contains("timer")
                    || n == "texte_minuteur")
                {
                    texteAffichage = t;
                    Debug.Log("[minuteurEnigme] texteAffichage auto-trouve: "
                        + t.gameObject.name);
                    break;
                }
            }
        }
        if (sliderAffichage == null)
        {
            var tousSliders = GetComponentsInChildren<Slider>(true);
            if (tousSliders.Length > 0)
            {
                sliderAffichage = tousSliders[0];
                Debug.Log("[minuteurEnigme] sliderAffichage auto-trouve: "
                    + tousSliders[0].gameObject.name);
            }
        }

        tempsRestant = dureeTotale;
        MettreAJourUI();
    }

    void Update()
    {
        if (!actif) return;
        tempsRestant -= Time.deltaTime;
        if (tempsRestant <= 0f)
        {
            tempsRestant = 0f;
            ArreterMinuteur();
            Debug.Log("[minuteurEnigme] Temps ecoule.");
            onTempsEcoule.Invoke();
            // Si lie a gestionEnigmeTuyauterie, on declenche le reset
            // (via la methode privee ReinitialiserPuzzle... pas
            // accessible. On simule via les Errors max).
            // Note : le user doit hook onTempsEcoule a une methode qui
            // force le reset (ex : appeler un methode publique).
        }
        MettreAJourUI();
    }

    private void AuActionSignalee(string id)
    {
        if (id == idActionDemarrage)
            DemarrerMinuteur();
    }

    public void DemarrerMinuteur()
    {
        // Avant tout : activer le GameObject et tous ses ancetres si
        // l'UI etait cachee (cas typique : minuteur_enigme desactive
        // au demarrage pour ne pas etre visible avant le lancement de
        // l'enigme). Sans ca, Update() ne tournerait pas et l'UI ne
        // s'afficherait pas.
        if (!gameObject.activeInHierarchy)
        {
            Transform t = transform;
            while (t != null)
            {
                if (!t.gameObject.activeSelf) t.gameObject.SetActive(true);
                t = t.parent;
            }
        }

        // Cas 1 : deja en pause (le joueur avait quitte l'enigme avec
        // Esc). On reprend la oui on s'etait arrete, SANS reset.
        if (enPause)
        {
            Reprendre();
            return;
        }
        // Cas 2 : deja actif (re-appel redondant), ne rien faire.
        if (actif) return;

        // Cas 3 : premier demarrage OU redemarrage apres arret complet.
        actif = true;
        aDejaDemarre = true;
        enPause = false;
        tempsRestant = dureeTotale;
        MettreAJourUI();
        Debug.Log($"[minuteurEnigme] Demarre ({dureeTotale}s).");
        onMinuteurDemarre.Invoke();
    }

    public void ArreterMinuteur()
    {
        if (!actif) return;
        actif = false;
        enPause = false;
        Debug.Log("[minuteurEnigme] Arrete.");
    }

    /// <summary>
    /// Met le minuteur en pause sans le reset. Le temps restant est
    /// conserve. Appele depuis zoneLancementEnigme.QuitterEnigme()
    /// quand le joueur fait Esc pour quitter l'enigme.
    /// </summary>
    public void MettreEnPause()
    {
        if (!actif) return;
        actif = false;
        enPause = true;
        Debug.Log($"[minuteurEnigme] {name} mis en pause a " +
            $"{tempsRestant:F1}s.");
    }

    /// <summary>
    /// Met en pause TOUTES les instances de minuteurEnigmeTuyauterie
    /// (au cas ou il y en aurait plusieurs dans la scene — typiquement
    /// 1 logique + 1 UI sous des GameObjects distincts). Securise par
    /// rapport a MettreEnPause() qui ne touche que Instance.
    /// </summary>
    public static void MettreEnPauseTous()
    {
        var tous = Object.FindObjectsByType<minuteurEnigmeTuyauterie>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var inst in tous)
        {
            inst.MettreEnPause();
        }
    }

    /// <summary>
    /// Reprend le minuteur la oui il avait ete mis en pause.
    /// Appele depuis zoneLancementEnigme.LancerEnigme() quand le
    /// joueur relance l'enigme apres l'avoir quittee.
    /// </summary>
    public void Reprendre()
    {
        if (!enPause) return;
        actif = true;
        enPause = false;
        Debug.Log($"[minuteurEnigme] {name} repris a {tempsRestant:F1}s.");
    }

    /// <summary>
    /// Reprend toutes les instances en pause. Pendant de MettreEnPauseTous.
    /// </summary>
    public static void ReprendreTous()
    {
        var tous = Object.FindObjectsByType<minuteurEnigmeTuyauterie>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var inst in tous)
        {
            inst.Reprendre();
        }
    }

    public void ReinitialiserMinuteur()
    {
        tempsRestant = dureeTotale;
        actif = false;
        enPause = false;
        aDejaDemarre = false;
        MettreAJourUI();
    }

    private void MettreAJourUI()
    {
        if (texteAffichage != null)
        {
            int min = Mathf.FloorToInt(tempsRestant / 60f);
            int sec = Mathf.FloorToInt(tempsRestant % 60f);
            texteAffichage.text = $"{min:00}:{sec:00}";
        }
        if (sliderAffichage != null && dureeTotale > 0f)
        {
            sliderAffichage.value = tempsRestant / dureeTotale;
        }
    }
}
