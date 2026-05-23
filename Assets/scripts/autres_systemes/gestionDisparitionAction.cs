using System.Collections;
using UnityEngine;

/// <summary>
/// [OBSOLETE pour les snap points] : utiliser plutot l'option
/// 'desactiverApresRemplir' directement sur pointAncrageTuyau, plus
/// integree au cycle de vie naturel du snap.
///
/// Ce script reste utile pour les cas generaux ou tu veux desactiver
/// un GameObject quelconque (pas un snap) en reaction a une action
/// signalee — par ex. un indicateur visuel, un helper UI, un effet
/// particule de guidage, etc.
///
/// Desactive automatiquement un GameObject lorsque une action specifique
/// est signalee via gestionChapitres.SignalerAction. Generique :
/// reutilisable pour faire disparaitre tout indicateur, helper visuel,
/// etc. apres que le joueur ait accompli l'action correspondante.
///
/// EXEMPLE D'USAGE :
/// - snap_table_bouteille avec idActionDisparition = "bouteille_deposee_table"
///   → disparait des que le joueur a depose la bouteille sur la table.
/// - snap_comptoir_verre_pnj avec idActionDisparition = "verre_depose_comptoir"
///   → disparait des que le verre est rendu au comptoir.
///
/// SETUP UNITY :
/// 1. Selectionner le GameObject a faire disparaitre (ex : snap_table_bouteille).
/// 2. Add Component → gestionDisparitionAction.
/// 3. Inspector : renseigner idActionDisparition avec l'idAction qui doit
///    declencher la disparition (ex : "bouteille_deposee_table").
/// 4. Optionnel : activer dureeFadeOut > 0 pour une disparition en fondu
///    au lieu d'une coupure brusque.
///
/// LIMITES :
/// - Le GameObject doit etre actif au demarrage (sinon Awake n'est pas
///   appele et le script ne s'abonne pas).
/// - Le script utilise SetActive(false), donc le GameObject est totalement
///   desactive (pas juste cache). Si tu veux juste cacher visuellement,
///   utilise dureeFadeOut + un CanvasGroup ou un Renderer.
/// </summary>
public class gestionDisparitionAction : MonoBehaviour
{
    [Header("Declenchement")]
    [Tooltip("ID d'action qui declenche la disparition de ce GameObject. " +
        "Doit correspondre a une action signalee via gestionChapitres." +
        "SignalerAction. Ex : 'bouteille_deposee_table'.")]
    [SerializeField] private string idActionDisparition;

    [Tooltip("Si > 0, fait un fade out (alpha 1->0) sur cette duree (en " +
        "secondes) avant de desactiver le GameObject. Necessite que le " +
        "GameObject ait un Renderer (3D) ou un CanvasGroup (UI). Si 0 " +
        "(defaut), disparition instantanee via SetActive(false).")]
    [SerializeField] private float dureeFadeOut = 0f;

    [Tooltip("(Optionnel) Delai avant de declencher la disparition apres " +
        "que l'action soit signalee. Utile pour laisser un instant de " +
        "respiration narratif. Default 0.")]
    [SerializeField] private float delaiAvantDisparition = 0f;

    // Flag pour eviter de redeclencher si l'action est signalee plusieurs
    // fois (par ex. si l'utilisateur depose puis reprend puis redepose).
    private bool disparitionDeclenchee = false;

    void Start()
    {
        if (string.IsNullOrEmpty(idActionDisparition))
        {
            Debug.LogWarning($"[DisparitionAction] {name} : " +
                "idActionDisparition n'est pas renseigne, le script ne " +
                "fera rien. Renseigne-le dans l'Inspector.");
            return;
        }

        if (gestionChapitres.Instance == null)
        {
            Debug.LogWarning($"[DisparitionAction] {name} : " +
                "gestionChapitres.Instance introuvable au Start. " +
                "Le script ne pourra pas ecouter les actions.");
            return;
        }

        gestionChapitres.Instance.OnActionSignalee += AuActionSignalee;
    }

    void OnDestroy()
    {
        if (gestionChapitres.Instance != null)
            gestionChapitres.Instance.OnActionSignalee -= AuActionSignalee;
    }

    private void AuActionSignalee(string idAction)
    {
        if (disparitionDeclenchee) return;
        if (idAction != idActionDisparition) return;

        disparitionDeclenchee = true;
        Debug.Log($"[DisparitionAction] {name} : action " +
            $"'{idAction}' signalee, disparition dans " +
            $"{delaiAvantDisparition}s.");
        StartCoroutine(DisparaitreApresDelai());
    }

    private IEnumerator DisparaitreApresDelai()
    {
        if (delaiAvantDisparition > 0f)
            yield return new WaitForSecondsRealtime(delaiAvantDisparition);

        if (dureeFadeOut > 0f)
            yield return StartCoroutine(FadeOut());

        gameObject.SetActive(false);
    }

    /// <summary>
    /// Fade out alpha 1 -> 0. Cherche d'abord un CanvasGroup (UI), sinon
    /// un Renderer (3D, fade via material.color.a si le shader le supporte).
    /// </summary>
    private IEnumerator FadeOut()
    {
        CanvasGroup cg = GetComponent<CanvasGroup>();
        Renderer rend = GetComponent<Renderer>();

        float t = 0f;
        while (t < dureeFadeOut)
        {
            t += Time.unscaledDeltaTime;
            float alpha = 1f - Mathf.Clamp01(t / dureeFadeOut);

            if (cg != null)
            {
                cg.alpha = alpha;
            }
            else if (rend != null && rend.material.HasProperty("_Color"))
            {
                Color c = rend.material.color;
                c.a = alpha;
                rend.material.color = c;
            }
            yield return null;
        }

        if (cg != null) cg.alpha = 0f;
    }
}
