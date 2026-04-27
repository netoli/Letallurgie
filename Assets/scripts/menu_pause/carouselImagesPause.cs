using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class carouselImagesPause : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image imageAsset;
    [SerializeField] private Transform ensembleCercle;

    [Header("Banque d images")]
    [SerializeField] private Sprite[] banqueImages;

    [Header("Parametres")]
    [SerializeField] private float delaiChangement;
    [SerializeField] private float echelleCercleActif;
    [SerializeField] private float echelleCercleInactif;
    [SerializeField] private float vitesseTransitionCercle;
    [SerializeField] private float vitesseFadeImage;

    private Sprite[] imagesSelectionnees;
    private RectTransform[] cercles;
    private int indexActuel;
    private Coroutine coroutineCarousel;

    void OnEnable()
    {
        InitialiserCercles();
        SelectionnerImagesAleatoires();
        indexActuel = 0;
        AfficherImage(indexActuel, false);
        coroutineCarousel = StartCoroutine(BoucleCarousel());
    }

    void OnDisable()
    {
        if (coroutineCarousel != null)
            StopCoroutine(coroutineCarousel);
    }

    private void InitialiserCercles()
    {
        int nombreCercles = ensembleCercle.childCount;
        cercles = new RectTransform[nombreCercles];

        for (int i = 0; i < nombreCercles; i++)
        {
            cercles[i] = ensembleCercle.GetChild(i)
                .GetComponent<RectTransform>();
        }
    }

    private void SelectionnerImagesAleatoires()
    {
        int nombreASelectionner = cercles.Length;

        if (banqueImages.Length <= nombreASelectionner)
        {
            imagesSelectionnees = new Sprite[banqueImages.Length];
            banqueImages.CopyTo(imagesSelectionnees, 0);
            ShuffleArray(imagesSelectionnees);
            return;
        }

        imagesSelectionnees = new Sprite[nombreASelectionner];
        bool[] dejaUtilise = new bool[banqueImages.Length];

        for (int i = 0; i < nombreASelectionner; i++)
        {
            int index;
            do
            {
                index = Random.Range(0, banqueImages.Length);
            }
            while (dejaUtilise[index]);

            dejaUtilise[index] = true;
            imagesSelectionnees[i] = banqueImages[index];
        }
    }

    private void ShuffleArray(Sprite[] array)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Sprite temp = array[i];
            array[i] = array[j];
            array[j] = temp;
        }
    }

    private IEnumerator BoucleCarousel()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(delaiChangement);

            int prochainIndex = (indexActuel + 1)
                % imagesSelectionnees.Length;

            AfficherImage(prochainIndex, true);
            indexActuel = prochainIndex;
        }
    }

    private void AfficherImage(int index, bool avecFade)
    {
        // Mettre a jour les cercles
        for (int i = 0; i < cercles.Length; i++)
        {
            float echeleCible = (i == index)
                ? echelleCercleActif
                : echelleCercleInactif;

            StartCoroutine(AnimerCercle(cercles[i], echeleCible));
        }

        if (avecFade)
            StartCoroutine(FadeChangerImage(index));
        else
        {
            imageAsset.sprite = imagesSelectionnees[index];
            imageAsset.color = Color.white;
        }
    }

    private IEnumerator AnimerCercle(
        RectTransform cercle, float echelleCible)
    {
        Vector3 cible = Vector3.one * echelleCible;

        while (Vector3.Distance(cercle.localScale, cible) > 0.01f)
        {
            cercle.localScale = Vector3.Lerp(
                cercle.localScale, cible,
                Time.unscaledDeltaTime * vitesseTransitionCercle);

            yield return null;
        }

        cercle.localScale = cible;
    }

    private IEnumerator FadeChangerImage(int index)
    {
        // Fade out
        Color couleur = imageAsset.color;

        while (couleur.a > 0.01f)
        {
            couleur.a = Mathf.Lerp(
                couleur.a, 0f,
                Time.unscaledDeltaTime * vitesseFadeImage);

            imageAsset.color = couleur;
            yield return null;
        }

        // Changer image
        imageAsset.sprite = imagesSelectionnees[index];

        // Fade in
        while (couleur.a < 0.99f)
        {
            couleur.a = Mathf.Lerp(
                couleur.a, 1f,
                Time.unscaledDeltaTime * vitesseFadeImage);

            imageAsset.color = couleur;
            yield return null;
        }

        couleur.a = 1f;
        imageAsset.color = couleur;
    }
}