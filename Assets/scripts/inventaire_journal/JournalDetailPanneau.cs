// ============================================================
// JournalDetailPanneau.cs
// ------------------------------------------------------------
// Auteur      : Fanny Fortier
// Date        : 28/03/2026
// ------------------------------------------------------------
// Description :
//   G�re le panneau de d�tail gauche du journal. Attach� sur
//   indices_agrandis. Affiche ou cache le panneau (et son effet
//   sonore, fade in fade out) selon les interactions du slot s�lectionn�. 
// ------------------------------------------------------------
// D�pendances :
//   - JournalSlotUI.cs : appelle OuvrirPanneauDetail() et FermerPanneauDetail()
// ============================================================
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;


public class JournalDetailPanneau : MonoBehaviour
{
    public static JournalDetailPanneau Instance { get; private set; }

    [Header("Elements du UI - Details")]
    public Image imageIndice;
    public TMP_Text textTitre;
    public TMP_Text textDescription;
    public TMP_Text textInsight;

    [Header("Audio")]
    AudioSource sourceAudio;
    [SerializeField] AudioClip sonBrouillard;
    [SerializeField] float dureeFade;
    [SerializeField] float volumeMax;

    Coroutine fadeActif;

    private void Awake()
    {
        Instance = this;
        sourceAudio = GetComponent<AudioSource>();
        gameObject.SetActive(false);
    }

    public void OuvrirPanneauDetail(Sprite img, string titre,
    string description, string insight)
    {
        if (imageIndice != null) imageIndice.sprite = img;
        if (textTitre != null) textTitre.text = titre;
        if (textDescription != null) textDescription.text = description;
        if (textInsight != null) textInsight.text = insight;

        gameObject.SetActive(true);

        Debug.Log("sourceAudio : " + sourceAudio);
        Debug.Log("sonBrouillard : " + sonBrouillard);

        if (sourceAudio != null && sonBrouillard != null)
        {
            Debug.Log("Démarrage son");
            if (!sourceAudio.isPlaying)
            {
                sourceAudio.clip = sonBrouillard;
                sourceAudio.loop = true;
                sourceAudio.volume = 0f;
                sourceAudio.Play();
            }

            if (fadeActif != null) StopCoroutine(fadeActif);
            fadeActif = StartCoroutine(FadeVolume(volumeMax));
        }
        else
        {
            Debug.LogWarning("sourceAudio ou sonBrouillard est null !");
        }
    }

    public void FermerPanneauDetail()
    {
        if (sourceAudio != null && sourceAudio.isPlaying)
        {
            if (fadeActif != null) StopCoroutine(fadeActif);
            fadeActif = StartCoroutine(FadeVolume(0f, arretSon: true));
        }

        gameObject.SetActive(false);
    }

    IEnumerator FadeVolume(float cible, bool arretSon = false)
    {
        float initial = sourceAudio.volume;
        float timer = 0f;

        while (timer < dureeFade)
        {
            timer += Time.unscaledDeltaTime;
            sourceAudio.volume = Mathf.Lerp(initial, cible, timer / dureeFade);
            yield return null;
        }

        sourceAudio.volume = cible;
        if (arretSon && cible == 0f) sourceAudio.Stop();
        fadeActif = null;
    }
}