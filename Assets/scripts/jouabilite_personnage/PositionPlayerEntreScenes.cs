// ============================================================
// PositionPlayerEntreScenes.cs
// ------------------------------------------------------------
// Stockage statique de la position et rotation du Player pour
// les preserver entre deux scenes consecutives (typiquement
// SCENE0-Menu-Tuto -> SCENE1-Taverne1
// apres la cinematique de fin du chapitre une_aide_precieuse).
//
// Utilisation :
//   - AVANT SceneManager.LoadScene : appeler Capturer(player)
//   - Dans PlayerMovement.Start de la scene cible : appeler
//     AppliquerSiDisponible(transform, controller).
//
// Le champ static survit naturellement au changement de scene
// (memoire statique, pas un GameObject).
// ============================================================
using UnityEngine;

public static class PositionPlayerEntreScenes
{
    private static Vector3? positionEnregistree;
    private static Quaternion? rotationEnregistree;

    /// <summary>
    /// Capture la position et la rotation du Player donne.
    /// A appeler juste avant SceneManager.LoadScene.
    /// </summary>
    public static void Capturer(Transform playerTransform)
    {
        if (playerTransform == null) return;
        positionEnregistree = playerTransform.position;
        rotationEnregistree = playerTransform.rotation;
        Debug.Log("[PositionPlayer] Capture : pos="
            + positionEnregistree + " rot=" + rotationEnregistree);
    }

    /// <summary>
    /// Applique la position et rotation enregistrees (si elles
    /// existent) sur le Transform donne. Consomme les valeurs
    /// stockees apres usage pour eviter de les re-appliquer.
    ///
    /// Si un CharacterController est fourni, il est temporairement
    /// desactive pour permettre la teleportation (sinon il
    /// remplace les changements de transform.position).
    /// </summary>
    public static bool AppliquerSiDisponible(
        Transform playerTransform,
        CharacterController controller = null)
    {
        if (playerTransform == null) return false;
        if (!positionEnregistree.HasValue) return false;

        bool controllerEtaitActif = false;
        if (controller != null)
        {
            controllerEtaitActif = controller.enabled;
            controller.enabled = false;
        }

        playerTransform.position = positionEnregistree.Value;
        if (rotationEnregistree.HasValue)
            playerTransform.rotation = rotationEnregistree.Value;

        if (controller != null && controllerEtaitActif)
            controller.enabled = true;

        Debug.Log("[PositionPlayer] Restaure : pos="
            + positionEnregistree + " rot=" + rotationEnregistree);

        // Consomme : les valeurs ne seront pas re-appliquees au
        // prochain Start dans cette meme scene.
        positionEnregistree = null;
        rotationEnregistree = null;
        return true;
    }
}
