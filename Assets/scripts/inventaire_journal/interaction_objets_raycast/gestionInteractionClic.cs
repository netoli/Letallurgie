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

public class gestionInteractionClic : MonoBehaviour
{

    [Header("Paramètres")]
    [SerializeField] private float distObjet = 3f;
    [SerializeField] private LayerMask coucheObjet;

    private RamasserIndice _indiceVise;
    private objetRamassable _tuyauVise;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        _DetecterObjet();

        if (Input.GetMouseButtonDown(0) && _indiceVise != null)
        {
            _indiceVise.Ramasser();
        }
        else if (Input.GetMouseButtonDown(0) && _tuyauVise != null)
        {
            _tuyauVise.Ramasser();
        }

    }

    private void _DetecterObjet()
    {
        // Raycast du centre vers l'objet visé
        Ray rayon = new Ray(transform.position, transform.forward);

        if (Physics.Raycast(rayon, out RaycastHit impact, distObjet, coucheObjet))
        {
            string tag = impact.collider.tag;

            if (tag == "indice")
            {
                // Récupérer son composant RamasserIndice
                impact.collider.TryGetComponent(out _indiceVise);
                _tuyauVise = null;
            }
            else if (tag == "tuyau")
            {
                impact.collider.TryGetComponent(out _tuyauVise);
                _indiceVise = null;
            }
            else
            {
                // L'objet est aucun des deux
                _indiceVise = null;
                _tuyauVise = null;
            }
        }
        else
        {
            // Aucun objet touché
            _indiceVise = null;
            _tuyauVise = null;
        }
    }
}
