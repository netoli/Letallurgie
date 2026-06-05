#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// ============================================================
// outilsPrefsVisuelles.cs (EDITEUR seulement)
// ------------------------------------------------------------
// Menu "Letallurgie" dans la barre Unity : purge les preferences
// joueur enregistrees par les tests. Tant que ces prefs existent,
// le jeu les applique au lancement (comportement normal d'un menu
// d'options) — ce qui, en phase de design, donne l'impression que
// "les valeurs de l'Inspector changent toutes seules".
// ============================================================

public static class outilsPrefsVisuelles
{
    [MenuItem("Letallurgie/Purger les prefs VISUELLES " +
        "(luminosite, vignette, brouillard)")]
    private static void PurgerVisuelles()
    {
        PlayerPrefs.DeleteKey("luminosite");
        PlayerPrefs.DeleteKey("intensiteVignette");
        PlayerPrefs.DeleteKey("intensiteBrouillard");
        PlayerPrefs.Save();
        Debug.Log("[outilsPrefsVisuelles] Prefs visuelles purgees : "
            + "le jeu repart sur les valeurs Inspector du profil.");
    }

    [MenuItem("Letallurgie/Purger TOUTES les prefs du jeu " +
        "(options completes — pas les sauvegardes)")]
    private static void PurgerToutes()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("[outilsPrefsVisuelles] TOUTES les prefs purgees "
            + "(volumes, tailles de texte, controles...). Les "
            + "sauvegardes (fichiers) ne sont pas touchees.");
    }
}
#endif
