// ============================================================
// AudioManager.cs
// ------------------------------------------------------------
// Projet      : Létallurgie
// Auteur      : Fanny Fortier
// Date        : 09/04/2026
// ------------------------------------------------------------
// Description :
//   Gère la musique de fond du jeu et transitionne de manière fluude
//   Attaché à audio_manager (objet vide dans la scène).
// ------------------------------------------------------------
// Dépendances :
//   - gestionsTransitions.cs : appelle ChangerMusique() lors
//     des transitions de scène/état
// ============================================================
using UnityEngine;
using System.Collections;

public class GestionAudio : MonoBehaviour
{
    public static GestionAudio Instance;

    [Header("Audio Source")]
    [SerializeField] private AudioSource sourceMusique; 

    [Header("Musiques")]
    [SerializeField] private AudioClip musiqueIntro; 
    [SerializeField] private AudioClip musiqueTaverne; 
    [SerializeField] private AudioClip musiqueUsine;

    [Header("Paramètres")]
    [SerializeField] private float dureeTransition = 1.5f;

    // Débug : évite d'avoir 2 transitions en même temps
    private Coroutine _transitionEnCours;

    private void Awake()
    {
        // Le script survit aux changements de scène et débug : éviter d'avoir plusieurs instances du script
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject); 
    }

    private void Start()
    {
        // Musique du menu principal joue au démarrage
        JouerMusiqueIntro();
    }

    public void JouerMusiqueIntro() 
        // Pour quand on retourne vers le menu principal
    {
        TransitionnerVers(musiqueIntro);
    }

    public void JouerMusiqueTaverne()
    {
        // Appelée par gestionsTransitions
        TransitionnerVers(musiqueTaverne);
    }

    public void JouerMusiqueUsine() 
    {
        // Appelée au début de la scène Usine (par gestionTransitions)
        TransitionnerVers(musiqueUsine);
    }

    // Gestion du fondu entre les chansons
    private void TransitionnerVers(AudioClip nouvelleMusique)
    {
        // Débug pour me permettre que ce script existe avant que Nacer nous fasse la musique :'(
        if (nouvelleMusique == null) return;
        // Si on joue déja cette musique la en ce moment, on annule
        if (sourceMusique.clip == nouvelleMusique) return;

        if (_transitionEnCours != null)
            // Annuler la transition en cours avant d'en commencer une autre
            StopCoroutine(_transitionEnCours);

        _transitionEnCours = StartCoroutine(FondreVers(nouvelleMusique));
    }

    // Coroutine pour le fondu
    private IEnumerator FondreVers(AudioClip nouvelleMusique)
    {
        float volumeDepart = sourceMusique.volume; // Sauvegarde le volume actuel avant de commencer le fondu

        // SORTANT : on baisse le volume graduellement avec un Lerp
        float temps = 0f;
        while (temps < dureeTransition)
        {
            temps += Time.deltaTime;
            sourceMusique.volume = Mathf.Lerp(volumeDepart, 0f, temps / dureeTransition);
            yield return null;
        }

        // Quand le volume arrive a 0, on part la prochaine
        sourceMusique.clip = nouvelleMusique;
        sourceMusique.loop = true;
        sourceMusique.Play();

        // ENTRANT : on remonte le volume
        temps = 0f;
        while (temps < dureeTransition)
        {
            temps += Time.deltaTime;
            sourceMusique.volume = Mathf.Lerp(0f, volumeDepart, temps / dureeTransition);
            yield return null;
        }

        // Après le fondu, le volume revientà sa valeur initiale
        sourceMusique.volume = volumeDepart;

        // Encore du débug pour éviter de doubler mes transitions parce qu'on a jamais trop de débug
        _transitionEnCours = null;
    }
}