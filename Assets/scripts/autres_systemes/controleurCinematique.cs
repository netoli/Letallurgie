// ============================================================
// controleurCinematique.cs
// ------------------------------------------------------------
// Auteur      : Fanny Fortier
// Date créée  : 2026-05-23
// ------------------------------------------------------------
// Description :
//   Composant réutilisable pour jouer une cinématique vidéo dans
//   n'importe quelle scène du jeu. Gère lui-même :
//     - La fermeture des fenêtres UI ouvertes
//     - Le blocage des inputs (ModeCinematique)
//     - L'arrêt / la reprise de la musique
//     - Le callback de fin (chargement de scène, unlock, etc.)
//
//   Une instance par scène. Les autres scripts appellent :
//     controleurCinematique.Instance.Jouer("nomClip", () => { ... });
//   ou :
//     controleurCinematique.Instance.Jouer(clipReference, () => { ... });
//
//   Setup Unity (une seule fois par scène) :
//     1. Créer un GameObject "controleur_cinematique"
//     2. Y ajouter un VideoPlayer + ce script
//     3. Dans le VideoPlayer : Source = Video Clip, désactiver
//        Play On Awake, choisir le Render Mode (Camera Near/Far
//        Plane ou Render Texture selon ta config)
//     4. Assigner le VideoPlayer dans l'Inspector de ce script
//     5. (Optionnel) Assigner les VideoClip[] pour appel par nom
//
//   Appels typiques depuis d'autres scripts :
//     // Cinématique puis chargement de scène :
//     controleurCinematique.Instance.Jouer("cinematique_boss", () =>
//         gestionEcranChargement.Instance.ChargerScene("SCENE4-Manoir"));
//
//     // Cinématique puis callback custom :
//     controleurCinematique.Instance.Jouer(monClip, UnlockPorte);
// ============================================================

using System.Collections;
using UnityEngine;
using UnityEngine.Video;

public class controleurCinematique : MonoBehaviour
{
    public static controleurCinematique Instance { get; private set; }

    [Header("Lecteur vidéo")]
    [Tooltip("VideoPlayer sur ce GameObject ou un enfant. " +
             "Play On Awake doit être décoché.")]
    [SerializeField] private VideoPlayer _lecteur;

    [Header("Clips disponibles (appel par nom)")]
    [Tooltip("Remplir pour pouvoir appeler Jouer(\"nomClip\"). " +
             "Laisser vide si tu passes toujours une référence directe.")]
    [SerializeField] private VideoClip[] _clips;

    [Header("Délai avant lecture")]
    [Tooltip("Délai en secondes entre l'appel à Jouer() et le " +
             "début effectif de la vidéo. Permet une courte pause " +
             "narrative avant la cinématique.")]
    [SerializeField] private float _delaiAvantLecture = 0f;

    // ── État interne ──────────────────────────────────────────

    private System.Action _callbackFin;
    private gestionInputsJeu _inputs;
    private bool _cinematiqueEnCours = false;

    // ── Unity ────────────────────────────────────────────────

    void Awake()
    {
        // Singleton local (par scène — pas DontDestroyOnLoad, car
        // chaque scène a son propre VideoPlayer et ses propres clips).
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void OnDestroy()
    {
        // Nettoyer l'abonnement au VideoPlayer pour éviter les fuites
        if (_lecteur != null)
            _lecteur.loopPointReached -= OnCinematiqueFinie;

        if (Instance == this)
            Instance = null;
    }

    // ── API publique ──────────────────────────────────────────

    /// <summary>
    /// Joue une cinématique identifiée par son nom (doit être dans
    /// le tableau _clips de l'Inspector).
    /// </summary>
    /// <param name="nomClip">Nom exact du VideoClip (sans extension).</param>
    /// <param name="onFini">Callback appelé quand la vidéo se termine.
    /// Typiquement : charger la scène suivante, débloquer un objet, etc.
    /// Peut être null.</param>
    public void Jouer(string nomClip, System.Action onFini = null)
    {
        if (_cinematiqueEnCours)
        {
            Debug.LogWarning("[Cinematique] Une cinématique est déjà " +
                             "en cours — appel ignoré.");
            return;
        }

        VideoClip clip = System.Array.Find(_clips, c => c.name == nomClip);
        if (clip == null)
        {
            Debug.LogWarning($"[Cinematique] Clip '{nomClip}' introuvable " +
                             $"dans le tableau de l'Inspector.");
            // Appeler quand même le callback pour ne pas bloquer le jeu
            onFini?.Invoke();
            return;
        }

        Jouer(clip, onFini);
    }

    /// <summary>
    /// Joue une cinématique avec une référence directe au VideoClip.
    /// </summary>
    public void Jouer(VideoClip clip, System.Action onFini = null)
    {
        if (_cinematiqueEnCours)
        {
            Debug.LogWarning("[Cinematique] Une cinématique est déjà " +
                             "en cours — appel ignoré.");
            return;
        }

        if (_lecteur == null)
        {
            Debug.LogError("[Cinematique] VideoPlayer non assigné dans " +
                           "l'Inspector !");
            onFini?.Invoke();
            return;
        }

        StartCoroutine(SequenceCinematique(clip, onFini));
    }

    /// <summary>
    /// Interrompt la cinématique en cours et appelle le callback de fin.
    /// Utile pour un bouton "Passer" (skip).
    /// </summary>
    public void Passer()
    {
        if (!_cinematiqueEnCours) return;

        _lecteur.Stop();
        _lecteur.loopPointReached -= OnCinematiqueFinie;
        TerminerCinematique();
    }

    // ── Coroutine principale ──────────────────────────────────

    private IEnumerator SequenceCinematique(VideoClip clip, System.Action onFini)
    {
        _cinematiqueEnCours = true;
        _callbackFin = onFini;

        // 1. Fermer toutes les fenêtres UI et bloquer les inputs
        //    AVANT le délai pour éviter qu'elles restent visibles
        //    pendant la pause narrative.
        _inputs = FindFirstObjectByType<gestionInputsJeu>();
        _inputs?.FermerToutesLesFenetres();
        _inputs?.ModeCinematique(true);

        // 2. Arrêter la musique de fond
        if (gestionAudio.Instance != null)
            gestionAudio.Instance.ArreterMusique();

        // 3. Délai narratif optionnel avant la lecture
        if (_delaiAvantLecture > 0f)
            yield return new WaitForSecondsRealtime(_delaiAvantLecture);

        // 4. Lancer la vidéo
        Debug.Log($"[Cinematique] Lecture : '{clip.name}'");
        _lecteur.clip = clip;
        _lecteur.loopPointReached -= OnCinematiqueFinie; // anti-double-abonnement
        _lecteur.loopPointReached += OnCinematiqueFinie;
        _lecteur.Play();

        // La fin est gérée par le callback OnCinematiqueFinie
    }

    // ── Fin de cinématique ────────────────────────────────────

    private void OnCinematiqueFinie(VideoPlayer vp)
    {
        _lecteur.loopPointReached -= OnCinematiqueFinie;
        TerminerCinematique();
    }

    private void TerminerCinematique()
    {
        Debug.Log("[Cinematique] Cinématique terminée.");

        _cinematiqueEnCours = false;

        // Sortir du mode cinématique (réactive inputs et curseur)
        _inputs?.ModeCinematique(false);
        _inputs = null;

        // Reprendre la musique de fond
        if (gestionAudio.Instance != null)
            gestionAudio.Instance.ReprendreMusique();

        // Appeler le callback (chargement de scène, déblocage, etc.)
        System.Action cb = _callbackFin;
        _callbackFin = null;
        cb?.Invoke();
    }
}
