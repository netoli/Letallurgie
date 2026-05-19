using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class gestionBanniere : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text texteNomChapitre;
    [SerializeField] private CanvasGroup groupeBanniere;
    [SerializeField] private Animator animatorBanniere;
    [SerializeField] private AudioSource audioSource;

    [Header("Parametres animation")]
    [SerializeField] private string triggerApparition;
    [SerializeField] private string triggerDisparition;
    [SerializeField] private float dureeFadeFinal;

    [Header("Vignette pendant la banniere (optionnel)")]
    [Tooltip("Volume global URP qui contient le profil de " +
        "post-processing. Si null, la vignette est desactivee.")]
    [SerializeField] private Volume volumeGlobal;

    [Tooltip("Intensite cible de la vignette pendant la banniere. " +
        "0 = aucune vignette, 1 = tres sombre. Recommande : 0.4-0.6.")]
    [Range(0f, 1f)]
    [SerializeField] private float vignetteIntensiteCible = 0.5f;

    [Tooltip("Smoothness de la vignette. 0 = bord net (anneau " +
        "sombre + gros cercle clair au centre), 1 = transition tres " +
        "douce qui descend jusqu'au centre. Augmente pour reduire " +
        "l'espace clair au milieu. Recommande : 0.8-1.0.")]
    [Range(0f, 1f)]
    [SerializeField] private float vignetteSmoothness = 1.0f;

    [Tooltip("Si coche, vignette circulaire parfaite (rounded). " +
        "Sinon, suit le ratio de l'ecran (rectangulaire). Decoche " +
        "pour mieux couvrir les bords sur ecran large 16:9.")]
    [SerializeField] private bool vignetteRounded = true;

    [Tooltip("Couleur de la vignette. Noir par defaut pour " +
        "assombrissement classique. Bleu sombre pour effet glacial, " +
        "rouge pour effet inquietant, dore pour effet 'magique'.")]
    [SerializeField] private Color vignetteCouleur = Color.black;

    [Tooltip("Duree (s) du fade in/out de la vignette.")]
    [SerializeField] private float vignetteFadeDuree = 0.5f;

    [Header("Effets de particules plein ecran (optionnel)")]
    [Tooltip("ParticleSystems a declencher quand la banniere " +
        "apparait. Doivent etre enfants de canvas_hud (avec " +
        "UIParticle si UI) pour couvrir tout l'ecran, PAS de la " +
        "tuile. Leur 'Play On Awake' doit etre decoche.")]
    [SerializeField] private ParticleSystem[] particulesPleinEcran;

    [Tooltip("Si coche, n'active les particules qu'a partir de la " +
        "2e banniere (changements de chapitre), comme la vignette. " +
        "Decoche pour les activer a chaque banniere.")]
    [SerializeField] private bool particulesSeulementChangementChapitre = true;

    [Tooltip("Delai (s) avant que les particules demarrent. Utile " +
        "pour les synchroniser avec l'apparition du texte du chapitre " +
        "(l'Animator du texte prend du temps a faire le fade in). " +
        "Recommande : 0.2-0.5s selon ton animation. 0 = immediat.")]
    [SerializeField] private float delaiAvantParticules = 0.3f;

    private Vignette vignette;
    private Coroutine vignetteCoroutineActive;

    // Compteur d'affichages de la banniere. Sert a ne PAS activer
    // la vignette lors de la toute premiere banniere (le joueur
    // arrive dans le jeu, on ne veut pas de transition cinematique),
    // et a l'activer seulement aux changements de chapitre suivants.
    private int nombreAffichages = 0;

    void Awake()
    {
        if (groupeBanniere != null)
            groupeBanniere.alpha = 0f;

        // Initialisation de la vignette (ajoute le override au
        // profil URP s'il n'existe pas deja, et le laisse desactive).
        // On configure aussi smoothness, roundness et couleur pour
        // que la vignette occupe tout l'ecran (sans le grand cercle
        // clair au centre dû au smoothness par defaut de 0.2).
        if (volumeGlobal != null && volumeGlobal.profile != null)
        {
            if (!volumeGlobal.profile.TryGet(out vignette))
                vignette = volumeGlobal.profile.Add<Vignette>(true);
            vignette.active = false;
            vignette.intensity.Override(0f);
            vignette.smoothness.Override(vignetteSmoothness);
            vignette.rounded.Override(vignetteRounded);
            vignette.color.Override(vignetteCouleur);
        }
    }

    private IEnumerator FadeVignette(float cible, float duree)
    {
        if (vignette == null) yield break;

        vignette.active = true;
        float depart = vignette.intensity.value;
        float t = 0f;

        while (t < duree)
        {
            t += Time.unscaledDeltaTime;
            float v = Mathf.Lerp(depart, cible,
                Mathf.Clamp01(t / duree));
            vignette.intensity.Override(v);
            yield return null;
        }

        vignette.intensity.Override(cible);
        // Si on fade jusqu'a 0, on desactive completement pour
        // libérer le post-processing.
        if (cible <= 0.001f) vignette.active = false;

        vignetteCoroutineActive = null;
    }

    private void DemarrerFadeVignette(float cible)
    {
        if (vignette == null) return;
        if (vignetteCoroutineActive != null)
            StopCoroutine(vignetteCoroutineActive);
        vignetteCoroutineActive = StartCoroutine(
            FadeVignette(cible, vignetteFadeDuree));
    }

    private IEnumerator DemarrerParticulesApresDelai(float delai)
    {
        if (delai > 0f)
            yield return new WaitForSecondsRealtime(delai);

        if (particulesPleinEcran == null) yield break;

        foreach (var ps in particulesPleinEcran)
        {
            if (ps != null)
            {
                ps.gameObject.SetActive(true);
                ps.Play(true);
            }
        }
    }

public IEnumerator AfficherBanniere(
        string nomChapitre, float duree)
    {
        Debug.Log("[Banniere] AfficherBanniere: " + nomChapitre);

        gameObject.SetActive(true);

        if (texteNomChapitre != null)
            texteNomChapitre.text = nomChapitre;

        if (animatorBanniere != null
            && !string.IsNullOrEmpty(triggerApparition))
            animatorBanniere.SetTrigger(triggerApparition);

        if (groupeBanniere != null)
            groupeBanniere.alpha = 1f;

        // Joue le son de la banniere
        if (audioSource != null && audioSource.clip != null)
            audioSource.Play();

        nombreAffichages++;

        // Vignette : on n'active l'effet qu'a partir de la 2e banniere
        // (changement de chapitre). La toute premiere banniere du jeu
        // ne declenche pas la vignette pour ne pas saturer l'entree.
        bool activerVignettePourCetteBanniere = nombreAffichages >= 2;
        if (activerVignettePourCetteBanniere)
        {
            DemarrerFadeVignette(vignetteIntensiteCible);
        }

        // Particules plein ecran : Play sur tous les systemes
        // configures, AVEC UN DELAI optionnel pour les synchroniser
        // avec l'apparition du texte (souvent l'animator du texte
        // met du temps a faire son fade in, et on veut que les
        // particules apparaissent en meme temps que le texte).
        bool activerParticules = particulesPleinEcran != null
            && particulesPleinEcran.Length > 0
            && (!particulesSeulementChangementChapitre
                || nombreAffichages >= 2);
        if (activerParticules)
        {
            StartCoroutine(DemarrerParticulesApresDelai(
                delaiAvantParticules));
        }
        Debug.Log("[Banniere] Attente "
            + Mathf.Max(0.1f, duree - dureeFadeFinal) + "s");

        yield return new WaitForSecondsRealtime(
            Mathf.Max(0.1f, duree - dureeFadeFinal));

        Debug.Log("[Banniere] Debut fade out");

        if (animatorBanniere != null
            && !string.IsNullOrEmpty(triggerDisparition))
            animatorBanniere.SetTrigger(triggerDisparition);

        // Fade out de la vignette en parallele du fade de la banniere
        // (uniquement si elle avait ete activee pour cette banniere).
        if (activerVignettePourCetteBanniere)
        {
            DemarrerFadeVignette(0f);
        }

        // Arret des particules plein ecran (Stop laisse les particules
        // existantes finir leur lifetime naturellement, donc l'effet
        // s'estompe doucement).
        if (activerParticules)
        {
            foreach (var ps in particulesPleinEcran)
            {
                if (ps != null) ps.Stop(true,
                    ParticleSystemStopBehavior.StopEmitting);
            }
        }

        float t = 0f;
        while (t < dureeFadeFinal)
        {
            t += Time.unscaledDeltaTime;
            if (groupeBanniere != null)
                groupeBanniere.alpha = Mathf.Lerp(1f, 0f,
                    t / dureeFadeFinal);
            yield return null;
        }

        Debug.Log("[Banniere] Fade out termine");

        if (groupeBanniere != null)
            groupeBanniere.alpha = 0f;

        gameObject.SetActive(false);
    }
}