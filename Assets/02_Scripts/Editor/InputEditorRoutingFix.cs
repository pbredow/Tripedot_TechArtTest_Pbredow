#if UNITY_EDITOR && ENABLE_INPUT_SYSTEM
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Ensures Input System "Play Mode Input Behavior" is set to
/// "All Device Input Always Goes To Game View" so clicks work
/// in Game view (not only in the Device Simulator).
/// Runs once on editor load.
/// </summary>
[InitializeOnLoad]
internal static class InputEditorRoutingFix
{
    static InputEditorRoutingFix()
    {
        EditorApplication.delayCall += Apply;
    }

    private static void Apply()
    {
        var settings = InputSystem.settings;
        if (settings == null)
        {
            // Try to load the project asset directly.
            var guids = AssetDatabase.FindAssets("t:InputSettings");
            if (guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                settings = AssetDatabase.LoadAssetAtPath<InputSettings>(path);
                if (settings != null)
                    InputSystem.settings = settings;
            }
        }

        if (settings == null)
            return;

        const InputSettings.EditorInputBehaviorInPlayMode desired =
            InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;

        if (settings.editorInputBehaviorInPlayMode == desired)
            return;

        settings.editorInputBehaviorInPlayMode = desired;
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssetIfDirty(settings);

        Debug.Log(
            "[InputEditorRoutingFix] Set Play Mode Input Behavior to " +
            "'All Device Input Always Goes To Game View'.");
    }
}
#endif
