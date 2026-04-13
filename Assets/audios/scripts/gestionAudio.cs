// ============================================================
// AudioManager.cs
// ------------------------------------------------------------
// Projet      : Létallurgie
// Auteur      : Fanny Fortier
// Date        : 09/04/2026
// ------------------------------------------------------------
// Description :
//   Gère la musique de fond du jeu et transitionne de manière fluide.
//   Attaché à audio_manager (objet vide dans la scène).
// ------------------------------------------------------------
// Dépendances :
//   - gestionsTransitions.cs : appelle les fonctions JouerMusique...()
// ============================================================
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GestionAudio : MonoBehaviour
{
    public static GestionAudio Instance;

    [Header("Audio Source")]
    [SerializeField] private AudioSource sourceMusique;

    [Header("Musiques")]
    [SerializeField] private AudioClip[] musiquesIntro;
    [SerializeField] private AudioClip[] musiquesTutoriel;
    [SerializeField] private AudioClip[] musiquesTaverne;
    [SerializeField] private AudioClip[] musiquesUsine;

    [Header("Paramètres")]
    [SerializeField] private float dureeTransition = 1.5f;

    private Coroutine transitionEnCours;
    private AudioClip[] playlistActuelle;
    // Liste d'index pour éviter de répéter des chansons avant d'avoir joué les autres
    private List<int> indexRestants = new List<int>();

    private void Awake()
    {
        // Debug : Si une autre instance de GestionAudio existe déjà, détruis le nouvel objet
        if (Instance != null && Instance != this) 
        { 
            Destroy(gameObject); 
            return; 
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Par défaut on joue la musique intro
        JouerMusiquesIntro();
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

    // Change la playlist en cours et transitionne
    private void ChangerPlaylist(AudioClip[] nouvellePlaylist)
    {
        // Si la playlist est null ou vide on cancel 
        if (nouvellePlaylist == null || nouvellePlaylist.Length == 0) return;
        // Si c'est déja cette playlist la qui joue, on cancel
        if (nouvellePlaylist == playlistActuelle) return;

        playlistActuelle = nouvellePlaylist;
        // Ordre aléatoire
        MelangerPlaylist(); 
        // S'il y a déja une transition en cours, on l'arrête pour en commencer une autre
        if (transitionEnCours != null)
            StopCoroutine(transitionEnCours);
        // Tansition vers la première chanson de la nouvelle playlist
        transitionEnCours = StartCoroutine(FondreVers(ProchainClip()));
    }

    private void MelangerPlaylist()
    {
        // On reset la liste d'index restants
        indexRestants.Clear();
        // Tant que la playlist est pas vide, on ajoute des index
        for (int i = 0; i < playlistActuelle.Length; i++)
            indexRestants.Add(i); 

        // algorithme fisher-yates pour mélanger les index
        for (int i = indexRestants.Count - 1; i > 0; i--)
        {
            // Choisir un index aléatoire entre 0 et 1
            int j = Random.Range(0, i + 1);
            // Switcher les éléments i et j
            int temp = indexRestants[i];
            indexRestants[i] = indexRestants[j];
            indexRestants[j] = temp;
        }
    }

    private AudioClip ProchainClip()
    {
        // Quand toute la playlist a joué, on mélange a nouveau
        if (indexRestants.Count == 0)
            MelangerPlaylist();

        // Saisir le premier index mélangé
        int index = indexRestants[0];
        // Enlever l'index de la liste pour ne pas le répéter
        indexRestants.RemoveAt(0);
        // Retourner le clip qui correspond à cet index
        return playlistActuelle[index];
    }

    private IEnumerator FondreVers(AudioClip nouvelleMusique)
    {
        // Si on a pas de musique à jouer, on arrête la transition
        if (nouvelleMusique == null) yield break;

        // Volume de départ avant le lerp
        float volumeDepart = sourceMusique.volume;

        // FADE OUT
        float temps = 0f;
        // Lerp dure le temps de dureeTransition
        while (temps < dureeTransition)
        {
            temps += Time.deltaTime;
            sourceMusique.volume = Mathf.Lerp(volumeDepart, 0f, temps / dureeTransition);
            yield return null;
        }

        sourceMusique.clip = nouvelleMusique;
        sourceMusique.loop = false;
        sourceMusique.Play();

        // FADE IN
        temps = 0f;
        while (temps < dureeTransition)
        {
            temps += Time.deltaTime;
            sourceMusique.volume = Mathf.Lerp(0f, volumeDepart, temps / dureeTransition);
            yield return null;
        }

        sourceMusique.volume = volumeDepart;
        transitionEnCours = null;

        // Attendre la fin de la musique avant de faire la transition
        yield return new WaitForSeconds(nouvelleMusique.length - dureeTransition);
        transitionEnCours = StartCoroutine(FondreVers(ProchainClip()));
    }
}