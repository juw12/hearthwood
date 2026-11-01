using UnityEngine;

namespace Hearthwood.Core
{
    /// <summary>
    /// Scene metadata. The SceneController uses this to load scenes by id (not by stringly-typed
    /// path) and to define additive scene groups (e.g. a "village" scene + "tavern_interior" additive).
    /// </summary>
    [CreateAssetMenu(fileName = "Scene_", menuName = "Hearthwood/Scenes/Scene", order = 500)]
    public sealed class SceneData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField, Tooltip("Stable id. NEVER change after release.")]
        private string _id = "scene_";

        [SerializeField, Tooltip("Friendly name shown in save data UI.")]
        private string _displayName = "Scene";

        [Header("Build")]
        [SerializeField, Tooltip("Scene asset path under Assets/. Used to find the scene at runtime via SceneDatabase.")]
        private string _scenePath = "Assets/Scenes/";

        [SerializeField, Tooltip("If set, this scene's name in the build settings (Build Settings ▸ Scene name).")]
        private string _buildName = "";

        [SerializeField, Tooltip("Additive scenes loaded alongside this one. Empty = single load.")]
        private string[] _additiveSceneBuildNames = System.Array.Empty<string>();

        [SerializeField, Tooltip("Default spawn point id for the player. Empty = first spawn.")]
        private string _defaultSpawnPointId = "";

        public string Id => _id;
        public string DisplayName => _displayName;
        public string ScenePath => _scenePath;
        public string BuildName => _buildName;
        public string[] AdditiveSceneBuildNames => _additiveSceneBuildNames;
        public string DefaultSpawnPointId => _defaultSpawnPointId;
    }
}
