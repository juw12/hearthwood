using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Hearthwood.Core;

namespace Hearthwood.EditorTools
{
    /// <summary>Custom inspector for <see cref="SceneData"/> with scene-locating and build-settings buttons.</summary>
    [CustomEditor(typeof(SceneData))]
    public class SceneDataEditor : Editor
    {
        /// <summary>Render the custom inspector UI for SceneData.</summary>
        public override void OnInspectorGUI()
        {
            var scene = (SceneData)target;
            var so = serializedObject;
            so.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Scene", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            // Identity
            EditorGUILayout.PropertyField(so.FindProperty("_id"));
            EditorGUILayout.PropertyField(so.FindProperty("_displayName"));

            // Build
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Build", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(so.FindProperty("_scenePath"), new GUIContent("Scene Path"));
            EditorGUILayout.PropertyField(so.FindProperty("_buildName"), new GUIContent("Build Name"));
            EditorGUILayout.PropertyField(so.FindProperty("_additiveSceneBuildNames"), new GUIContent("Additive Scene Build Names"));
            EditorGUILayout.PropertyField(so.FindProperty("_defaultSpawnPointId"), new GUIContent("Default Spawn Point Id"));

            so.ApplyIfModified();

            // Action buttons
            EditorGUILayout.Space(8);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Find Scene Asset"))
            {
                FindAndAssignScene(scene);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUI.enabled = !string.IsNullOrEmpty(scene.ScenePath) && !string.IsNullOrEmpty(scene.BuildName);
            if (GUILayout.Button("Add to Build Settings"))
            {
                AddToBuildSettings(scene);
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>Locates the scene asset at the configured path and reports whether it exists.</summary>
        private void FindAndAssignScene(SceneData scene)
        {
            var path = scene.ScenePath;
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning("[SceneDataEditor] Scene path is empty.");
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null)
            {
                Debug.Log($"[SceneDataEditor] Found scene at: {path}");
                EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<SceneAsset>(path));
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
            }
            else
            {
                Debug.LogWarning($"[SceneDataEditor] No scene asset found at: {path}");
            }
        }

        /// <summary>Adds the primary scene and any additive scenes to the EditorBuildSettings list.</summary>
        private void AddToBuildSettings(SceneData scene)
        {
            var primaryPath = scene.ScenePath;
            var primaryName = scene.BuildName;

            if (string.IsNullOrEmpty(primaryPath) || string.IsNullOrEmpty(primaryName))
            {
                Debug.LogWarning("[SceneDataEditor] Both ScenePath and BuildName must be set.");
                return;
            }

            var settings = new List<EditorBuildSettingsScene>();
            foreach (var s in EditorBuildSettings.scenes)
                settings.Add(s);

            // Add primary scene if not already present
            if (!settings.Exists(s => s.path == primaryPath))
            {
                settings.Add(new EditorBuildSettingsScene(primaryPath, true));
            }

            // Add additive scenes if not already present
            var additiveNames = scene.AdditiveSceneBuildNames;
            if (additiveNames != null)
            {
                foreach (var additiveName in additiveNames)
                {
                    if (string.IsNullOrEmpty(additiveName)) continue;

                    // Construct additive path from build name: look in same directory as primary
                    var dir = primaryPath.Substring(0, primaryPath.LastIndexOf('/'));
                    var additivePath = $"{dir}/{additiveName}.unity";

                    if (AssetDatabase.LoadAssetAtPath<SceneAsset>(additivePath) != null &&
                        !settings.Exists(s => s.path == additivePath))
                    {
                        settings.Add(new EditorBuildSettingsScene(additivePath, false));
                    }
                }
            }

            EditorBuildSettings.scenes = settings.ToArray();
            Debug.Log($"[SceneDataEditor] Updated build settings. '{primaryName}' added as enabled scene.");
        }
    }
}
