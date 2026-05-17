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

    [Tooltip("Si coche, l'orientation n'est PAS verifiee lors du " +
        "placement (la piece accepte toutes les orientations). " +
        "Utile pour les objets qui n'ont pas de notion d'orientation " +
        "(ex: une bouteille, un verre).")]
    public bool ignorerOrientation = false;

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

            // Applique l'echelle definie sur l'asset objetInventaire,
            // tout en compensant la scale du parent (le snap). Ainsi
            // la taille finale reste celle voulue par le designer,
            // quelle que soit la scale du snap.
            AppliquerEchelleAuPlacement(ghostInstancie, piece);

            DesactiverColliders(ghostInstancie);
            DesactiverCamerasEtLights(ghostInstancie);
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

    /// <summary>
    /// Desactive toutes les Camera et Light enfants de l'instance.
    /// Necessaire pour les meshes importes depuis Blender qui
    /// embarquent souvent une FbxCamera et une Light dans le .fbx
    /// (ex: bottle.fbx). Sans cette desactivation, ces objets
    /// instancies prennent le controle du rendu (la Camera avec un
    /// depth eleve rend la Game view depuis l'angle de la bouteille,
    /// rendant le jeu inutilisable).
    /// </summary>
    private void DesactiverCamerasEtLights(GameObject instance)
    {
        if (instance == null) return;

        Camera[] cams = instance.GetComponentsInChildren<Camera>(true);
        foreach (Camera c in cams)
        {
            c.enabled = false;
            c.gameObject.SetActive(false);
        }

        Light[] lights = instance.GetComponentsInChildren<Light>(true);
        foreach (Light l in lights)
        {
            l.enabled = false;
            l.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Applique au modele instancie la localScale definie sur le root
    /// de son prefab, en compensant la scale du parent (le snap). Ainsi
    /// la taille visuelle finale correspond exactement a celle du
    /// prefab, quelle que soit la scale du snap_table.
    /// </summary>
    private void AppliquerEchelleAuPlacement(
        GameObject instance, objetInventaire piece)
    {
        if (instance == null || piece == null) return;
        if (piece.prefabModele3D == null) return;

        Vector3 echelleCible = piece.prefabModele3D.transform.localScale;
        Vector3 parentScale = transform.lossyScale;

        instance.transform.localScale = new Vector3(
            echelleCible.x / Mathf.Max(0.0001f, parentScale.x),
            echelleCible.y / Mathf.Max(0.0001f, parentScale.y),
            echelleCible.z / Mathf.Max(0.0001f, parentScale.z));

        // Diagnostic taille : imprime ce qui influence reellement la
        // taille visuelle finale. Si l'instance apparait trop grosse
        // malgre ce calcul, les coupables sont generalement les scales
        // internes du prefab (children) ou le Scale Factor du FBX.
        Bounds b = CalculerBoundsMonde(instance);
        Vector3 prefabRootScale = piece.prefabModele3D.transform.localScale;
        Debug.Log($"[pointAncrageTuyau:{name}] DIAG TAILLE pour " +
            $"'{piece.nomObjet}' | prefabRoot.localScale={prefabRootScale} " +
            $"| snap.lossyScale={parentScale} " +
            $"| instance.localScale={instance.transform.localScale} " +
            $"| instance.lossyScale={instance.transform.lossyScale} " +
            $"| bounding box monde={b.size}");
    }

    private Bounds CalculerBoundsMonde(GameObject go)
    {
        Renderer[] rends = go.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0)
            return new Bounds(go.transform.position, Vector3.zero);
        Bounds b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
        return b;
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
        bool bonneOrientation = ignorerOrientation
            ? true
            : orientationJoueur == orientationAttendue;

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

            AppliquerEchelleAuPlacement(pieceInstanciee, piece);
            DesactiverCamerasEtLights(pieceInstanciee);
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