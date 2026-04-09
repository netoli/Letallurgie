using UnityEngine;
using UnityEngine.EventSystems;

public class gestionEffetsBoutonsCliques : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler
{
    [Header("Effets de particules")]
    [SerializeField] private ParticleSystem effetSurvol;
    [SerializeField] private ParticleSystem effetClic;

    [Header("Parametres particules survol")]
    [SerializeField] private float intensiteSurvol;

    [Header("Parametres particules clic")]
    [SerializeField] private float intensiteClic;
    [SerializeField] private int nombreParticulesBurst;

    [Header("Grossissement au survol")]
    [SerializeField] private float echelleSurvol;
    [SerializeField] private float vitesseTransition;

    [Header("Sons")]
    [SerializeField] private AudioClip sonSurvol;
    [SerializeField] private AudioClip sonClic;
    [SerializeField] private float volumeSurvol;
    [SerializeField] private float volumeClic;

    private Vector3 echelleOriginale;
    private bool estSurvole = false;
    private AudioSource sourceAudio;

    void Start()
    {
        echelleOriginale = transform.localScale;
        ConfigurerAudio();
        ConfigurerParticules();
    }

    void OnEnable()
    {
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
        Vector3 echelleCible = estSurvole ?
            echelleOriginale * echelleSurvol : echelleOriginale;
        transform.localScale = Vector3.Lerp(
            transform.localScale, echelleCible,
            Time.unscaledDeltaTime * vitesseTransition);
    }

    public void OnPointerEnter(PointerEventData donneesEvenement)
    {
        estSurvole = true;

        if (effetSurvol != null)
        {
            var emission = effetSurvol.emission;
            emission.rateOverTime = intensiteSurvol;
            effetSurvol.Play();
        }

        float volume = ObtenirVolumeBouton();
        if (sonSurvol != null && sourceAudio != null && volume > 0f)
            sourceAudio.PlayOneShot(sonSurvol, volumeSurvol * volume);
    }

    public void OnPointerExit(PointerEventData donneesEvenement)
    {
        estSurvole = false;
        if (effetSurvol != null)
            effetSurvol.Stop();
    }

    public void OnPointerDown(PointerEventData donneesEvenement)
    {
        LancerEffetClic();
    }

    public void OnPointerUp(PointerEventData donneesEvenement)
    {
        ArreterEffetClic();
    }

    public void LancerEffetClic()
    {
        if (effetClic != null)
        {
            var emission = effetClic.emission;
            emission.rateOverTime = intensiteClic;
            effetClic.Play();
            effetClic.Emit(nombreParticulesBurst);
        }

        float volume = ObtenirVolumeBouton();
        if (sonClic != null && sourceAudio != null && volume > 0f)
            sourceAudio.PlayOneShot(sonClic, volumeClic * volume);
    }

    public void ArreterEffetClic()
    {
        if (effetClic != null)
            effetClic.Stop();
    }
}