// ============================================================
// bandeauTuyauxRestants.cs
// ------------------------------------------------------------
// Auteur      : Olivier Vernet
// Date créée  : 2026-05-28
// ------------------------------------------------------------
// Description :
//   Affiche un bandeau infos dynamique chaque fois que le joueur
//   ramasse un tuyau, indiquant combien il en reste a ramasser.
//   Ex : "Il reste 5 tuyaux a ramasser."
//
//   REMPLACE le bandeau static "joueur_a_4_tuyaux" qui ne
//   s'affichait qu'une fois au seuil atteint. Le user voit
//   maintenant une progression continue.
//
// SETUP UNITY :
//   1. Creer un GameObject vide nomme "bandeau_tuyaux_restants"
//      dans scene2_usine.
//   2. Add Component → bandeauTuyauxRestants.
//   3. Inspector :
//      - Categorie Cible : Tuyaux
//      - Total A Ramasser : 9
//      - Texte Format : "Il reste {N} tuyau(x) a ramasser."
//        ({N} sera remplace par le nombre restant)
//      - Duree Affichage : 3
//
//   4. (Optionnel) Desactiver le bandeau static "joueur_a_4_tuyaux"
//      en supprimant son GameObject surveillance_4_tuyaux OU en
//      laissant le seuil a un nombre impossible (ex 999).
// ============================================================

using UnityEngine;

public class bandeauTuyauxRestants : MonoBehaviour
{
    [Header("Critere")]
    [Tooltip("Categorie d'objet a compter (ex : Tuyaux).")]
    [SerializeField] private CategorieObjet categorieCible =
        CategorieObjet.Tuyaux;

    [Tooltip("Nombre total de tuyaux a ramasser dans la scene. " +
        "Ex : 9 pour l'usine. Le bandeau affichera (total - quantite).")]
    [SerializeField] private int totalARamasser = 9;

    [Header("Texte du bandeau")]
    [Tooltip("Texte affiche au ramassage. Le placeholder {N} est " +
        "remplace par le nombre de tuyaux restants. Ex : " +
        "'Il reste {N} tuyaux a ramasser.'")]
    [SerializeField, TextArea(2, 4)] private string texteFormat =
        "Il reste {N} tuyau(x) a ramasser.";

    [Tooltip("Texte alternatif affiche QUAND tous les tuyaux sont " +
        "ramasses (au lieu du format ci-dessus avec {N}=0).")]
    [SerializeField, TextArea(2, 4)] private string texteTous =
        "Tous les tuyaux sont ramasses. Va commencer l'enigme.";

    [Header("Affichage")]
    [Tooltip("Duree en secondes pendant laquelle le bandeau reste " +
        "affiche apres chaque ramassage.")]
    [SerializeField] private float dureeAffichage = 3f;

    [Tooltip("Si coche, le bandeau s'affiche aussi quand le joueur " +
        "RETIRE un tuyau (ex : placement reussi sur snap_point). " +
        "Defaut : decoche (pollue moins pendant l'enigme).")]
    [SerializeField] private bool afficherSurRetrait = false;

    private int dernierNombreVu = -1;

    void OnEnable()
    {
        if (gestionInventaire.Instance != null)
        {
            gestionInventaire.Instance.onInventaireModifie
                += AuModifInventaire;
            // Initialiser le compteur pour ne pas afficher le bandeau a
            // l'activation si le nombre n'a pas change.
            dernierNombreVu = CompterTuyaux();
        }
        else
        {
            Debug.LogWarning($"[bandeauTuyauxRestants] {name} : " +
                "gestionInventaire.Instance introuvable au OnEnable.");
        }
    }

    void OnDisable()
    {
        if (gestionInventaire.Instance != null)
            gestionInventaire.Instance.onInventaireModifie
                -= AuModifInventaire;
    }

    private int CompterTuyaux()
    {
        if (gestionInventaire.Instance == null) return 0;
        var objets = gestionInventaire.Instance
            .ObtenirParCategorie(categorieCible);
        int total = 0;
        if (objets != null)
            foreach (var kvp in objets) total += kvp.Value;
        return total;
    }

    private void AuModifInventaire()
    {
        int nombreActuel = CompterTuyaux();

        // Ignorer les modifs qui ne changent pas le total (ex : autre
        // categorie modifiee dans gestionInventaire).
        if (nombreActuel == dernierNombreVu) return;

        bool estAjout = nombreActuel > dernierNombreVu;
        bool estRetrait = nombreActuel < dernierNombreVu;

        if (estRetrait && !afficherSurRetrait)
        {
            dernierNombreVu = nombreActuel;
            return;
        }

        dernierNombreVu = nombreActuel;

        // Calcul du nombre restant a ramasser.
        int restant = Mathf.Max(0, totalARamasser - nombreActuel);

        string texteAffiche;
        if (restant <= 0)
        {
            texteAffiche = texteTous;
        }
        else
        {
            texteAffiche = texteFormat.Replace("{N}", restant.ToString());
        }

        gestionBandeauInfo.Afficher(texteAffiche, dureeAffichage);
        Debug.Log($"[bandeauTuyauxRestants] Ramassage detecte " +
            $"({nombreActuel}/{totalARamasser}) → " +
            $"bandeau : '{texteAffiche}'.");
    }
}
