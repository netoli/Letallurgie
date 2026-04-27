using UnityEngine;
using UnityEngine.InputSystem;

public class gestionEffetsTouchesClavier : MonoBehaviour
{
    [Header("Touche associee")]
    [SerializeField] private Key touche;

    [Header("Effets")]
    [SerializeField] private ParticleSystem effetAppuie;
    [SerializeField] private int nombreParticulesBurst;

    [Header("Sons")]
    [SerializeField] private AudioClip sonTouche;
    [SerializeField] private float volumeTouche;

    private CanvasGroup monCanvasGroup;
    private AudioSource sourceAudio;

    void Start()
    {
        TrouverCanvasGroup();
        ConfigurerAudio();
        ConfigurerParticules();
    }

    void OnEnable()
    {
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
        if (effetAppuie != null)
        {
            var main = effetAppuie.main;
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
        if (Keyboard.current == null) return;
        if (!Keyboard.current[touche].wasPressedThisFrame) return;

        if (monCanvasGroup != null
            && (!monCanvasGroup.interactable
                || monCanvasGroup.alpha < 0.1f))
            return;

        gestionInputsJeu inputs =
            FindFirstObjectByType<gestionInputsJeu>();
        if (inputs != null && !inputs.PeutJouerEffets())
            return;

        if (effetAppuie != null)
        {
            effetAppuie.Play();
            effetAppuie.Emit(nombreParticulesBurst);
        }

        float volume = ObtenirVolumeBouton();
        if (sonTouche != null && sourceAudio != null && volume > 0f)
            sourceAudio.PlayOneShot(sonTouche, volumeTouche * volume);
    }
}