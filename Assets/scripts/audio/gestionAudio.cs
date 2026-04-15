using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class gestionAudio : MonoBehaviour
{
    public static gestionAudio Instance;

    [Header("Audio Source")]
    [SerializeField] private AudioSource sourceMusique;

    [Header("Musiques")]
    [SerializeField] private AudioClip[] musiquesIntro;
    [SerializeField] private AudioClip[] musiquesTutoriel;
    [SerializeField] private AudioClip[] musiquesTaverne;
    [SerializeField] private AudioClip[] musiquesUsine;

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
    }

    void Start()
    {
        volumeCible = ObtenirVolumeMusique();
        sourceMusique.volume = volumeCible;
        JouerMusiquesIntro();
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
}