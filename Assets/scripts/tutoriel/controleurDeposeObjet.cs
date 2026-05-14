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
// UX identique a l'enigme tuyauterie :
//   1. Joueur clique un objet dans l'inventaire (selection)
//   2. Il bouge la souris vers le snap point cible -> ghost
//   3. Il appuie sur E pour confirmer la pose
//
// Une fois place, le pointAncrageTuyau gere lui-meme l'event
// onRempli (a configurer dans l'Inspector pour signaler un
// idAction a gestionChapitres).
// ============================================================

using UnityEngine;
using UnityEngine.InputSystem;

public class controleurDeposeObjet : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera cameraJoueur;

    [Header("Inputs (New Input System)")]
    [SerializeField] private Key touchePlacement = Key.E;

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
        if (Keyboard.current == null) return;
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

        // Placement (E)
        if (Keyboard.current[touchePlacement].wasPressedThisFrame)
        {
            TenterPlacer(selection);
        }
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

        Vector2 positionSouris = Mouse.current.position.ReadValue();
        Ray rayon = cameraJoueur.ScreenPointToRay(positionSouris);

        RaycastHit hit;
        if (Physics.Raycast(rayon, out hit, distanceRaycast))
        {
            return hit.collider.GetComponent<pointAncrageTuyau>();
        }

        return null;
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
        }
    }
}
