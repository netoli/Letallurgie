using UnityEngine;
using UnityEngine.Events;

public enum resultatPlacement
{
    Succes,
    MauvaisePiece,
    MauvaiseOrientation,
    DejaRempli
}

public class pointAncrageTuyau : MonoBehaviour
{
    [Header("Configuration du trou")]
    public objetInventaire pieceAttendue;
    public orientationTuyau orientationAttendue;

    [Header("Rayon de détection (legacy, gardé pour compat)")]
    public float rayonDetection;

    [Header("Matériaux ghost")]
    [SerializeField] private Material materiauGhostNeutre;
    [SerializeField] private Material materiauGhostCorrect;
    [SerializeField] private Material materiauGhostIncorrect;

    [Header("Événements")]
    public UnityEvent onRempli;
    public UnityEvent<resultatPlacement> onPlacementTente;

    private bool estRempli;
    private GameObject pieceInstanciee;
    private GameObject ghostInstancie;

    public bool EstRempli()
    {
        return estRempli;
    }

    public void AfficherGhost(objetInventaire piece, orientationTuyau orientation)
    {
        if (estRempli) return;
        if (piece == null || piece.prefabModele3D == null) return;

        if (ghostInstancie == null)
        {
            ghostInstancie = Instantiate(
                piece.prefabModele3D,
                transform.position,
                Quaternion.Euler(0f, 0f, orientation.EnDegres()),
                transform);

            DesactiverColliders(ghostInstancie);
        }
        else
        {
            ghostInstancie.transform.rotation =
                Quaternion.Euler(0f, 0f, orientation.EnDegres());
        }

        AppliquerMateriauSelonValidite(ghostInstancie, piece);
    }

    private void AppliquerMateriauSelonValidite(GameObject ghost, objetInventaire piece)
    {
        Material materiauAUtiliser;

        if (piece == pieceAttendue)
        {
            materiauAUtiliser = materiauGhostCorrect;
        }
        else
        {
            materiauAUtiliser = materiauGhostIncorrect;
        }

        if (materiauAUtiliser == null)
        {
            materiauAUtiliser = materiauGhostNeutre;
        }

        Renderer[] renderers = ghost.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            r.material = materiauAUtiliser;
        }
    }

    private void DesactiverColliders(GameObject ghost)
    {
        Collider[] colliders = ghost.GetComponentsInChildren<Collider>();
        foreach (Collider c in colliders)
        {
            c.enabled = false;
        }
    }

    public void CacherGhost()
    {
        if (ghostInstancie != null)
        {
            Destroy(ghostInstancie);
            ghostInstancie = null;
        }
    }

    public resultatPlacement TenterPlacement(
        objetInventaire piece,
        orientationTuyau orientationJoueur)
    {
        if (estRempli)
        {
            onPlacementTente.Invoke(resultatPlacement.DejaRempli);
            return resultatPlacement.DejaRempli;
        }

        bool bonnePiece = piece == pieceAttendue;
        bool bonneOrientation = orientationJoueur == orientationAttendue;

        resultatPlacement resultat;
        if (bonnePiece && bonneOrientation)
        {
            Remplir(piece, orientationJoueur);
            resultat = resultatPlacement.Succes;
        }
        else if (bonnePiece && !bonneOrientation)
        {
            resultat = resultatPlacement.MauvaiseOrientation;
        }
        else
        {
            resultat = resultatPlacement.MauvaisePiece;
        }

        onPlacementTente.Invoke(resultat);
        return resultat;
    }

    private void Remplir(objetInventaire piece, orientationTuyau orientation)
    {
        estRempli = true;
        CacherGhost();

        Quaternion rotation = Quaternion.Euler(
            0f, 0f, orientation.EnDegres());

        if (piece.prefabModele3D != null)
        {
            pieceInstanciee = Instantiate(
                piece.prefabModele3D,
                transform.position,
                rotation,
                transform);
        }

        onRempli.Invoke();
    }

    public void ReinitialiserPourReset()
    {
        if (pieceInstanciee != null)
        {
            Destroy(pieceInstanciee);
        }
        CacherGhost();
        estRempli = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, rayonDetection);
    }
}