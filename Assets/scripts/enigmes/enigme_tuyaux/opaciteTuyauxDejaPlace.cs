// ============================================================
// opaciteTuyauxDejaPlace.cs
// ------------------------------------------------------------
// Diminue l'opacite (alpha du material) des Renderers enfants
// quand l'enigme est lancee, et la restaure quand l'enigme est
// quittee. But : faire ressortir les snap_points vides par
// contraste avec les tuyaux deja en place qui deviennent
// semi-transparents.
//
// SETUP UNITY :
// 1. Attacher ce script sur le GameObject parent qui contient
//    tous les tuyaux deja en place (ex : 'tuyau_deja_en_place').
// 2. Les Renderers enfants doivent avoir un Material en mode
//    Transparent (sinon le changement d'alpha ne sera pas
//    visible). Si Opaque, on bascule en Transparent au runtime.
// 3. Ajuster opaciteEnigme (defaut 0.5).
//
// ACTIONS ECOUTEES (via gestionChapitres) :
// - 'enigme_tuyauterie_lancee' : opacite descend a opaciteEnigme
// - 'enigme_tuyauterie_quittee' : opacite remonte a 1.0
// ============================================================

using System.Collections.Generic;
using UnityEngine;

public class opaciteTuyauxDejaPlace : MonoBehaviour
{
    [Header("Actions ecoutees")]
    [SerializeField] private string idActionLancement = "enigme_tuyauterie_lancee";
    [SerializeField] private string idActionSortie = "enigme_tuyauterie_quittee";

    [Header("Opacite")]
    [Tooltip("Opacite des tuyaux deja places quand l'enigme est " +
        "lancee. 0 = invisible, 1 = pleine. Defaut 0.4 pour bien " +
        "faire ressortir les emplacements vides par contraste.")]
    [Range(0f, 1f)]
    [SerializeField] private float opaciteEnigme = 0.4f;

    [Tooltip("Duree (s) du fade entre opacite normale et opacite " +
        "energie. 0 = changement instantane.")]
    [SerializeField] private float dureeFade = 0.5f;

    private readonly List<Renderer> renderers = new List<Renderer>();
    private float opaciteCible = 1f;
    private float opaciteActuelle = 1f;

    void Awake()
    {
        // Collecter tous les Renderers enfants (incluant inactifs).
        var found = GetComponentsInChildren<Renderer>(true);
        renderers.AddRange(found);
        Debug.Log($"[opaciteTuyauxDejaPlace] {name} : " +
            $"{renderers.Count} Renderers collectes.");
    }

    void Start()
    {
        if (gestionChapitres.Instance != null)
            gestionChapitres.Instance.OnActionSignalee += AuActionSignalee;
    }

    void OnDestroy()
    {
        if (gestionChapitres.Instance != null)
            gestionChapitres.Instance.OnActionSignalee -= AuActionSignalee;
    }

    void Update()
    {
        // Fade progressif vers opaciteCible
        if (Mathf.Abs(opaciteActuelle - opaciteCible) > 0.001f)
        {
            if (dureeFade > 0f)
            {
                opaciteActuelle = Mathf.MoveTowards(
                    opaciteActuelle, opaciteCible,
                    Time.deltaTime / dureeFade);
            }
            else
            {
                opaciteActuelle = opaciteCible;
            }
            AppliquerOpacite(opaciteActuelle);
        }
    }

    private void AuActionSignalee(string id)
    {
        if (id == idActionLancement)
        {
            opaciteCible = opaciteEnigme;
            Debug.Log($"[opaciteTuyauxDejaPlace] Action '{id}' : " +
                $"opacite cible {opaciteEnigme}.");
        }
        else if (id == idActionSortie)
        {
            opaciteCible = 1f;
            Debug.Log($"[opaciteTuyauxDejaPlace] Action '{id}' : " +
                "opacite cible 1 (restauree).");
        }
    }

    private void AppliquerOpacite(float alpha)
    {
        foreach (var r in renderers)
        {
            if (r == null) continue;
            // On modifie .material (instance) plutot que .sharedMaterial
            // pour ne pas affecter d'autres GameObjects qui partagent le
            // meme material asset.
            foreach (var mat in r.materials)
            {
                if (mat == null) continue;
                if (mat.HasProperty("_Color"))
                {
                    Color c = mat.color;
                    c.a = alpha;
                    mat.color = c;
                }
                // URP Lit : la couleur est "_BaseColor"
                if (mat.HasProperty("_BaseColor"))
                {
                    Color c = mat.GetColor("_BaseColor");
                    c.a = alpha;
                    mat.SetColor("_BaseColor", c);
                }
                // Si le material est en Opaque, switch en Transparent
                // pour que l'alpha soit visible. (Heuristique simple
                // qui marche sur Standard et URP/Lit.)
                if (alpha < 1f && mat.HasProperty("_Surface"))
                {
                    mat.SetFloat("_Surface", 1); // 1 = Transparent en URP
                    mat.renderQueue = 3000;
                }
            }
        }
    }
}
