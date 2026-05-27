using UnityEngine;
using UnityEngine.InputSystem;

public class controleurPlacementTuyau : MonoBehaviour
{
    [Header("Références")]
    [SerializeField] private gestionEnigmeTuyauterie gestionnaireEnigme;
    [SerializeField] private Camera cameraJoueur;
    [SerializeField] private iconeFlottanteCurseur iconeFlottante;

    [Header("Inputs (New Input System)")]
    [SerializeField] private Key touchePlacement = Key.E;
    [SerializeField] private Key toucheRotation = Key.R;

    [Header("Distance maximale du raycast")]
    [SerializeField] private float distanceRaycast = 10f;

    private orientationTuyau rotationActuelle;
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

        // Le placement n'est autorise QUE pendant que l'enigme est active
        // (joueur a fait Enter dans la zone). Sans ce check, le joueur
        // pouvait placer des tuyaux meme sans avoir lance l'enigme.
        if (!zoneLancementEnigme.EnigmeActive)
        {
            NettoyerGhost();
            return;
        }

        objetInventaire selection =
            gestionSelectionInventaire.Instance.ObtenirSelection();

        if (selection == null)
        {
            NettoyerGhost();
            return;
        }

        if (selection.categorie != CategorieObjet.Tuyaux
            || selection.prefabModele3D == null)
        {
            NettoyerGhost();
            return;
        }

        // Rotation
        if (Keyboard.current[toucheRotation].wasPressedThisFrame)
        {
            rotationActuelle = rotationActuelle.Suivante();
            if (iconeFlottante != null)
            {
                iconeFlottante.MettreAJourRotation(rotationActuelle);
            }
        }

        // Détection via raycast caméra → souris
        MettreAJourGhost(selection);

        // Placement
        if (Keyboard.current[touchePlacement].wasPressedThisFrame)
        {
            TenterPlacer(selection);
        }
    }

    private pointAncrageTuyau ObtenirSnapSousCurseur()
    {
        if (Mouse.current == null) return null;

        Vector2 positionSouris = Mouse.current.position.ReadValue();
        Ray rayon = cameraJoueur.ScreenPointToRay(positionSouris);

        // RaycastAll : on ramasse TOUS les colliders sur le rayon, et on
        // cherche le plus proche qui porte un pointAncrageTuyau. Sans ca,
        // un tuyau deja en place (Collider sur son mesh) peut bloquer le
        // ray avant qu'il n'atteigne le snap_point derriere.
        var hits = Physics.RaycastAll(rayon, distanceRaycast);
        if (hits == null || hits.Length == 0) return null;

        // Trier par distance croissante pour cibler le snap le plus proche
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var h in hits)
        {
            if (h.collider == null) continue;
            var snap = h.collider.GetComponent<pointAncrageTuyau>();
            if (snap != null) return snap;
        }
        return null;
    }

    private void MettreAJourGhost(objetInventaire selection)
    {
        pointAncrageTuyau cible = ObtenirSnapSousCurseur();

        // On a changé de cible : nettoyer l'ancien ghost
        if (snapActuelSousGhost != null && snapActuelSousGhost != cible)
        {
            snapActuelSousGhost.CacherGhost();
            snapActuelSousGhost = null;
        }

        // Nouvelle cible : afficher le ghost
        if (cible != null && !cible.EstRempli())
        {
            cible.AfficherGhost(selection, rotationActuelle);
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
        rotationActuelle = orientationTuyau.Zero;
        if (iconeFlottante != null)
        {
            iconeFlottante.MettreAJourRotation(rotationActuelle);
        }
        NettoyerGhost();
    }

    private void TenterPlacer(objetInventaire selection)
    {
        pointAncrageTuyau cible = ObtenirSnapSousCurseur();

        if (cible == null)
        {
            Debug.Log("TenterPlacer - aucun snap sous le curseur");
            return;
        }

        Debug.Log("TenterPlacer - snap visé: " + cible.name);

        resultatPlacement resultat =
            cible.TenterPlacement(selection, rotationActuelle);

        Debug.Log("TenterPlacer - résultat: " + resultat);

        if (resultat == resultatPlacement.Succes)
        {
            gestionInventaire.Instance.RetirerObjet(selection);
            gestionSelectionInventaire.Instance.Deselectionner();
            rotationActuelle = orientationTuyau.Zero;
            snapActuelSousGhost = null;
        }
    }
}