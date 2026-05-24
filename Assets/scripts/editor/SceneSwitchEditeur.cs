using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

[CustomEditor(typeof(MonoBehaviour), true)]
public class SceneSwitchEditeur : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        // On v�rifie le nom de l'objet une seule fois
        if (target.name == "scene_switch_tester")
        {
            if (Application.isPlaying)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Navigation Mode Play", EditorStyles.boldLabel);

                if (GUILayout.Button("Charger Sc�ne Principale"))
                {
                    SceneManager.LoadScene("scene0_tuto");
                }

                if (GUILayout.Button("Charger Sc�ne Taverne1"))
                {
                    SceneManager.LoadScene("scene1_taverne1");
                }

                if (GUILayout.Button("Charger Sc�ne Usine"))
                {
                    SceneManager.LoadScene("scene2_usine");
                }

                if (GUILayout.Button("Charger Sc�ne Taverne2"))
                {
                    SceneManager.LoadScene("scene3_taverne2");
                }

                if (GUILayout.Button("Charger Sc�ne Manoir"))
                {
                    SceneManager.LoadScene("scene4_manoir");
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Lancez le jeu pour utiliser les boutons de navigation.", MessageType.Info);
            }
        }
    }
}