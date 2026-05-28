// ============================================================
// surveillanceInventaire.cs
// ------------------------------------------------------------
// Surveille gestionInventaire et signale une action narrative
// quand le nombre d'objets d'une catégorie atteint un seuil.
//
// USAGE TYPIQUE :
//   - Le joueur ramasse 4 tuyaux dans l'usine
//   - Un bandeau apparaît : "Tu peux commencer à placer les tuyaux"
//
// SETUP UNITY :
//   1. Créer un GameObject vide nommé "surveillance_4_tuyaux"
//      dans la scene2_usine.
//   2. Add Component → surveillanceInventaire.
//   3. Inspector :
//      - Catégorie Cible : Tuyaux
//      - Seuil : 4
//      - Id Action Atteindre Seuil : "joueur_a_4_tuyaux"
//      - Auto Reset : ✗ décoché (on veut signaler une seule fois)
//
//   4. Créer un DonneesBandeauInfo scriptable :
//      - Texte : "Vous pouvez commencer à placer les tuyaux."
//      - Duree Affichage : 2
//      - Id Action Declenchement : "joueur_a_4_tuyaux"
//
//   5. Le bandeau s'affichera automatiquement (le système
//      gestionBandeauInfo écoute cette action).
// ============================================================
using UnityEngine;

public class surveillanceInventaire : MonoBehaviour
{
    [Header("Critère")]
    [Tooltip("Catégorie d'objet à surveiller dans l'inventaire " +
        "(ex : Tuyaux). Le script compte la somme des quantités de " +
        "tous les objets de cette catégorie.")]
    [SerializeField] private CategorieObjet categorieCible =
        CategorieObjet.Tuyaux;

    [Tooltip("Nombre d'objets à atteindre pour déclencher l'action " +
        "(ex : 4 tuyaux).")]
    [SerializeField] private int seuil = 4;

    [Header("Action à signaler")]
    [Tooltip("idAction signalée à gestionChapitres quand le seuil " +
        "est atteint. Ex : 'joueur_a_4_tuyaux'. À utiliser ensuite " +
        "comme idActionDeclenchement d'un DonneesBandeauInfo, ou " +
        "comme déclencheur de tout autre événement.")]
    [SerializeField] private string idActionAtteindreSeuil =
        "joueur_a_atteint_seuil_inventaire";

    [Header("Options")]
    [Tooltip("Si coché, le seuil peut être déclenché plusieurs fois " +
        "(à chaque retombée puis remontée). Sinon, on signale une " +
        "seule fois par session. Defaut : décoché.")]
    [SerializeField] private bool autoReset = false;

    private bool dejaSignale = false;

    void OnEnable()
    {
        if (gestionInventaire.Instance != null)
        {
            gestionInventaire.Instance.onInventaireModifie
                += SurInventaireModifie;
            // Check immédiat au cas où le seuil est déjà atteint à
            // l'activation (ex : si le joueur revient dans la scène
            // avec déjà 4 tuyaux).
            VerifierSeuil();
        }
        else
        {
            Debug.LogWarning($"[surveillanceInventaire] {name} : " +
                "gestionInventaire.Instance introuvable au OnEnable.");
        }
    }

    void OnDisable()
    {
        if (gestionInventaire.Instance != null)
            gestionInventaire.Instance.onInventaireModifie
                -= SurInventaireModifie;
    }

    private void SurInventaireModifie()
    {
        VerifierSeuil();
    }

    private void VerifierSeuil()
    {
        if (dejaSignale && !autoReset) return;
        if (gestionInventaire.Instance == null) return;

        var objets = gestionInventaire.Instance
            .ObtenirParCategorie(categorieCible);
        int total = 0;
        if (objets != null)
        {
            foreach (var kvp in objets) total += kvp.Value;
        }

        if (total >= seuil && !dejaSignale)
        {
            dejaSignale = true;
            Debug.Log($"[surveillanceInventaire] {name} : seuil de " +
                $"{seuil} {categorieCible} atteint (total={total}). " +
                $"Signalement action '{idActionAtteindreSeuil}'.");
            if (gestionChapitres.Instance != null
                && !string.IsNullOrEmpty(idActionAtteindreSeuil))
                gestionChapitres.Instance.SignalerAction(
                    idActionAtteindreSeuil);
        }
        else if (autoReset && total < seuil && dejaSignale)
        {
            dejaSignale = false;
            Debug.Log($"[surveillanceInventaire] {name} : seuil " +
                "désarmé (autoReset).");
        }
    }
}
