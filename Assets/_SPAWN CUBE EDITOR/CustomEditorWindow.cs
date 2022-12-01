using UnityEditor;
using UnityEngine;

public class CustomEditorWindow : EditorWindow
{
    [MenuItem("EditorEducation/Custom Window")]
    public static void ShowWindow()
    {
        GetWindow<CustomEditorWindow>("CustomEditorWindow");
    }
    private void OnGUI()
    {
        GUILayout.Label("Cube Spawner", EditorStyles.boldLabel);
        if (GUILayout.Button("Spawn Cube"))
        {
            SpawnCube();
        }
    }

    private void SpawnCube()
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.position = Vector3.zero;
    }
}
