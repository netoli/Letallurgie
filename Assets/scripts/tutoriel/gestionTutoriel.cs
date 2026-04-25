using System;
using System.Collections;
using System.Collections.Generic;
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

    [Header("Effets")]
    [SerializeField] private GameObject fxBrouillard;

    [Header("Parametres")]
    [SerializeField] private float vitesseFade;

    public DonneesTutoriel TutoActuel => tutoActuel;
    public event Action<DonneesTutoriel> OnTutoAffiche;
    public event Action<DonneesTutoriel> OnTutoFerme;

    private DonneesTutoriel tutoActuel;
    private float tempsAffichage;
    private Coroutine coroutineMaximum;
    private Coroutine coroutineFade;

    void Awake()
    {
        if (groupeTuile != null)
        {
            groupeTuile.alpha = 0f;
            groupeTuile.interactable = false;
            groupeTuile.blocksRaycasts = false;
        }

        DesactiverBrouillardSecurise();
    }

    // -------- AFFICHAGE GÉNÉRAL --------
    public void AfficherTuto(DonneesTutoriel tuto)
    {
        if (tuto == null) return;

        Debug.Log("[Tutoriel] AfficherTuto recu: " + tuto.titre);

        tutoActuel = tuto;
        tempsAffichage = 0f;

        if (texteTitre != null) texteTitre.text = tuto.titre;
        if (texteExplication != null) texteExplication.text = tuto.explication;

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

        if (groupeTuile != null)
        {
            groupeTuile.alpha = 0f;
            groupeTuile.interactable = true;
            groupeTuile.blocksRaycasts = true;
        }

        DesactiverBrouillardSecurise();

        if (coroutineFade != null)
            StopCoroutine(coroutineFade);
        coroutineFade = StartCoroutine(FadeInPuisBrouillard());

        if (coroutineMaximum != null)
            StopCoroutine(coroutineMaximum);

        // Lancer seulement si dureeMaximum > 0
        if (tuto.dureeMaximum > 0f)
            coroutineMaximum = StartCoroutine(FermerApresDureeMaximum(tuto.dureeMaximum));
        else
            coroutineMaximum = null;

        // Notifier les abonnés (gestionChapitres pourra activer le detecteur correspondant)
        OnTutoAffiche?.Invoke(tutoActuel);
    }

    public void FermerTuto()
    {
        if (tutoActuel == null) return;

        Debug.Log("[Tutoriel] FermerTuto");

        if (coroutineMaximum != null)
            StopCoroutine(coroutineMaximum);

        // Notifier avant de nuller
        OnTutoFerme?.Invoke(tutoActuel);

        tutoActuel = null;

        DesactiverBrouillardSecurise();

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

    // Brouillard
    private void DesactiverBrouillardSecurise()
    {
        if (fxBrouillard == null) return;
        try { fxBrouillard.SetActive(false); }
        catch (MissingReferenceException) { fxBrouillard = null; }
    }

    private void ActiverBrouillardSecurise()
    {
        if (fxBrouillard == null) return;
        try { fxBrouillard.SetActive(true); }
        catch (MissingReferenceException) { fxBrouillard = null; }
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

        ActiverBrouillardSecurise();
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

        if (groupeTuile != null)
        {
            groupeTuile.interactable = false;
            groupeTuile.blocksRaycasts = false;
        }
    }

    // Timer de fermeture automatique après la durée maximum
    private IEnumerator FermerApresDureeMaximum(float duree)
    {
        Debug.Log($"[Tutoriel] FermerApresDureeMaximum attendu: {duree} s");
        yield return new WaitForSeconds(duree);
        if (tutoActuel != null)
            FermerTuto();
    }
}
