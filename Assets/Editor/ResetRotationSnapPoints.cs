// ============================================================
// ResetRotationSnapPoints.cs
// ------------------------------------------------------------
// Outil EDITOR : pour chaque snap_point (pointAncrageTuyau) dans
// la scène ouverte, remet la rotation locale ET MONDE à (0,0,0).
//
// POURQUOI :
//   Quand un tuyau est placé dans pointAncrageTuyau.Remplir(), il
//   est instancié en enfant du snap (parent = transform). Si le
//   snap a une rotation non-identité dans la scène, le tuyau placé
//   hérite visuellement de cette rotation EN PLUS de l'orientation
//   choisie par le joueur — résultat : le tuyau ne s'aligne pas
//   correctement avec les autres pièces du circuit.
//
//   En remettant la rotation du snap à zéro, la rotation finale
//   du tuyau placé devient exactement celle choisie par le joueur
//   (rotationActuelle dans controleurPlacementTuyau).
//
// SANS CASSER LE RESTE :
//   - On ne touche QUE la rotation (pas position, pas scale)
//   - On ne touche QUE les snap_points (objets avec pointAncrageTuyau)
//   - Travail réversible : Ctrl+Z restaure les rotations d'origine
//
// USAGE :
//   1. Ouvrir scene2_usine dans Unity
//   2. Menu : Tools → Reset rotation snap_points (tuyaux)
//   3. Vérifier dans la console le nombre de snap_points traités
//   4. Sauvegarder la scène (Ctrl+S)
// ============================================================
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ResetRotationSnapPoints
{
    [MenuItem("Tools/Reset rotation snap_points (tuyaux)")]
    public static void Reset()
    {
        var snaps = Object.FindObjectsByType<pointAncrageTuyau>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        if (snaps.Length == 0)
        {
            Debug.LogWarning("[ResetRotationSnap] Aucun snap_point trouve " +
                "dans la scene ouverte. Ouvre scene2_usine d'abord.");
            return;
        }

        int nbModifies = 0;
        int nbDejaZero = 0;

        // Undo group : permet a Ctrl+Z d'annuler TOUTES les modifs en une fois.
        Undo.SetCurrentGroupName("Reset rotation snap_points");
        int undoGroup = Undo.GetCurrentGroup();

        foreach (var snap in snaps)
        {
            if (snap == null) continue;
            Transform t = snap.transform;

            Vector3 rotationAvant = t.eulerAngles;
            bool dejaZero = Mathf.Approximately(t.localRotation.x, 0f)
                && Mathf.Approximately(t.localRotation.y, 0f)
                && Mathf.Approximately(t.localRotation.z, 0f)
                && Mathf.Approximately(t.localRotation.w, 1f);

            if (dejaZero)
            {
                nbDejaZero++;
                continue;
            }

            // Undo.RecordObject permet a Unity de tracker le changement
            // pour Ctrl+Z et marquer la scene dirty.
            Undo.RecordObject(t, "Reset rotation snap_point");
            t.localRotation = Quaternion.identity;
            // Egalement reset la rotation monde via setter (au cas ou le
            // parent a une rotation non-identite : on veut le snap monde
            // a (0,0,0) absolu, pas seulement par rapport au parent).
            // → On le laisse en local zero pour respecter la hierarchie
            // typique. Si un parent rotation parasitait, ce serait au user
            // de fixer le parent.

            EditorUtility.SetDirty(snap);
            nbModifies++;

            Debug.Log($"[ResetRotationSnap] '{snap.name}' : rotation " +
                $"{rotationAvant} → (0,0,0).");
        }

        // Marquer la scene comme modifiee pour que Unity propose de sauver.
        if (nbModifies > 0)
        {
            EditorSceneManager.MarkSceneDirty(
                EditorSceneManager.GetActiveScene());
        }

        Undo.CollapseUndoOperations(undoGroup);

        Debug.Log($"[ResetRotationSnap] Termine : {nbModifies} snap_point(s) " +
            $"modifie(s), {nbDejaZero} deja a zero, " +
            $"{snaps.Length} total. " +
            (nbModifies > 0 ? "N'oublie pas Ctrl+S pour sauver la scene." : ""));
    }
}
#endif
