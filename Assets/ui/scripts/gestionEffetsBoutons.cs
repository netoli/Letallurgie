using UnityEngine;
using UnityEngine.EventSystems;

public class gestionEffetsBoutons : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler
{
    [Header("Effets de particules")]
    [SerializeField] private ParticleSystem effetSurvol;
    [SerializeField] private ParticleSystem effetClic;

    [Header("Paramètres particules survol")]
    [SerializeField] private float intensiteSurvol = 50f;

    [Header("Paramètres particules clic")]
    [SerializeField] private float intensiteClic = 150f;
    [SerializeField] private int nombreParticulesBurst = 30;

    [Header("Grossissement au survol")]
    [SerializeField] private float echelleSurvol = 1.15f;
    [SerializeField] private float vitesseTransition = 8f;

    private Vector3 echelleOriginale;
    private bool estSurvole = false;

    void Start()
    {
        echelleOriginale = transform.localScale;
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
    }

    public void OnPointerExit(PointerEventData donneesEvenement)
    {
        estSurvole = false;

        if (effetSurvol != null)
            effetSurvol.Stop();
    }

    public void OnPointerDown(PointerEventData donneesEvenement)
    {
        if (effetClic != null)
        {
            var emission = effetClic.emission;
            emission.rateOverTime = intensiteClic;
            effetClic.Play();
            effetClic.Emit(nombreParticulesBurst);
        }
    }

    public void OnPointerUp(PointerEventData donneesEvenement)
    {
        if (effetClic != null)
            effetClic.Stop();
    }
}