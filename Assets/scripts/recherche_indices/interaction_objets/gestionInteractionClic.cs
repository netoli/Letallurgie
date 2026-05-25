// ============================================================
// gestionInteractionClic.cs
// ------------------------------------------------------------
// Auteur      : Fanny Fortier
// Date        : 22/04/2026
// ------------------------------------------------------------
// Description :
//   Attach� sur la cam�ra first person. Au clic gauche, envoie
//   un raycast depuis le centre de l'�cran. Si l'objet touch�
//   a le tag "indice" ou "obj_int", appelle la m�thode de ramassage
// ------------------------------------------------------------
// D�pendances :
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
    [Tooltip("Camera utilisee pour le raycast d'interaction. Si laisse " +
        "vide, sera trouvee automatiquement au Start (Camera.main, puis " +
        "FindFirstObjectByType<Camera>). Permet d'utiliser ce script " +
        "dans un PREFAB instancie dans plusieurs scenes (chaque scene " +
        "ayant sa propre camera).")]
    [SerializeField] private Camera cam;
    [Header("Interactivité")]
    [Tooltip("gestionPointeur a utiliser. Si laisse vide, sera trouve " +
        "automatiquement au Start via FindFirstObjectByType.")]
    [SerializeField] private gestionPointeur pointeur;

    private RamasserIndice _indiceVise;
    private objetRamassable _objetVise;
    private gestionHighlightHover _highlightVise;
    private DialogueTuto _tavernierVise;
    private gestionInputsJeu _gestionInputs;


    void Start()
    {
        // Auto-resolution des references si elles n'ont pas ete assignees
        // dans l'Inspector. Permet l'utilisation en prefab cross-scenes.
        if (cam == null)
        {
            cam = Camera.main;
            if (cam == null)
                cam = FindFirstObjectByType<Camera>(
                    FindObjectsInactive.Include);
            if (cam == null)
                Debug.LogWarning("[gestionInteractionClic] Aucune Camera " +
                    "trouvee. Le raycast d'interaction ne fonctionnera pas.");
        }
        if (pointeur == null)
        {
            pointeur = FindFirstObjectByType<gestionPointeur>(
                FindObjectsInactive.Include);
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Garde-fou : si la camera ou le pointeur n'ont pas pu etre
        // trouves au Start, on ne peut pas faire le raycast ni changer
        // l'etat du pointeur. On retente une fois pour gerer les cas
        // ou la scene n'avait pas encore charge ses objets au Start.
        if (cam == null)
        {
            cam = Camera.main ?? FindFirstObjectByType<Camera>(
                FindObjectsInactive.Include);
            if (cam == null) return;
        }
        if (pointeur == null)
        {
            pointeur = FindFirstObjectByType<gestionPointeur>(
                FindObjectsInactive.Include);
            // Si toujours pas trouve, on continue quand meme : le raycast
            // marchera mais les changements d'etat pointeur sont skip.
        }

        // Lookup gestionInputsJeu si pas encore en cache.
        if (_gestionInputs == null)
            _gestionInputs = FindFirstObjectByType<gestionInputsJeu>(
                FindObjectsInactive.Include);

        // BLOQUER les interactions quand le jeu n'est PAS en gameplay
        // actif (menu pause, options, journal, inventaire, credits,
        // confirmations de menu, cinematique). Sans ce filtre, le
        // joueur pouvait ramasser un indice pendant que le menu
        // pause etait ouvert. JeuEnCoursActif = true uniquement quand
        // etatActuel == EnJeu et jeuActif == true.
        if (_gestionInputs != null && !_gestionInputs.JeuEnCoursActif)
            return;

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

    // Helper : ne change l'etat du pointeur que s'il est present.
    // Evite les NRE quand le prefab est utilise dans une scene sans
    // gestionPointeur (cas degrade).
    private void SetPointeur(gestionPointeur.EtatPointeur etat)
    {
        if (pointeur != null) pointeur.ChangerEtat(etat);
    }

    private void _DetecterObjet()
    {
        // Raycast du centre vers l'objet vis�
        Ray rayon = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));


        if (Physics.Raycast(rayon, out RaycastHit impact, distObjet, coucheObjet))
        {

            gestionHighlightHover highlight = impact.collider.GetComponentInParent<gestionHighlightHover>();
            if (highlight != null)
            {
                // Si on change d'objet vis�, on enl�ve l'ancien highlight
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

            //D�bug console pour v�rifier que le raycast touche un objet
            //Debug.Log("Raycast touche : " + impact.collider.name + " | Tag : " + impact.collider.tag);

            string tag = impact.collider.tag;



            if (tag == "indice")
            {
                _indiceVise = impact.collider.GetComponentInParent<RamasserIndice>();
                _objetVise = null;
                _tavernierVise = null;

                // Si l'indice est porte par un PNJ (le npc / pnj_mysterieux
                // de la taverne), on affiche le curseur PNJ plutot que
                // le curseur Interactif generique. Le clic continue
                // d'appeler RamasserIndice.Ramasser() qui jouera le
                // dialogue 4-repliques + ajoutera l'entree au journal.
                bool indiceEstPnj = _indiceVise != null
                    && (_indiceVise.gameObject.name.Contains("pnj_mysterieux")
                        || _indiceVise.gameObject.name == "npc");
                SetPointeur(indiceEstPnj
                    ? gestionPointeur.EtatPointeur.PNJ
                    : gestionPointeur.EtatPointeur.Interactif);
                _highlightVise?.Highlighter(true);
            }
            else if (tag == "obj_int")
            {
                _objetVise = impact.collider.GetComponentInParent<objetRamassable>();
                _indiceVise = null;
                _tavernierVise = null;
                SetPointeur(gestionPointeur.EtatPointeur.Interactif);
            }
            else if (tag == "tavernier" || tag == "pnj")
            {
                _tavernierVise = impact.collider.GetComponentInParent<DialogueTuto>();
                _indiceVise = null;
                _objetVise = null;
                SetPointeur(gestionPointeur.EtatPointeur.PNJ);
                _highlightVise?.Highlighter(true);
            }
            else
            {
                // Catch-all : tout autre objet de la scene (mur,
                // meuble, decor, pointeur tuto visuel, etc.) affiche
                // le pointeur Mecanique. Seul le "rien-en-face" du
                // raycast (branche else plus bas) reste en Defaut.
                _indiceVise = null;
                _objetVise = null;
                _tavernierVise = null;
                SetPointeur(gestionPointeur.EtatPointeur.Mecanique);
            }
        }
        else
        {
            _indiceVise = null;
            _objetVise = null;
            _tavernierVise = null;
            SetPointeur(gestionPointeur.EtatPointeur.Defaut);
            if (_highlightVise != null)
            {
                _highlightVise.Highlighter(false);
                _highlightVise = null;
            }
        }


    }
}
