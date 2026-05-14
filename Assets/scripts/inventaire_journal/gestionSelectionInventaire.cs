using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class gestionSelectionInventaire : MonoBehaviour
{
    private static gestionSelectionInventaire _instance;

    // Singleton auto-cree : si aucun GameObject n'a ce composant
    // dans la scene, on en cree un automatiquement au premier acces.
    // Comme ca, slotObjetInventaire.OnPointerClick fonctionne meme
    // si on a oublie d'ajouter le manager a la scene.
    public static gestionSelectionInventaire Instance
    {
        get
        {
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