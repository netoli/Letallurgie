// ============================================================
// GenerateurIconesObjetInventaire.cs
// ------------------------------------------------------------
// Outil EDITOR (pas de runtime) : régénère automatiquement les
// sprites 'icone' de tous les scriptables 'objetInventaire' à
// partir de leur prefabModele3D. Utilise AssetPreview d'Unity
// qui produit la MÊME vue 3D que celle affichée dans le Project
// view (orientation 3/4 par défaut).
//
// USAGE :
//   1. Menu Unity : Tools → Régénérer icônes objetInventaire
//   2. Attendre quelques secondes (peut prendre 30s pour 20+ objets)
//   3. Les sprites des scriptables sont remplacés par des PNG
//      générés dans Assets/ui/icones_generees/
//
// AVANTAGES :
//   - Plus de confusion sprite 2D vs mesh 3D : l'icône EST le mesh
//   - Coût runtime = 0 (les icônes restent des sprites 2D classiques
//     utilisés par slotObjetInventaire.cs sans modification)
//   - Régénérable à volonté quand tu changes un prefab
//
// LIMITES :
//   - L'orientation d'AssetPreview est fixée (vue 3/4 standard)
//     Si tu veux une autre vue, il faudra une caméra custom
//     (à demander si besoin)
//   - Unity peut prendre 1-2s pour générer chaque preview la
//     première fois. Si une preview est null, le script ré-essaie.
// ============================================================
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class GenerateurIconesObjetInventaire
{
    private const string DOSSIER_SORTIE = "Assets/ui/icones_generees";
    private const int TAILLE_PREVIEW = 256; // taille du PNG généré

    [MenuItem("Tools/Régénérer icônes objetInventaire (3D)")]
    public static void RegenererToutes()
    {
        // 1. Chercher tous les scriptables objetInventaire
        string[] guids = AssetDatabase.FindAssets("t:objetInventaire");
        if (guids.Length == 0)
        {
            EditorUtility.DisplayDialog("Aucun objetInventaire",
                "Aucun scriptable objetInventaire trouvé dans le projet.",
                "OK");
            return;
        }

        if (!Directory.Exists(DOSSIER_SORTIE))
            Directory.CreateDirectory(DOSSIER_SORTIE);

        int nbReussis = 0;
        int nbEchecs = 0;

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            objetInventaire asset = AssetDatabase.LoadAssetAtPath<objetInventaire>(path);

            EditorUtility.DisplayProgressBar(
                "Régénération icônes 3D",
                $"({i+1}/{guids.Length}) {asset?.name ?? "?"}",
                (float)i / guids.Length);

            if (asset == null)
            {
                nbEchecs++;
                continue;
            }
            if (asset.prefabModele3D == null)
            {
                Debug.LogWarning($"[GenerateurIcones] {asset.name} : " +
                    "prefabModele3D non assigné, skip.");
                nbEchecs++;
                continue;
            }

            // 2. Demander à Unity de générer la preview (asynchrone)
            //    On force le préchargement et on attend qu'elle soit prête.
            int instanceID = asset.prefabModele3D.GetInstanceID();
            AssetPreview.SetPreviewTextureCacheSize(guids.Length + 100);

            Texture2D preview = null;
            int essais = 0;
            const int MAX_ESSAIS = 40; // 40 × 50ms = 2 secondes max

            while (essais < MAX_ESSAIS)
            {
                preview = AssetPreview.GetAssetPreview(asset.prefabModele3D);
                if (preview != null
                    && !AssetPreview.IsLoadingAssetPreview(instanceID))
                    break;
                System.Threading.Thread.Sleep(50);
                essais++;
            }

            if (preview == null)
            {
                Debug.LogWarning($"[GenerateurIcones] {asset.name} : " +
                    $"preview non générée après {MAX_ESSAIS} essais. Skip.");
                nbEchecs++;
                continue;
            }

            // 3. Re-render à TAILLE_PREVIEW (la preview de base fait 128px)
            //    Pour avoir plus de qualité on copie dans une Texture2D de
            //    la bonne taille.
            Texture2D textureReadable = new Texture2D(
                preview.width, preview.height, TextureFormat.RGBA32, false);
            // Copie via RenderTexture pour contourner readability flag
            RenderTexture rt = RenderTexture.GetTemporary(
                preview.width, preview.height, 0,
                RenderTextureFormat.Default, RenderTextureReadWrite.sRGB);
            Graphics.Blit(preview, rt);
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;
            textureReadable.ReadPixels(
                new Rect(0, 0, preview.width, preview.height), 0, 0);
            textureReadable.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);

            // 4. Sauvegarder le PNG
            string pngPath = $"{DOSSIER_SORTIE}/{asset.name}_preview3D.png";
            File.WriteAllBytes(pngPath, textureReadable.EncodeToPNG());
            Object.DestroyImmediate(textureReadable);
            AssetDatabase.ImportAsset(pngPath, ImportAssetOptions.ForceUpdate);

            // 5. Configurer le PNG comme Sprite UI (pas Texture2D)
            TextureImporter importer = AssetImporter.GetAtPath(pngPath)
                as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }

            // 6. Assigner le sprite au scriptable
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);
            if (sprite != null)
            {
                asset.icone = sprite;
                EditorUtility.SetDirty(asset);
                nbReussis++;
                Debug.Log($"[GenerateurIcones] OK {asset.name} → " +
                    $"{pngPath}");
            }
            else
            {
                Debug.LogWarning($"[GenerateurIcones] {asset.name} : " +
                    $"sprite null après import. PNG = {pngPath}");
                nbEchecs++;
            }
        }

        EditorUtility.ClearProgressBar();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Régénération terminée",
            $"{nbReussis} icône(s) régénérée(s).\n" +
            $"{nbEchecs} échec(s).\n\n" +
            $"PNG sauvegardés dans :\n{DOSSIER_SORTIE}/",
            "OK");
    }

    [MenuItem("Tools/Régénérer icônes objetInventaire (3D) — SEULEMENT tuyaux")]
    public static void RegenererTuyauxSeulement()
    {
        string[] guids = AssetDatabase.FindAssets("t:objetInventaire");
        if (!Directory.Exists(DOSSIER_SORTIE))
            Directory.CreateDirectory(DOSSIER_SORTIE);

        int nbReussis = 0;
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            // Filtrer seulement les tuyaux (par chemin)
            if (!path.Contains("tuyau") && !path.Contains("Tuyau")) continue;

            objetInventaire asset = AssetDatabase.LoadAssetAtPath<objetInventaire>(path);
            if (asset == null || asset.prefabModele3D == null) continue;

            EditorUtility.DisplayProgressBar(
                "Régénération icônes tuyaux 3D",
                asset.name,
                (float)i / guids.Length);

            Texture2D preview = null;
            int essais = 0;
            while (essais < 40)
            {
                preview = AssetPreview.GetAssetPreview(asset.prefabModele3D);
                if (preview != null
                    && !AssetPreview.IsLoadingAssetPreview(
                        asset.prefabModele3D.GetInstanceID()))
                    break;
                System.Threading.Thread.Sleep(50);
                essais++;
            }

            if (preview == null) continue;

            RenderTexture rt = RenderTexture.GetTemporary(
                preview.width, preview.height);
            Graphics.Blit(preview, rt);
            RenderTexture prevRT = RenderTexture.active;
            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(preview.width, preview.height);
            tex.ReadPixels(
                new Rect(0, 0, preview.width, preview.height), 0, 0);
            tex.Apply();
            RenderTexture.active = prevRT;
            RenderTexture.ReleaseTemporary(rt);

            string pngPath = $"{DOSSIER_SORTIE}/{asset.name}_preview3D.png";
            File.WriteAllBytes(pngPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(pngPath, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = AssetImporter.GetAtPath(pngPath)
                as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);
            if (sprite != null)
            {
                asset.icone = sprite;
                EditorUtility.SetDirty(asset);
                nbReussis++;
            }
        }

        EditorUtility.ClearProgressBar();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Tuyaux régénérés",
            $"{nbReussis} icône(s) de tuyaux régénérée(s).",
            "OK");
    }
}
#endif
