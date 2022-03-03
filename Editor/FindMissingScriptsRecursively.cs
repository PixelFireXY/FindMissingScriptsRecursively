using UnityEngine;
using UnityEditor;
using System.Linq;
using UnityEditor.SceneManagement;

/// <summary>
/// WARNING: If you have to remove the missing components, and the gameobject in the scene is a prefab,
/// make sure to do it on the asset prefab or unpack the prefab in the scene,
/// otherwise you will lose the changes when you press play.
/// </summary>
public class FindMissingScriptsRecursively : EditorWindow
{
    private static int _goCount;
    private static int _componentsCount;
    private static int _missingCount;
    private static int _removedCount;

    private static bool autoRemove = false;

    private readonly string instructionsText = "WARNING: If you have to remove the missing components, \nand the gameobject in the scene is a prefab, \nmake sure to do it on the asset prefab or unpack the prefab in the scene, \notherwise you will lose the changes when you press play.";

    /******************** Editor ********************/

    [MenuItem("Window/FindMissingScriptsRecursively")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(FindMissingScriptsRecursively));
    }

    /******************** Mono ********************/

    private void OnDestroy() => ResetVariables();

    public void OnGUI()
    {
        autoRemove = GUILayout.Toggle(autoRemove, "Auto remove NULL script");

        GUILayout.Space(10);

        if (GUILayout.Button("Find Missing Scripts in selected GameObjects"))
        {
            FindInSelected();
        }

        GUILayout.Space(10);

        if (_goCount > 0)
        {
            EditorGUILayout.TextField($"GameObjects found: {_goCount}");

            EditorGUILayout.TextField($"Components found: {_componentsCount}");

            EditorGUILayout.TextField($"Missing scripts: {_missingCount}");

            EditorGUILayout.TextField($"Deleted scripts: {_removedCount}");
        }

        GUILayout.Space(100);

        EditorGUILayout.TextArea(instructionsText, EditorStyles.textField, GUILayout.ExpandHeight(true));
    }

    /******************** Components management ********************/

    private static void FindInSelected()
    {
        var go = Selection.gameObjects;

        ResetVariables();

        foreach (var g in go)
        {
            FindInGo(g);
        }

        AssetDatabase.SaveAssets();
    }

    private static void FindInGo(GameObject g)
    {
        _goCount++;

        var components = g.GetComponents<Component>();

        _componentsCount += components.Length;

        int missingComponentsCount = components.Where(x => x == null).Count();
        _missingCount += missingComponentsCount;

        // Create the gameobject path in the project
        var s = g.name;
        var t = g.transform;

        while (t.parent != null)
        {
            s = t.parent.name + "/" + s;
            t = t.parent;
        }

        Debug.Log($"{s} has a missing script", g);

        // If the auto remove is active remove the component
        if (missingComponentsCount > 0 && autoRemove)
        {
            int removedComponents = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(g);

            if (removedComponents <= 0)
                Debug.LogError($"Cannot remove automatically the component at {s}. Please remove it manually.", g);
            else
            {
                _removedCount += removedComponents;

                EditorUtility.SetDirty(g);
            }
        }

        // Find if the children has some missing component
        foreach (Transform childT in g.transform)
        {
            FindInGo(childT.gameObject);
        }
    }

    /******************** Utils ********************/

    private static void ResetVariables()
    {
        _goCount = 0;
        _componentsCount = 0;
        _missingCount = 0;
        _removedCount = 0;
    }
}
