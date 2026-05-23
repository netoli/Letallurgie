// ============================================================
// sfxAmbiancePnj.cs
// ------------------------------------------------------------
// Auteur      : Fanny Fortier
// Date créée  : 2026-05-23
// ------------------------------------------------------------
// Description :
//   Gère le volume des sons d'ambiance de taverne (pnjs_happy /
//   pnjs_triste). Fait un fade progressif vers 0 quand le menu
//   principal, le journal, les options ou une cinématique sont
//   actifs, et remonte progressivement au volume normal autrement.
//
//   À placer sur le prefab taverne0-envhappy ou taverne1-envtriste.
//   Assigner toutes les AudioSources d'ambiance du prefab dans
//   le tableau _sourcesAmbiance via l'Inspector.
// ============================================================

using UnityEngine;

public class sfxAmbiancePnj : MonoBehaviour
{
    [Header("Sources audio à gérer")]
    [Tooltip("Toutes les AudioSources d'ambiance du prefab " +
             "(sons de PNJ, murmures, bruits de fond, etc.).")]
    [SerializeField] private AudioSource[] _sourcesAmbiance;

    [Header("Paramètres de volume")]
    [Range(0f, 1f)]
    [Tooltip("Volume cible quand le jeu est en cours (aucun menu ouvert).")]
    [SerializeField] private float _volumeJeu = 1f;

    [Range(0f, 1f)]
    [Tooltip("Volume cible quand un menu/journal/options/cinématique est ouvert.")]
    [SerializeField] private float _volumeMute = 0f;

    [Tooltip("Vitesse du fade (unités de volume par seconde). " +
             "1.5 = fondu en ~0.7s, 0.5 = fondu en ~2s.")]
    [SerializeField] private float _vitesseFade = 1.5f;

    // Référence cachée pour éviter un FindFirstObjectByType par frame
    private gestionInputsJeu _gestionInputs;

    void Start()
    {
        _gestionInputs = FindFirstObjectByType<gestionInputsJeu>();

        // Volume de départ : muet si on est hors-gameplay (ex : menu)
        float volumeInitial = EstEnJeuActif() ? _volumeJeu : _volumeMute;
        AppliquerVolume(volumeInitial);
    }

    void Update()
    {
        float cible = EstEnJeuActif() ? _volumeJeu : _volumeMute;

        foreach (AudioSource source in _sourcesAmbiance)
        {
            if (source == null) continue;

            source.volume = Mathf.MoveTowards(
                source.volume,
                cible,
                _vitesseFade * Time.unscaledDeltaTime);
        }
    }

    // ── Méthodes privées ──────────────────────────────────────

    /// <summary>
    /// Retourne true si le gameplay est actif sans aucun menu ouvert.
    /// Si gestionInputsJeu est absent (scène sans système de menu),
    /// on suppose que le jeu est en cours.
    /// </summary>
    private bool EstEnJeuActif()
    {
        if (_gestionInputs == null) return true;
        return _gestionInputs.JeuEnCoursActif;
    }

    /// <summary>
    /// Applique un volume instantané sur toutes les sources,
    /// sans transition (utilisé pour l'initialisation).
    /// </summary>
    private void AppliquerVolume(float volume)
    {
        foreach (AudioSource source in _sourcesAmbiance)
        {
            if (source != null)
                source.volume = volume;
        }
    }
}
