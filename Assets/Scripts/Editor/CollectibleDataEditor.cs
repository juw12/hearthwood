using UnityEditor;
using UnityEngine;
using System.Text;
using Hearthwood.Core;

namespace Hearthwood.EditorTools
{
    /// <summary>Custom inspector for <see cref="CollectibleData"/> with quest-link validation.</summary>
    [CustomEditor(typeof(CollectibleData))]
    public class CollectibleDataEditor : Editor
    {
        private string _validationMessage = "";
        private bool _validationError = false;

        /// <summary>Render the custom inspector UI for CollectibleData.</summary>
        public override void OnInspectorGUI()
        {
            var collectible = (CollectibleData)target;
            var so = serializedObject;
            so.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Collectible", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            // Identity
            EditorGUILayout.PropertyField(so.FindProperty("_id"));
            EditorGUILayout.PropertyField(so.FindProperty("_displayName"));
            EditorGUILayout.PropertyField(so.FindProperty("_collectPrompt"));

            // Reward
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Reward", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(so.FindProperty("_rewardItem"), new GUIContent("Reward Item"));
            EditorGUILayout.PropertyField(so.FindProperty("_rewardCount"), new GUIContent("Reward Count"));

            // Quest Link
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Quest Link", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(so.FindProperty("_questId"), new GUIContent("Quest Id"));
            EditorGUILayout.PropertyField(so.FindProperty("_objectiveId"), new GUIContent("Objective Id"));

            if (collectible.HasQuestLink)
            {
                EditorGUILayout.Space(4);
                if (GUILayout.Button("Validate Quest Link", GUILayout.Height(22)))
                {
                    ValidateQuestLink(collectible);
                }

                if (_validationMessage.Length > 0)
                {
                    EditorGUILayout.Space(2);
                    EditorGUILayout.HelpBox(_validationMessage, _validationError ? MessageType.Error : MessageType.Info);
                }
            }

            // Visuals
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Visuals", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(so.FindProperty("_icon"), new GUIContent("Icon"));

            // Respawn
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Respawn", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(so.FindProperty("_respawnable"), new GUIContent("Respawnable"));
            EditorGUILayout.PropertyField(so.FindProperty("_respawnDelaySeconds"), new GUIContent("Respawn Delay (s)"));

            so.ApplyIfModified();
        }

        /// <summary>Validates that the quest and objective referenced by this collectible actually exist.</summary>
        private void ValidateQuestLink(CollectibleData collectible)
        {
            var sb = new StringBuilder();
            var hasErrors = false;

            var questId = collectible.QuestId;
            var objectiveId = collectible.ObjectiveId;

            // Find the quest
            QuestData quest = null;
            var guids = AssetDatabase.FindAssets("t:QuestData");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var q = AssetDatabase.LoadAssetAtPath<QuestData>(path);
                if (q != null && q.Id == questId)
                {
                    quest = q;
                    break;
                }
            }

            if (quest == null)
            {
                sb.AppendLine($"QuestData with id '{questId}' not found.");
                hasErrors = true;
            }
            else
            {
                // Find the objective within the quest
                var objective = quest.GetObjective(objectiveId);
                if (string.IsNullOrEmpty(objective.ObjectiveId))
                {
                    sb.AppendLine($"Objective id '{objectiveId}' not found in quest '{questId}'.");
                    hasErrors = true;
                }
                else
                {
                    // Verify this collectible's id matches the objective's target
                    if (objective.Kind == ObjectiveKind.CollectItem || objective.Kind == ObjectiveKind.DeliverItem)
                    {
                        if (objective.TargetCollectibleId != collectible.Id)
                        {
                            sb.AppendLine($"Objective '{objectiveId}' expects collectible id '{objective.TargetCollectibleId}' but this collectible is '{collectible.Id}'.");
                            hasErrors = true;
                        }
                    }
                }
            }

            _validationMessage = hasErrors
                ? sb.ToString().TrimEnd()
                : "Quest link validated successfully.";
            _validationError = hasErrors;
        }
    }
}
