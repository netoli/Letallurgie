// ============================================================
// AutoConfigSnapPoints.cs
// ------------------------------------------------------------
// Outil EDITOR : pour chaque snap_point (pointAncrageTuyau) dans
// la scène actuellement ouverte, cherche le scriptable
// objetInventaire correspondant au NOM du GameObject (ex : un
// snap nommé 'tuyau_droit_petit' reçoit le scriptable
// 'tuyau_droit_petit.asset').
//
// USAGE :
//   1. Ouvrir scene2_usine dans Unity
//   2. Menu : Tools → Auto-config snap_points (par nom)
//   3. Tous les snap_points sans pieceAttendue sont configurés
//
// LIMITES :
//   - Travaille sur la scène actuellement ouverte uniquement
//   - Matching par nom : insensible à la casse, accepte préfixe/suffixe
//   - Si plusieurs scriptables matchent, prend le premier trouvé
//   - Skip les snap_points qui ont déjà pieceAttendue assigné
//     (pour ne pas écraser le travail manuel)
// ============================================================
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class AutoConfigSnapPoints
{
    [MenuItem("Tools/Auto-config snap_points (assigner pieceAttendue par nom)")]
    public static void Configurer()
    {
        // 1. Récupérer tous les scriptables objetInventaire du projet
        string[] guids = AssetDatabase.FindAssets("t:objetInventaire");
        Dictionary<string, objetInventaire> mapping =
            new Dictionary<string, objetInventaire>();
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            objetInventaire asset = AssetDatabase
                .LoadAssetAtPath<objetInventaire>(path);
            if (asset != null)
            {
                mapping[asset.name.ToLower()] = asset;
            }
        }
        Debug.Log($"[AutoConfigSnap] {mapping.Count} scriptables " +
            "objetInventaire trouvés dans le projet.");

        // 2. Récupérer tous les pointAncrageTuyau de la scène ouverte
        pointAncrageTuyau[] snaps = Object.FindObjectsByType<pointAncrageTuyau>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        Debug.Log($"[AutoConfigSnap] {snaps.Length} snap_points trouvés " +
            "dans la scène ouverte.");

        // 3. Pour chaque snap, matcher par nom
        int nbAssignes = 0;
        int nbDejaConfig = 0;
        int nbNonTrouves = 0;

        foreach (pointAncrageTuyau snap in snaps)
        {
            if (snap.pieceAttendue != null)
            {
                nbDejaConfig++;
                continue;
            }

            string nomSnap = snap.gameObject.name.ToLower();
            // Nettoyer le nom (retirer espaces, parenthèses, suffixes courants)
            nomSnap = nomSnap.Replace(" ", "").Replace("(", "").Replace(")", "");

            objetInventaire trouve = null;
            // Matching exact d'abord
            if (mapping.ContainsKey(nomSnap))
            {
                trouve = mapping[nomSnap];
            }
            else
            {
                // Matching par contains
                foreach (var kvp in mapping)
                {
                    if (kvp.Key == nomSnap
                        || kvp.Key.Contains(nomSnap)
                        || nomSnap.Contains(kvp.Key))
                    {
                        trouve = kvp.Value;
                        break;
                    }
                }
            }

            if (trouve != null)
            {
                // Utiliser SerializedObject pour modifier proprement
                // (déclenche le dirty flag correctement pour les prefab
                // instances).
                SerializedObject so = new SerializedObject(snap);
                SerializedProperty prop = so.FindProperty("pieceAttendue");
                prop.objectReferenceValue = trouve;
                so.ApplyModifiedProperties();

                EditorUtility.SetDirty(snap);
                if (PrefabUtility.IsPartOfAnyPrefab(snap.gameObject))
                {
                    PrefabUtility.RecordPrefabInstancePropertyModifications(snap);
                }

                nbAssignes++;
                Debug.Log($"[AutoConfigSnap] OK snap '{snap.name}' " +
                    $"→ pieceAttendue = {trouve.name}");
            }
            else
            {
                nbNonTrouves++;
                Debug.LogWarning($"[AutoConfigSnap] Snap '{snap.name}' : " +
                    "aucun scriptable objetInventaire matchant trouvé. " +
                    "À configurer manuellement.");
            }
        }

        // 4. Sauvegarder la scène
        if (nbAssignes > 0)
        {
            EditorSceneManager.MarkSceneDirty(
                EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
        }

        EditorUtility.DisplayDialog(
            "Auto-config snap_points terminé",
            $"{nbAssignes} snap_point(s) configuré(s)\n" +
            $"{nbDejaConfig} déjà configuré(s) (skip)\n" +
            $"{nbNonTrouves} non trouvé(s) (à configurer manuellement)\n\n" +
            $"⚠️ N'oublie pas de SAUVEGARDER la scène (Cmd+S).",
            "OK");
    }
}
#endif
