// ============================================================
// CreerChaineNarrativeEnigme.cs
// ------------------------------------------------------------
// Outil EDITOR : crée automatiquement les 5 GameObjects de la
// chaîne narrative énigme tuyauterie dans la scène ouverte, avec
// tous leurs composants configurés et leurs refs auto-trouvées.
//
// USAGE :
//   1. Ouvrir scene2_usine dans Unity
//   2. Menu : Tools → Créer chaîne narrative énigme (scene2)
//   3. Les 5 GameObjects sont créés sous "chaine_narrative_enigme"
//   4. Vérifier la console pour les warnings (refs non trouvées)
//   5. Drag-drop manuellement les refs manquantes si besoin
//
// COMPOSANTS CRÉÉS :
//   1. chaine_contact_tavernier_to_banniere :
//      tavernier_trouve → +3s → démarre chapitre 'enigme_tuyauterie'
//   2. chaine_banniere_to_pointeur_enigme :
//      chapitre_enigme_tuyauterie_banniere_terminee → +3s
//      → active pointeur_enigme
//   3. chaine_pointeur_to_tuyaux :
//      joueur_a_touche_pointeur_enigme → +1s
//      → active tuyaux_a_placer
//   4. chaine_victoire_to_pointeur_porte :
//      enigme_tuyauterie_reussie → +3s → active pointeur_porte_sortie
//   5. transition_scene3 :
//      joueur_va_a_scene3 → +1s → charge scene3_taverne2
//   6. surveillance_4_tuyaux :
//      surveillanceInventaire qui signale joueur_a_4_tuyaux
//
// CE SCRIPT NE FAIT PAS :
//   - Créer les DonneesBandeauInfo scriptables (à faire à la main)
//   - Câbler l'UnityEvent onVictoire → SignalerAction
//   - Modifier la scène. Tu dois sauvegarder (Cmd+S) après exécution.
// ============================================================
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CreerChaineNarrativeEnigme
{
    [MenuItem("Tools/Créer chaîne narrative énigme (scene2)")]
    public static void Creer()
    {
        // Vérifier que la scène est ouverte
        var scene = EditorSceneManager.GetActiveScene();
        if (!scene.IsValid())
        {
            EditorUtility.DisplayDialog("Aucune scène",
                "Ouvre scene2_usine d'abord.", "OK");
            return;
        }

        // Auto-find les refs nécessaires
        GameObject pointeurEnigme = TrouverGameObjectParNom("pointeur_enigme")
            ?? TrouverGameObjectParNom("prefab_pointeur_enigme");
        GameObject tuyauxAPlacer = TrouverGameObjectParNom("tuyaux_a_placer");
        GameObject pointeurPorte = TrouverGameObjectParNom("pointeur_porte_sortie")
            ?? TrouverGameObjectParNom("pointeur_porte")
            ?? TrouverGameObjectParNom("prefab_pointeur_porte");

        if (pointeurEnigme == null)
            Debug.LogWarning("[CreerChaineNarrative] pointeur_enigme " +
                "introuvable dans la scène — drag-drop manuellement après.");
        if (tuyauxAPlacer == null)
            Debug.LogWarning("[CreerChaineNarrative] tuyaux_a_placer " +
                "introuvable — drag-drop manuellement après.");
        if (pointeurPorte == null)
            Debug.LogWarning("[CreerChaineNarrative] pointeur_porte_sortie " +
                "introuvable — drag-drop manuellement après.");

        // Créer parent commun
        GameObject parent = GameObject.Find("chaine_narrative_enigme");
        if (parent == null)
        {
            parent = new GameObject("chaine_narrative_enigme");
            Undo.RegisterCreatedObjectUndo(parent, "Création chaîne narrative");
        }

        // 1. Contact tavernier → bannière chapitre
        var chaine1 = CreerGOAvecChaine(parent,
            "chaine_contact_tavernier_to_banniere",
            idActionEcoutee: "tavernier_trouve",
            delai: 3f,
            idChapitreADemarrer: "enigme_tuyauterie",
            objetAActiver: null);

        // 2. Fin bannière → pointeur énigme
        var chaine2 = CreerGOAvecChaine(parent,
            "chaine_banniere_to_pointeur_enigme",
            idActionEcoutee: "chapitre_enigme_tuyauterie_banniere_terminee",
            delai: 3f,
            idChapitreADemarrer: null,
            objetAActiver: pointeurEnigme);

        // 3. Contact pointeur → tuyaux apparaissent
        var chaine3 = CreerGOAvecChaine(parent,
            "chaine_pointeur_to_tuyaux",
            idActionEcoutee: "joueur_a_touche_pointeur_enigme",
            delai: 1f,
            idChapitreADemarrer: null,
            objetAActiver: tuyauxAPlacer);

        // 4. Victoire → pointeur porte
        var chaine4 = CreerGOAvecChaine(parent,
            "chaine_victoire_to_pointeur_porte",
            idActionEcoutee: "enigme_tuyauterie_reussie",
            delai: 3f,
            idChapitreADemarrer: null,
            objetAActiver: pointeurPorte);

        // 5. (Transition scene3 retirée — utiliser declencheurChangementScene
        //    qui existe déjà dans le projet, sur le pointeur_porte_sortie.)

        // 6. Surveillance 4 tuyaux
        GameObject goSurv = GameObject.Find("surveillance_4_tuyaux");
        if (goSurv == null)
        {
            goSurv = new GameObject("surveillance_4_tuyaux");
            goSurv.transform.SetParent(parent.transform);
            Undo.RegisterCreatedObjectUndo(goSurv, "Création surveillance");
            var surv = goSurv.AddComponent<surveillanceInventaire>();
            SerializedObject soS = new SerializedObject(surv);
            soS.FindProperty("categorieCible").enumValueIndex =
                (int)CategorieObjet.Tuyaux;
            soS.FindProperty("seuil").intValue = 4;
            soS.FindProperty("idActionAtteindreSeuil").stringValue =
                "joueur_a_4_tuyaux";
            soS.ApplyModifiedProperties();
        }

        EditorSceneManager.MarkSceneDirty(scene);

        int nbRefsManquantes = 0;
        if (pointeurEnigme == null) nbRefsManquantes++;
        if (tuyauxAPlacer == null) nbRefsManquantes++;
        if (pointeurPorte == null) nbRefsManquantes++;

        string msg = "✅ 5 GameObjects créés sous 'chaine_narrative_enigme' + " +
            "1 surveillance_4_tuyaux.\n\n";
        if (nbRefsManquantes > 0)
            msg += $"⚠️ {nbRefsManquantes} ref(s) auto-find non trouvée(s). " +
                "Vérifie les warnings dans la Console et drag-drop " +
                "manuellement dans l'Inspector des chaînes concernées.\n\n";
        msg += "À FAIRE MANUELLEMENT :\n" +
            "1. Créer 2 DonneesBandeauInfo (cherche tuyaux, tu peux placer)\n" +
            "2. Sur gestionEnigmeTuyauterie.onVictoire (UnityEvent) :\n" +
            "   ajouter call gestionChapitres.SignalerAction('enigme_tuyauterie_reussie')\n" +
            "3. Cmd+S pour sauvegarder la scène";
        EditorUtility.DisplayDialog("Chaîne narrative créée", msg, "OK");
    }

    private static GameObject CreerGOAvecChaine(
        GameObject parent,
        string nom,
        string idActionEcoutee,
        float delai,
        string idChapitreADemarrer,
        GameObject objetAActiver)
    {
        // Skip si déjà existant
        Transform existing = parent.transform.Find(nom);
        if (existing != null)
        {
            Debug.Log($"[CreerChaineNarrative] {nom} existe déjà, skip.");
            return existing.gameObject;
        }

        GameObject go = new GameObject(nom);
        go.transform.SetParent(parent.transform);
        Undo.RegisterCreatedObjectUndo(go, "Création " + nom);

        var comp = go.AddComponent<chaineActionsAvecDelai>();
        SerializedObject so = new SerializedObject(comp);
        so.FindProperty("idActionEcoutee").stringValue = idActionEcoutee;
        so.FindProperty("delaiAvantSuite").floatValue = delai;
        if (!string.IsNullOrEmpty(idChapitreADemarrer))
            so.FindProperty("idChapitreADemarrer").stringValue =
                idChapitreADemarrer;
        if (objetAActiver != null)
            so.FindProperty("objetAActiver").objectReferenceValue =
                objetAActiver;
        so.ApplyModifiedProperties();

        return go;
    }

    private static GameObject TrouverGameObjectParNom(string nom)
    {
        var allTransforms = Object.FindObjectsByType<Transform>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var t in allTransforms)
        {
            if (t.gameObject.name == nom)
                return t.gameObject;
        }
        return null;
    }
}
#endif
