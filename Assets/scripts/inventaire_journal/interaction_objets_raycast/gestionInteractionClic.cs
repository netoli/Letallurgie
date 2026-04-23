// ============================================================
// gestionInteractionClic.cs
// ------------------------------------------------------------
// Auteur      : Fanny Fortier
// Date        : 22/04/2026
// ------------------------------------------------------------
// Description :
//   Attaché sur la caméra first person. Au clic gauche, envoie
//   un raycast depuis le centre de l'écran. Si l'objet touché
//   a un composant RamasserIndice, déclenche le ramassage.
// ------------------------------------------------------------
// Dépendances :
//   - RamasserIndice.cs
// ============================================================

using UnityEngine;
using UnityEngine.InputSystem;

public class gestionInteractionClic : MonoBehaviour
{

    [Header("Paramètres")]
    [SerializeField] private float distObjet = 3f;
    [SerializeField] private LayerMask coucheObjet;
    [Header("Render")]
    [SerializeField] private Camera cam;

    private RamasserIndice _indiceVise;
    private objetRamassable _objetVise;

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
        }

}

    private void _DetecterObjet()
    {
        // Raycast du centre vers l'objet visé
        Ray rayon = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(rayon, out RaycastHit impact, distObjet, coucheObjet))
        {
            //Débug console pour vérifier que le raycast touche un objet
            Debug.Log("Raycast touche : " + impact.collider.name + " | Tag : " + impact.collider.tag);

            string tag = impact.collider.tag;

            if (tag == "indice")
            {
                impact.collider.TryGetComponent(out _indiceVise);
                _objetVise = null;
            }
            else if (tag == "obj_int")
            {
                impact.collider.TryGetComponent(out _objetVise);
                _indiceVise = null;
            }
            else
            {
                _indiceVise = null;
                _objetVise = null;
            }
        }
        else
        {
            _indiceVise = null;
            _objetVise = null;
        }
    }
}
