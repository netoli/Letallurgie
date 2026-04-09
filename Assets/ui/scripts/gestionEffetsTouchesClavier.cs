using UnityEngine;
using UnityEngine.InputSystem;

public class gestionEffetsTouchesClavier : MonoBehaviour
{
    [Header("Touche associee")]
    [SerializeField] private Key touche;

    [Header("Effets")]
    [SerializeField] private ParticleSystem effetSurvol;
    [SerializeField] private ParticleSystem effetClic;
    [SerializeField] private float intensiteClic;
    [SerializeField] private int nombreParticulesBurst;

    [Header("Grossissement")]
    [SerializeField] private float echelleClic;
    [SerializeField] private float vitesseRetour;

    [Header("Sons")]
    [SerializeField] private AudioClip sonTouche;
    [SerializeField] private float volumeTouche;

    private Vector3 echelleOriginale;
    private CanvasGroup monCanvasGroup;
    private AudioSource sourceAudio;

    void Start()
    {
        echelleOriginale = transform.localScale;
        TrouverCanvasGroup();
        ConfigurerAudio();
        ConfigurerParticules();
    }

    void OnEnable()
    {
        if (echelleOriginale == Vector3.zero)
            echelleOriginale = transform.localScale;
        TrouverCanvasGroup();
        ConfigurerParticules();
    }

    private void ConfigurerAudio()
    {
        sourceAudio = GetComponent<AudioSource>();
        if (sourceAudio == null)
        {
            sourceAudio = gameObject.AddComponent<AudioSource>();
            sourceAudio.playOnAwake = false;
            sourceAudio.spatialBlend = 0f;
        }
    }

    private void TrouverCanvasGroup()
    {
        Transform parent = transform.parent;
        while (parent != null)
        {
            CanvasGroup groupe = parent.GetComponent<CanvasGroup>();
            if (groupe != null)
            {
                monCanvasGroup = groupe;
                return;
            }
            parent = parent.parent;
        }
    }

    private void ConfigurerParticules()
    {
        if (effetSurvol != null)
        {
            var main = effetSurvol.main;
            main.useUnscaledTime = true;
        }
        if (effetClic != null)
        {
            var main = effetClic.main;
            main.useUnscaledTime = true;
        }
    }

    private float ObtenirVolumeBouton()
    {
        gestionOptionsAudio options =
            FindFirstObjectByType<gestionOptionsAudio>();

        if (options != null)
            return options.ObtenirVolumeBouton();

        return 1f;
    }

    void Update()
    {
        transform.localScale = Vector3.Lerp(
            transform.localScale, echelleOriginale,
            Time.unscaledDeltaTime * vitesseRetour);

        if (Keyboard.current == null) return;

        if (Keyboard.current[touche].wasPressedThisFrame)
        {
            if (monCanvasGroup != null
                && (!monCanvasGroup.interactable
                    || monCanvasGroup.alpha < 0.1f))
                return;

            transform.localScale = echelleOriginale * echelleClic;

            if (effetClic != null)
            {
                var emission = effetClic.emission;
                emission.rateOverTime = intensiteClic;
                effetClic.Play();
                effetClic.Emit(nombreParticulesBurst);
            }

            float volume = ObtenirVolumeBouton();
            if (sonTouche != null && sourceAudio != null && volume > 0f)
                sourceAudio.PlayOneShot(sonTouche, volumeTouche * volume);
        }
    }
}