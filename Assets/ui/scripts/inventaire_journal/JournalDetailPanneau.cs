// ============================================================
// JournalDetailPanneau.cs
// ------------------------------------------------------------
// Auteur      : Fanny Fortier
// Date        : 28/03/2026
// ------------------------------------------------------------
// Description :
//   Gère le panneau de détail gauche du journal. Attaché sur
//   indices_agrandis. Affiche ou cache le panneau (et son effet
//   sonore, fade in fade out) selon les interactions du slot sélectionné. 
// ------------------------------------------------------------
// Dépendances :
//   - JournalSlotUI.cs : appelle OuvrirPanneauDetail() et FermerPanneauDetail()
// ============================================================
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class JournalDetailPanneau : MonoBehaviour
{
    // Instance singleton pour référer facilement au panneau
    public static JournalDetailPanneau Instance;

    [Header("Éléments du UI - Détails")]
    public GameObject panneauDetails;
    public Image imageIndice;
    public TMP_Text textTitre;
    public TMP_Text textDescription;
    public TMP_Text textInsight;

    [Header("Audio")]
    [SerializeField] AudioSource sourceAudio;
    [SerializeField] AudioClip sonBrouillard;
    [SerializeField] float dureeFade = 0.5f;
    [SerializeField] float volumeMax = 0.4f;

    Coroutine fadeActif;

    private void Awake()
    {
        // Debug : Si un autre panneau existe déjà, détruis ce panneau
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        // Enregistrer cette instance 
        Instance = this;

        // Désactivé par défaut
        panneauDetails.SetActive(false);
    }

    // Appelé dans JournalSlotUI
    public void OuvrirPanneauDetail(Sprite img, string titre, string description, string insight) 
    {
        // Lier les éléments du panneau aux données de l'indice
        imageIndice.sprite = img;
        textTitre.text = titre;
        textDescription.text = description;
        textInsight.text = insight;

        // Activer le panneau détails
        panneauDetails.SetActive(true);

        // Si un clip est assigné (encore du debug) et qu'il ne joue pas déja,
        // jouer l'effet sonore
        if (sourceAudio != null && sonBrouillard != null)
        {
            if (!sourceAudio.isPlaying)
            {
                sourceAudio.clip = sonBrouillard;
                sourceAudio.loop = true; //en boucle pendant que le panneau est ouvert
                sourceAudio.volume = 0f; // 0 au début (fade in)
                sourceAudio.Play();
            }
            // Si on est en train de fade out au moment ou on veut fade in, on annule le
            // fade out et on monte le volume progressivement
            if (fadeActif != null)
                StopCoroutine(fadeActif);

            fadeActif = StartCoroutine(FadeVolume(volumeMax));

        }



    }

    public void FermerPanneauDetail()
    {
        // Arrêter le son de brouillard (si il joue) en fade out
        if (sourceAudio != null && sourceAudio.isPlaying)
        {
            if (fadeActif != null)
                StopCoroutine(fadeActif);
            fadeActif = StartCoroutine(FadeVolume(0f, arretSon: true));
        }

        // Désactiver le panneau détails
        panneauDetails.SetActive(false);
    }


    // Coroutine du fade in et fade out de volume
    IEnumerator FadeVolume(float cibleVolume, bool arretSon = false)
    {
        float volumeInitial = sourceAudio.volume;
        float timer = 0f;

        // Tant que le timer n'a pas atteint la durée du fade, on continue le lerp
        while (timer < dureeFade)
        {
            timer += Time.deltaTime;
            sourceAudio.volume = Mathf.Lerp(volumeInitial, cibleVolume, timer / dureeFade);
            yield return null;
        }
        sourceAudio.volume = cibleVolume;
        // Si on a atteint un volume de 0, on arrête le son
        if (arretSon && cibleVolume == 0f)
            sourceAudio.Stop();
    }

}

