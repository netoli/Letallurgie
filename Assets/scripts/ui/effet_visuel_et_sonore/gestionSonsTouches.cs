using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class gestionSonsTouches : MonoBehaviour
{
    [Header("Son")]
    [SerializeField] private AudioClip sonTouche;
    [SerializeField] private float volume;

    [Header("Touches")]
    [SerializeField] private List<Key> touches;

    private AudioSource sourceAudio;

    void Start()
    {
        sourceAudio = GetComponent<AudioSource>();
        if (sourceAudio == null)
        {
            sourceAudio = gameObject.AddComponent<AudioSource>();
            sourceAudio.playOnAwake = false;
            sourceAudio.spatialBlend = 0f;
        }
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        foreach (Key touche in touches)
        {
            if (Keyboard.current[touche].wasPressedThisFrame)
            {
                JouerSon();
                break;
            }
        }
    }

    // Include les INACTIFS (le panneau audio est ferme la plupart du
    // temps; le find actif-seulement ignorait les toggles). Cache
    // statique : un seul find par scene.
    private static gestionOptionsAudio optionsAudioCache;

    private void JouerSon()
    {
        if (sonTouche == null || sourceAudio == null) return;

        if (optionsAudioCache == null)
            optionsAudioCache =
                FindFirstObjectByType<gestionOptionsAudio>(
                    FindObjectsInactive.Include);

        float vol = volume;
        if (optionsAudioCache != null)
            vol *= optionsAudioCache.ObtenirVolumeBouton();

        if (vol > 0f)
            sourceAudio.PlayOneShot(sonTouche, vol);
    }
}