using UnityEngine;

/// <summary>
/// [OBSOLETE] Cette animation est maintenant integree directement dans
/// gestionGlowIndices (champs Animer Glow + Mode Animation + intensites
/// dans l'Inspector). Plus simple : tout est centralise sur un seul
/// script. Ne pas utiliser ce script standalone.
///
/// Conserve pour les cas particuliers ou tu voudrais animer une Light
/// independante de gestionGlowIndices (ex : torche d'ambiance dans une
/// autre scene).
///
/// Anime la Light d'un GameObject Glow pour donner un effet de reflet
/// vivant (pulsation, scintillement, flicker). A attacher sur le meme
/// GameObject que la Light (typiquement l'enfant 'Glow' d'un indice).
///
/// 3 modes disponibles :
/// - Pulse : oscillation sinusoidale reguliere (respiration douce).
/// - Flicker : variation aleatoire rapide (style flamme de chandelle).
/// - Pulse + Flicker : combine les deux (subtil reflet vivant).
///
/// SETUP UNITY :
/// 1. Sur le GameObject Glow (qui contient deja la Light), Add Component
///    → effetGlowPulse.
/// 2. Inspector : choisir le mode et ajuster les valeurs.
/// 3. Si plusieurs indices ont le meme prefab Glow, ils auront tous la
///    meme animation. Pour varier, ajoute un petit decalage aleatoire
///    via 'offsetTempsAleatoire' (chaque indice demarre a une phase
///    differente, evite l'effet "tout pulse en meme temps").
/// </summary>
[RequireComponent(typeof(Light))]
public class effetGlowPulse : MonoBehaviour
{
    public enum ModeAnimation
    {
        Pulse,           // Oscillation sinusoidale reguliere
        Flicker,         // Variation aleatoire (style flamme)
        PulseEtFlicker   // Combinaison des deux
    }

    [Header("Mode")]
    [Tooltip("Type d'animation a appliquer sur l'intensite de la Light.")]
    [SerializeField] private ModeAnimation mode = ModeAnimation.Pulse;

    [Header("Intensite")]
    [Tooltip("Intensite minimale de la Light pendant l'animation.")]
    [SerializeField] private float intensiteMin = 1.2f;

    [Tooltip("Intensite maximale de la Light pendant l'animation.")]
    [SerializeField] private float intensiteMax = 2.4f;

    [Header("Pulse (mode Pulse / PulseEtFlicker)")]
    [Tooltip("Duree (s) d'un cycle complet (min -> max -> min). " +
        "Defaut 2s = respiration lente. 0.5s = rapide.")]
    [SerializeField] private float dureeCycle = 2f;

    [Header("Flicker (mode Flicker / PulseEtFlicker)")]
    [Tooltip("Amplitude du flicker (variation aleatoire de l'intensite). " +
        "0 = pas de flicker. 0.3 = leger. 1 = tres marque. Combine avec " +
        "le pulse de base si mode PulseEtFlicker.")]
    [SerializeField, Range(0f, 1f)] private float amplitudeFlicker = 0.2f;

    [Tooltip("Vitesse du flicker (Hz). 5 = rapide style flamme. " +
        "1 = lent style respiration aleatoire.")]
    [SerializeField] private float vitesseFlicker = 4f;

    [Header("Variation entre instances")]
    [Tooltip("Si > 0, chaque instance demarre avec un offset temporel " +
        "aleatoire (jusqu'a cette valeur en secondes). Evite que tous " +
        "les glow de la scene pulsent en synchro. Recommande : 1-2s.")]
    [SerializeField] private float offsetTempsAleatoire = 1f;

    private Light lumiere;
    private float tempsDemarrage;
    private float seedFlicker;

    void Awake()
    {
        lumiere = GetComponent<Light>();
        // Decalage temporel aleatoire pour desynchroniser les instances
        tempsDemarrage = -Random.Range(0f, offsetTempsAleatoire);
        // Seed unique pour le Perlin Noise du flicker
        seedFlicker = Random.Range(0f, 1000f);
    }

    void Update()
    {
        if (lumiere == null) return;

        float t = Time.time - tempsDemarrage;
        float intensite = intensiteMin;

        // Composante pulse (sinusoidale 0..1)
        if (mode == ModeAnimation.Pulse
            || mode == ModeAnimation.PulseEtFlicker)
        {
            // Sin oscille entre -1 et 1. On normalise sur 0..1 avec
            // (sin + 1) / 2, puis on map vers intensiteMin..intensiteMax.
            float phase = (t / dureeCycle) * Mathf.PI * 2f;
            float pulse01 = (Mathf.Sin(phase) + 1f) / 2f;
            intensite = Mathf.Lerp(intensiteMin, intensiteMax, pulse01);
        }

        // Composante flicker (Perlin noise, plus organique qu'un random)
        if ((mode == ModeAnimation.Flicker
            || mode == ModeAnimation.PulseEtFlicker)
            && amplitudeFlicker > 0f)
        {
            float bruit = Mathf.PerlinNoise(
                t * vitesseFlicker, seedFlicker);
            // Le Perlin retourne 0..1. On centre autour de 0 et on
            // applique l'amplitude.
            float deltaFlicker = (bruit - 0.5f) * 2f * amplitudeFlicker
                * (intensiteMax - intensiteMin);

            if (mode == ModeAnimation.Flicker)
            {
                // En mode flicker pur, on part de la moyenne et on varie
                float moyenne = (intensiteMin + intensiteMax) * 0.5f;
                intensite = moyenne + deltaFlicker;
            }
            else
            {
                // En mode combine, on ajoute le flicker au pulse
                intensite += deltaFlicker;
            }
        }

        // Clamp final pour eviter les valeurs negatives en cas de
        // flicker tres amplifie
        lumiere.intensity = Mathf.Max(0f, intensite);
    }
}
