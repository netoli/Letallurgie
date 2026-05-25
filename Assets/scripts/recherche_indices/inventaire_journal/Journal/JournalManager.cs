// ============================================================
// JournalManager.cs
// ------------------------------------------------------------
// Auteur      : Fanny Fortier
// Date créé   : 09/04/2026
// Dernière modification : 2026-05-23 - Fanny Fortier
// ------------------------------------------------------------
// Description :
//   Gère la création des entrées dans le journal.
//   Attaché sur gestion_journal (game object vide dans les managers).
// ------------------------------------------------------------
// Dépendances :
//   - JournalSlotUI.cs  : appelle InitialiserSlot() sur chaque slot créé
//   - RamasserIndice.cs : appelle AjouterEntreeJournal() pour ajouter un indice
// ============================================================
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class JournalManager : MonoBehaviour
{
    public static JournalManager Instance;
    public List<EntreeJournal> entrees = new List<EntreeJournal>();

    /// <summary>
    /// Event diffuse a chaque ajout d'entree au journal. L'argument
    /// transmet le nombre total d'entrees apres l'ajout. Permet a un
    /// composant de scene (ex: compteurIndicesScene) de detecter quand
    /// le seuil "tous indices ramasses" est atteint sans coupler la
    /// logique de signalement au JournalManager.
    /// </summary>
    public static event System.Action<int> OnIndiceAjoute;

    [Header("HUD")]
    [SerializeField] private TMP_Text compteurHUD;

    [Header("Flash rouge")]
    [Tooltip("Durée en secondes pendant laquelle le compteur reste " +
             "rouge quand une nouvelle entrée est ajoutée au journal.")]
    [SerializeField] private float _dureeFlashRouge = 2f;

    // ── État interne ──────────────────────────────────────────

    private Color _couleurNormale = Color.white;
    private Coroutine _coroutineFlash;

    // ── Unity ────────────────────────────────────────────────

    private void Awake()
    {
        // Assure que le JournalManager est un singleton persistant
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ── API publique ──────────────────────────────────────────

    /// <summary>
    /// Appelé par le compteur HUD de chaque scène dans son Start()
    /// pour s'enregistrer auprès du manager persistant.
    /// Sans ça, la référence devient nulle après un changement de scène.
    /// </summary>
    public void EnregistrerCompteurHUD(TMP_Text compteur)
    {
        compteurHUD = compteur;

        // Sauvegarder la couleur d'origine du nouveau compteur
        if (compteurHUD != null)
            _couleurNormale = compteurHUD.color;

        // Annuler un flash en cours sur l'ancien compteur (devenu invalide)
        if (_coroutineFlash != null)
        {
            StopCoroutine(_coroutineFlash);
            _coroutineFlash = null;
        }

        MettreAJourCompteur();
    }

    public void AjouterEntreeJournal(Sprite icone, string titre,
        string description, string insight)
    {
        entrees.Add(new EntreeJournal(icone, titre, description, insight));
        MettreAJourCompteur();

        // Flash rouge : nouvelle entrée ajoutée
        if (compteurHUD != null)
        {
            if (_coroutineFlash != null)
                StopCoroutine(_coroutineFlash);
            _coroutineFlash = StartCoroutine(FlashRougeCompteur());
        }

        // Notifier les abonnes (ex: compteurIndicesScene) qu'un indice
        // vient d'etre ajoute. Diffuse le nombre total d'entrees apres
        // l'ajout. Try/catch pour qu'un abonne en erreur ne bloque pas
        // l'ajout d'indices.
        try { OnIndiceAjoute?.Invoke(entrees.Count); }
        catch (System.Exception e)
        {
            Debug.LogError($"[JournalManager] Erreur dans un handler " +
                $"OnIndiceAjoute : {e.Message}");
        }
    }

    public void MettreAJourCompteur()
    {
        if (compteurHUD == null) return;
        compteurHUD.text = entrees.Count.ToString();
    }

    // ── Coroutine flash ───────────────────────────────────────

    private IEnumerator FlashRougeCompteur()
    {
        compteurHUD.color = Color.red;
        yield return new WaitForSecondsRealtime(_dureeFlashRouge);

        // Remettre la couleur normale seulement si le compteur est
        // toujours le même (pas changé de scène entre-temps)
        if (compteurHUD != null)
            compteurHUD.color = _couleurNormale;

        _coroutineFlash = null;
    }

    // ── Classes internes ──────────────────────────────────────

    [System.Serializable]
    public class EntreeJournal
    {
        public Sprite icone;
        public string titre;
        public string description;
        public string insight;

        public EntreeJournal(Sprite i, string t, string d, string ins)
        {
            icone = i;
            titre = t;
            description = d;
            insight = ins;
        }
    }
}
