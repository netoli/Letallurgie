using UnityEngine;
using UnityEngine.UI;

public class gestionEffetsScroll : MonoBehaviour
{
    [Header("Son")]
    [SerializeField] private AudioClip sonScroll;
    [SerializeField] private float volumeScroll;
    [SerializeField] private float delaiEntreDeuxSons;

    [Header("Decoupe du son (en secondes)")]
    [SerializeField] private float debutSon;
    [SerializeField] private float finSon;

    private ScrollRect scrollRect;
    private AudioSource sourceAudio;
    private float dernierTempsJoue;
    private Vector2 dernierePosition;

    void Start()
    {
        scrollRect = GetComponent<ScrollRect>();

        sourceAudio = GetComponent<AudioSource>();
        if (sourceAudio == null)
        {
            sourceAudio = gameObject.AddComponent<AudioSource>();
            sourceAudio.playOnAwake = false;
            sourceAudio.spatialBlend = 0f;
        }

        sourceAudio.clip = sonScroll;
        sourceAudio.volume = volumeScroll;

        if (finSon <= 0f && sonScroll != null)
            finSon = sonScroll.length;

        if (scrollRect != null)
        {
            dernierePosition = scrollRect.normalizedPosition;
            scrollRect.onValueChanged.AddListener(OnScroll);
        }
    }

    private void OnScroll(Vector2 position)
    {
        float distance = Vector2.Distance(position, dernierePosition);

        if (distance > 0.01f
            && Time.unscaledTime - dernierTempsJoue > delaiEntreDeuxSons)
        {
            JouerSonDecoupe();
            dernierTempsJoue = Time.unscaledTime;
        }

        dernierePosition = position;
    }

    private void JouerSonDecoupe()
    {
        if (sonScroll == null || sourceAudio == null) return;

        sourceAudio.Stop();
        sourceAudio.clip = sonScroll;
        sourceAudio.time = debutSon;
        sourceAudio.Play();

        float duree = finSon - debutSon;
        StartCoroutine(ArreterApresDelai(duree));
    }

    private System.Collections.IEnumerator ArreterApresDelai(float delai)
    {
        yield return new WaitForSecondsRealtime(delai);
        sourceAudio.Stop();
    }

    void OnDestroy()
    {
        if (scrollRect != null)
            scrollRect.onValueChanged.RemoveListener(OnScroll);
    }
}