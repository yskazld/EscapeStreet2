// Assets/Editor/IgnoreFolderSelection.cs
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class IgnoreFolderSelection
{
    const string PrefKey = "IgnoreFolderSelection.Enabled";

    static Object previousObject;
    static bool isRestoring;
    static bool enabled;

    static IgnoreFolderSelection()
    {
        enabled = EditorPrefs.GetBool(PrefKey, false);
        UpdateSubscription();
    }

    [MenuItem("Tools/Selection/Ignore Folder Selection %&f", false, 0)]
    static void Toggle()
    {
        enabled = !enabled;
        EditorPrefs.SetBool(PrefKey, enabled);
        UpdateSubscription();
    }

    [MenuItem("Tools/Selection/Ignore Folder Selection %&f", true)]
    static bool ToggleValidate()
    {
        Menu.SetChecked("Tools/Selection/Ignore Folder Selection %&f", enabled);
        return true;
    }

    static void UpdateSubscription()
    {
        Selection.selectionChanged -= OnSelectionChanged;

        if (enabled)
        {
            Selection.selectionChanged += OnSelectionChanged;
        }
    }

    static void OnSelectionChanged()
    {
        if (!enabled || isRestoring)
            return;

        var active = Selection.activeObject;
        if (active == null)
            return;

        string path = AssetDatabase.GetAssetPath(active);
        bool isFolder = !string.IsNullOrEmpty(path) && AssetDatabase.IsValidFolder(path);
        if (isFolder)
        {
            if (previousObject == null)
                return;

            isRestoring = true;
            EditorApplication.delayCall += RestorePreviousSelection;
        }
        else
        {
            previousObject = active;
        }
    }

    static void RestorePreviousSelection()
    {
        Selection.activeObject = previousObject;
        isRestoring = false;
    }
}
