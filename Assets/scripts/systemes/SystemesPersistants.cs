using UnityEngine;

/// <summary>
/// Script ROOT du prefab "--SystemesPersistants--". A attacher au
/// GameObject racine qui contient tous les managers et UI a faire
/// persister entre les scenes (DontDestroyOnLoad).
///
/// PHILOSOPHIE :
/// - Une SEULE instance de ce GameObject existe dans le jeu (singleton).
/// - Il est cree dans la scene de demarrage (scene0_tuto
///   ou meme avant via scene_menu) et survit aux changements de scene.
/// - Si le joueur recharge le jeu (retour menu, etc.) et qu'une nouvelle
///   instance arrive depuis une scene avec son propre prefab, le Awake
///   ci-dessous detruit le doublon pour ne garder que l'original.
///
/// CONTENU TYPIQUE DU PREFAB :
/// --SystemesPersistants-- (root, ce script)
/// ├── Managers (GameObject vide regroupant les logiques)
/// │   ├── gestionAudio (musique de fond)
/// │   ├── gestionBandeauInfo (cf. canvas_persistent)
/// │   └── gestionDeclencheurBandeau (listener bandeau)
/// └── canvas_persistent (Canvas en mode Screen Space - Overlay)
///     └── bandeau_info
///         └── texte_bandeau (TMP_Text)
///
/// IMPORTANT - REFERENCES :
/// Les composants attaches dans ce prefab peuvent UNIQUEMENT referencer :
///   - D'autres composants enfants de ce meme prefab (refs internes OK)
///   - Des ScriptableObjects / assets (refs OK, pas dependants de la scene)
/// Ils NE PEUVENT PAS referencer :
///   - Des GameObjects de la scene (Player, Canvas du HUD, etc.) : ces
///     references seront cassees au changement de scene.
/// Pour referencer la scene, utiliser FindFirstObjectByType au runtime.
///
/// PATTERN D'INITIALISATION DANS UNITY :
/// 1. Dans scene0_tuto, creer un GameObject vide nomme
///    exactement "--SystemesPersistants--".
/// 2. Attacher ce script.
/// 3. Y deplacer les managers et UI qu'on veut persister (voir guide
///    de migration).
/// 4. Glisser ce GameObject vers le dossier Prefabs pour creer le prefab.
/// 5. Dans CHAQUE autre scene (scene1_taverne1, etc.),
///    glisser une instance du prefab. Au demarrage de la scene, le
///    doublon se detruira si une instance existe deja (chargement
///    direct de la scene 1 -> 2 via cinematique : seule la 1ere est
///    gardee. Lancement direct de la scene 2 : la 2eme est gardee).
///
/// NOTE SUR L'ORDRE D'EXECUTION :
/// Le Awake de ce script ne fait pas grand chose (juste DDOL + singleton),
/// donc l'ordre d'execution avec les enfants n'est pas critique. Chaque
/// manager enfant (gestionChapitres, gestionBandeauInfo, etc.) gere
/// son propre Awake et son propre singleton.
/// </summary>
public class SystemesPersistants : MonoBehaviour
{
    public static SystemesPersistants Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.Log($"[SystemesPersistants] Doublon detecte ('{name}'), " +
                "destruction. L'instance originale persiste.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log($"[SystemesPersistants] '{name}' marque " +
            "DontDestroyOnLoad. Persiste entre les scenes.");
    }
}
