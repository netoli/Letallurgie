// ============================================================
// minuteurEnigmeTuyauterie.cs
// ------------------------------------------------------------
// Minuteur d'enigme : decompte une duree configurable. A zero,
// declenche un signal d'echec (typiquement le reset force de
// l'enigme via gestionEnigmeTuyauterie).
//
// DEMARRAGE :
// - Sur signal d'action 'enigme_tuyauterie_lancee' (depuis
//   zoneLancementEnigme).
// - OU appel public DemarrerMinuteur() depuis un autre script.
//
// ARRET :
// - Sur victoire (event onVictoire de gestionEnigmeTuyauterie).
// - OU appel public ArreterMinuteur().
// - OU expiration : declenche onTempsEcoule.
//
// UI :
// - tempsRestant lisible en public pour binding UI (ex : Text
//   ou Slider). Format : float secondes.
// ============================================================

using UnityEngine;
using UnityEngine.Events;
using TMPro;
using UnityEngine.UI;

public class minuteurEnigmeTuyauterie : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Duree totale du minuteur en secondes.")]
    [SerializeField] private float dureeTotale = 120f;

    [Tooltip("Si coche, demarre automatiquement le minuteur quand " +
        "l'action 'enigme_tuyauterie_lancee' est signalee a " +
        "gestionChapitres.")]
    [SerializeField] private bool autoDemarrerSurAction = true;

    [Tooltip("ID action ecoutee pour demarrer le minuteur.")]
    [SerializeField] private string idActionDemarrage = "enigme_tuyauterie_lancee";

    [Header("UI (optionnel)")]
    [Tooltip("Texte TMP qui affiche le temps restant (format MM:SS).")]
    [SerializeField] private TMP_Text texteAffichage;

    [Tooltip("Slider qui montre la progression du temps " +
        "(value: 1 au depart, 0 a la fin).")]
    [SerializeField] private Slider sliderAffichage;

    [Header("Events")]
    public UnityEvent onTempsEcoule;
    public UnityEvent onMinuteurDemarre;

    private bool actif = false;
    private float tempsRestant;
    private gestionEnigmeTuyauterie enigme;

    public float TempsRestant => tempsRestant;
    public bool EstActif => actif;

    void Start()
    {
        // Auto-lien sur le composant gestionEnigmeTuyauterie (si present
        // sur le meme GameObject ou ailleurs dans la scene).
        enigme = GetComponent<gestionEnigmeTuyauterie>();
        if (enigme == null)
            enigme = FindFirstObjectByType<gestionEnigmeTuyauterie>();

        if (enigme != null)
        {
            enigme.onVictoire.AddListener(ArreterMinuteur);
        }

        if (autoDemarrerSurAction && gestionChapitres.Instance != null)
            gestionChapitres.Instance.OnActionSignalee += AuActionSignalee;

        tempsRestant = dureeTotale;
        MettreAJourUI();
    }

    void OnDestroy()
    {
        if (gestionChapitres.Instance != null)
            gestionChapitres.Instance.OnActionSignalee -= AuActionSignalee;
        if (enigme != null)
            enigme.onVictoire.RemoveListener(ArreterMinuteur);
    }

    void Update()
    {
        if (!actif) return;
        tempsRestant -= Time.deltaTime;
        if (tempsRestant <= 0f)
        {
            tempsRestant = 0f;
            ArreterMinuteur();
            Debug.Log("[minuteurEnigme] Temps ecoule.");
            onTempsEcoule.Invoke();
            // Si lie a gestionEnigmeTuyauterie, on declenche le reset
            // (via la methode privee ReinitialiserPuzzle... pas
            // accessible. On simule via les Errors max).
            // Note : le user doit hook onTempsEcoule a une methode qui
            // force le reset (ex : appeler un methode publique).
        }
        MettreAJourUI();
    }

    private void AuActionSignalee(string id)
    {
        if (id == idActionDemarrage)
            DemarrerMinuteur();
    }

    public void DemarrerMinuteur()
    {
        if (actif) return;
        actif = true;
        tempsRestant = dureeTotale;
        MettreAJourUI();
        Debug.Log($"[minuteurEnigme] Demarre ({dureeTotale}s).");
        onMinuteurDemarre.Invoke();
    }

    public void ArreterMinuteur()
    {
        if (!actif) return;
        actif = false;
        Debug.Log("[minuteurEnigme] Arrete.");
    }

    public void ReinitialiserMinuteur()
    {
        tempsRestant = dureeTotale;
        MettreAJourUI();
    }

    private void MettreAJourUI()
    {
        if (texteAffichage != null)
        {
            int min = Mathf.FloorToInt(tempsRestant / 60f);
            int sec = Mathf.FloorToInt(tempsRestant % 60f);
            texteAffichage.text = $"{min:00}:{sec:00}";
        }
        if (sliderAffichage != null && dureeTotale > 0f)
        {
            sliderAffichage.value = tempsRestant / dureeTotale;
        }
    }
}
