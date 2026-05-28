// ============================================================
// zoneLancementEnigme.cs
// ------------------------------------------------------------
// Zone trigger devant le systeme de tuyauterie : affiche un
// bandeau d'instruction (Entree pour lancer, Esc pour quitter)
// quand le joueur entre dans la zone. Sur Enter, affiche la
// tuile tutoriel de l'enigme + si inventaire vide, indique au
// joueur de ramasser les tuyaux.
//
// SETUP UNITY :
// 1. Creer un GameObject vide nomme 'zone_lancement_enigme_tuyaux'
//    devant le systeme de tuyauterie.
// 2. Add Component → BoxCollider, cocher 'Is Trigger', ajuster
//    la taille pour couvrir la zone d'approche.
// 3. Add Component → zoneLancementEnigme.
// 4. Inspector :
//    - Tuile Tutoriel Enigme : glisser le DonneesTutoriel
//      explicatif de l'enigme tuyauterie.
//    - Id Action Lancement : 'enigme_tuyauterie_lancee' (libre).
// 5. Le joueur doit avoir le tag 'Player'.
// ============================================================

using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class zoneLancementEnigme : MonoBehaviour
{
    [Header("Bandeau d'approche")]
    [Tooltip("Texte affiche quand le joueur entre dans la zone. " +
        "Reste affiche tant que le joueur est dans la zone.")]
    [SerializeField, TextArea(2, 4)] private string texteApproche =
        "Appuie sur <b>Entree</b> pour lancer l'enigme, " +
        "<b>Esc</b> pour quitter.";

    [Header("Tuile tutoriel a afficher au lancement")]
    [Tooltip("DonneesTutoriel explicative de l'enigme (texte + " +
        "image). Affichee via gestionTutoriel quand le joueur appuie " +
        "sur Entree. Si tu as un GameObject UI custom (ex : " +
        "ensemble_tuile_explicative), laisse vide et utilise plutot " +
        "le champ 'Tuile Explicative GameObject' ci-dessous.")]
    [SerializeField] private DonneesTutoriel tuileTutorielEnigme;

    [Tooltip("GameObject UI custom a activer directement (SetActive " +
        "true) au lancement de l'enigme. Alternative a DonneesTutoriel " +
        "si tu as ton propre canvas/tuile (ex : ensemble_tuile_" +
        "explicative dans la scene). Sera desactive a la fermeture " +
        "via la touche Esc.")]
    [SerializeField] private GameObject tuileExplicativeGameObject;

    [Tooltip("Si coche, le joueur peut fermer la tuile explicative " +
        "(GameObject ci-dessus) en appuyant sur Esc.")]
    [SerializeField] private bool fermableAvecEsc = true;

    [Header("Inventaire vide")]
    [Tooltip("Texte affiche si l'inventaire ne contient aucun tuyau " +
        "(categorie Tuyaux). Indique au joueur de chercher les " +
        "tuyaux dans la scene.")]
    [SerializeField, TextArea(2, 4)] private string texteInventaireVide =
        "Cherche et ramasse les morceaux de tuyaux dans l'usine.";

    [Header("Action signalee au lancement")]
    [Tooltip("idAction signalee a gestionChapitres quand le joueur " +
        "appuie sur Entree (lance l'enigme). Permet de chainer une " +
        "logique externe (ex : demarrer le minuteur).")]
    [SerializeField] private string idActionLancement = "enigme_tuyauterie_lancee";

    [Header("Action signalee a la sortie (Esc)")]
    [Tooltip("idAction signalee quand le joueur appuie sur Esc pour " +
        "quitter l'enigme (apres lancement). Permet de chainer la " +
        "restauration visuelle (ex : opacite des tuyaux deja places).")]
    [SerializeField] private string idActionSortie = "enigme_tuyauterie_quittee";

    [Header("Activation differee de la zone")]
    [Tooltip("Si vide : la zone est ACTIVE des le debut de la scene. " +
        "Si rempli : la zone reste INACTIVE jusqu'a ce que cette " +
        "action soit signalee. Utile pour empecher le joueur de lancer " +
        "l'enigme avant un moment narratif precis. " +
        "Ex : 'joueur_a_atteint_enigme' (signale au contact du " +
        "prefab_pointeur_enigme).")]
    [SerializeField] private string idActionPourActiverZone = "";

    [Header("Texte affiche dans la tuile explicative (3 options)")]
    [Tooltip("OPTION 1 (recommandee) : glisse un DonneesTutoriel scriptable " +
        "object. Son champ 'titre' et 'explication' seront injectes dans " +
        "les TMP_Text enfants de Tuile Explicative GameObject. Permet de " +
        "modifier le texte sans toucher au code, et de reutiliser le meme " +
        "asset ailleurs (ex : tuile dans scene0_tuto via gestionTutoriel).")]
    [SerializeField] private DonneesTutoriel donneesTuileSource;

    [Tooltip("OPTION 2 : tape directement le titre ici. Si DonneesTutoriel " +
        "est aussi renseigne, ce champ est ignore (priorite a l'asset).")]
    [SerializeField, TextArea(1, 3)] private string titreTuile = "";

    [Tooltip("OPTION 3 : tape directement l'explication ici. Si DonneesTutoriel " +
        "est aussi renseigne, ce champ est ignore (priorite a l'asset).")]
    [SerializeField, TextArea(3, 10)] private string explicationTuile = "";

    [Header("Categorie objetInventaire a verifier")]
    [Tooltip("CategorieObjet a chercher pour determiner si l'inventaire " +
        "a deja des tuyaux. Defaut : Tuyaux.")]
    [SerializeField] private CategorieObjet categorieAttendue =
        CategorieObjet.Tuyaux;

    [Tooltip("Nombre minimum de tuyaux (categorieAttendue) dans " +
        "l'inventaire pour NE PAS afficher le bandeau 'ramasse les " +
        "tuyaux' au lancement. Si le joueur a moins de ce nombre, le " +
        "bandeau s'affiche. Defaut : 4.")]
    [SerializeField] private int minimumTuyauxRequis = 4;

    [Header("Element a cacher pendant l'enigme (optionnel)")]
    [Tooltip("GameObject (ex : indices_jouabilite, ATH HUD) a desactiver " +
        "quand l'enigme est lancee et a reactiver quand le joueur quitte. " +
        "Si vide, auto-find par nom 'indices_jouabilite'.")]
    [SerializeField] private GameObject indicesJouabilite;

    private bool joueurDansZone = false;
    private bool enigmeLancee = false;
    private bool zoneActivee = true;
    private gestionInputsJeu cacheInputs;

    /// <summary>
    /// Vrai si une enigme est actuellement en cours (entre Enter et Esc).
    /// Consulte par gestionInputsJeu pour ne pas declencher le menu pause
    /// quand le joueur appuie sur Esc pour quitter l'enigme.
    /// </summary>
    public static bool EnigmeActive { get; private set; }

    void Start()
    {
        // AUTO-FIND : si tuileExplicativeGameObject n'est pas assigne
        // dans l'Inspector, on cherche un GameObject nomme
        // 'ensemble_tuile_explicative' dans la scene (inclus inactifs).
        // Evite au user le drag-drop manuel.
        if (tuileExplicativeGameObject == null)
        {
            var tous = FindObjectsByType<Transform>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var t in tous)
            {
                if (t.name == "ensemble_tuile_explicative"
                    || t.name == "tuile_explicative")
                {
                    tuileExplicativeGameObject = t.gameObject;
                    Debug.Log("[zoneLancementEnigme] AUTO-FIND : tuile " +
                        $"explicative trouvee : {t.name}");
                    break;
                }
            }
            if (tuileExplicativeGameObject == null)
                Debug.LogWarning("[zoneLancementEnigme] Auto-find " +
                    "tuile_explicative : aucun GameObject 'ensemble_" +
                    "tuile_explicative' ou 'tuile_explicative' trouve " +
                    "dans la scene. La tuile ne s'affichera pas.");
        }

        // AUTO-FIND indices_jouabilite par nom si non assigne.
        if (indicesJouabilite == null)
        {
            var tous = FindObjectsByType<Transform>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var t in tous)
            {
                if (t.name == "indices_jouabilite"
                    || t.name == "indice_jouabilite")
                {
                    indicesJouabilite = t.gameObject;
                    Debug.Log("[zoneLancementEnigme] AUTO-FIND : " +
                        $"indices_jouabilite trouve : {t.name}");
                    break;
                }
            }
        }

        // Cache de gestionInputsJeu pour FermerInventaire au QuitterEnigme.
        cacheInputs = FindFirstObjectByType<gestionInputsJeu>(
            FindObjectsInactive.Include);

        // Si une action d'activation est requise, on attend qu'elle
        // soit signalee avant de reagir aux OnTriggerEnter.
        if (!string.IsNullOrEmpty(idActionPourActiverZone))
        {
            zoneActivee = false;
            if (gestionChapitres.Instance != null)
                gestionChapitres.Instance.OnActionSignalee
                    += AuActionActivation;
        }
    }

    void OnDestroy()
    {
        if (gestionChapitres.Instance != null
            && !string.IsNullOrEmpty(idActionPourActiverZone))
            gestionChapitres.Instance.OnActionSignalee
                -= AuActionActivation;
    }

    private void AuActionActivation(string id)
    {
        if (id == idActionPourActiverZone)
        {
            zoneActivee = true;
            Debug.Log("[zoneLancementEnigme] Zone activee suite a " +
                $"l'action '{id}'.");
            // Si le joueur est deja DANS le trigger au moment ou la zone
            // s'active, on declenche manuellement le bandeau.
            if (joueurDansZone)
                gestionBandeauInfo.Afficher(texteApproche, 999f);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (enigmeLancee) return;
        joueurDansZone = true;
        if (!zoneActivee)
        {
            Debug.Log("[zoneLancementEnigme] Joueur dans zone mais " +
                "zone pas encore activee (attend '" +
                idActionPourActiverZone + "').");
            return;
        }
        // Duree tres longue : le bandeau reste tant que le joueur est
        // dans la zone. On l'efface manuellement a OnTriggerExit.
        gestionBandeauInfo.Afficher(texteApproche, 999f);
        Debug.Log("[zoneLancementEnigme] Joueur entre dans zone " +
            "(zone activee).");
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        joueurDansZone = false;
        Debug.Log("[zoneLancementEnigme] Joueur sort de zone.");

        if (enigmeLancee)
        {
            // Si l'enigme etait en cours et le joueur s'eloigne de la
            // zone sans appuyer Esc : on traite ca comme une sortie
            // d'enigme (auto-Esc). Met le minuteur en pause, ferme la
            // tuile, et le bandeau 'Appuyer Entree' reapparaitra
            // automatiquement quand le joueur reviendra dans la zone
            // (via OnTriggerEnter).
            Debug.Log("[zoneLancementEnigme] Joueur quitte la zone " +
                "pendant l'enigme → auto-Esc.");
            QuitterEnigme();
            // QuitterEnigme appelle gestionBandeauInfo.Effacer() ET
            // n'affiche pas le bandeau approche (car joueurDansZone=false).
        }
        else
        {
            // Cas normal : juste effacer le bandeau d'approche.
            gestionBandeauInfo.Effacer();
        }
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        // Avant lancement : Enter dans la zone = lancer l'enigme
        // (uniquement si zone activee)
        if (joueurDansZone && !enigmeLancee && zoneActivee)
        {
            if (Keyboard.current.enterKey.wasPressedThisFrame)
            {
                LancerEnigme();
            }
            return;
        }

        // Apres lancement : Esc ferme d'ABORD la tuile explicative si
        // elle est ouverte, et seulement APRES (sur un second Esc) quitte
        // l'enigme. Sans cette priorite, le joueur qui fait Esc juste
        // pour fermer la tuile (lecture terminee) quittait toute l'enigme
        // et perdait ses ghost / minuteur en pause.
        if (enigmeLancee
            && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (tuileExplicativeGameObject != null
                && tuileExplicativeGameObject.activeSelf
                && fermableAvecEsc)
            {
                tuileExplicativeGameObject.SetActive(false);
                Debug.Log("[zoneLancementEnigme] Esc : tuile explicative "
                    + "fermee. L'enigme reste active.");
                return;
            }
            QuitterEnigme();
        }
    }

    private void QuitterEnigme()
    {
        Debug.Log("[zoneLancementEnigme] Joueur quitte l'enigme " +
            "(Esc) -> action '" + idActionSortie + "'.");

        // 1. Fermer la tuile explicative si encore ouverte
        if (tuileExplicativeGameObject != null
            && tuileExplicativeGameObject.activeSelf)
            tuileExplicativeGameObject.SetActive(false);

        // 2. Effacer un eventuel bandeau d'inventaire vide en cours
        gestionBandeauInfo.Effacer();

        // 3. Fermer l'inventaire si ouvert (le joueur peut avoir clique
        // dans l'inventaire pendant l'enigme — Esc doit tout fermer).
        if (cacheInputs == null)
            cacheInputs = FindFirstObjectByType<gestionInputsJeu>(
                FindObjectsInactive.Include);
        if (cacheInputs != null && cacheInputs.EstInventaireOuvert)
        {
            cacheInputs.FermerInventaire();
            Debug.Log("[zoneLancementEnigme] Inventaire ferme " +
                "automatiquement a la sortie de l'enigme.");
        }

        // 4. Reactiver indices_jouabilite (l'ATH explicatif joueur).
        if (indicesJouabilite != null && !indicesJouabilite.activeSelf)
        {
            indicesJouabilite.SetActive(true);
            Debug.Log("[zoneLancementEnigme] indices_jouabilite reactive.");
        }

        // 5. Signaler l'action de sortie (restaure opacite, etc.)
        if (gestionChapitres.Instance != null
            && !string.IsNullOrEmpty(idActionSortie))
            gestionChapitres.Instance.SignalerAction(idActionSortie);

        // 6. Mettre TOUS les minuteurs en pause (sans reset). Le
        // temps restant est conserve. Il peut y avoir plusieurs
        // instances (1 logique + 1 UI), donc on les pause toutes.
        minuteurEnigmeTuyauterie.MettreEnPauseTous();

        // 7. Reinitialiser pour permettre de relancer l'enigme
        enigmeLancee = false;
        EnigmeActive = false;
        if (joueurDansZone)
            gestionBandeauInfo.Afficher(texteApproche, 999f);
    }

    private void LancerEnigme()
    {
        enigmeLancee = true;
        EnigmeActive = true;
        gestionBandeauInfo.Effacer();
        Debug.Log("[zoneLancementEnigme] Lancement enigme.");

        // Cacher indices_jouabilite (ATH explicatif joueur) pendant
        // l'enigme — sera reaffiche au QuitterEnigme.
        if (indicesJouabilite != null && indicesJouabilite.activeSelf)
        {
            indicesJouabilite.SetActive(false);
            Debug.Log("[zoneLancementEnigme] indices_jouabilite cache.");
        }

        // 1. Signaler l'action de lancement (hook externe : minuteur, etc.)
        if (gestionChapitres.Instance != null
            && !string.IsNullOrEmpty(idActionLancement))
            gestionChapitres.Instance.SignalerAction(idActionLancement);

        // 2. Afficher la tuile explicative :
        //    A) Si tuileExplicativeGameObject est renseigne, on l'active
        //       directement (approche UI custom du user).
        //    B) Sinon, si tuileTutorielEnigme (DonneesTutoriel) est
        //       renseigne, on l'envoie au systeme gestionTutoriel.
        if (tuileExplicativeGameObject != null)
        {
            // Activer le GameObject ET tous ses ancetres au cas ou un
            // parent serait desactive (ex : ensemble_tuile_explicative
            // sous un canvas_hud inactif).
            Transform t = tuileExplicativeGameObject.transform;
            while (t != null)
            {
                if (!t.gameObject.activeSelf) t.gameObject.SetActive(true);
                t = t.parent;
            }
            tuileExplicativeGameObject.SetActive(true);
            // Source des textes : priorite a DonneesTutoriel (asset),
            // sinon fallback sur les champs string inline.
            string titreFinal = donneesTuileSource != null
                ? donneesTuileSource.titre : titreTuile;
            string explicationFinal = donneesTuileSource != null
                ? donneesTuileSource.explication : explicationTuile;
            // Strategie de remplissage des textes :
            // 1) Tenter par nom exact (titre_tuto, titre, explication_tuto,
            //    explication).
            // 2) Si echec, fallback par INDEX : 1er TMP_Text descendant
            //    = titre, 2e = explication. Robuste face aux renommages.
            bool titreRempli = RemplirTexteEnfant(tuileExplicativeGameObject,
                new[] { "titre_tuto", "titre" }, titreFinal);
            bool explicRempli = RemplirTexteEnfant(tuileExplicativeGameObject,
                new[] { "explication_tuto", "explication" },
                explicationFinal);

            if (!titreRempli || !explicRempli)
            {
                var tousTextes = tuileExplicativeGameObject
                    .GetComponentsInChildren<TMP_Text>(true);
                if (tousTextes.Length >= 1 && !titreRempli
                    && !string.IsNullOrEmpty(titreFinal))
                {
                    tousTextes[0].text = titreFinal;
                    Debug.Log("[zoneLancementEnigme] Titre rempli par " +
                        $"fallback index (1er TMP_Text : '{tousTextes[0].name}').");
                }
                if (tousTextes.Length >= 2 && !explicRempli
                    && !string.IsNullOrEmpty(explicationFinal))
                {
                    tousTextes[1].text = explicationFinal;
                    Debug.Log("[zoneLancementEnigme] Explication remplie " +
                        $"par fallback index (2e TMP_Text : '{tousTextes[1].name}').");
                }
            }
            Debug.Log("[zoneLancementEnigme] Tuile explicative " +
                "GameObject activee.");
        }
        else if (tuileTutorielEnigme != null)
        {
            // gestionTutoriel n'a pas de singleton Instance, donc on le
            // trouve via FindFirstObjectByType (avec inactifs inclus).
            var tuto = FindFirstObjectByType<gestionTutoriel>(
                FindObjectsInactive.Include);
            if (tuto != null)
                tuto.AfficherTuto(tuileTutorielEnigme);
            else
                Debug.LogWarning("[zoneLancementEnigme] " +
                    "gestionTutoriel introuvable dans la scene.");
        }
        else
        {
            Debug.LogWarning("[zoneLancementEnigme] Ni Tuile " +
                "Explicative GameObject ni DonneesTutoriel assigne " +
                "dans l'Inspector.");
        }

        // 3. Verifier inventaire : si le joueur n'a pas le minimum de
        //    tuyaux requis (somme des quantites de la categorie), on
        //    affiche le bandeau qui lui dit d'aller en chercher.
        if (gestionInventaire.Instance != null)
        {
            var tuyauxEnInventaire = gestionInventaire.Instance
                .ObtenirParCategorie(categorieAttendue);
            int totalTuyaux = 0;
            if (tuyauxEnInventaire != null)
            {
                foreach (var kvp in tuyauxEnInventaire)
                    totalTuyaux += kvp.Value;
            }
            if (totalTuyaux < minimumTuyauxRequis)
            {
                gestionBandeauInfo.Afficher(texteInventaireVide, 5f);
                Debug.Log($"[zoneLancementEnigme] Inventaire : " +
                    $"{totalTuyaux}/{minimumTuyauxRequis} tuyaux, bandeau " +
                    "'cherche les tuyaux' affiche.");
            }
            else
            {
                Debug.Log($"[zoneLancementEnigme] Inventaire : " +
                    $"{totalTuyaux} tuyaux (>={minimumTuyauxRequis}), " +
                    "pas de bandeau.");
            }
        }
    }

    /// <summary>
    /// Reinitialise le flag enigmeLancee. A appeler si le joueur quitte
    /// l'enigme avec Esc (logique externe) pour pouvoir relancer.
    /// </summary>
    public void Reinitialiser()
    {
        enigmeLancee = false;
        if (joueurDansZone)
            gestionBandeauInfo.Afficher(texteApproche, 999f);
    }

    /// <summary>
    /// Ferme toute tuile explicative actuellement active dans la scene.
    /// Appele depuis gestionInputsJeu.OuvrirInventaire() pour eviter
    /// que la tuile bloque les clics sur les slots d'inventaire.
    /// </summary>
    public static void FermerTuilesActives()
    {
        var instances = FindObjectsByType<zoneLancementEnigme>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var z in instances)
        {
            if (z.tuileExplicativeGameObject != null
                && z.tuileExplicativeGameObject.activeSelf)
            {
                z.tuileExplicativeGameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Cherche un GameObject enfant par un de ses noms candidats (sensibles
    /// a la casse) et ecrit le texte fourni dans son TMP_Text (s'il y en a
    /// un). Ne fait rien si le texte est vide (preserve le contenu existant
    /// dans l'Inspector).
    /// </summary>
    private bool RemplirTexteEnfant(
        GameObject racine, string[] nomsCandidats, string texte)
    {
        if (string.IsNullOrEmpty(texte)) return false;
        var tousTextes = racine.GetComponentsInChildren<TMP_Text>(true);
        foreach (var t in tousTextes)
        {
            if (t == null) continue;
            foreach (var nom in nomsCandidats)
            {
                if (t.gameObject.name == nom)
                {
                    t.text = texte;
                    return true;
                }
            }
        }
        Debug.LogWarning($"[zoneLancementEnigme] Aucun TMP_Text enfant " +
            $"avec un nom dans [{string.Join(",", nomsCandidats)}] trouve " +
            $"sous '{racine.name}'.");
        return false;
    }
}
