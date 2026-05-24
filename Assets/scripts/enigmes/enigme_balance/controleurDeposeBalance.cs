// ============================================================
// controleurDeposeBalance.cs
// ------------------------------------------------------------
// Auteur      : Fanny Fortier
// Date créée  : 2026
// ------------------------------------------------------------
// Description :
//   Contrôleur de dépôt d'objets sur la balance (scene4_manoir).
//   Parallèle à controleurDeposeObjet (tutoriel/usine), mais
//   spécialisé pour l'énigme balance :
//
//   UX complète :
//     1. Joueur ouvre l'inventaire, clique un objet Alchimie
//        → gestionSelectionInventaire le sélectionne
//     2. Il vise le plateau de la balance avec la souris
//        → ghost translucide apparaît sur le snapPointBalance
//     3. Il clique gauche pour confirmer le dépôt
//        → objet posé, retiré de l'inventaire,
//          ZoneDepotJoueur notifié, inventaire fermé
//     4. Pour récupérer un objet : viser l'objet posé et
//        cliquer droit → retour dans l'inventaire
//
//   Ne touche pas au système tuyaux ni au tutoriel.
// ------------------------------------------------------------
// Dépendances :
//   - gestionSelectionInventaire : ObtenirSelection
//   - snapPointBalance           : AfficherGhost / Deposer / Retirer
//   - gestionInventaire          : RetirerObjet / AjouterObjet
//   - gestionInputsJeu           : FermerInventaire
// ------------------------------------------------------------
// Setup dans Unity Editor :
//   - Attacher ce script sur le même GameObject que la caméra
//     first-person du joueur dans scene4_manoir
//   - Assigner _cameraJoueur
// ============================================================

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class controleurDeposeBalance : MonoBehaviour
{
    // ===================== INSPECTEUR =====================

    [Header("Références")]
    [SerializeField] private Camera _cameraJoueur;

    [Header("Paramètres")]
    [Tooltip("Distance maximale du raycast vers les snap points.")]
    [SerializeField] private float _distanceRaycast = 35f;

    [Tooltip("Touche clavier pour confirmer le dépôt (utilisée quand l'inventaire est ouvert).")]
    [SerializeField] private UnityEngine.InputSystem.Key _toucheDepot = UnityEngine.InputSystem.Key.E;

    [Header("Debug")]
    [SerializeField] private bool _debugLogs = false;

    // ===================== ÉTAT INTERNE =====================

    private snapPointBalance _snapSousGhost;
    private gestionInputsJeu _cacheInputs;

    // ===================== UNITY =====================

    void OnEnable()
    {
        if (gestionSelectionInventaire.Instance != null)
            gestionSelectionInventaire.Instance.onSelectionChangee
                += SurSelectionChangee;
    }

    void OnDisable()
    {
        if (gestionSelectionInventaire.Instance != null)
            gestionSelectionInventaire.Instance.onSelectionChangee
                -= SurSelectionChangee;

        NettoyerGhost();
    }

    void Update()
    {
        if (gestionSelectionInventaire.Instance == null) return;
        if (Mouse.current == null) return;
        if (_cameraJoueur == null) return;

        objetInventaire selection =
            gestionSelectionInventaire.Instance.ObtenirSelection();

        // Ce contrôleur n'agit que pour la catégorie Alchimie
        if (selection == null
            || selection.categorie != CategorieObjet.Alchimie)
        {
            NettoyerGhost();
            return;
        }

        // Pas de prefab disponible → on ne peut pas montrer de ghost
        if (selection.prefab3D == null && selection.prefabModele3D == null)
        {
            NettoyerGhost();
            if (_debugLogs)
                Debug.LogWarning($"[controleurDeposeBalance] " +
                    $"'{selection.nomObjet}' n'a ni prefab3D " +
                    "ni prefabModele3D — ghost impossible.");
            return;
        }

        MettreAJourGhost(selection);

        // Clic gauche : déposer (si pas sur l'UI)
        if (Mouse.current.leftButton.wasPressedThisFrame
            && !PointeurEstSurUI())
        {
            TenterDeposer(selection);
        }

        // Touche clavier : déposer sans vérification UI
        // (fonctionne même quand l'inventaire est ouvert)
        if (Keyboard.current != null
            && Keyboard.current[_toucheDepot].wasPressedThisFrame)
        {
            TenterDeposer(selection);
        }

        // Clic droit : récupérer un objet déjà posé
        if (Mouse.current.rightButton.wasPressedThisFrame
            && !PointeurEstSurUI())
        {
            TenterRetirer();
        }
    }

    // ===================== MÉTHODES PRIVÉES =====================

    private bool PointeurEstSurUI()
    {
        return EventSystem.current != null
            && EventSystem.current.IsPointerOverGameObject();
    }

    /// <summary>
    /// Cherche un snapPointBalance sous le curseur via raycast.
    /// Utilise QueryTriggerInteraction.Collide pour inclure les
    /// colliders IsTrigger (cas fréquent pour les zones de dépôt).
    /// </summary>
    private snapPointBalance ObtenirSnapSousCurseur()
    {
        if (Mouse.current == null || _cameraJoueur == null)
            return null;

        // En mode verrouillé (FPS), le curseur est au centre de l'écran.
        // Mouse.current.position peut retourner la dernière position
        // libre (ex: pendant l'inventaire), d'où l'utilisation explicite
        // du centre quand le curseur est verrouillé.
        Vector2 posSouris = Cursor.lockState == CursorLockMode.Locked
            ? new Vector2(Screen.width * 0.5f, Screen.height * 0.5f)
            : Mouse.current.position.ReadValue();
        Ray rayon = _cameraJoueur.ScreenPointToRay(posSouris);

        if (Physics.Raycast(
            rayon,
            out RaycastHit hit,
            _distanceRaycast,
            ~0,
            QueryTriggerInteraction.Collide))
        {
            return hit.collider.GetComponentInParent<snapPointBalance>();
        }

        return null;
    }

    /// <summary>
    /// Met à jour l'affichage du ghost en fonction du snap point
    /// actuellement sous le curseur.
    /// </summary>
    private void MettreAJourGhost(objetInventaire selection)
    {
        snapPointBalance cible = ObtenirSnapSousCurseur();

        // On a changé de cible : nettoyer l'ancien ghost
        if (_snapSousGhost != null && _snapSousGhost != cible)
        {
            _snapSousGhost.CacherGhost();
            _snapSousGhost = null;
        }

        // Nouvelle cible libre : afficher le ghost
        if (cible != null && !cible.EstOccupe())
        {
            cible.AfficherGhost(selection);
            _snapSousGhost = cible;
        }
    }

    private void NettoyerGhost()
    {
        if (_snapSousGhost != null)
        {
            _snapSousGhost.CacherGhost();
            _snapSousGhost = null;
        }
    }

    private void SurSelectionChangee(objetInventaire nouvelle)
    {
        NettoyerGhost();

        if (_debugLogs)
        {
            if (nouvelle == null)
                Debug.Log("[controleurDeposeBalance] Sélection vidée.");
            else
                Debug.Log($"[controleurDeposeBalance] Sélection : " +
                    $"'{nouvelle.nomObjet}' " +
                    $"(catégorie={nouvelle.categorie})");
        }
    }

    /// <summary>
    /// Tente de déposer l'objet sélectionné sur le snap point
    /// actuellement sous le curseur.
    /// </summary>
    private void TenterDeposer(objetInventaire selection)
    {
        // Priorité au snap où le ghost est déjà affiché ;
        // évite un deuxième raycast qui pourrait rater
        snapPointBalance cible = _snapSousGhost != null
            ? _snapSousGhost
            : ObtenirSnapSousCurseur();

        if (cible == null)
        {
            if (_debugLogs)
                Debug.Log("[controleurDeposeBalance] Aucun snap sous le curseur.");
            return;
        }

        if (cible.EstOccupe())
        {
            if (_debugLogs)
                Debug.Log($"[controleurDeposeBalance] '{cible.name}' est déjà occupé.");
            return;
        }

        bool succes = cible.Deposer(selection);

        if (succes)
        {
            if (gestionInventaire.Instance != null)
                gestionInventaire.Instance.RetirerObjet(selection);

            gestionSelectionInventaire.Instance.Deselectionner();
            _snapSousGhost = null;

            FermerInventaireSiOuvert();

            if (_debugLogs)
                Debug.Log($"[controleurDeposeBalance] '{selection.nomObjet}' déposé.");
        }
    }

    /// <summary>
    /// Tente de retirer l'objet posé sur le snap point sous
    /// le curseur et de le retourner dans l'inventaire.
    /// </summary>
    private void TenterRetirer()
    {
        snapPointBalance cible = ObtenirSnapSousCurseur();

        if (cible == null || !cible.EstOccupe()) return;

        objetInventaire recupere = cible.Retirer();

        if (recupere != null && gestionInventaire.Instance != null)
        {
            gestionInventaire.Instance.AjouterObjet(recupere);

            if (_debugLogs)
                Debug.Log($"[controleurDeposeBalance] " +
                    $"'{recupere.nomObjet}' récupéré dans l'inventaire.");
        }
    }

    /// <summary>
    /// Ferme le panel inventaire après un dépôt réussi pour
    /// rendre le contrôle first-person au joueur.
    /// Inclut le double-toggle workaround macOS pour forcer
    /// la recapture de la souris (identique à controleurDeposeObjet).
    /// </summary>
    private void FermerInventaireSiOuvert()
    {
        if (_cacheInputs == null)
            _cacheInputs = FindAnyObjectByType<gestionInputsJeu>();

        if (_cacheInputs != null)
            _cacheInputs.FermerInventaire();

        // Forcer le verrouillage souris (workaround macOS Editor)
        Cursor.lockState = CursorLockMode.None;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
