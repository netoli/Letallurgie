// ============================================================
// sfxLoopAleatoire.cs
// ------------------------------------------------------------
// Auteur      : Fanny Fortier
// Date créée  : 2026-05-23
// ------------------------------------------------------------
// Description :
//   Joue un AudioSource en boucle avec un délai aléatoire entre
//   chaque lecture. Remplace le "Loop" natif de l'AudioSource
//   pour éviter un enchaînement trop mécanique.
//
//   Usage :
//     1. Décocher "Loop" sur l'AudioSource concernée.
//     2. Ajouter ce script sur le même GameObject.
//     3. Assigner la source et configurer les délais min/max.
//
//   Compatible avec sfxAmbiancePnj : si les deux scripts sont
//   sur le même prefab, sfxAmbiancePnj gère le volume et ce
//   script gère le timing — ils ne se conflictuent pas.
// ============================================================

using System.Collections;
using UnityEngine;

public class sfxLoopAleatoire : MonoBehaviour
{
    [Header("Source audio")]
    [Tooltip("AudioSource à looper. Son option Loop doit être décochée.")]
    [SerializeField] private AudioSource _source;

    [Header("Délai entre les loops")]
    [Tooltip("Délai minimum (secondes) avant de rejouer le clip.")]
    [SerializeField] private float _delaiMin = 3f;

    [Tooltip("Délai maximum (secondes) avant de rejouer le clip. " +
             "Doit être >= delaiMin.")]
    [SerializeField] private float _delaiMax = 8f;

    [Tooltip("Si coché, le premier play se fait aussi après un délai " +
             "aléatoire (utile pour désynchroniser plusieurs instances " +
             "du même prefab placées dans la scène).")]
    [SerializeField] private bool _delaiAuDemarrage = true;

    void Start()
    {
        if (_source == null)
            _source = GetComponent<AudioSource>();

        if (_source == null)
        {
            Debug.LogWarning("[sfxLoopAleatoire] Aucune AudioSource " +
                             $"trouvée sur {name}. Script désactivé.");
            enabled = false;
            return;
        }

        // S'assurer que Loop est désactivé pour ne pas interférer
        _source.loop = false;
        _source.Stop();

        StartCoroutine(BoucleAvecDelai());
    }

    private IEnumerator BoucleAvecDelai()
    {
        // Délai initial optionnel pour désynchroniser les instances
        if (_delaiAuDemarrage)
        {
            float delaiInitial = Random.Range(0f, _delaiMax);
            yield return new WaitForSeconds(delaiInitial);
        }

        while (true)
        {
            // Jouer le clip
            if (_source != null && _source.clip != null)
                _source.Play();

            // Attendre la fin du clip + le délai aléatoire
            float dureeClip = (_source != null && _source.clip != null)
                ? _source.clip.length
                : 0f;

            float delaiSupplementaire = Random.Range(_delaiMin, _delaiMax);
            yield return new WaitForSeconds(dureeClip + delaiSupplementaire);
        }
    }

    // ── API publique ──────────────────────────────────────────

    /// <summary>
    /// Arrête le loop en cours. Utile si l'objet doit se désactiver
    /// proprement (ex : disparition du PNJ).
    /// </summary>
    public void ArreterLoop()
    {
        StopAllCoroutines();
        if (_source != null)
            _source.Stop();
    }

    /// <summary>
    /// Redémarre le loop (après un ArreterLoop ou une réactivation).
    /// </summary>
    public void RedemarrerLoop()
    {
        StopAllCoroutines();
        StartCoroutine(BoucleAvecDelai());
    }
}
