using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class gestionTutoriel : MonoBehaviour
{
    [Header("References UI")]
    [SerializeField] private TMP_Text texteTitre;
    [SerializeField] private TMP_Text texteExplication;
    [SerializeField] private Image imageTutoriel;
    [SerializeField] private CanvasGroup groupeTuile;
    [SerializeField] private GameObject ensembleTuile;

    [Header("Effets")]
    [SerializeField] private GameObject fxBrouillard;

    [Header("Parametres")]
    [SerializeField] private float vitesseFade;

    private DonneesTutoriel tutoActuel;
    private float tempsAffichage;
    private Coroutine coroutineMaximum;
    private Coroutine coroutineFade;

    void Awake()
    {
        if (groupeTuile != null)
            groupeTuile.alpha = 0f;
    }

    public void AfficherTuto(DonneesTutoriel tuto)
    {
        if (tuto == null) return;

        Debug.Log("[Tutoriel] AfficherTuto recu: " + tuto.titre);

        tutoActuel = tuto;
        tempsAffichage = 0f;

        if (texteTitre != null) texteTitre.text = tuto.titre;
        if (texteExplication != null)
            texteExplication.text = tuto.explication;

        if (imageTutoriel != null)
        {
            if (tuto.image != null)
            {
                imageTutoriel.sprite = tuto.image;
                imageTutoriel.gameObject.SetActive(true);
            }
            else
            {
                imageTutoriel.gameObject.SetActive(false);
            }
        }

        if (ensembleTuile != null)
            ensembleTuile.SetActive(true);

        if (groupeTuile != null)
            groupeTuile.alpha = 0f;

        if (coroutineFade != null)
            StopCoroutine(coroutineFade);
        coroutineFade = StartCoroutine(FadeInPuisBrouillard());

        if (coroutineMaximum != null)
            StopCoroutine(coroutineMaximum);
        coroutineMaximum = StartCoroutine(
            FermerApresDureeMaximum(tuto.dureeMaximum));
    }

    public void FermerTuto()
    {
        if (tutoActuel == null) return;

        Debug.Log("[Tutoriel] FermerTuto");

        if (coroutineMaximum != null)
            StopCoroutine(coroutineMaximum);

        tutoActuel = null;

        if (fxBrouillard != null)
            fxBrouillard.SetActive(false);

        if (coroutineFade != null)
            StopCoroutine(coroutineFade);
        coroutineFade = StartCoroutine(FadeOut());
    }

    public bool PeutEtreFermeParAction()
    {
        if (tutoActuel == null) return false;
        return tempsAffichage >= tutoActuel.dureeMinimum;
    }

    void Update()
    {
        if (tutoActuel != null)
            tempsAffichage += Time.deltaTime;
    }

    private IEnumerator FadeInPuisBrouillard()
    {
        if (groupeTuile == null) yield break;

        while (groupeTuile.alpha < 0.99f)
        {
            groupeTuile.alpha = Mathf.Lerp(groupeTuile.alpha, 1f,
                Time.deltaTime * vitesseFade);
            yield return null;
        }
        groupeTuile.alpha = 1f;

        // Active le brouillard maintenant - Play On Awake le jouera
        if (fxBrouillard != null)
            fxBrouillard.SetActive(true);
    }

    private IEnumerator FadeOut()
    {
        if (groupeTuile == null) yield break;

        while (groupeTuile.alpha > 0.01f)
        {
            groupeTuile.alpha = Mathf.Lerp(groupeTuile.alpha, 0f,
                Time.deltaTime * vitesseFade);
            yield return null;
        }
        groupeTuile.alpha = 0f;

        if (ensembleTuile != null)
            ensembleTuile.SetActive(false);
    }

    private IEnumerator FermerApresDureeMaximum(float duree)
    {
        yield return new WaitForSeconds(duree);
        FermerTuto();
    }
}