// ============================================================
// JournalManager.cs
// ------------------------------------------------------------
// Auteur      : Fanny Fortier
// Date créé   : 09/04/2026
// Dernière modification : 2026-05-23 - Fanny Fortier
// ------------------------------------------------------------
// Description :
//   Gère la création des entrées dans le journal.
//   Attaché sur gestion_journal (game object vide dans les managers).
// ------------------------------------------------------------
// Dépendances :
//   - JournalSlotUI.cs  : appelle InitialiserSlot() sur chaque slot créé
//   - RamasserIndice.cs : appelle AjouterEntreeJournal() pour ajouter un indice
// ============================================================
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class JournalManager : MonoBehaviour
{
    public static JournalManager Instance;
    public List<EntreeJournal> entrees = new List<EntreeJournal>();

    /// <summary>
    /// Event diffuse a chaque ajout d'entree au journal. L'argument
    /// transmet le nombre total d'entrees apres l'ajout. Permet a un
    /// composant de scene (ex: compteurIndicesScene) de detecter quand
    /// le seuil "tous indices ramasses" est atteint sans coupler la
    /// logique de signalement au JournalManager.
    /// </summary>
    public static event System.Action<int> OnIndiceAjoute;

    [Header("HUD")]
    [SerializeField] private TMP_Text compteurHUD;

    [Header("Flash rouge")]
    [Tooltip("Durée en secondes pendant laquelle le compteur reste " +
             "rouge quand une nouvelle entrée est ajoutée au journal.")]
    [SerializeField] private float _dureeFlashRouge = 2f;

    // ── État interne ──────────────────────────────────────────

    private Color _couleurNormale = Color.white;
    private Coroutine _coroutineFlash;

    // ── Unity ────────────────────────────────────────────────

    private void Awake()
    {
        // Assure que le JournalManager est un singleton persistant
        if (Instance != null && Instance != this)
        {
            // FIX CRITIQUE (bug indices invisibles scene1) :
            // Si ce GameObject (doublon) a des enfants, ceux-ci seront
            // detruits avec lui par Destroy(gameObject). Cas observe :
            // gestion_journal de scene1 contient les indices de scene1
            // dans sa hierarchie. Sans ce fix, ils disparaissent au load.
            // Solution : on reparente les enfants du doublon sous le
            // singleton persistant AVANT de detruire ce GameObject.
            int nbEnfantsTransferes = 0;
            while (transform.childCount > 0)
            {
                Transform enfant = transform.GetChild(0);
                enfant.SetParent(Instance.transform, true);
                nbEnfantsTransferes++;
            }
            if (nbEnfantsTransferes > 0)
            {
                Debug.Log($"[JournalManager] Doublon '{name}' detecte avec " +
                    $"{nbEnfantsTransferes} enfant(s) — transferes au " +
                    "singleton persistant avant destruction.");
            }
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // CRITIQUE : DontDestroyOnLoad ne fonctionne QUE sur les
        // GameObjects ROOT. Si ce composant est sur un enfant (cas
        // observé : gestion_journal était sous un canvas dans scene1),
        // DDOL échoue silencieusement → l'objet est détruit au
        // LoadScene → le journal se vide en arrivant en scene2.
        // On le détache du parent AVANT de marquer DDOL.
        if (transform.parent != null)
        {
            Debug.LogWarning($"[JournalManager] '{name}' était parenté à " +
                $"'{transform.parent.name}'. Détachement pour que " +
                "DontDestroyOnLoad fonctionne.");
            transform.SetParent(null, true);
        }
        DontDestroyOnLoad(gameObject);

        // AUTO-FIND du compteur HUD à chaque chargement de scène. Sans ça,
        // le compteurHUD [SerializeField] pointe vers le TMP_Text de la
        // scène où le JournalManager a été instancié initialement (scene0),
        // qui est détruit au LoadScene → reference morte → le compteur des
        // autres scènes (scene1, 2, 3, 4) reste à 0 même quand entrees++.
        // L'auto-find evite d'avoir a ajouter un composant manuel sur
        // chaque TMP_Text dans Unity.
        SceneManager.sceneLoaded -= AuChargementScene;
        SceneManager.sceneLoaded += AuChargementScene;

        // Cas particulier : si le JournalManager est instancie APRES le
        // chargement de la scene courante (ce qui est le cas en scene0
        // ou si on demarre directement dans une autre scene), on doit
        // aussi declencher la recherche pour la scene en cours.
        AuChargementScene(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= AuChargementScene;
    }

    /// <summary>
    /// Cherche un TMP_Text dont le GameObject porte un nom typique de
    /// compteur d'indices journal, et l'enregistre comme compteurHUD.
    /// Strategie en 2 passes : d'abord scene chargee (priorite), puis
    /// fallback DDOL / autres scenes (cas typique : canvas_hud parente
    /// sous un GameObject DDOL → t.gameObject.scene = DontDestroyOnLoad,
    /// pas scene1_taverne1).
    /// </summary>
    private void AuChargementScene(Scene scene, LoadSceneMode mode)
    {
        // Noms de GameObjects acceptes (insensible a la casse, match partiel).
        string[] nomsCandidats = new string[]
        {
            "nombre_indice_journal",
            "compteur_indice_journal",
            "compteur_indices_journal",
            "compteur_journal",
            "compteur_indices_hud",
            "nombre_indices_hud"
        };

        var tousTexts = FindObjectsByType<TMP_Text>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        // PASS 1 : priorite a la scene chargee.
        TMP_Text trouveDansScene = ChercherDansListe(tousTexts, nomsCandidats,
            t => t.gameObject.scene == scene);
        if (trouveDansScene != null)
        {
            EnregistrerCompteurHUD(trouveDansScene);
            Debug.Log($"[JournalManager] Compteur HUD auto-decouvert dans " +
                $"scene '{scene.name}' : '{trouveDansScene.gameObject.name}'. " +
                $"Valeur initiale: {trouveDansScene.text}.");
            return;
        }

        // PASS 2 : fallback hors-scene (DDOL ou autres scenes additives).
        // Couvre le cas ou le canvas_hud est parente sous un GameObject DDOL.
        TMP_Text trouveDDOL = ChercherDansListe(tousTexts, nomsCandidats,
            t => t.gameObject.scene != scene);
        if (trouveDDOL != null)
        {
            EnregistrerCompteurHUD(trouveDDOL);
            Debug.Log($"[JournalManager] Compteur HUD auto-decouvert HORS " +
                $"scene chargee : '{trouveDDOL.gameObject.name}' " +
                $"(scene='{trouveDDOL.gameObject.scene.name}'). " +
                $"Cas typique : HUD parente sous DontDestroyOnLoad. " +
                $"Valeur initiale: {trouveDDOL.text}.");
            return;
        }

        // PASS 3 : DIAGNOSTIC complet. Liste tous les TMP_Text dispos pour
        // que le user voie pourquoi aucun match n'a ete fait. Limite a 30
        // pour eviter spam si la scene a tres beaucoup de textes.
        Debug.LogWarning($"[JournalManager] Aucun compteur HUD trouve pour " +
            $"scene '{scene.name}'. Diagnostic : {tousTexts.Length} " +
            $"TMP_Text(s) total dans le projet runtime. Cherche un nom " +
            $"contenant : {string.Join(", ", nomsCandidats)}.");

        int max = Mathf.Min(tousTexts.Length, 30);
        for (int i = 0; i < max; i++)
        {
            var t = tousTexts[i];
            if (t == null) continue;
            Debug.Log($"[JournalManager]   TMP_Text #{i}: " +
                $"'{t.gameObject.name}' " +
                $"(scene='{t.gameObject.scene.name}', " +
                $"actif={t.gameObject.activeInHierarchy}, " +
                $"text='{t.text}').");
        }
    }

    /// <summary>
    /// Helper : cherche dans une liste de TMP_Text le premier dont le nom
    /// (ou le nom du parent direct) match un des candidats ET passe le
    /// predicat de scene fourni. Le check du parent couvre le cas frequent
    /// ou le TMP_Text est un enfant nomme "Text" sous un GameObject parent
    /// porteur du nom semantique (ex : 'nombre_indice_journal/Text').
    /// </summary>
    private TMP_Text ChercherDansListe(TMP_Text[] liste,
        string[] nomsCandidats,
        System.Func<TMP_Text, bool> predicatScene)
    {
        foreach (var t in liste)
        {
            if (t == null) continue;
            if (!predicatScene(t)) continue;

            string nomSelf = t.gameObject.name.ToLower();
            string nomParent = t.transform.parent != null
                ? t.transform.parent.name.ToLower()
                : "";

            foreach (var cible in nomsCandidats)
            {
                string c = cible.ToLower();
                if (nomSelf.Contains(c) || nomParent.Contains(c))
                    return t;
            }
        }
        return null;
    }

    // ── API publique ──────────────────────────────────────────

    /// <summary>
    /// Appelé par le compteur HUD de chaque scène dans son Start()
    /// pour s'enregistrer auprès du manager persistant.
    /// Sans ça, la référence devient nulle après un changement de scène.
    /// </summary>
    public void EnregistrerCompteurHUD(TMP_Text compteur)
    {
        compteurHUD = compteur;

        // Sauvegarder la couleur d'origine du nouveau compteur
        if (compteurHUD != null)
            _couleurNormale = compteurHUD.color;

        // Annuler un flash en cours sur l'ancien compteur (devenu invalide)
        if (_coroutineFlash != null)
        {
            StopCoroutine(_coroutineFlash);
            _coroutineFlash = null;
        }

        MettreAJourCompteur();
    }

    public void AjouterEntreeJournal(Sprite icone, string titre,
        string description, string insight)
    {
        entrees.Add(new EntreeJournal(icone, titre, description, insight));
        MettreAJourCompteur();

        // Flash rouge : nouvelle entrée ajoutée
        if (compteurHUD != null)
        {
            if (_coroutineFlash != null)
                StopCoroutine(_coroutineFlash);
            _coroutineFlash = StartCoroutine(FlashRougeCompteur());
        }

        // Notifier les abonnes (ex: compteurIndicesScene) qu'un indice
        // vient d'etre ajoute. Diffuse le nombre total d'entrees apres
        // l'ajout. Try/catch pour qu'un abonne en erreur ne bloque pas
        // l'ajout d'indices.
        try { OnIndiceAjoute?.Invoke(entrees.Count); }
        catch (System.Exception e)
        {
            Debug.LogError($"[JournalManager] Erreur dans un handler " +
                $"OnIndiceAjoute : {e.Message}");
        }
    }

    public void MettreAJourCompteur()
    {
        // STRATEGIE ANTI-CACHE : ne plus se fier au [SerializeField]
        // compteurHUD. Cause profonde du bug "scene1 reste a 0" :
        //   - JournalManager DDOL persiste depuis scene0
        //   - compteurHUD pointait vers TMP_Text scene0 (Inspector)
        //   - Au LoadScene scene1, ce TMP_Text est detruit (zombie)
        //   - Mes precedents auto-find pouvaient assigner un mauvais
        //     candidat (DDOL, scene0 zombie, autre TMP_Text similaire)
        //     → compteurHUD != null mais pointe vers la mauvaise UI
        //     → assignation .text sans effet visuel
        //
        // Solution : a CHAQUE appel, re-chercher le TMP_Text en
        // PRIORITE celui qui est ACTIF dans la scene ACTUELLE (= la
        // scene ou le joueur est, donc l'UI qu'il voit a l'ecran).
        // Cout : 1 FindObjectsByType par ramassage d'indice. OK.
        TMP_Text bonCompteur = TrouverMeilleurCompteur();
        if (bonCompteur == null)
        {
            // Garde le compteurHUD precedent comme fallback (cas tres
            // rare : aucun TMP_Text matching trouve). Ne plante pas.
            if (compteurHUD != null)
            {
                compteurHUD.text = entrees.Count.ToString();
            }
            return;
        }

        // Si le meilleur compteur trouve est DIFFERENT du cache, on
        // met a jour (et logue pour visibilite).
        if (bonCompteur != compteurHUD)
        {
            string sceneNom = bonCompteur.gameObject.scene.name;
            Debug.Log($"[JournalManager] Compteur HUD redirige vers " +
                $"'{bonCompteur.gameObject.name}' (parent=" +
                $"'{(bonCompteur.transform.parent != null ? bonCompteur.transform.parent.name : "(racine)")}', " +
                $"scene='{sceneNom}', actif=" +
                $"{bonCompteur.gameObject.activeInHierarchy}).");
            compteurHUD = bonCompteur;
            _couleurNormale = bonCompteur.color;
        }

        bonCompteur.text = entrees.Count.ToString();
    }

    /// <summary>
    /// Trouve le MEILLEUR TMP_Text compteur d'indices journal selon
    /// 3 niveaux de priorite :
    /// 1. ACTIF dans la scene actuelle (= ce que le joueur voit)
    /// 2. ACTIF dans DontDestroyOnLoad (HUD partage entre scenes)
    /// 3. ACTIF dans n'importe quelle scene (fallback)
    /// Retourne null si aucun candidat.
    /// </summary>
    private TMP_Text TrouverMeilleurCompteur()
    {
        string[] nomsCandidats = new string[]
        {
            "nombre_indice_journal",
            "compteur_indice_journal",
            "compteur_indices_journal",
            "compteur_journal",
            "compteur_indices_hud",
            "nombre_indices_hud"
        };

        var tousTexts = FindObjectsByType<TMP_Text>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        var sceneActuelle = SceneManager.GetActiveScene();

        // Niveau 1 : ACTIF dans scene actuelle (priorite max — c'est ce
        // que le joueur voit a l'ecran).
        TMP_Text n1 = ChercherSelonPredicat(tousTexts, nomsCandidats,
            t => t.gameObject.activeInHierarchy
                && t.gameObject.scene == sceneActuelle);
        if (n1 != null) return n1;

        // Niveau 2 : ACTIF dans DontDestroyOnLoad (HUD partage entre
        // scenes — cas typique : arborescence_canvas_ui est DDOL).
        TMP_Text n2 = ChercherSelonPredicat(tousTexts, nomsCandidats,
            t => t.gameObject.activeInHierarchy
                && t.gameObject.scene.name == "DontDestroyOnLoad");
        if (n2 != null) return n2;

        // Niveau 3 : n'importe quel ACTIF (fallback).
        TMP_Text n3 = ChercherSelonPredicat(tousTexts, nomsCandidats,
            t => t.gameObject.activeInHierarchy);
        if (n3 != null) return n3;

        return null;
    }

    /// <summary>
    /// Helper : cherche le premier TMP_Text dont le nom (ou nom du parent)
    /// match un des candidats ET satisfait le predicat fourni.
    /// </summary>
    private TMP_Text ChercherSelonPredicat(TMP_Text[] liste,
        string[] nomsCandidats,
        System.Func<TMP_Text, bool> predicat)
    {
        foreach (var t in liste)
        {
            if (t == null) continue;
            if (!predicat(t)) continue;

            string nomSelf = t.gameObject.name.ToLower();
            string nomParent = t.transform.parent != null
                ? t.transform.parent.name.ToLower()
                : "";

            foreach (var cible in nomsCandidats)
            {
                string c = cible.ToLower();
                if (nomSelf.Contains(c) || nomParent.Contains(c))
                    return t;
            }
        }
        return null;
    }

    // ── Coroutine flash ───────────────────────────────────────

    private IEnumerator FlashRougeCompteur()
    {
        compteurHUD.color = Color.red;
        yield return new WaitForSecondsRealtime(_dureeFlashRouge);

        // Remettre la couleur normale seulement si le compteur est
        // toujours le même (pas changé de scène entre-temps)
        if (compteurHUD != null)
            compteurHUD.color = _couleurNormale;

        _coroutineFlash = null;
    }

    // ── Classes internes ──────────────────────────────────────

    [System.Serializable]
    public class EntreeJournal
    {
        public Sprite icone;
        public string titre;
        public string description;
        public string insight;

        public EntreeJournal(Sprite i, string t, string d, string ins)
        {
            icone = i;
            titre = t;
            description = d;
            insight = ins;
        }
    }
}
