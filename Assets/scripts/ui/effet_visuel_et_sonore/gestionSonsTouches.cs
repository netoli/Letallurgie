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

    private void JouerSon()
    {
        if (sonTouche == null || sourceAudio == null) return;

        gestionOptionsAudio options =
            FindFirstObjectByType<gestionOptionsAudio>();

        float vol = volume;
        if (options != null)
            vol *= options.ObtenirVolumeBouton();

        if (vol > 0f)
            sourceAudio.PlayOneShot(sonTouche, vol);
    }
}