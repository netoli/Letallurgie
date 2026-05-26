// ============================================================
// gestionHighlightHover.cs
// ------------------------------------------------------------
// Auteur      : Fanny Fortier
// ------------------------------------------------------------
// Description :
//   Gère l'effet de surbrillance au survol d'un objet
//   interactif. Utilise un ParticleSystem (brouillard circulaire)
//   et une Point Light dont l'intensité lerpe au survol.
//
//   Setup dans l'Inspecteur :
//     - Créer un enfant "highlight_particules" sur l'objet
//       ParticleSystem : Play On Awake OFF, Looping ON
//       Simulation Space : World, Scaling Mode : Local
//     - Assigner la Point Light de l'enfant dans _lumiereHighlight
// ============================================================

using System.Collections;
using UnityEngine;

public class gestionHighlightHover : MonoBehaviour
{
    [Header("Particules de highlight")]
    [Tooltip("ParticleSystem enfant de l'objet (brouillard circulaire). Play On Awake doit être OFF.")]
    [SerializeField] private ParticleSystem _particulesHighlight;

    [Header("Lumière de highlight")]
    [Tooltip("Point Light sur l'enfant HighlightObjet.")]
    [SerializeField] private Light _lumiereHighlight;
    [Tooltip("Intensité de base de la lumière (état inactif).")]
    [SerializeField] private float _intensiteBase   = 5f;
    [Tooltip("Intensité cible au survol.")]
    [SerializeField] private float _intensiteSurvol = 50f;
    [Tooltip("Durée du lerp d'intensité (secondes).")]
    [SerializeField] private float _dureeLerp       = 0.3f;

    private Coroutine _coroutineLumiere;

    // ===================== MÉTHODE PUBLIQUE =====================

    public void Highlighter(bool actif)
    {
        // --- Particules ---
        if (_particulesHighlight != null)
        {
            if (actif)
                _particulesHighlight.Play();
            else
                _particulesHighlight.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        // --- Lumière ---
        if (_lumiereHighlight != null)
        {
            if (_coroutineLumiere != null)
                StopCoroutine(_coroutineLumiere);

            float cible = actif ? _intensiteSurvol : _intensiteBase;
            _coroutineLumiere = StartCoroutine(LerperIntensiteLumiere(cible));
        }
    }

    // ===================== COROUTINE =====================

    private IEnumerator LerperIntensiteLumiere(float intensiteCible)
    {
        float depart = _lumiereHighlight.intensity;
        float t = 0f;

        while (t < _dureeLerp)
        {
            t += Time.deltaTime;
            _lumiereHighlight.intensity = Mathf.Lerp(depart, intensiteCible, t / _dureeLerp);
            yield return null;
        }

        _lumiereHighlight.intensity = intensiteCible;
        _coroutineLumiere = null;
    }
}
