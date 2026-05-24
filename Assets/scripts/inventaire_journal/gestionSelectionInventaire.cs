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
        // Si c'est l'instance principale qui se fait detruire (changement
        // de scene, fin du jeu), on bloque toute auto-recreation pour
        // eviter le warning "spawned during scene close".
        if (_instance == this)
        {
            _isShuttingDown = true;
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
            Destroy(gameObject);
            return;
        }
        _instance = this;
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