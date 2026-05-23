using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gere l'apparition des indices dans la scene de recherche :
/// - Optionnellement, CACHE les indices au demarrage (SetActive false
///   sur les GameObjects taggues "indice").
/// - Active l'effet glow (halo lumineux enfant "Glow") sur les indices
///   au moment du declencheur configure.
/// - Si cacherIndicesAuDemarrage est coche, ce sont les indices entiers
///   qui sont reveles au declencheur (avec leur glow).
///
/// SOURCES DE DECLENCHEMENT (au choix) :
/// 1. Fin de la banniere d'un chapitre (OnBanniereChapitreTerminee).
///    Configurer idChapitreCible. Defaut : 'mener_enquete'.
/// 2. Signal d'une action specifique (OnActionSignalee). Configurer
///    idActionDeclencheur. Ex : 'dialogue_pnj_mysterieux_fini' pour
///    reveler les indices APRES que le joueur ait parle au npc.
///    Prioritaire sur le chapitre si renseigne.
///
/// SETUP UNITY :
/// 1. Sur chaque indice (GameObject avec tag 'indice'), ajouter un
///    GameObject ENFANT nomme 'Glow' (Light point / ParticleSystem).
///    DESACTIVER ce GameObject 'Glow' dans l'Inspector. Le script
///    l'activera au bon moment.
/// 2. Optionnellement, DESACTIVER aussi le GameObject de chaque indice
///    lui-meme dans l'Inspector (case decochee) ET cocher
///    'cacherIndicesAuDemarrage' sur ce script. Les indices seront
///    invisibles jusqu'au declencheur.
///    Attention : le script utilise FindGameObjectsWithTag qui IGNORE
///    les GameObjects inactifs. Pour cacher des indices via
///    cacherIndicesAuDemarrage, GARDE-les actifs dans l'Inspector et
///    laisse le script les desactiver au Start.
/// 3. Attacher ce script sur un GameObject vide dans la scene
///    (ex : 'GestionnaireGlow' a la racine).
/// 4. Inspector : configurer le declencheur souhaite (action OU chapitre).
///
/// IMPORTANT POUR LE NPC :
/// Si tu veux que le NPC reste visible et accessible au demarrage
/// (pour pouvoir lui parler), assure-toi qu'il N'EST PAS dans la liste
/// 'tagsAReveler'. Par defaut, on ne met que 'indice' (pas 'pnj'),
/// donc le npc reste accessible. Si tu veux quand meme un glow sur lui
/// au declencheur, ajoute son tag a 'tagsRecevantGlowSeulement'.
/// </summary>
public class gestionGlowIndices : MonoBehaviour
{
    [Header("Quels objets sont reveles")]
    [Tooltip("Tags des GameObjects a CACHER au demarrage et REVELER au " +
        "declencheur (avec leur glow). Par defaut : 'indice'. N'inclure " +
        "que les objets qui doivent etre invisibles au depart.")]
    [SerializeField] private string[] tagsAReveler = { "indice" };

    [Tooltip("Tags des GameObjects qui ne sont PAS caches au demarrage " +
        "mais qui doivent recevoir un glow au declencheur. Ex : 'pnj' " +
        "si tu veux que le npc ait aussi un halo au moment ou les " +
        "indices apparaissent. Vide par defaut.")]
    [SerializeField] private string[] tagsRecevantGlowSeulement
        = new string[0];

    [Tooltip("Si coche, desactive (SetActive false) les GameObjects de " +
        "tagsAReveler au Start, et les reactive au declencheur. Si " +
        "decoche, les indices restent visibles des le debut et seul le " +
        "glow est ajoute au declencheur.")]
    [SerializeField] private bool cacherIndicesAuDemarrage = true;

    [Header("Effet glow")]
    [Tooltip("Nom EXACT du GameObject enfant qui contient la Light/" +
        "ParticleSystem du glow sur chaque indice. Par defaut : 'Glow'.")]
    [SerializeField] private string nomEnfantGlow = "Glow";

    [Header("Animation du glow")]
    [Tooltip("Si coche, anime l'intensite de la Light de chaque Glow " +
        "pour donner un effet de reflet vivant (respiration + " +
        "scintillement). Si decoche, le glow est statique (intensite " +
        "initiale de la Light).")]
    [SerializeField] private bool animerGlow = true;

    public enum ModeAnimationGlow
    {
        Pulse,           // Oscillation sinusoidale reguliere
        Flicker,         // Variation aleatoire (style flamme)
        PulseEtFlicker   // Combinaison (recommande)
    }

    [Tooltip("Type d'animation applique sur la Light de chaque Glow.")]
    [SerializeField]
    private ModeAnimationGlow modeAnimation
        = ModeAnimationGlow.PulseEtFlicker;

    [Tooltip("Intensite minimale de la Light pendant l'animation.")]
    [SerializeField] private float intensiteMin = 1.2f;

    [Tooltip("Intensite maximale de la Light pendant l'animation.")]
    [SerializeField] private float intensiteMax = 2.4f;

    [Tooltip("Duree (s) d'un cycle complet pulse (min -> max -> min). " +
        "Defaut 2s = respiration lente.")]
    [SerializeField] private float dureeCyclePulse = 2f;

    [Tooltip("Amplitude du flicker (0 = pas de flicker, 0.15 = subtil, " +
        "0.5 = marque). Combine avec le pulse si mode PulseEtFlicker.")]
    [SerializeField, Range(0f, 1f)] private float amplitudeFlicker = 0.15f;

    [Tooltip("Vitesse du flicker (Hz). 4-6 = rapide style flamme. " +
        "1-2 = lent style respiration aleatoire.")]
    [SerializeField] private float vitesseFlicker = 3f;

    [Header("Declencheur")]
    [Tooltip("(Priorite 1) ID d'action signalee qui declenche " +
        "l'apparition. Ex : 'dialogue_pnj_mysterieux_fini' pour reveler " +
        "les indices apres que le joueur ait fini de parler au npc. Si " +
        "renseigne, c'est cette action qui est ecoutee (et idChapitreCible " +
        "est ignore).")]
    [SerializeField] private string idActionDeclencheur;

    [Tooltip("(Priorite 2) idChapitre dont la fin de banniere declenche " +
        "l'apparition. Utilise uniquement si idActionDeclencheur est vide. " +
        "Par defaut : 'mener_enquete'.")]
    [SerializeField] private string idChapitreCible = "mener_enquete";

    [Header("Debug")]
    [Tooltip("Si coche, revele tout immediatement au Start() au lieu " +
        "d'attendre. Utile pour debug.")]
    [SerializeField] private bool activerImmediatement = false;

    // Listes collectees au Start pour pouvoir activer en un coup
    private readonly List<GameObject> indicesACacherEtReveler =
        new List<GameObject>();
    private readonly List<GameObject> glowObjets = new List<GameObject>();

    // Donnees d'animation par Light (offset temporel + seed flicker
    // pour desynchroniser les instances et avoir un rendu naturel).
    private struct LightAnime
    {
        public Light lumiere;
        public float offsetTemps;
        public float seedFlicker;
    }
    private readonly List<LightAnime> lightsAnimees = new List<LightAnime>();
    private bool glowActifs = false;

    void Start()
    {
        // 1. Collecter tous les indices et leurs glow
        CollecterIndicesEtGlow();

        // 2. Cacher les indices au demarrage si demande
        if (cacherIndicesAuDemarrage)
        {
            foreach (var indice in indicesACacherEtReveler)
            {
                if (indice != null)
                    indice.SetActive(false);
            }
            Debug.Log($"[GlowIndices] {indicesACacherEtReveler.Count} " +
                "indices caches au demarrage.");
        }

        // 3. Mode debug : tout reveler immediatement
        if (activerImmediatement)
        {
            Debug.Log("[GlowIndices] activerImmediatement = true, " +
                "revelation directe.");
            RevelerTout();
            return;
        }

        // 4. S'abonner au declencheur (action prioritaire sur chapitre)
        if (gestionChapitres.Instance == null)
        {
            Debug.LogWarning("[GlowIndices] gestionChapitres.Instance " +
                "introuvable au Start, le declencheur ne fonctionnera pas.");
            return;
        }

        if (!string.IsNullOrEmpty(idActionDeclencheur))
        {
            gestionChapitres.Instance.OnActionSignalee += AuActionSignalee;
            Debug.Log($"[GlowIndices] Abonne a OnActionSignalee, " +
                $"attente de '{idActionDeclencheur}'.");
        }
        else
        {
            gestionChapitres.Instance.OnBanniereChapitreTerminee +=
                AuFinBanniere;
            Debug.Log($"[GlowIndices] Abonne a OnBanniereChapitreTerminee, " +
                $"attente du chapitre '{idChapitreCible}'.");
        }
    }

    void OnDestroy()
    {
        if (gestionChapitres.Instance != null)
        {
            gestionChapitres.Instance.OnActionSignalee -= AuActionSignalee;
            gestionChapitres.Instance.OnBanniereChapitreTerminee -=
                AuFinBanniere;
        }
    }

    private void CollecterIndicesEtGlow()
    {
        indicesACacherEtReveler.Clear();
        glowObjets.Clear();

        // Indices a cacher + reveler : collecter le GameObject ET son glow
        foreach (string tag in tagsAReveler)
        {
            if (string.IsNullOrEmpty(tag)) continue;
            GameObject[] indices = TrouverParTagSafe(tag);
            foreach (var indice in indices)
            {
                indicesACacherEtReveler.Add(indice);
                AjouterGlowSiPresent(indice);
            }
        }

        // Objets a ne pas cacher mais qui doivent recevoir le glow
        foreach (string tag in tagsRecevantGlowSeulement)
        {
            if (string.IsNullOrEmpty(tag)) continue;
            GameObject[] objets = TrouverParTagSafe(tag);
            foreach (var obj in objets)
            {
                AjouterGlowSiPresent(obj);
            }
        }

        Debug.Log($"[GlowIndices] {indicesACacherEtReveler.Count} " +
            $"indices a reveler + {glowObjets.Count} effets glow trouves.");
    }

    private GameObject[] TrouverParTagSafe(string tag)
    {
        try
        {
            return GameObject.FindGameObjectsWithTag(tag);
        }
        catch
        {
            Debug.LogWarning($"[GlowIndices] Tag '{tag}' invalide " +
                "(non defini dans Tag Manager). Ignore.");
            return new GameObject[0];
        }
    }

    private void AjouterGlowSiPresent(GameObject indice)
    {
        Transform glow = indice.transform.Find(nomEnfantGlow);
        if (glow != null)
        {
            glowObjets.Add(glow.gameObject);

            // Si une Light est presente sur le Glow, on l'enregistre
            // pour l'animation. Chaque Light a un offset temporel
            // aleatoire pour que les indices ne pulsent pas en synchro.
            Light lum = glow.GetComponent<Light>();
            if (lum != null)
            {
                lightsAnimees.Add(new LightAnime
                {
                    lumiere = lum,
                    offsetTemps = Random.Range(0f, 2f),
                    seedFlicker = Random.Range(0f, 1000f)
                });
            }
        }
        else
        {
            Debug.LogWarning($"[GlowIndices] L'indice " +
                $"'{indice.name}' n'a pas d'enfant " +
                $"'{nomEnfantGlow}'. Glow ignore (mais l'indice " +
                "sera quand meme revele).");
        }
    }

    void Update()
    {
        // Animation : ne s'active qu'apres que les glow soient reveles
        // ET si l'option animerGlow est cochee. Sinon Update ne fait rien.
        if (!glowActifs || !animerGlow) return;

        foreach (var la in lightsAnimees)
        {
            if (la.lumiere == null) continue;

            float t = Time.time - la.offsetTemps;
            float intensite = intensiteMin;

            // Composante pulse (sinusoidale)
            if (modeAnimation == ModeAnimationGlow.Pulse
                || modeAnimation == ModeAnimationGlow.PulseEtFlicker)
            {
                float phase = (t / dureeCyclePulse) * Mathf.PI * 2f;
                float pulse01 = (Mathf.Sin(phase) + 1f) / 2f;
                intensite = Mathf.Lerp(intensiteMin, intensiteMax, pulse01);
            }

            // Composante flicker (Perlin noise pour effet organique)
            if ((modeAnimation == ModeAnimationGlow.Flicker
                || modeAnimation == ModeAnimationGlow.PulseEtFlicker)
                && amplitudeFlicker > 0f)
            {
                float bruit = Mathf.PerlinNoise(
                    t * vitesseFlicker, la.seedFlicker);
                float deltaFlicker = (bruit - 0.5f) * 2f * amplitudeFlicker
                    * (intensiteMax - intensiteMin);

                if (modeAnimation == ModeAnimationGlow.Flicker)
                {
                    float moyenne = (intensiteMin + intensiteMax) * 0.5f;
                    intensite = moyenne + deltaFlicker;
                }
                else
                {
                    intensite += deltaFlicker;
                }
            }

            la.lumiere.intensity = Mathf.Max(0f, intensite);
        }
    }

    private void AuActionSignalee(string idAction)
    {
        if (idAction != idActionDeclencheur) return;

        Debug.Log($"[GlowIndices] Action '{idAction}' signalee, " +
            "revelation des indices.");
        RevelerTout();

        // Desabonnement apres declenchement (pas besoin de re-trigger)
        if (gestionChapitres.Instance != null)
            gestionChapitres.Instance.OnActionSignalee -= AuActionSignalee;
    }

    private void AuFinBanniere(string idChapitre)
    {
        if (idChapitre != idChapitreCible) return;

        Debug.Log($"[GlowIndices] Banniere '{idChapitre}' terminee, " +
            "revelation des indices.");
        RevelerTout();
    }

    private void RevelerTout()
    {
        // 1. Reactiver les indices caches
        foreach (var indice in indicesACacherEtReveler)
        {
            if (indice != null)
                indice.SetActive(true);
        }
        // 2. Activer tous les glow
        foreach (var glow in glowObjets)
        {
            if (glow != null)
                glow.SetActive(true);
        }
        // 3. Activer la boucle d'animation (Update ne fera rien tant
        //    que glowActifs = false, pour eviter les calculs inutiles
        //    en attendant le declencheur).
        glowActifs = true;
    }
}
