// ============================================================
// snapPointBalance.cs
// ------------------------------------------------------------
// Auteur      : Fanny Fortier
// Date créée  : 2026
// ------------------------------------------------------------
// Description :
//   Snap point placé sur le plateau joueur de la balance.
//   Joue le même rôle que pointAncrageTuyau pour le tutoriel,
//   mais spécialisé pour l'énigme balance :
//     - Accepte n'importe quel objetInventaire de catégorie Alchimie
//     - Affiche un ghost translucide quand un objet est sélectionné
//       et que le curseur survole la zone
//     - À la pose : instancie le prefab3D (ou prefabModele3D en
//       fallback), récupère son objetPesable, et notifie
//       ZoneDepotJoueur
//     - Supporte le retrait : re-clic sur l'objet posé pour le
//       reprendre dans l'inventaire
//   Plusieurs snapPointBalance peuvent coexister sur le même
//   plateau (un par emplacement d'objet).
// ------------------------------------------------------------
// Dépendances :
//   - ZoneDepotJoueur     : AjouterObjet / RetirerObjet
//   - objetInventaire     : prefab3D, prefabModele3D
//   - objetPesable        : composant requis sur le prefab posé
//   - gestionInventaire   : AjouterObjet (au retrait)
// ============================================================

using UnityEngine;

public class snapPointBalance : MonoBehaviour
{
    // ===================== INSPECTEUR =====================

    [Header("Références")]
    [SerializeField] private ZoneDepotJoueur _zoneDepot;

    [Header("Ghost")]
    [Tooltip("Matériau translucide appliqué à l'aperçu ghost.")]
    [SerializeField] private Material _materiauGhost;

    [Header("Debug")]
    [SerializeField] private bool _debugLogs = false;

    // ===================== ÉTAT INTERNE =====================

    private GameObject _ghostActuel;
    private objetPesable _objetDepose;
    private objetInventaire _inventaireDepose; // gardé pour le retrait

    // ===================== UNITY =====================

    private void Update()
    {
        // Détecte si l'objet déposé a été ramassé par objetRamassable
        // (Destroy sans passer par Retirer). L'opérateur ! d'Unity
        // retourne true si l'objet est détruit.
        if (_inventaireDepose != null && !_objetDepose)
        {
            if (_zoneDepot != null)
                _zoneDepot.SupprimerObjetDetruit(_objetDepose);

            _objetDepose = null;
            _inventaireDepose = null;

            if (_debugLogs)
                Debug.Log($"[snapPointBalance:{name}] Objet ramassé directement — état réinitialisé.");
        }
    }

    // ===================== MÉTHODES PUBLIQUES =====================

    /// <summary>
    /// True si un objet a été déposé sur ce snap point.
    /// </summary>
    public bool EstOccupe() => _objetDepose != null;

    /// <summary>
    /// Affiche le ghost de l'objet sélectionné sur ce snap point.
    /// Appelé chaque frame par controleurDeposeBalance tant que
    /// le curseur survole cette zone.
    /// </summary>
    public void AfficherGhost(objetInventaire objet)
    {
        if (objet == null || EstOccupe()) return;

        GameObject prefab = ChoisirPrefab(objet);
        if (prefab == null)
        {
            if (_debugLogs)
                Debug.LogWarning($"[snapPointBalance:{name}] " +
                    $"{objet.nomObjet} n'a ni prefab3D ni prefabModele3D.");
            return;
        }

        // Créer le ghost une seule fois, le garder tant qu'on survole
        if (_ghostActuel == null)
        {
            _ghostActuel = Instantiate(
                prefab,
                transform.position,
                transform.rotation);

            DesactiverPhysique(_ghostActuel);
            DesactiverCamerasEtLights(_ghostActuel);
            AppliquerMateriauGhost(_ghostActuel);

            if (_debugLogs)
                Debug.Log($"[snapPointBalance:{name}] " +
                    $"Ghost affiché pour '{objet.nomObjet}'.");
        }
    }

    /// <summary>
    /// Détruit le ghost actuel. Appelé quand le curseur quitte
    /// la zone ou quand la sélection change.
    /// </summary>
    public void CacherGhost()
    {
        if (_ghostActuel != null)
        {
            Destroy(_ghostActuel);
            _ghostActuel = null;
        }
    }

    /// <summary>
    /// Dépose l'objet sur ce snap point.
    /// Instancie le prefab, ajoute l'objetPesable à ZoneDepotJoueur.
    /// Retourne true si le dépôt a réussi.
    /// </summary>
    public bool Deposer(objetInventaire objet)
    {
        if (objet == null || EstOccupe()) return false;

        GameObject prefab = ChoisirPrefab(objet);
        if (prefab == null)
        {
            Debug.LogWarning($"[snapPointBalance:{name}] " +
                $"Impossible de déposer '{objet.nomObjet}' : " +
                "aucun prefab assigné sur le ScriptableObject.");
            return false;
        }

        CacherGhost();

        // Instancier en enfant du snap point pour qu'il suive
        // l'animation de la balance (le plateau est son parent)
        GameObject instance = Instantiate(
            prefab,
            transform.position,
            transform.rotation,
            transform);

        DesactiverCamerasEtLights(instance);

        // Rendre tous les Rigidbody kinématiques : les prefabs joueur
        // ont gravity + no constraints, ce qui ferait tomber l'objet
        // immédiatement hors de la balance. Le poids est logique
        // (valeurPoids sur objetPesable), pas physique.
        foreach (Rigidbody rb in
            instance.GetComponentsInChildren<Rigidbody>(true))
        {
            rb.isKinematic = true;
        }

        _objetDepose = instance.GetComponent<objetPesable>();

        // Fallback : chercher dans les enfants si pas sur la racine
        if (_objetDepose == null)
            _objetDepose = instance.GetComponentInChildren<objetPesable>();

        // Fallback : ajouter objetPesable dynamiquement si le prefab
        // ne l'a pas (ex: parent prefab manquant après un merge).
        if (_objetDepose == null)
        {
            _objetDepose = instance.AddComponent<objetPesable>();
            _objetDepose.DefinirPoids(objet.valeurPoidsBalance);
            Debug.LogWarning($"[snapPointBalance:{name}] " +
                $"objetPesable absent du prefab '{objet.nomObjet}' — " +
                $"ajouté dynamiquement (poids={_objetDepose.valeurPoids}).");
        }

        _inventaireDepose = objet;

        if (_zoneDepot != null)
            _zoneDepot.AjouterObjet(_objetDepose);

        if (_debugLogs)
            Debug.Log($"[snapPointBalance:{name}] " +
                $"'{objet.nomObjet}' déposé " +
                $"(poids={_objetDepose.valeurPoids}).");

        return true;
    }

    /// <summary>
    /// Retire l'objet posé et le retourne à l'inventaire.
    /// Retourne le ScriptableObject de l'objet récupéré,
    /// ou null si le snap point était vide.
    /// </summary>
    public objetInventaire Retirer()
    {
        if (!EstOccupe()) return null;

        if (_zoneDepot != null)
            _zoneDepot.RetirerObjet(_objetDepose);

        objetInventaire recupere = _inventaireDepose;

        Destroy(_objetDepose.gameObject);
        _objetDepose = null;
        _inventaireDepose = null;

        if (_debugLogs)
            Debug.Log($"[snapPointBalance:{name}] " +
                $"'{recupere?.nomObjet}' retiré et retourné à l'inventaire.");

        return recupere;
    }

    // ===================== MÉTHODES PRIVÉES =====================

    /// <summary>
    /// Choisit prefab3D en priorité, prefabModele3D en fallback.
    /// Permet la cohabitation des anciens assets (balance) et
    /// des nouveaux (tuyaux/tuto) pendant la transition.
    /// </summary>
    private GameObject ChoisirPrefab(objetInventaire objet)
    {
        if (objet == null) return null;
        return objet.prefab3D != null
            ? objet.prefab3D
            : objet.prefabModele3D;
    }

    /// <summary>
    /// Désactive Collider et rend le Rigidbody kinématique sur
    /// le ghost pour qu'il ne tombe pas et n'interfère pas avec
    /// la physique de la scène.
    /// </summary>
    private void DesactiverPhysique(GameObject go)
    {
        foreach (Collider col in go.GetComponentsInChildren<Collider>())
            col.enabled = false;

        foreach (Rigidbody rb in go.GetComponentsInChildren<Rigidbody>())
            rb.isKinematic = true;
    }

    /// <summary>
    /// Désactive les Camera et Light embarquées dans les FBX
    /// importés (ex: Blender exporte souvent une caméra et une
    /// lumière avec le mesh). Sans ça, la caméra du FBX peut
    /// prendre le contrôle du rendu.
    /// </summary>
    private void DesactiverCamerasEtLights(GameObject go)
    {
        foreach (Camera cam in go.GetComponentsInChildren<Camera>(true))
            cam.gameObject.SetActive(false);

        foreach (Light light in go.GetComponentsInChildren<Light>(true))
            light.gameObject.SetActive(false);
    }

    /// <summary>
    /// Remplace tous les matériaux du ghost par le matériau
    /// translucide assigné dans l'Inspecteur.
    /// Si aucun matériau n'est assigné, le ghost reste avec
    /// son matériau d'origine (pas idéal visuellement mais
    /// fonctionnel).
    /// </summary>
    private void AppliquerMateriauGhost(GameObject go)
    {
        if (_materiauGhost == null) return;

        foreach (Renderer r in go.GetComponentsInChildren<Renderer>())
            r.material = _materiauGhost;
    }

    // ===================== GIZMOS =====================

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = EstOccupe()
            ? new Color(1f, 0.5f, 0f, 0.6f)  // orange = occupé
            : new Color(0f, 1f, 0.5f, 0.4f); // vert  = libre
        Gizmos.DrawWireSphere(transform.position, 0.15f);
    }
}
