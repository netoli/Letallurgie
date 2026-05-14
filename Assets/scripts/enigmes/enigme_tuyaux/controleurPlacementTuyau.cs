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

        RaycastHit hit;
        if (Physics.Raycast(rayon, out hit, distanceRaycast))
        {
            pointAncrageTuyau snap =
                hit.collider.GetComponent<pointAncrageTuyau>();
            return snap;
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