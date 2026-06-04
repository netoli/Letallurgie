using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class gestionSelectionInventaire : MonoBehaviour
{
    private static gestionSelectionInventaire _instance;

    // Flag pour empecher l'auto-creation pendant la fermeture de la
    // scene ou de l'application. Sinon les OnDisable() des autres
    // scripts (qui se desabonnent de onSelectionChangee) declenchent
    // la creation du singleton APRES que Unity ait commence a
    // detruire la scene, ce qui genere le warning :
    // "Some objects were not cleaned up when closing the scene".
    private static bool _isShuttingDown = false;

    // Singleton auto-cree : si aucun GameObject n'a ce composant
    // dans la scene, on en cree un automatiquement au premier acces.
    // Comme ca, slotObjetInventaire.OnPointerClick fonctionne meme
    // si on a oublie d'ajouter le manager a la scene.
    public static gestionSelectionInventaire Instance
    {
        get
        {
            // Pendant le shutdown, ne pas recreer le singleton.
            // Les callers font deja un null-check avant utilisation.
            if (_isShuttingDown) return null;

            if (_instance == null)
            {
                _instance = FindFirstObjectByType<gestionSelectionInventaire>(
                    FindObjectsInactive.Include);

                if (_instance == null)
                {
                    var go = new GameObject("gestion_selection_inventaire_auto");
                    _instance = go.AddComponent<gestionSelectionInventaire>();
                    DontDestroyOnLoad(go);
                    Debug.Log("[gestionSelectionInventaire] Singleton " +
                        "auto-cree (aucun n'existait dans la scene).");
                }
            }
            return _instance;
        }
    }

    void OnApplicationQuit()
    {
        _isShuttingDown = true;
    }

    void OnDestroy()
    {
        // BUG HISTORIQUE : ce code mettait _isShuttingDown = true a
        // CHAQUE destroy de l'instance — y compris lors d'un LoadScene
        // non-DDOL. Resultat : Instance retournait null pour le reste
        // de la session, cassant le clic d'inventaire en scene2 quand
        // on venait de scene0. Maintenant, Awake fait
        // DontDestroyOnLoad → OnDestroy n'est appele qu'a la fermeture
        // du jeu (geree par OnApplicationQuit). On nettoie juste le
        // pointeur sans toucher au flag _isShuttingDown.
        if (_instance == this)
        {
            _instance = null;
        }
    }

    // En Editor, les champs statiques persistent entre les sessions Play
    // (Domain Reload optionnel). Sans ce reset, _isShuttingDown resterait
    // a true apres le premier stop et le singleton ne se creerait plus
    // a la prochaine session Play. RuntimeInitializeOnLoadMethod garantit
    // un etat propre au demarrage de chaque session.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStaticAuDemarrage()
    {
        _isShuttingDown = false;
        _instance = null;
    }

    public event Action<objetInventaire> onSelectionChangee;

    private objetInventaire objetSelectionne;

    void Awake()
    {
        // Si un autre singleton existe deja, on detruit ce doublon.
        if (_instance != null && _instance != this)
        {
            // FIX (analogue JournalManager) : si ce doublon a des enfants,
            // les transferer au singleton persistant pour qu'ils ne soient
            // pas detruits avec ce GameObject. Cas rare ici car gestion_
            // selection_inventaire n'a pas typiquement d'enfants, mais on
            // applique le pattern par robustesse.
            int nbEnfantsTransferes = 0;
            while (transform.childCount > 0)
            {
                Transform enfant = transform.GetChild(0);
                enfant.SetParent(_instance.transform, true);
                nbEnfantsTransferes++;
            }
            if (nbEnfantsTransferes > 0)
            {
                Debug.Log($"[gestionSelectionInventaire] Doublon '{name}' " +
                    $"detecte avec {nbEnfantsTransferes} enfant(s) — " +
                    "transferes au singleton avant destruction.");
            }
            Destroy(gameObject);
            return;
        }
        _instance = this;

        // CRITIQUE : detache du parent + DontDestroyOnLoad. Sinon le
        // GameObject est detruit au prochain LoadScene, ce qui declenche
        // OnDestroy() → _isShuttingDown = true → le getter Instance
        // retourne null pour TOUTE la session. Le clic d'inventaire ne
        // marche plus en scene2 si on vient de scene0.
        if (transform.parent != null)
        {
            Debug.LogWarning($"[gestionSelectionInventaire] '{name}' " +
                $"était parenté à '{transform.parent.name}'. " +
                "Détachement pour que DontDestroyOnLoad fonctionne.");
            transform.SetParent(null, true);
        }
        DontDestroyOnLoad(gameObject);
    }

    public void Selectionner(objetInventaire objet)
    {
        if (objetSelectionne == objet)
        {
            Deselectionner();
            return;
        }
        objetSelectionne = objet;
        onSelectionChangee?.Invoke(objetSelectionne);
    }

    public void Deselectionner()
    {
        objetSelectionne = null;
        onSelectionChangee?.Invoke(null);
    }

    public objetInventaire ObtenirSelection()
    {
        return objetSelectionne;
    }

    public bool AQuelqueChoseDeSelectionne()
    {
        return objetSelectionne != null;
    }

    void Update()
    {
        if (Keyboard.current != null
            && Keyboard.current.escapeKey.wasPressedThisFrame
            && AQuelqueChoseDeSelectionne())
        {
            Deselectionner();
        }
    }
}