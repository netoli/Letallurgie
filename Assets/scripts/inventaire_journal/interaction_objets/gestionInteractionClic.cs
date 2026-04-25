// ============================================================
// gestionInteractionClic.cs
// ------------------------------------------------------------
// Auteur      : Fanny Fortier
// Date        : 22/04/2026
// ------------------------------------------------------------
// Description :
//   Attaché sur la caméra first person. Au clic gauche, envoie
//   un raycast depuis le centre de l'écran. Si l'objet touché
//   a le tag "indice" ou "obj_int", appelle la méthode de ramassage
// ------------------------------------------------------------
// Dépendances :
//   - RamasserIndice.cs
// ============================================================

using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public class gestionInteractionClic : MonoBehaviour
{

    [Header("Paramètres")]
    [SerializeField] private float distObjet = 3f;
    [SerializeField] private LayerMask coucheObjet;
    [Header("Render")]
    [SerializeField] private Camera cam;
    [Header("Interactivité")]
    [SerializeField] private gestionPointeur pointeur;

    private RamasserIndice _indiceVise;
    private objetRamassable _objetVise;
    private gestionHighlightHover _highlightVise;
    private DialogueTuto _tavernierVise;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        _DetecterObjet();

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (_indiceVise != null)
                _indiceVise.Ramasser();
            else if (_objetVise != null)
                _objetVise.Ramasser();
            else if (_tavernierVise != null)
                _tavernierVise.Interagir();
        }


    }

    private void _DetecterObjet()
    {
        // Raycast du centre vers l'objet visé
        Ray rayon = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));


        if (Physics.Raycast(rayon, out RaycastHit impact, distObjet, coucheObjet))
        {

            gestionHighlightHover highlight = impact.collider.GetComponentInParent<gestionHighlightHover>();
            if (highlight != null)
            {
                // Si on change d'objet visé, on enlève l'ancien highlight
                if (_highlightVise != highlight)
                {
                    if (_highlightVise != null)
                        _highlightVise.Highlighter(false);

                    _highlightVise = highlight;
                    _highlightVise.Highlighter(true);
                }
            }
            else
            {
                // Si on ne vise plus un objet highlightable
                if (_highlightVise != null)
                {
                    _highlightVise.Highlighter(false);
                    _highlightVise = null;
                }
            }

            //Débug console pour vérifier que le raycast touche un objet
            //Debug.Log("Raycast touche : " + impact.collider.name + " | Tag : " + impact.collider.tag);

            string tag = impact.collider.tag;



            if (tag == "indice")
            {
                _indiceVise = impact.collider.GetComponentInParent<RamasserIndice>();
                _objetVise = null;
                _tavernierVise = null;
                pointeur.ChangerEtat(gestionPointeur.EtatPointeur.Interactif);
                _highlightVise?.Highlighter(true);
            }
            else if (tag == "obj_int")
            {
                _objetVise = impact.collider.GetComponentInParent<objetRamassable>();
                _indiceVise = null;
                _tavernierVise = null;
                pointeur.ChangerEtat(gestionPointeur.EtatPointeur.Interactif);
            }
            else if (tag == "tavernier")
            {
                _tavernierVise = impact.collider.GetComponentInParent<DialogueTuto>();
                _indiceVise = null;
                _objetVise = null;
                pointeur.ChangerEtat(gestionPointeur.EtatPointeur.Interactif);
                _highlightVise?.Highlighter(true);
            }

            else
            {
                _indiceVise = null;
                _objetVise = null;
                _tavernierVise = null;
                pointeur.ChangerEtat(gestionPointeur.EtatPointeur.Defaut);
            }
        }
        else
        {
            _indiceVise = null;
            _objetVise = null;
            _tavernierVise = null;
            pointeur.ChangerEtat(gestionPointeur.EtatPointeur.Defaut);
            if (_highlightVise != null)
            {
                _highlightVise.Highlighter(false);
                _highlightVise = null;
            }
        }


    }
}
