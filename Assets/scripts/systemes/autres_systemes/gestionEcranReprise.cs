using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;


public class gestionEcranReprise : MonoBehaviour
{
    [Header("Elements UI")]
    [SerializeField] private TMP_Text texteCompteur;
    [SerializeField] private GameObject texteReprise;
    [SerializeField] private GameObject texteOops;
    [SerializeField] private ParticleSystem effetImplosion;
    [SerializeField] private CanvasGroup groupeCanvas;

    [Header("Parametres")]
    [SerializeField] private float dureeCompteur;
    [SerializeField] private float delaiAvantCompteur;
    [SerializeField] private float vitesseFadeIn;

    private bool estActif = false;

    [ContextMenu("Tester Erreur")]
    public void TesterErreur()
    {
        LancerErreur();
    }

    [ContextMenu("Tester Reprise")]
    public void TesterReprise()
    {
        LancerReprise();
    }

    public void LancerErreur()
    {
        if (estActif) return;
        ActiverSoiEtAncetres();
        StartCoroutine(SequenceErreur());
    }

    public void LancerReprise()
    {
        if (estActif) return;
        ActiverSoiEtAncetres();
        StartCoroutine(SequenceReprise());
    }

    // Active ce GameObject ET tous ses ancetres inactifs AVANT de
    // demarrer la coroutine. CRITIQUE : StartCoroutine echoue quand le
    // GameObject est inactif — or le canvas reprise est desactive par
    // defaut (c'est un popup). C'est la cause du bug "le canvas ne
    // s'affiche ni en echec ni en reussite" : LancerErreur/LancerReprise
    // ne demarraient jamais leur coroutine. De plus, LancerReprise est
    // branche sur l'event onVictoire ; l'exception levee par
    // StartCoroutine sur GameObject inactif coupait la chaine des
    // listeners onVictoire, empechant le son de reussite (joue par
    // sonsEnigmeTuyauterie, abonne au meme event) de se declencher.
    // Activer le GameObject avant regle les deux symptomes d'un coup.
    private void ActiverSoiEtAncetres()
    {
        Transform t = transform;
        while (t != null)
        {
            if (!t.gameObject.activeSelf) t.gameObject.SetActive(true);
            t = t.parent;
        }
    }

    private IEnumerator SequenceErreur()
    {
        estActif = true;

        groupeCanvas.alpha = 0f;
        gameObject.SetActive(true);

        float elapsed = 0f;
        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * vitesseFadeIn;
            groupeCanvas.alpha = Mathf.Lerp(0f, 1f, elapsed);
            yield return null;
        }
        groupeCanvas.alpha = 1f;

        texteOops.SetActive(true);
        texteCompteur.gameObject.SetActive(false);
        texteReprise.SetActive(false);

        if (effetImplosion != null)
            effetImplosion.Play();

        yield return new WaitForSeconds(delaiAvantCompteur);

        texteOops.SetActive(false);
        texteReprise.SetActive(true);
        texteCompteur.gameObject.SetActive(true);

        yield return StartCoroutine(Compteur());

        estActif = false;
        gameObject.SetActive(false);
    }

    private IEnumerator SequenceReprise()
    {
        estActif = true;

        groupeCanvas.alpha = 0f;
        gameObject.SetActive(true);

        float elapsed = 0f;
        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * vitesseFadeIn;
            groupeCanvas.alpha = Mathf.Lerp(0f, 1f, elapsed);
            yield return null;
        }
        groupeCanvas.alpha = 1f;

        texteOops.SetActive(false);
        texteReprise.SetActive(true);
        texteCompteur.gameObject.SetActive(true);

        if (effetImplosion != null)
            effetImplosion.Play();

        yield return StartCoroutine(Compteur());

        estActif = false;
        gameObject.SetActive(false);
    }

    private IEnumerator Compteur()
    {
        float tempsRestant = dureeCompteur;
        int dernierSeconde = -1;

        while (tempsRestant > 0)
        {
            tempsRestant -= Time.deltaTime;
            int secondes = Mathf.CeilToInt(tempsRestant);

            if (secondes != dernierSeconde)
            {
                dernierSeconde = secondes;
                texteCompteur.text = secondes.ToString();

                if (effetImplosion != null)
                    effetImplosion.Play();
            }

            float fraction = tempsRestant - Mathf.Floor(tempsRestant);
            float scale = 1f + (1f - fraction) * 0.15f;
            texteCompteur.transform.localScale = Vector3.one * scale;

            yield return null;
        }

        texteCompteur.text = "0";
    }
}