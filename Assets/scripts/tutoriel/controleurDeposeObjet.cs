// ============================================================
// controleurDeposeObjet.cs
// ------------------------------------------------------------
// Version simplifiee de controleurPlacementTuyau, utilisee pour
// les objets simples du tutoriel (bouteille, verre) qui doivent
// etre deposes sur un pointAncrageTuyau MAIS qui :
//   - n'ont pas de notion d'orientation (pas de touche R)
//   - ne dependent pas de l'enigme tuyauterie (pas de
//     gestionEnigmeTuyauterie pour valider le puzzle)
//   - peuvent appartenir a n'importe quelle categorie d'objet
//     (configurable dans l'Inspector)
//
// UX :
//   1. Joueur clique un objet dans l'inventaire (selection)
//   2. Il bouge la souris vers le snap point cible -> ghost
//   3. Il clique sur le snap point pour confirmer la pose
//      (les clics sur l'UI - slots d'inventaire - sont ignores)
//
// Une fois place, le pointAncrageTuyau gere lui-meme l'event
// onRempli (a configurer dans l'Inspector pour signaler un
// idAction a gestionChapitres).
// ============================================================

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class controleurDeposeObjet : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera cameraJoueur;

    // Placement par clic gauche souris sur le snap point.
    // Les clics sur l'UI (slots d'inventaire) sont automatiquement
    // ignores via EventSystem.IsPointerOverGameObject.

    [Header("Distance maximale du raycast")]
    [SerializeField] private float distanceRaycast = 10f;

    [Header("Categories d'objets acceptees")]
    [Tooltip("Seuls les objets de l'inventaire dont la categorie est " +
        "dans cette liste peuvent declencher l'apparition du ghost et " +
        "etre deposes. Ex: Tuto pour la bouteille du tavernier.")]
    [SerializeField] private CategorieObjet[] categoriesAcceptees =
        new[] { CategorieObjet.Tuto };

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private pointAncrageTuyau snapActuelSousGhost;

    // Suivi du dernier collider hit pour ne logger qu'au changement
    // (evite de spammer la console chaque frame).
    private string dernierColliderHitLogue = "<init>";

    void OnEnable()
    {
        if (gestionSelectionInventaire.Instance != null)
        {
            gestionSelectionInventaire.Instance.onSelectionChangee
                += SurSelectionChangee;
        }
    }

    void OnDisable()
    {
        if (gestionSelectionInventaire.Instance != null)
        {
            gestionSelectionInventaire.Instance.onSelectionChangee
                -= SurSelectionChangee;
        }
        NettoyerGhost();
    }

    void Update()
    {
        if (gestionSelectionInventaire.Instance == null) return;
        if (Mouse.current == null) return;
        if (cameraJoueur == null) return;

        objetInventaire selection =
            gestionSelectionInventaire.Instance.ObtenirSelection();

        if (selection == null)
        {
            NettoyerGhost();
            return;
        }

        if (!CategorieEstAcceptee(selection.categorie)
            || selection.prefabModele3D == null)
        {
            NettoyerGhost();
            return;
        }

        // Detection via raycast camera -> souris
        MettreAJourGhost(selection);

        // Placement par clic gauche, sauf si la souris est sur de l'UI
        // (un slot d'inventaire) - dans ce cas on laisse l'UI gerer.
        if (Mouse.current.leftButton.wasPressedThisFrame
            && !PointeurEstSurUI())
        {
            TenterPlacer(selection);
        }
    }

    private bool PointeurEstSurUI()
    {
        return EventSystem.current != null
            && EventSystem.current.IsPointerOverGameObject();
    }

    private bool CategorieEstAcceptee(CategorieObjet cat)
    {
        if (categoriesAcceptees == null) return false;
        foreach (var c in categoriesAcceptees)
        {
            if (c == cat) return true;
        }
        return false;
    }

    private pointAncrageTuyau ObtenirSnapSousCurseur()
    {
        if (Mouse.current == null) return null;
        if (cameraJoueur == null) return null;

        Vector2 positionSouris = Mouse.current.position.ReadValue();
        Ray rayon = cameraJoueur.ScreenPointToRay(positionSouris);

        // QueryTriggerInteraction.Collide pour inclure aussi les colliders
        // marques IsTrigger (au cas ou le snap est configure en trigger).
        RaycastHit hit;
        bool aHit = Physics.Raycast(
            rayon,
            out hit,
            distanceRaycast,
            ~0,
            QueryTriggerInteraction.Collide);

        pointAncrageTuyau snap = null;
        string nomCollider = aHit ? hit.collider.name : "(rien)";

        if (aHit)
        {
            // GetComponentInParent : si le pointAncrageTuyau est sur le
            // GameObject parent et que le collider est sur un enfant
            // visuel (cas courant pour snap_table_bouteille), on le
            // trouvera quand meme.
            snap = hit.collider.GetComponentInParent<pointAncrageTuyau>();
        }

        if (debugLogs && nomCollider != dernierColliderHitLogue)
        {
            dernierColliderHitLogue = nomCollider;
            if (!aHit)
            {
                Debug.Log("[controleurDeposeObjet] Raycast: rien sous le curseur.");
            }
            else if (snap == null)
            {
                Debug.Log($"[controleurDeposeObjet] Raycast: hit '{nomCollider}' " +
                    $"(layer={LayerMask.LayerToName(hit.collider.gameObject.layer)}) " +
                    "mais aucun pointAncrageTuyau dessus ni dans ses parents.");
            }
            else
            {
                Debug.Log($"[controleurDeposeObjet] Raycast: hit '{nomCollider}' " +
                    $"=> snap trouve '{snap.name}' (estRempli={snap.EstRempli()}).");
            }
        }

        return snap;
    }

    private void MettreAJourGhost(objetInventaire selection)
    {
        pointAncrageTuyau cible = ObtenirSnapSousCurseur();

        // On a change de cible : nettoyer l'ancien ghost
        if (snapActuelSousGhost != null && snapActuelSousGhost != cible)
        {
            snapActuelSousGhost.CacherGhost();
            snapActuelSousGhost = null;
        }

        // Nouvelle cible valide : afficher le ghost (orientation
        // Zero par defaut, ignoree par le snap si ignorerOrientation)
        if (cible != null && !cible.EstRempli())
        {
            cible.AfficherGhost(selection, orientationTuyau.Zero);
            snapActuelSousGhost = cible;
        }
    }

    private void NettoyerGhost()
    {
        if (snapActuelSousGhost != null)
        {
            snapActuelSousGhost.CacherGhost();
            snapActuelSousGhost = null;
        }
    }

    private void SurSelectionChangee(objetInventaire nouvelle)
    {
        NettoyerGhost();

        if (!debugLogs) return;

        if (nouvelle == null)
        {
            Debug.Log("[controleurDeposeObjet] DIAG | Selection videe.");
            return;
        }

        bool catOk = CategorieEstAcceptee(nouvelle.categorie);
        bool prefabOk = nouvelle.prefabModele3D != null;
        bool camOk = cameraJoueur != null;
        bool compEnabled = enabled;
        bool goActif = gameObject.activeInHierarchy;

        Debug.Log($"[controleurDeposeObjet] DIAG SELECTION " +
            $"| objet='{nouvelle.nomObjet}' " +
            $"| categorie={nouvelle.categorie} (acceptee={catOk}) " +
            $"| prefabModele3D={(prefabOk ? nouvelle.prefabModele3D.name : "NULL")} " +
            $"| cameraJoueur={(camOk ? cameraJoueur.name : "NULL")} " +
            $"| component.enabled={compEnabled} " +
            $"| GO.activeInHierarchy={goActif}");

        if (!catOk || !prefabOk || !camOk || !compEnabled || !goActif)
        {
            string raison = "";
            if (!catOk) raison += "categorie non acceptee. ";
            if (!prefabOk) raison += "prefabModele3D null sur l'asset. ";
            if (!camOk) raison += "cameraJoueur non assignee. ";
            if (!compEnabled) raison += "composant disabled. ";
            if (!goActif) raison += "GameObject inactif. ";
            Debug.LogWarning(
                "[controleurDeposeObjet] DIAG => BLOQUE : " + raison);
        }
        else
        {
            Debug.Log("[controleurDeposeObjet] DIAG => Preconditions OK. " +
                "Si le ghost n'apparait toujours pas, le probleme est le " +
                "raycast (collider absent, trigger=true, ou pas de " +
                "pointAncrageTuyau sur le collider hit).");
        }
    }

    private void TenterPlacer(objetInventaire selection)
    {
        pointAncrageTuyau cible = ObtenirSnapSousCurseur();

        if (cible == null)
        {
            if (debugLogs)
                Debug.Log("[controleurDeposeObjet] " +
                    "Aucun snap sous le curseur.");
            return;
        }

        if (debugLogs)
            Debug.Log("[controleurDeposeObjet] Snap vise: " + cible.name);

        resultatPlacement resultat =
            cible.TenterPlacement(selection, orientationTuyau.Zero);

        if (debugLogs)
            Debug.Log("[controleurDeposeObjet] Resultat: " + resultat);

        if (resultat == resultatPlacement.Succes)
        {
            if (gestionInventaire.Instance != null)
                gestionInventaire.Instance.RetirerObjet(selection);
            gestionSelectionInventaire.Instance.Deselectionner();
            snapActuelSousGhost = null;

            // Fermer l'inventaire pour rendre le controle au joueur :
            // sans ca, l'etat reste 'DansInventaire', la souris reste
            // deverrouillee, et la camera ne suit plus le regard du
            // joueur => impression d'etre fige.
            FermerInventaireSiOuvert();
        }
    }

    private gestionInputsJeu cacheInputs;

    private void FermerInventaireSiOuvert()
    {
        if (cacheInputs == null)
        {
            cacheInputs = FindAnyObjectByType<gestionInputsJeu>();
        }
        if (cacheInputs != null)
        {
            cacheInputs.FermerInventaire();
            if (debugLogs)
                Debug.Log("[controleurDeposeObjet] " +
                    "Inventaire ferme apres placement reussi.");
        }

        // Fix defensif : forcer explicitement le verrouillage souris.
        // Workaround macOS Editor : sur Mac, Cursor.lockState=Locked
        // ne capture pas la souris immediatement si la Game window n'a
        // pas le focus. Le truc consiste a passer par None puis Locked
        // dans la meme frame, ce qui force Unity a re-capturer.
        UnityEngine.Cursor.lockState =
            UnityEngine.CursorLockMode.None;
        UnityEngine.Cursor.lockState =
            UnityEngine.CursorLockMode.Locked;
        UnityEngine.Cursor.visible = false;

        if (debugLogs)
            Debug.Log("[controleurDeposeObjet] " +
                "Forcage souris locked apres placement " +
                "(double-toggle workaround macOS).");
    }
}
