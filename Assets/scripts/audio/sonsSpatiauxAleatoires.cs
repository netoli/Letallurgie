// ============================================================
// sonsSpatiauxAleatoires.cs
// ------------------------------------------------------------
// Joue aleatoirement des AudioClips depuis une liste, sur un
// AudioSource 3D dont le volume s'attenue avec la distance.
//
// USAGE TYPIQUE :
// - Attache ce script sur un GameObject porteur d'un AudioSource
//   3D (ex : la source du tavernier qui souffre dans l'usine).
// - Glisse les clips dans 'clips' (tu peux en ajouter autant que
//   tu veux, la liste est dynamique).
// - Quand le joueur s'approche, le volume monte; loin, silence.
//
// COUPURE EN DOUCEUR :
// - Le script ecoute gestionChapitres.OnActionSignalee. Quand
//   l'action 'idActionCoupure' (defaut "tavernier_trouve") est
//   signalee, il fait un FONDU de volume vers 0 sur
//   'dureeFadeCoupure' secondes, puis s'arrete — au lieu d'une
//   coupure seche.
// - IMPORTANT : pour que le fondu se fasse, ce GameObject doit
//   RESTER ACTIF pendant le fondu. Ne le fais donc PAS desactiver
//   (SetActive false) par detecteurTuto / gestionDisparitionAction
//   sur la meme action. Laisse ce script gerer la coupure : c'est
//   lui qui s'eteint en douceur. (Le pointeur VISUEL, lui, peut
//   continuer a se desactiver de son cote.)
//
// COURBE DE DISTANCE :
// - 'utiliserCourbePersonnalisee' remplace l'attenuation lineaire
//   par une courbe (AnimationCurve) que tu peux modeler avec
//   autant de points que tu veux pour une attenuation plus fluide.
// ============================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class sonsSpatiauxAleatoires : MonoBehaviour
{
    [Header("Clips")]
    [Tooltip("Liste de clips audio a jouer aleatoirement. Le script " +
        "en pioche un a chaque intervalle. Ajoute-en autant que tu veux.")]
    [SerializeField] private List<AudioClip> clips;

    [Header("Intervalles entre 2 sons (secondes)")]
    [SerializeField] private float intervalleMin = 4f;
    [SerializeField] private float intervalleMax = 10f;

    [Header("Delai initial")]
    [Tooltip("Delai avant le tout premier son (s).")]
    [SerializeField] private float delaiInitial = 1f;

    [Header("Configuration AudioSource (applique en Awake)")]
    [Tooltip("Si coche, force la configuration 3D de l'AudioSource au " +
        "demarrage (Spatial Blend = 1, Min/Max Distance, rolloff).")]
    [SerializeField] private bool forcerConfigSpatiale = true;

    [Tooltip("Distance en dessous de laquelle le son est a volume max.")]
    [SerializeField] private float distanceMin = 2f;
    [Tooltip("Distance au-dela de laquelle le son est inaudible.")]
    [SerializeField] private float distanceMax = 200f;
    [Tooltip("Volume du son a pleine proximite (avant attenuation).")]
    [Range(0f, 1f)]
    [SerializeField] private float volumeMaxClip = 1f;

    [Header("Courbe de distance (fluidite)")]
    [Tooltip("Si coche, l'attenuation suit la courbe ci-dessous (rolloff " +
        "Custom) au lieu d'un rolloff lineaire. Ajoute des points a la " +
        "courbe pour une attenuation plus fine et plus fluide. X = " +
        "distance normalisee (0 = source, 1 = distanceMax), Y = volume.")]
    [SerializeField] private bool utiliserCourbePersonnalisee = true;

    [Tooltip("Courbe d'attenuation (volume selon la distance normalisee). " +
        "Defaut : descente douce en S de 1 vers 0.")]
    [SerializeField] private AnimationCurve courbeAttenuation =
        new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.15f, 0.9f),
            new Keyframe(0.5f, 0.45f),
            new Keyframe(0.8f, 0.12f),
            new Keyframe(1f, 0f));

    [Header("Declenchement / Fondus")]
    [Tooltip("Si renseigne, les sons NE jouent PAS tant que cette action " +
        "n'a pas ete signalee. Defaut : l'action du bandeau 'Trouve " +
        "l'endroit ou le tavernier est emprisonne'. Laisse vide pour " +
        "jouer des l'activation de l'objet.")]
    [SerializeField] private string idActionDemarrage =
        "chapitre_le_sauvetage_banniere_terminee";

    [Tooltip("Duree du fondu d'entree quand un clip demarre (s).")]
    [SerializeField] private float dureeFadeIn = 0.4f;

    [Tooltip("Action qui declenche la coupure EN DOUCEUR des sons " +
        "spatiaux (fondu vers 0). Defaut : 'tavernier_trouve'. Doit etre " +
        "l'idAction signalee quand le joueur atteint le tavernier.")]
    [SerializeField] private string idActionCoupure = "tavernier_trouve";

    [Tooltip("Duree du fondu de coupure (s) quand idActionCoupure est " +
        "signalee. Plus grand = coupure plus douce.")]
    [SerializeField] private float dureeFadeCoupure = 1.5f;

    private AudioSource source;
    private float prochainSonTime;
    private bool coupe = false;
    private bool demarre = false;
    private bool inscrit = false;
    private Coroutine fadeEnCours;

    void Awake()
    {
        source = GetComponent<AudioSource>();
        if (source == null) return;

        if (forcerConfigSpatiale)
        {
            source.spatialBlend = 1f;
            source.minDistance = distanceMin;
            source.maxDistance = distanceMax;
            source.playOnAwake = false;
            source.loop = false;

            if (utiliserCourbePersonnalisee && courbeAttenuation != null
                && courbeAttenuation.length > 0)
            {
                source.rolloffMode = AudioRolloffMode.Custom;
                source.SetCustomCurve(
                    AudioSourceCurveType.CustomRolloff, courbeAttenuation);
            }
            else
            {
                source.rolloffMode = AudioRolloffMode.Linear;
            }
        }

        source.volume = volumeMaxClip;
    }

    void OnEnable()
    {
        coupe = false;
        // Si une action de demarrage est requise, les sons attendent
        // qu'elle soit signalee. Sinon (champ vide), ils jouent des
        // l'activation de l'objet.
        demarre = string.IsNullOrEmpty(idActionDemarrage);
        if (demarre)
            prochainSonTime = Time.time + delaiInitial;
        SInscrireCoupure();
    }

    void OnDisable()
    {
        DesinscrireCoupure();
    }

    // ── Inscription a l'action de coupure ─────────────────────

    private void SInscrireCoupure()
    {
        if (inscrit) return;
        if (string.IsNullOrEmpty(idActionCoupure)) return;
        if (gestionChapitres.Instance == null) return;
        gestionChapitres.Instance.OnActionSignalee += AuActionSignalee;
        inscrit = true;
    }

    private void DesinscrireCoupure()
    {
        if (!inscrit) return;
        if (gestionChapitres.Instance != null)
            gestionChapitres.Instance.OnActionSignalee -= AuActionSignalee;
        inscrit = false;
    }

    private void AuActionSignalee(string id)
    {
        // Demarrage : tant que l'action de demarrage n'est pas signalee,
        // aucun son. Quand elle l'est, on amorce la sequence de lecture.
        if (!demarre && id == idActionDemarrage)
        {
            demarre = true;
            prochainSonTime = Time.time + delaiInitial;
            Debug.Log($"[sonsSpatiauxAleatoires] {name} : demarrage sur " +
                $"action '{id}'.");
            return;
        }

        if (coupe) return;
        if (id == idActionCoupure)
            CouperEnDouceur(dureeFadeCoupure);
    }

    // ── Boucle de lecture ─────────────────────────────────────

    void Update()
    {
        // Garde-fou : si l'inscription n'a pas pu se faire en OnEnable
        // (gestionChapitres pas encore pret), on retente.
        if (!inscrit) SInscrireCoupure();

        if (!demarre) return;   // attend l'action de demarrage
        if (coupe) return;
        if (source == null) return;
        if (clips == null || clips.Count == 0) return;

        if (Time.time >= prochainSonTime && !source.isPlaying)
        {
            JouerClipAleatoire();
            prochainSonTime = Time.time + Random.Range(
                intervalleMin, intervalleMax);
        }
    }

    private void JouerClipAleatoire()
    {
        AudioClip clip = clips[Random.Range(0, clips.Count)];
        if (clip == null) return;

        // Fondu d'entree : on part de 0 et on remonte vers volumeMaxClip.
        // (source.volume module aussi le PlayOneShot en cours.)
        if (fadeEnCours != null) StopCoroutine(fadeEnCours);
        source.volume = 0f;
        source.PlayOneShot(clip, 1f);
        fadeEnCours = StartCoroutine(FondreVolume(volumeMaxClip, dureeFadeIn));
    }

    // ── Coupure en douceur ────────────────────────────────────

    /// <summary>
    /// Coupe le son en fondu sur 'duree' secondes, puis stoppe et
    /// empeche tout nouveau clip de demarrer. Appelable de l'exterieur
    /// si tu veux declencher la coupure autrement que par l'action.
    /// </summary>
    public void CouperEnDouceur(float duree)
    {
        coupe = true;
        if (fadeEnCours != null) StopCoroutine(fadeEnCours);
        fadeEnCours = StartCoroutine(FondrePuisStopper(duree));
        Debug.Log($"[sonsSpatiauxAleatoires] {name} : coupure en douceur " +
            $"sur {duree:F1}s.");
    }

    private IEnumerator FondrePuisStopper(float duree)
    {
        yield return FondreVolume(0f, duree);
        if (source != null) source.Stop();
        fadeEnCours = null;
    }

    private IEnumerator FondreVolume(float cible, float duree)
    {
        if (source == null) yield break;
        float depart = source.volume;
        if (duree <= 0f)
        {
            source.volume = cible;
            yield break;
        }
        float t = 0f;
        while (t < duree)
        {
            t += Time.deltaTime;
            source.volume = Mathf.Lerp(depart, cible, t / duree);
            yield return null;
        }
        source.volume = cible;
    }
}
