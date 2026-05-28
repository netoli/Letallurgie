// ============================================================
// SimplifierChaineNarrative.cs
// ------------------------------------------------------------
// Outil EDITOR : nettoie les duplications créées par
// CreerChaineNarrativeEnigme :
//   - Supprime les 3 GameObjects 'chaine_banniere_to_pointeur_enigme',
//     'chaine_pointeur_to_tuyaux', 'chaine_victoire_to_pointeur_porte'
//     (remplacés par des gestionActivationAction natifs)
//   - Supprime 'transition_scene3' (doublon de declencheurChangementScene)
//   - Ajoute gestionActivationAction directement sur :
//       * pointeur_enigme (écoute chapitre_enigme_tuyauterie_banniere_terminee)
//       * tuyaux_a_placer (écoute joueur_a_atteint_enigme)
//       * pointeur_porte (écoute enigme_tuyauterie_reussie)
//   - Conserve 'chaine_contact_tavernier_to_banniere' (utile)
//   - Conserve 'surveillance_4_tuyaux' (utile)
//
// USAGE :
//   1. Ouvrir scene2_usine
//   2. Menu : Tools → Simplifier chaîne narrative (enlever doublons)
//   3. Vérifier la console + sauvegarder (Cmd+S)
// ============================================================
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SimplifierChaineNarrative
{
    [MenuItem("Tools/Simplifier chaîne narrative (enlever doublons)")]
    public static void Simplifier()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (!scene.IsValid())
        {
            EditorUtility.DisplayDialog("Aucune scène",
                "Ouvre scene2_usine d'abord.", "OK");
            return;
        }

        int nbSupprimes = 0;
        int nbAjoutes = 0;

        // 1. Supprimer les GameObjects redondants
        string[] aSupprimer = {
            "chaine_banniere_to_pointeur_enigme",
            "chaine_pointeur_to_tuyaux",
            "chaine_victoire_to_pointeur_porte",
            "transition_scene3"
        };
        foreach (string nom in aSupprimer)
        {
            GameObject go = GameObject.Find(nom);
            if (go != null)
            {
                Undo.DestroyObjectImmediate(go);
                nbSupprimes++;
                Debug.Log($"[Simplifier] Supprimé : {nom}");
            }
        }

        // 2. Ajouter gestionActivationAction sur les 3 pointeurs
        // 2A) pointeur_enigme (écoute fin de bannière, délai 3s)
        GameObject pe = TrouverGOParNom("pointeur_enigme")
            ?? TrouverGOParNom("prefab_pointeur_enigme");
        if (pe != null && pe.GetComponent<gestionActivationAction>() == null)
        {
            var c = Undo.AddComponent<gestionActivationAction>(pe);
            ConfigGAA(c,
                "chapitre_enigme_tuyauterie_banniere_terminee",
                pe, 3f);
            nbAjoutes++;
            Debug.Log($"[Simplifier] gestionActivationAction ajouté sur {pe.name}");
        }

        // 2B) tuyaux_a_placer (écoute joueur_a_atteint_enigme, délai 1s)
        GameObject tap = TrouverGOParNom("tuyaux_a_placer");
        if (tap != null && tap.GetComponent<gestionActivationAction>() == null)
        {
            var c = Undo.AddComponent<gestionActivationAction>(tap);
            ConfigGAA(c, "joueur_a_atteint_enigme", tap, 1f);
            nbAjoutes++;
            Debug.Log($"[Simplifier] gestionActivationAction ajouté sur {tap.name}");
        }

        // 2C) pointeur_porte (écoute enigme_tuyauterie_reussie, délai 3s)
        GameObject pp = TrouverGOParNom("pointeur_porte_sortie")
            ?? TrouverGOParNom("pointeur_porte")
            ?? TrouverGOParNom("prefab_pointeur_porte");
        if (pp != null && pp.GetComponent<gestionActivationAction>() == null)
        {
            var c = Undo.AddComponent<gestionActivationAction>(pp);
            ConfigGAA(c, "enigme_tuyauterie_reussie", pp, 3f);
            nbAjoutes++;
            Debug.Log($"[Simplifier] gestionActivationAction ajouté sur {pp.name}");
        }

        EditorSceneManager.MarkSceneDirty(scene);

        EditorUtility.DisplayDialog(
            "Simplification terminée",
            $"{nbSupprimes} GameObjects redondants supprimés.\n" +
            $"{nbAjoutes} gestionActivationAction natifs ajoutés.\n\n" +
            "Restant : chaine_contact_tavernier_to_banniere + " +
            "surveillance_4_tuyaux (utiles, conservés).\n\n" +
            "⚠️ N'oublie pas Cmd+S pour sauvegarder la scène.",
            "OK");
    }

    private static GameObject TrouverGOParNom(string nom)
    {
        var all = Object.FindObjectsByType<Transform>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var t in all)
            if (t.name == nom) return t.gameObject;
        return null;
    }

    private static void ConfigGAA(gestionActivationAction c,
        string idActionActivation, GameObject objetAActiver, float delai)
    {
        SerializedObject so = new SerializedObject(c);
        so.FindProperty("idActionActivation").stringValue = idActionActivation;
        so.FindProperty("objetAActiver").objectReferenceValue = objetAActiver;
        so.FindProperty("delaiAvantActivation").floatValue = delai;
        so.ApplyModifiedProperties();
    }
}
#endif
