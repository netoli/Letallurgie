using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

[CustomEditor(typeof(MonoBehaviour), true)]
public class SceneSwitchEditeur : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        // On vérifie le nom de l'objet une seule fois
        if (target.name == "scene_switch_tester")
        {
            if (Application.isPlaying)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Navigation Mode Play", EditorStyles.boldLabel);

                if (GUILayout.Button("Charger Scène Principale"))
                {
                    SceneManager.LoadScene("scene_taverne_tutoriel");
                }

                if (GUILayout.Button("Charger Scène Taverne1"))
                {
                    SceneManager.LoadScene("scene_taverne_recherche_indices");
                }

                if (GUILayout.Button("Charger Scène Usine"))
                {
                    SceneManager.LoadScene("SCENE2-Usine");
                }

                if (GUILayout.Button("Charger Scène Taverne2"))
                {
                    SceneManager.LoadScene("SCENE3-Taverne2");
                }

                if (GUILayout.Button("Charger Scène Manoir"))
                {
                    SceneManager.LoadScene("SCENE4-Manoir");
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Lancez le jeu pour utiliser les boutons de navigation.", MessageType.Info);
            }
        }
    }
}