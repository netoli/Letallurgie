// ============================================================
// sonsEnigmeTuyauterie.cs
// ------------------------------------------------------------
// S'abonne aux events de gestionEnigmeTuyauterie pour jouer
// les sons appropries : bon placement, mauvais placement,
// echec global, reussite globale.
//
// SETUP UNITY :
// 1. Sur le GameObject qui porte gestionEnigmeTuyauterie
//    (typiquement 'enigme_tuyauterie'), Add Component →
//    sonsEnigmeTuyauterie + AudioSource.
// 2. Glisser les 4 clips dans les champs.
// 3. Ce script s'auto-abonne aux events au Start.
// ============================================================

using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class sonsEnigmeTuyauterie : MonoBehaviour
{
    [Header("Clips sons")]
    [SerializeField] private AudioClip sonBonPlacement;
    [SerializeField] private AudioClip sonMauvaisPlacement;
    [SerializeField] private AudioClip sonEchec;
    [SerializeField] private AudioClip sonReussite;

    [Header("Volumes")]
    [Range(0f, 1f)]
    [SerializeField] private float volumeBon = 1f;
    [Range(0f, 1f)]
    [SerializeField] private float volumeMauvais = 1f;
    [Range(0f, 1f)]
    [SerializeField] private float volumeEchec = 1f;
    [Range(0f, 1f)]
    [SerializeField] private float volumeReussite = 1f;

    private AudioSource source;
    private gestionEnigmeTuyauterie enigme;

    void Awake()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f; // 2D pour des sons UI/feedback
        enigme = GetComponent<gestionEnigmeTuyauterie>();
    }

    void Start()
    {
        if (enigme == null)
        {
            enigme = FindFirstObjectByType<gestionEnigmeTuyauterie>();
        }
        if (enigme == null)
        {
            Debug.LogWarning("[sonsEnigmeTuyauterie] " +
                "gestionEnigmeTuyauterie introuvable.");
            return;
        }

        enigme.onFeedback.AddListener(AuFeedback);
        enigme.onVictoire.AddListener(AuVictoire);
        enigme.onReset.AddListener(AuReset);
    }

    void OnDestroy()
    {
        if (enigme == null) return;
        enigme.onFeedback.RemoveListener(AuFeedback);
        enigme.onVictoire.RemoveListener(AuVictoire);
        enigme.onReset.RemoveListener(AuReset);
    }

    private void AuFeedback(resultatPlacement r)
    {
        switch (r)
        {
            case resultatPlacement.Succes:
                if (sonBonPlacement != null)
                    source.PlayOneShot(sonBonPlacement, volumeBon);
                break;
            case resultatPlacement.MauvaisePiece:
            case resultatPlacement.MauvaiseOrientation:
                if (sonMauvaisPlacement != null)
                    source.PlayOneShot(sonMauvaisPlacement, volumeMauvais);
                break;
        }
    }

    private void AuVictoire()
    {
        if (sonReussite != null)
            source.PlayOneShot(sonReussite, volumeReussite);
    }

    private void AuReset()
    {
        if (sonEchec != null)
            source.PlayOneShot(sonEchec, volumeEchec);
    }
}
