// ============================================================
// gestionAudio.cs
// ------------------------------------------------------------
// Auteur      : Fanny Fortier
// Date créé   : 07/04/2026
// Modifié par Olivier V. le 13/04/2026
// ------------------------------------------------------------
// Description :
//   Gère la musique d'ambiance et les effets sonores du jeu. Permet
//   de jouer des musiques spécifiques pour différentes scènes et
//   de jouer des effets sonores ponctuels. JouerSFX est appelé dans RamasserIndice
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

    [Header("Audio Sources")]
    [SerializeField] private AudioSource sourceMusique;
    [SerializeField] private AudioSource sourceSFX;

    [Header("Musiques")]
    [SerializeField] private AudioClip[] musiquesIntro;
    [SerializeField] private AudioClip[] musiquesTutoriel;
    [SerializeField] private AudioClip[] musiquesTaverne;
    [SerializeField] private AudioClip[] musiquesUsine;
    [SerializeField] private AudioClip[] musiquesManoir;

    [Header("Parametres")]
    [SerializeField] private float dureeTransition;
    [SerializeField][Range(0f, 1f)] private float volumeMax;

    private Coroutine transitionEnCours;
    private AudioClip[] playlistActuelle;
    private List<int> indexRestants = new List<int>();
    private float volumeCible;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        volumeCible = ObtenirVolumeMusique();
        sourceMusique.volume = volumeCible;

        if (SceneManager.GetActiveScene().name == "SCENE0-Menu-Tuto")
        {
            JouerMusiquesIntro();
        } else if (SceneManager.GetActiveScene().name == "SCENE1-Taverne1" || SceneManager.GetActiveScene().name == "SCENE3-Taverne2")
        {
            JouerMusiquesTaverne();
        } else if (SceneManager.GetActiveScene().name == "SCENE2-Usine")
        {
            JouerMusiquesUsine();
        } else if (SceneManager.GetActiveScene().name == "SCENE4-Manoir")
        {
            JouerMusiquesManoir();
        }


    }

    private float ObtenirVolumeMusique()
    {
        gestionOptionsAudio options =
            FindFirstObjectByType<gestionOptionsAudio>();
        float slider = 1f;
        if (options != null)
            slider = options.ObtenirVolumeMusiqueAmbiance();
        else
            slider = PlayerPrefs.GetFloat("musiqueAmbiance", 1f);

        return slider * volumeMax;
    }

    public void MettreAJourVolume()
    {
        volumeCible = ObtenirVolumeMusique();
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

    private void ChangerPlaylist(AudioClip[] nouvellePlaylist)
    {
        if (nouvellePlaylist == null
            || nouvellePlaylist.Length == 0) return;
        if (nouvellePlaylist == playlistActuelle) return;

        playlistActuelle = nouvellePlaylist;
        MelangerPlaylist();

        if (transitionEnCours != null)
            StopCoroutine(transitionEnCours);

        volumeCible = ObtenirVolumeMusique();
        transitionEnCours = StartCoroutine(
            FondreVers(ProchainClip()));
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

    private AudioClip ProchainClip()
    {
        if (indexRestants.Count == 0)
            MelangerPlaylist();

        int index = indexRestants[0];
        indexRestants.RemoveAt(0);
        return playlistActuelle[index];
    }

    private IEnumerator FondreVers(AudioClip nouvelleMusique)
    {
        if (nouvelleMusique == null) yield break;

        float volumeDepart = sourceMusique.volume;

        float temps = 0f;
        while (temps < dureeTransition)
        {
            temps += Time.unscaledDeltaTime;
            sourceMusique.volume = Mathf.Lerp(
                volumeDepart, 0f, temps / dureeTransition);
            yield return null;
        }

        sourceMusique.volume = 0f;
        sourceMusique.clip = nouvelleMusique;
        sourceMusique.loop = false;
        sourceMusique.Play();

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

        float attente = nouvelleMusique.length - dureeTransition;
        float tempsEcoule = 0f;
        while (tempsEcoule < attente)
        {
            tempsEcoule += Time.unscaledDeltaTime;
            yield return null;
        }

        transitionEnCours = StartCoroutine(
            FondreVers(ProchainClip()));
    }

    // Jouer les effets sonores d'interaction objet
    public void JouerSFX(AudioClip clip)
    {
        if (clip != null && sourceSFX != null)
            sourceSFX.PlayOneShot(clip);
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