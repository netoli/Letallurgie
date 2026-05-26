// ============================================================
// sonsSpatiauxAleatoires.cs
// ------------------------------------------------------------
// Joue aleatoirement des AudioClips depuis une liste, sur un
// AudioSource 3D dont le volume s'attenue avec la distance.
//
// USAGE TYPIQUE :
// - Attache ce script sur prefab_pointeur_personnage (tavernier capture).
// - Le GameObject porte un AudioSource configure en 3D (Spatial
//   Blend = 1, Linear Rolloff, Min Distance = 2, Max Distance = 20).
// - Glisse les clips dans 'clips' (ex : tavernier_demande_aide,
//   tavernier_douleur_usine, tavernier_usine).
// - Le script joue un clip aleatoire toutes les X-Y secondes
//   (intervalle aleatoire entre intervalleMin et intervalleMax).
// - Quand le joueur s'approche, le volume monte. Loin, silence.
// - Si le GameObject est desactive (SetActive false), aucun son.
//   Donc parfait avec gestionActivationAction qui active le pointeur
//   au moment narratif voulu.
// ============================================================

using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class sonsSpatiauxAleatoires : MonoBehaviour
{
    [Header("Clips")]
    [Tooltip("Liste de clips audio a jouer aleatoirement. Le script " +
        "en pioche un a chaque intervalle.")]
    [SerializeField] private List<AudioClip> clips;

    [Header("Intervalles entre 2 sons (secondes)")]
    [Tooltip("Intervalle minimum entre 2 sons (s).")]
    [SerializeField] private float intervalleMin = 4f;

    [Tooltip("Intervalle maximum entre 2 sons (s). " +
        "Le delai effectif est aleatoire entre min et max.")]
    [SerializeField] private float intervalleMax = 10f;

    [Header("Delai initial")]
    [Tooltip("Delai avant le tout premier son (s). 0 = joue immediatement " +
        "au moment ou le GameObject devient actif.")]
    [SerializeField] private float delaiInitial = 1f;

    [Header("Configuration AudioSource (applique en Awake)")]
    [Tooltip("Si coche, force la configuration 3D de l'AudioSource au " +
        "demarrage (Spatial Blend = 1, Linear Rolloff, Min/Max Distance). " +
        "Decoche si tu prefere configurer l'AudioSource manuellement " +
        "dans l'Inspector.")]
    [SerializeField] private bool forcerConfigSpatiale = true;

    [Tooltip("Distance en dessous de laquelle le son est a volume max.")]
    [SerializeField] private float distanceMin = 2f;
    [Tooltip("Distance au-dela de laquelle le son est inaudible. " +
        "ATTENTION : adapter a la taille de ta scene. Si le pointeur " +
        "est a 150 unites du spawn joueur, il faut maxDistance >= 200.")]
    [SerializeField] private float distanceMax = 200f;
    [SerializeField] private float volumeMaxClip = 1f;

    private AudioSource source;
    private float prochainSonTime;

    void Awake()
    {
        source = GetComponent<AudioSource>();
        if (source == null)
        {
            Debug.LogError($"[sonsSpatiauxAleatoires] {name} : " +
                "Pas d'AudioSource trouve. Le RequireComponent aurait " +
                "du en ajouter un. Ajoute-le manuellement.");
            return;
        }

        if (forcerConfigSpatiale)
        {
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = distanceMin;
            source.maxDistance = distanceMax;
            source.volume = volumeMaxClip;
            source.playOnAwake = false;
            source.loop = false;
        }

        Debug.Log($"[sonsSpatiauxAleatoires] {name} Awake : " +
            $"AudioSource OK, spatialBlend={source.spatialBlend}, " +
            $"clips={(clips != null ? clips.Count : 0)}, " +
            $"AudioListener present={FindObjectOfType<AudioListener>() != null}");
    }

    void OnEnable()
    {
        // Quand le GameObject devient actif (typiquement au signal
        // narratif via gestionActivationAction), on programme le 1er
        // son apres delaiInitial.
        prochainSonTime = Time.time + delaiInitial;
        Debug.Log($"[sonsSpatiauxAleatoires] {name} OnEnable : " +
            $"1er son dans {delaiInitial}s.");
    }

    void Update()
    {
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
        if (clip == null)
        {
            Debug.LogWarning($"[sonsSpatiauxAleatoires] {name} : " +
                "clip aleatoire pioche est null.");
            return;
        }
        // Diagnostic : distance joueur et volume effectif estime
        var listener = FindObjectOfType<AudioListener>();
        float dist = listener != null
            ? Vector3.Distance(transform.position, listener.transform.position)
            : -1f;
        Debug.Log($"[sonsSpatiauxAleatoires] {name} JOUE clip " +
            $"'{clip.name}' (dur={clip.length:F1}s, dist joueur={dist:F1}, " +
            $"min={distanceMin}, max={distanceMax}, vol={volumeMaxClip}).");
        source.PlayOneShot(clip, volumeMaxClip);
    }
}
