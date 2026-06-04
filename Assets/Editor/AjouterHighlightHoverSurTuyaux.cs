// ============================================================
// AjouterHighlightHoverSurTuyaux.cs
// ------------------------------------------------------------
// Outil EDITOR : ajoute le composant gestionHighlightHover à
// tous les prefabs de tuyaux à ramasser qui n'en ont pas, et
// auto-link _particulesHighlight / _lumiereHighlight si des
// enfants nommés correctement existent.
//
// USAGE :
//   Menu : Tools → Ajouter HighlightHover aux tuyaux
// ============================================================
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

public static class AjouterHighlightHoverSurTuyaux
{
    private static readonly string[] DossiersACibler = new[]
    {
        "Assets/prefabs/enigmes/enigme_Tuyaux/place_holders/bons_tuyaux",
        "Assets/prefabs/enigmes/enigme_Tuyaux/place_holders/tuyaux_leurres",
    };

    [MenuItem("Tools/Ajouter HighlightHover aux tuyaux")]
    public static void Executer()
    {
        int nbAjoutes = 0, nbDejaOK = 0, nbWarnings = 0;

        foreach (var dossier in DossiersACibler)
        {
            if (!Directory.Exists(dossier)) continue;
            var prefabs = Directory.GetFiles(dossier, "*.prefab",
                SearchOption.AllDirectories);
            foreach (var path in prefabs)
            {
                GameObject racine = PrefabUtility.LoadPrefabContents(path);
                if (racine == null) continue;

                var existant = racine.GetComponent<gestionHighlightHover>();
                if (existant != null)
                {
                    nbDejaOK++;
                    PrefabUtility.UnloadPrefabContents(racine);
                    continue;
                }

                var comp = racine.AddComponent<gestionHighlightHover>();

                // Auto-link enfants typiques
                var particules = TrouverEnfantNomme(racine,
                    "highlight_particules");
                if (particules == null)
                    particules = TrouverEnfantNomme(racine, "HighlightObjet");

                var lumiere = TrouverLumiereDescendante(racine);

                if (particules != null)
                {
                    var ps = particules.GetComponent<ParticleSystem>();
                    if (ps != null)
                    {
                        var so = new SerializedObject(comp);
                        so.FindProperty("_particulesHighlight")
                            .objectReferenceValue = ps;
                        so.ApplyModifiedProperties();
                    }
                    else
                    {
                        nbWarnings++;
                        Debug.LogWarning($"[Outil] {Path.GetFileName(path)} : " +
                            "enfant 'highlight_particules' trouvé mais " +
                            "pas de ParticleSystem dessus.");
                    }
                }

                if (lumiere != null)
                {
                    var so = new SerializedObject(comp);
                    so.FindProperty("_lumiereHighlight")
                        .objectReferenceValue = lumiere;
                    so.ApplyModifiedProperties();
                }
                else
                {
                    nbWarnings++;
                    Debug.LogWarning($"[Outil] {Path.GetFileName(path)} : " +
                        "aucune Light descendante trouvée. Ajoute-en une " +
                        "manuellement (Point Light sur 'HighlightObjet').");
                }

                PrefabUtility.SaveAsPrefabAsset(racine, path);
                PrefabUtility.UnloadPrefabContents(racine);
                nbAjoutes++;
                Debug.Log($"[Outil] {Path.GetFileName(path)} : " +
                    "gestionHighlightHover ajouté.");
            }
        }

        // PAS d'AssetDatabase.SaveAssets/Refresh : PrefabUtility.SaveAsPrefabAsset
        // marque déjà les assets comme modifiés et Unity les sauvegarde
        // automatiquement à la prochaine compilation. Forcer SaveAssets+Refresh
        // peut interagir avec des AssetPostprocessor existants et créer une
        // boucle d'import.

        string msg = $"✅ {nbAjoutes} prefab(s) modifié(s).\n" +
            $"✓ {nbDejaOK} prefab(s) avaient déjà le composant.\n" +
            $"⚠️ {nbWarnings} warning(s) (vérifier la Console).";
        EditorUtility.DisplayDialog("HighlightHover ajouté", msg, "OK");
    }

    private static GameObject TrouverEnfantNomme(GameObject racine,
        string nom)
    {
        var transforms = racine.GetComponentsInChildren<Transform>(true);
        foreach (var t in transforms)
        {
            if (t.gameObject.name == nom)
                return t.gameObject;
        }
        return null;
    }

    private static Light TrouverLumiereDescendante(GameObject racine)
    {
        return racine.GetComponentInChildren<Light>(true);
    }
}
#endif
