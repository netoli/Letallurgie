// ============================================================
// visuelZoneEnigme.cs
// ------------------------------------------------------------
// Rend visible une zone trigger (BoxCollider invisible) en
// dessinant une boite semi-transparente qui epouse le collider.
//
// La zone de lancement de l'enigme tuyauterie est un BoxCollider
// sans rendu : le joueur ne la voit pas. Ce script materialise la
// zone avec un cube translucide aux dimensions du collider. Comme
// le joueur est confine dans cette zone pendant l'enigme, la boite
// montre aussi les limites de deplacement.
//
// AUTO : zoneLancementEnigme ajoute ce composant automatiquement
// s'il n'est pas deja present. Pour personnaliser la couleur ou
// fournir ton propre materiau, ajoute le composant toi-meme dans
// l'Inspector et regle les champs (l'instance auto utilise les
// valeurs par defaut).
// ============================================================

using UnityEngine;

public class visuelZoneEnigme : MonoBehaviour
{
    [Header("Apparence")]
    [Tooltip("Couleur (avec alpha) de la boite translucide. Utilisee " +
        "seulement si Materiau Override est vide.")]
    [SerializeField] private Color couleurZone =
        new Color(0.3f, 0.7f, 1f, 0.18f);

    [Tooltip("Materiau optionnel pour la boite. Si vide, un materiau " +
        "transparent URP est cree au runtime a partir de Couleur Zone. " +
        "Si la boite apparait opaque ou rose (shader introuvable), " +
        "glisse ici un materiau transparent que tu auras cree.")]
    [SerializeField] private Material materiauOverride;

    private GameObject boite;

    void Start()
    {
        BoxCollider bc = GetComponent<BoxCollider>();
        if (bc == null)
        {
            Debug.LogWarning($"[visuelZoneEnigme] {name} : pas de " +
                "BoxCollider sur ce GameObject, impossible de dessiner " +
                "la boite de zone.");
            return;
        }

        boite = GameObject.CreatePrimitive(PrimitiveType.Cube);
        boite.name = "visuel_zone_enigme";

        // Retirer le collider du cube : c'est un pur visuel, il ne doit
        // ni bloquer le joueur ni intercepter les raycasts d'interaction.
        Collider colCube = boite.GetComponent<Collider>();
        if (colCube != null) Destroy(colCube);

        // Caler la boite sur le BoxCollider (centre + taille, en local).
        // Le cube primitif fait 1x1x1 centre a l'origine : localScale =
        // bc.size suffit, et l'echelle du parent s'applique par-dessus
        // comme pour le collider.
        boite.transform.SetParent(transform, false);
        boite.transform.localPosition = bc.center;
        boite.transform.localRotation = Quaternion.identity;
        boite.transform.localScale = bc.size;

        Renderer rend = boite.GetComponent<Renderer>();
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rend.receiveShadows = false;
        rend.material = materiauOverride != null
            ? materiauOverride
            : CreerMateriauTransparent(couleurZone);
    }

    /// <summary>
    /// Cree un materiau URP transparent a la couleur donnee. Retombe sur
    /// l'Unlit URP puis le Standard si le shader Lit n'est pas trouve
    /// (projet non-URP). Les SetFloat sont gardes par HasProperty pour
    /// ne pas planter selon le shader retenu.
    /// </summary>
    private Material CreerMateriauTransparent(Color couleur)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material mat = new Material(shader);

        // Passage en mode transparent (blend alpha classique).
        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
        if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);
        if (mat.HasProperty("_SrcBlend"))
            mat.SetFloat("_SrcBlend",
                (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (mat.HasProperty("_DstBlend"))
            mat.SetFloat("_DstBlend",
                (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
        mat.DisableKeyword("_SURFACE_TYPE_OPAQUE");
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue =
            (int)UnityEngine.Rendering.RenderQueue.Transparent;

        // URP utilise _BaseColor ; le Standard utilise _Color.
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", couleur);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", couleur);

        return mat;
    }
}
