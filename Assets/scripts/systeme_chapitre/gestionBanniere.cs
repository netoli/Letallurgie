using System.Collections;
using TMPro;
using UnityEngine;

public class gestionBanniere : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text texteNomChapitre;
    [SerializeField] private CanvasGroup groupeBanniere;
    [SerializeField] private Animator animatorBanniere;
    [SerializeField] private AudioSource audioSource;

    [Header("Parametres animation")]
    [SerializeField] private string triggerApparition;
    [SerializeField] private string triggerDisparition;
    [SerializeField] private float dureeFadeFinal;

    void Awake()
    {
        if (groupeBanniere != null)
            groupeBanniere.alpha = 0f;
    }

    public IEnumerator AfficherBanniere(
        string nomChapitre, float duree)
    {
        Debug.Log("[Banniere] AfficherBanniere: " + nomChapitre);

        gameObject.SetActive(true);

        if (texteNomChapitre != null)
            texteNomChapitre.text = nomChapitre;

        if (animatorBanniere != null
            && !string.IsNullOrEmpty(triggerApparition))
            animatorBanniere.SetTrigger(triggerApparition);

        if (groupeBanniere != null)
            groupeBanniere.alpha = 1f;

        // Joue le son de la banniere
        if (audioSource != null && audioSource.clip != null)
            audioSource.Play();
        Debug.Log("[Banniere] Attente "
            + Mathf.Max(0.1f, duree - dureeFadeFinal) + "s");

        yield return new WaitForSecondsRealtime(
            Mathf.Max(0.1f, duree - dureeFadeFinal));

        Debug.Log("[Banniere] Debut fade out");

        if (animatorBanniere != null
            && !string.IsNullOrEmpty(triggerDisparition))
            animatorBanniere.SetTrigger(triggerDisparition);

        float t = 0f;
        while (t < dureeFadeFinal)
        {
            t += Time.unscaledDeltaTime;
            if (groupeBanniere != null)
                groupeBanniere.alpha = Mathf.Lerp(1f, 0f,
                    t / dureeFadeFinal);
            yield return null;
        }

        Debug.Log("[Banniere] Fade out termine");

        if (groupeBanniere != null)
            groupeBanniere.alpha = 0f;

        gameObject.SetActive(false);
    }
}