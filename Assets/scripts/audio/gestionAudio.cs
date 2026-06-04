// ============================================================
// gestionAudio.cs
// ------------------------------------------------------------
// Auteur      : Fanny Fortier
// Date créée  : 07/04/2026
// Modifié par Olivier V. le 13/04/2026
// Modifié par Olivier V. le 11/05/2026 : volume individuel par piste
// Modifié le 23/05/2026 : ajout ambiance sonore de pièce (whispers manoir)
// ------------------------------------------------------------
// Description :
//   Gère la musique d'ambiance, les effets sonores du jeu et
//   l'ambiance sonore de pièce (sons d'environnement en loop).
//   Permet de jouer des musiques spécifiques pour différentes scènes
//   et de jouer des effets sonores ponctuels. Chaque piste musicale
//   possède son propre multiplicateur de volume réglable dans
//   l'inspecteur pour créer de l'intensité sonore (ex.: menu tamisé,
//   musique de jeu plus forte). JouerSFX est appelé dans RamasserIndice.
// ------------------------------------------------------------
// Dépendances :
//   - gestionOptionsAudio.cs : pour obtenir les réglages de volume
//   - PlayerPrefs : pour sauvegarder et charger les réglages de volume
// ============================================================

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class gestionAudio : MonoBehaviour
{
    public static gestionAudio Instance;

    // Représente une piste musicale avec son propre volume relatif.
    // Le volume final = volumeSlider * volumeMax * piste.volume
    [System.Serializable]
    public class PisteMusique
    {
        public AudioClip clip;
        [Range(0f, 1f)]
        [Tooltip("Volume relatif de cette piste (multiplicateur). " +
                 "1 = volume normal, 0.3 = tamisé, 0 = muet.")]
        public float volume = 1f;
    }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource sourceMusique;
    [SerializeField] private AudioSource sourceSFX;
    [SerializeField] private AudioSource sourceAmbiance;

    [Header("Ambiance Sonore de Pièce")]
    [Tooltip("Sons d'environnement en loop, séparés de la musique. " +
             "Assigner une AudioSource dédiée avec Loop activé.")]
    [SerializeField] private AudioClip ambianceManoir;
    [Tooltip("Ambiance sonore de l'usine (machineries en arriere-plan). " +
             "Joue en loop pendant scene2_usine, par-dessus la musique.")]
    [SerializeField] private AudioClip ambianceUsine;
    [Range(0f, 1f)]
    [Tooltip("Volume de l'ambiance de pièce, indépendant de la musique.")]
    [SerializeField] private float volumeAmbiance = 0.35f;

    [Header("Musiques")]
    [SerializeField] private PisteMusique[] musiquesIntro;
    [SerializeField] private PisteMusique[] musiquesTutoriel;
    [SerializeField] private PisteMusique[] musiquesTaverne;
    [SerializeField] private PisteMusique[] musiquesUsine;
    [SerializeField] private PisteMusique[] musiquesManoir;

    [Header("Parametres")]
    [SerializeField] private float dureeTransition;
    [SerializeField][Range(0f, 1f)] private float volumeMax;

    private Coroutine transitionEnCours;
    private PisteMusique[] playlistActuelle;
    private PisteMusique pisteActuelle;
    private List<int> indexRestants = new List<int>();
    private float volumeCible;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.Log($"[gestionAudio] Doublon ('{name}'), destruction.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // FIX BUG : si sourceAmbiance et sourceMusique pointent vers la
        // MEME AudioSource (cas observe dans scene2_usine : 'audio_manager'
        // est rattache aux 2 champs Inspector), JouerMusiquesUsine() qui
        // change sourceMusique.clip ECRASE le clip d'ambiance. Resultat :
        // l'ambiance s'arrete des qu'une musique demarre. On detecte ce
        // cas et on cree une AudioSource dediee a l'ambiance, attachee
        // au meme GameObject, pour que les 2 sons coexistent.
        if (sourceAmbiance != null && sourceMusique != null
            && sourceAmbiance == sourceMusique)
        {
            Debug.LogWarning("[gestionAudio] sourceAmbiance == sourceMusique "
                + "(meme AudioSource). Creation d'une AudioSource dediee "
                + "pour l'ambiance pour eviter que la musique ecrase le "
                + "clip d'ambiance.");
            var nouvelleSource = sourceMusique.gameObject.AddComponent<AudioSource>();
            nouvelleSource.playOnAwake = false;
            nouvelleSource.spatialBlend = 0f;  // 2D
            nouvelleSource.loop = true;
            nouvelleSource.volume = volumeAmbiance;
            sourceAmbiance = nouvelleSource;
        }
    }

    void Start()
    {
        volumeCible = ObtenirVolumeBase();
        sourceMusique.volume = volumeCible;

        if (SceneManager.GetActiveScene().name == "scene0_tuto")
        {
            JouerMusiquesIntro();
        } else if (SceneManager.GetActiveScene().name == "scene1_taverne1" || SceneManager.GetActiveScene().name == "scene3_taverne2")
        {
            JouerMusiquesTaverne();
        } else if (SceneManager.GetActiveScene().name == "scene2_usine")
        {
            JouerMusiquesUsine();
            DemarrerAmbiancePiece(ambianceUsine);
        } else if (SceneManager.GetActiveScene().name == "scene4_manoir")
        {
            JouerMusiquesManoir();
            DemarrerAmbiancePiece(ambianceManoir);
        }
    }

    // Synchronise en continu le volume de l'AudioSource avec le volume
    // cible calculé (slider × volumeMax × piste.volume). Permet de
    // modifier le volume d'une piste en direct depuis l'inspecteur
    // ou via les sliders du menu d'options, sans attendre la
    // prochaine transition de piste.
    void Update()
    {
        // Ambiance de piece (machinerie usine / chuchotements manoir) :
        // pilotee par le MEME slider "musique d'ambiance" que la musique,
        // pour que tout le volume d'ambiance se regle au meme endroit.
        // volumeAmbiance sert de base/max; le slider (0..1) le module.
        if (sourceAmbiance != null && sourceAmbiance.isPlaying)
            sourceAmbiance.volume = volumeAmbiance * ObtenirSliderAmbiance();

        if (sourceMusique == null || pisteActuelle == null) return;

        volumeCible = CalculerVolumeCible(pisteActuelle);

        // Pendant un fondu, la coroutine FondreVers contrôle elle-même
        // sourceMusique.volume en lisant volumeCible — on ne touche pas
        // à la source pour ne pas casser le lerp.
        if (transitionEnCours == null)
            sourceMusique.volume = volumeCible;
    }

    // Volume de base partagé : slider utilisateur * volumeMax global.
    private float ObtenirVolumeBase()
    {
        // "Bande son" (renomme depuis "musique general", cle volumeGeneral)
        // controle UNIQUEMENT la musique des scenes. Avant, la musique
        // suivait "musiqueAmbiance"; cette cle pilote desormais l'ambiance
        // de piece (cf. ObtenirSliderAmbiance).
        float slider = PlayerPrefs.GetFloat("volumeGeneral", 1f);
        return slider * volumeMax;
    }

    // Valeur du slider "musique d'ambiance" (0..1). Module a la fois la
    // musique de fond ET l'ambiance de piece, pour que tout le volume
    // d'ambiance se regle au meme endroit (un seul slider).
    private float ObtenirSliderAmbiance()
    {
        return PlayerPrefs.GetFloat("musiqueAmbiance", 1f);
    }

    // Volume cible pour une piste spécifique (applique le multiplicateur de la piste).
    private float CalculerVolumeCible(PisteMusique piste)
    {
        float multiplicateur = (piste != null) ? piste.volume : 1f;
        return ObtenirVolumeBase() * multiplicateur;
    }

    public void MettreAJourVolume()
    {
        volumeCible = CalculerVolumeCible(pisteActuelle);
        if (transitionEnCours == null)
            sourceMusique.volume = volumeCible;
    }

    public void JouerMusiquesIntro()
    {
        ChangerPlaylist(musiquesIntro);
    }

    public void JouerMusiquesTutoriel()
    {
        ChangerPlaylist(musiquesTutoriel);
    }

    public void JouerMusiquesTaverne()
    {
        ChangerPlaylist(musiquesTaverne);
    }

    public void JouerMusiquesUsine()
    {
        ChangerPlaylist(musiquesUsine);
    }

    public void JouerMusiquesManoir()
    {
        ChangerPlaylist(musiquesManoir);
    }

    private void ChangerPlaylist(PisteMusique[] nouvellePlaylist)
    {
        if (nouvellePlaylist == null
            || nouvellePlaylist.Length == 0) return;
        if (nouvellePlaylist == playlistActuelle) return;

        playlistActuelle = nouvellePlaylist;
        MelangerPlaylist();

        if (transitionEnCours != null)
            StopCoroutine(transitionEnCours);

        transitionEnCours = StartCoroutine(
            FondreVers(ProchainePiste()));
    }

    private void MelangerPlaylist()
    {
        indexRestants.Clear();
        for (int i = 0; i < playlistActuelle.Length; i++)
            indexRestants.Add(i);

        for (int i = indexRestants.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int temp = indexRestants[i];
            indexRestants[i] = indexRestants[j];
            indexRestants[j] = temp;
        }
    }

    private PisteMusique ProchainePiste()
    {
        if (indexRestants.Count == 0)
            MelangerPlaylist();

        int index = indexRestants[0];
        indexRestants.RemoveAt(0);
        return playlistActuelle[index];
    }

    private IEnumerator FondreVers(PisteMusique nouvellePiste)
    {
        if (nouvellePiste == null || nouvellePiste.clip == null) yield break;

        float volumeDepart = sourceMusique.volume;

        // Fondu sortant de la piste précédente
        float temps = 0f;
        while (temps < dureeTransition)
        {
            temps += Time.unscaledDeltaTime;
            sourceMusique.volume = Mathf.Lerp(
                volumeDepart, 0f, temps / dureeTransition);
            yield return null;
        }

        sourceMusique.volume = 0f;
        sourceMusique.clip = nouvellePiste.clip;
        sourceMusique.loop = false;
        sourceMusique.Play();

        // Calculer le volume cible avec le multiplicateur de la nouvelle piste
        pisteActuelle = nouvellePiste;
        volumeCible = CalculerVolumeCible(pisteActuelle);

        // Fondu entrant vers le volume cible de cette piste
        temps = 0f;
        while (temps < dureeTransition)
        {
            temps += Time.unscaledDeltaTime;
            sourceMusique.volume = Mathf.Lerp(
                0f, volumeCible, temps / dureeTransition);
            yield return null;
        }

        sourceMusique.volume = volumeCible;
        transitionEnCours = null;

        float attente = nouvellePiste.clip.length - dureeTransition;
        float tempsEcoule = 0f;
        while (tempsEcoule < attente)
        {
            tempsEcoule += Time.unscaledDeltaTime;
            yield return null;
        }

        transitionEnCours = StartCoroutine(
            FondreVers(ProchainePiste()));
    }

    // Jouer les effets sonores d'interaction objet
    public void JouerSFX(AudioClip clip)
    {
        if (clip != null && sourceSFX != null)
            sourceSFX.PlayOneShot(clip);
    }

    // ── Ambiance sonore de pièce ────────────────────────────

    /// <summary>
    /// Démarre un son d'ambiance en loop pour la pièce actuelle.
    /// Remplace tout son d'ambiance déjà en cours.
    /// </summary>
    public void DemarrerAmbiancePiece(AudioClip clip)
    {
        if (sourceAmbiance == null || clip == null) return;

        sourceAmbiance.clip = clip;
        sourceAmbiance.loop = true;
        sourceAmbiance.volume = volumeAmbiance * ObtenirSliderAmbiance();
        sourceAmbiance.Play();
    }

    /// <summary>
    /// Arrête le son d'ambiance de pièce (ex.: entre les scènes).
    /// </summary>
    public void ArreterAmbiancePiece()
    {
        if (sourceAmbiance != null)
            sourceAmbiance.Stop();
    }

    // Mettre la musique sur pause (exemple: pour cinématiques)
    public void ArreterMusique()
    {
        if (sourceMusique != null)
            sourceMusique.Pause();
    }

    // Reprendre la musique après une pause
    public void ReprendreMusique()
    {
        if (sourceMusique != null && sourceMusique.clip != null)
            sourceMusique.UnPause();
    }
}
