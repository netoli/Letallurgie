// ============================================================
// binderBoutonsHud.cs
// ------------------------------------------------------------
// Auteur      : Claude (assistance Olivier V.)
// Date        : 2026-05-25
// ------------------------------------------------------------
// Description :
//   Script de secours pour scene2_usine en lancement standalone.
//   Cherche les boutons UI par nom dans la scene et leur cable
//   automatiquement le bon onClick vers gestionInputsJeu.Instance.
//   Necessaire car les onClick des boutons sont serialises avec
//   un fileID specifique a chaque scene : si une scene a ete
//   copiee depuis une autre, les onClick pointent vers la mauvaise
//   instance (ou null) et les boutons ne reagissent plus.
//
// USAGE :
//   1. Ajouter ce script sur n'importe quel GameObject actif de
//      la scene (ex : starter_chapitre_le_sauvetage).
//   2. Au Start, le script trouve les boutons et rebind les onClick.
// ============================================================

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class binderBoutonsHud : MonoBehaviour
{
    [Tooltip("Si coche, log dans la console chaque bouton trouve et " +
        "chaque onClick rebind.")]
    [SerializeField] private bool debugLogs = false;

    [Tooltip("Si vide : execute en toutes scenes. Sinon n'execute " +
        "qu'en cette scene specifique (ex : scene2_usine).")]
    [SerializeField] private string limiterAScene = "scene2_usine";

    void Start()
    {
        Debug.Log("[binderBoutonsHud] Start() execute dans scene "
            + SceneManager.GetActiveScene().name);

        if (!string.IsNullOrEmpty(limiterAScene)
            && SceneManager.GetActiveScene().name != limiterAScene)
        {
            Debug.Log("[binderBoutonsHud] Scene non-cible, abandon.");
            return;
        }

        var inputs = FindFirstObjectByType<gestionInputsJeu>(
            FindObjectsInactive.Include);
        if (inputs == null)
        {
            Debug.LogWarning("[binderBoutonsHud] gestionInputsJeu " +
                "introuvable, abandon.");
            return;
        }
        Debug.Log("[binderBoutonsHud] gestionInputsJeu trouve sur '"
            + inputs.gameObject.name + "'.");

        // S'assurer qu'un EventSystem existe (sans EventSystem, aucun
        // bouton UI ne reagit aux clics).
        EnsureEventSystem();

        // Liste des boutons a rebind : (nom_du_bouton, methode_de_inputs)
        // Note : si plusieurs boutons portent le meme nom (ex :
        // bouton_options HUD vs bouton_options menu_pause), on les
        // rebind tous a la meme methode parce qu'ils declenchent la
        // meme logique cote gestionInputsJeu (qui gere l'etat).
        var bindings = new (string nom, System.Action action)[]
        {
            ("bouton_journal", inputs.BoutonJournal),
            ("bouton_inventaire", inputs.BoutonInventaire),
            ("bouton_options", inputs.BoutonOptions),
            ("bouton_sauvegarder", inputs.SauvegarderPartie),
            ("bouton_sauvegarde", inputs.SauvegarderPartie),
            ("bouton_esc", inputs.MettreEnPause),
            ("bouton_menu_principal", inputs.AfficherConfirmationRetourMenu),
            ("bouton_quitter", inputs.AfficherConfirmationQuitter),
        };

        foreach (var (nom, action) in bindings)
        {
            RebindBoutonsParNom(nom, action);
        }

        if (debugLogs)
            Debug.Log("[binderBoutonsHud] Rebind termine.");
    }

    private void EnsureEventSystem()
    {
        var es = FindFirstObjectByType<EventSystem>(
            FindObjectsInactive.Include);
        if (es != null) return;

        var go = new GameObject("EventSystem (auto)");
        go.AddComponent<EventSystem>();
        go.AddComponent<StandaloneInputModule>();
        if (debugLogs)
            Debug.Log("[binderBoutonsHud] EventSystem cree (manquait).");
    }

    private void RebindBoutonsParNom(string nom, System.Action action)
    {
        // Cherche tous les boutons (meme inactifs) qui ont ce nom
        int trouves = 0;
        var tous = Resources.FindObjectsOfTypeAll<Button>();
        foreach (var btn in tous)
        {
            if (btn == null) continue;
            if (btn.gameObject == null) continue;
            if (btn.gameObject.name != nom) continue;
            // Exclure les prefabs assets
            if (btn.gameObject.hideFlags != HideFlags.None) continue;
            if (!btn.gameObject.scene.IsValid()) continue;

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => action());
            trouves++;
            Debug.Log($"[binderBoutonsHud] '{nom}' rebind ({btn.transform.parent?.name}/{btn.name}).");
        }
        if (trouves == 0)
            Debug.LogWarning($"[binderBoutonsHud] Aucun bouton trouve pour '{nom}'.");
    }
}
