using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Active l'effet glow (halo lumineux) sur tous les indices presents
/// dans la scene quand le chapitre "mener_enquete" demarre, apres que
/// sa banniere annonce-chapitre se soit affichee.
///
/// SETUP UNITY :
/// 1. Sur chaque indice (GameObject avec tag "indice" ou "pnj"),
///    ajouter un GameObject ENFANT nomme "Glow".
/// 2. Sur ce GameObject "Glow", ajouter un component Light :
///    - Type: Point
///    - Color: dore (#FFD700) ou ambre (#FFA500)
///    - Intensity: 1.5 - 3 (a ajuster)
///    - Range: 0.5 - 2 (selon taille de l'indice)
///    - Shadows: None (pour performance)
/// 3. DESACTIVER le GameObject "Glow" par defaut (decoche dans
///    l'Inspector). Ce script l'activera au bon moment.
/// 4. Attacher ce script gestionGlowIndices sur un GameObject de la
///    scene scene_taverne_recherche_indices (par exemple sur un
///    GameObject vide "GestionnaireGlow" a la racine).
///
/// FONCTIONNEMENT :
/// - Au demarrage, trouve tous les indices presents dans la scene
///   (par tag) et garde une reference a leur enfant "Glow".
/// - S'abonne a OnBanniereChapitreTerminee de gestionChapitres.
/// - Quand la banniere du chapitre "mener_enquete" se termine,
///   active tous les "Glow".
/// - Si l'indice est ramasse plus tard, son "Glow" disparait
///   naturellement (c'est un enfant qui est detruit avec lui).
/// </summary>
public class gestionGlowIndices : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Tags des objets consideres comme indices a faire briller. " +
        "Par defaut : 'indice' et 'pnj'. Tu peux en ajouter d'autres.")]
    [SerializeField] private string[] tagsIndices = { "indice", "pnj" };

    [Tooltip("Nom EXACT du GameObject enfant qui contient la Light/" +
        "ParticleSystem du glow sur chaque indice. Par defaut : 'Glow'.")]
    [SerializeField] private string nomEnfantGlow = "Glow";

    [Tooltip("idChapitre dont la fin de banniere declenche l'activation " +
        "du glow. Par defaut : 'mener_enquete'.")]
    [SerializeField] private string idChapitreCible = "mener_enquete";

    [Tooltip("Si coche, active le glow immediatement au Start() au lieu " +
        "d'attendre la fin de la banniere. Utile pour debug.")]
    [SerializeField] private bool activerImmediatement = false;

    // Liste des GameObjects "Glow" trouves au demarrage. Sera utilisee
    // pour les activer en une fois.
    private readonly List<GameObject> glowObjets = new List<GameObject>();

    void Start()
    {
        // Collecter tous les "Glow" enfants des indices dans la scene
        CollecterGlowObjets();

        if (activerImmediatement)
        {
            Debug.Log("[GlowIndices] activerImmediatement = true, " +
                "activation directe.");
            ActiverTousLesGlow();
            return;
        }

        // S'abonner a l'event de fin de banniere de chapitre
        if (gestionChapitres.Instance != null)
        {
            gestionChapitres.Instance.OnBanniereChapitreTerminee +=
                AuFinBanniere;
            Debug.Log("[GlowIndices] Abonne a " +
                "OnBanniereChapitreTerminee, attente du chapitre " +
                $"'{idChapitreCible}'.");
        }
        else
        {
            Debug.LogWarning("[GlowIndices] gestionChapitres.Instance " +
                "introuvable au Start.");
        }
    }

    void OnDestroy()
    {
        // Se desabonner pour eviter les fuites si la scene est rechargee
        if (gestionChapitres.Instance != null)
        {
            gestionChapitres.Instance.OnBanniereChapitreTerminee -=
                AuFinBanniere;
        }
    }

    private void CollecterGlowObjets()
    {
        glowObjets.Clear();

        foreach (string tag in tagsIndices)
        {
            if (string.IsNullOrEmpty(tag)) continue;

            GameObject[] indices;
            try
            {
                indices = GameObject.FindGameObjectsWithTag(tag);
            }
            catch
            {
                Debug.LogWarning($"[GlowIndices] Tag '{tag}' invalide " +
                    "(non defini dans Tag Manager). Ignore.");
                continue;
            }

            foreach (var indice in indices)
            {
                Transform glow = indice.transform.Find(nomEnfantGlow);
                if (glow != null)
                {
                    glowObjets.Add(glow.gameObject);
                }
                else
                {
                    Debug.LogWarning($"[GlowIndices] L'indice " +
                        $"'{indice.name}' n'a pas d'enfant " +
                        $"'{nomEnfantGlow}'. Glow ignore.");
                }
            }
        }

        Debug.Log($"[GlowIndices] {glowObjets.Count} effets glow " +
            "trouves dans la scene.");
    }

    private void AuFinBanniere(string idChapitre)
    {
        if (idChapitre != idChapitreCible) return;

        Debug.Log($"[GlowIndices] Banniere '{idChapitre}' terminee, " +
            "activation des glow.");
        ActiverTousLesGlow();
    }

    private void ActiverTousLesGlow()
    {
        foreach (var glow in glowObjets)
        {
            if (glow != null)
                glow.SetActive(true);
        }
    }
}
