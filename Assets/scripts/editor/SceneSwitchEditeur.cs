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
                    SceneManager.LoadScene("SCENE0-Menu-Tuto");
                }

                if (GUILayout.Button("Charger Sc�ne Taverne1"))
                {
                    SceneManager.LoadScene("SCENE1-Taverne1");
                }

                if (GUILayout.Button("Charger Sc�ne Usine"))
                {
                    SceneManager.LoadScene("SCENE2-Usine");
                }

                if (GUILayout.Button("Charger Sc�ne Taverne2"))
                {
                    SceneManager.LoadScene("SCENE3-Taverne2");
                }

                if (GUILayout.Button("Charger Sc�ne Manoir"))
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