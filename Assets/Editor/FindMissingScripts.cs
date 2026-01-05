using UnityEditor;
using UnityEngine;

public class FindMissingScripts : EditorWindow
{
    [MenuItem("Tools/Find Missing Scripts")]
    public static void ShowWindow()
    {
        GetWindow(typeof(FindMissingScripts));
    }

    public void OnGUI()
    {
        if (GUILayout.Button("Find Missing Scripts in Active Scene"))
        {
            FindInScene();
        }
    }

    private static void FindInScene()
    {
        GameObject[] gameObjects = FindObjectsOfType<GameObject>();
        int missingCount = 0;
        
        Debug.Log("========================================");
        Debug.Log("Searching for missing scripts...");

        foreach (GameObject go in gameObjects)
        {
            Component[] components = go.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] == null)
                {
                    missingCount++;
                    string path = go.name;
                    Transform t = go.transform;
                    while (t.parent != null)
                    {
                        path = t.parent.name + "/" + path;
                        t = t.parent;
                    }
                    Debug.LogWarning($"Missing script found on: '{path}'", go);
                }
            }
        }

        Debug.Log($"Search complete. Found {missingCount} missing scripts.");
        Debug.Log("========================================");
    }
}
