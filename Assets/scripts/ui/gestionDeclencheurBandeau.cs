using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Surveille les actions signalees par gestionChapitres et declenche
/// l'affichage des bandeaux info correspondants. Permet de decoupler
/// completement le systeme de bandeau info du systeme de tuiles tuto.
///
/// SETUP UNITY :
/// 1. Creer un GameObject "gestionnaire_bandeaux_infos" dans la scene
///    scene0_tuto (pour le tutoriel) ou dans n'importe
///    quelle scene ou tu veux des bandeaux.
/// 2. Attacher ce script gestionDeclencheurBandeau.
/// 3. Glisser les ScriptableObject DonneesBandeauInfo (bandeauInfos_*)
///    dans le tableau "Bandeaux" de l'Inspector.
/// 4. Chaque DonneesBandeauInfo a son propre idActionDeclenchement,
///    delaiAvantApparition, etc.
/// </summary>
public class gestionDeclencheurBandeau : MonoBehaviour
{
    public static gestionDeclencheurBandeau Instance { get; private set; }

    [Header("Bandeaux a surveiller")]
    [Tooltip("Liste des DonneesBandeauInfo a afficher quand leur " +
        "idActionDeclenchement est signale. Chaque bandeau est " +
        "configure independamment.")]
    [SerializeField] private DonneesBandeauInfo[] bandeaux;

    // Ids des bandeaux deja affiches (par leur idBandeau). Sert a
    // empecher le double affichage sauf si autoriserMultipleAffichages.
    private HashSet<string> bandeauxAffiches = new HashSet<string>();
    // Toutes les actions signalees depuis le demarrage. Sert a verifier
    // idActionAnnulation au moment du declenchement d'un bandeau.
    private HashSet<string> actionsRecues = new HashSet<string>();

    void Awake()
    {
        // Singleton de scene : empeche les doublons accidentels. PAS
        // de DontDestroyOnLoad ici parce que chaque scene a son propre
        // setup de bandeaux (le tableau "bandeaux" est config par scene).
        // gestionBandeauInfo (la UI) persiste entre scenes, mais le
        // declencheur (la logique de trigger) est scene-specific.
        if (Instance != null && Instance != this)
        {
            Debug.Log($"[BandeauDeclencheur] Doublon ('{name}') dans la scene, " +
                "destruction.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (gestionChapitres.Instance != null)
            gestionChapitres.Instance.OnActionSignalee -= AuActionSignalee;
    }

    void Start()
    {
        if (gestionChapitres.Instance != null)
        {
            gestionChapitres.Instance.OnActionSignalee += AuActionSignalee;
            Debug.Log($"[BandeauDeclencheur] {name} abonne a " +
                $"OnActionSignalee. {(bandeaux != null ? bandeaux.Length : 0)} " +
                "bandeaux configures.");
        }
        else
        {
            Debug.LogWarning("[BandeauDeclencheur] gestionChapitres.Instance " +
                "introuvable au Start. Le bandeau ne pourra pas etre declenche.");
        }
    }

    private void AuActionSignalee(string idAction)
    {
        actionsRecues.Add(idAction);

        if (bandeaux == null) return;

        foreach (var bandeau in bandeaux)
        {
            if (bandeau == null) continue;
            if (bandeau.idActionDeclenchement != idAction) continue;

            // Verifier si deja affiche (sauf si multiples autorises)
            if (!bandeau.autoriserMultipleAffichages
                && bandeauxAffiches.Contains(bandeau.idBandeau))
            {
                continue;
            }

            // Verifier idActionAnnulation : si l'action d'annulation
            // a deja ete signalee avant ce declenchement, on annule
            if (!string.IsNullOrEmpty(bandeau.idActionAnnulation)
                && actionsRecues.Contains(bandeau.idActionAnnulation))
            {
                Debug.Log($"[BandeauDeclencheur] '{bandeau.idBandeau}' " +
                    $"annule (action '{bandeau.idActionAnnulation}' " +
                    "deja signalee).");
                bandeauxAffiches.Add(bandeau.idBandeau);
                continue;
            }

            bandeauxAffiches.Add(bandeau.idBandeau);
            StartCoroutine(AfficherApresDelai(bandeau));
        }
    }

    private IEnumerator AfficherApresDelai(DonneesBandeauInfo bandeau)
    {
        if (bandeau.delaiAvantApparition > 0f)
        {
            yield return new WaitForSecondsRealtime(
                bandeau.delaiAvantApparition);
        }

        Debug.Log($"[BandeauDeclencheur] Affichage bandeau " +
            $"'{bandeau.idBandeau}' ({bandeau.dureeAffichage}s).");

        gestionBandeauInfo.Afficher(bandeau.texte, bandeau.dureeAffichage);

        // Synchronisation a l'apparition : si le bandeau a un
        // idActionAApparition, on le signale maintenant. Permet a
        // d'autres systemes (ex : activation d'un pointeur visuel)
        // d'apparaitre en meme temps que le bandeau sans avoir a
        // dupliquer le delai d'apparition.
        if (!string.IsNullOrEmpty(bandeau.idActionAApparition)
            && gestionChapitres.Instance != null)
        {
            Debug.Log($"[BandeauDeclencheur] Signal a apparition: " +
                $"{bandeau.idActionAApparition}");
            gestionChapitres.Instance.SignalerAction(
                bandeau.idActionAApparition);
        }

        // Synchronisation a la FIN d'affichage : attendre la duree
        // d'affichage du bandeau puis signaler l'action. Permet de
        // declencher un pointeur OU une etape suivante APRES que le
        // joueur ait fini de lire le message.
        if (!string.IsNullOrEmpty(bandeau.idActionAFinAffichage))
        {
            yield return new WaitForSecondsRealtime(bandeau.dureeAffichage);

            if (gestionChapitres.Instance != null)
            {
                Debug.Log($"[BandeauDeclencheur] Signal a fin affichage: " +
                    $"{bandeau.idActionAFinAffichage}");
                gestionChapitres.Instance.SignalerAction(
                    bandeau.idActionAFinAffichage);
            }
        }
    }
}
