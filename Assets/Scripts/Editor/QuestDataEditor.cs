using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Text;
using Hearthwood.Core;

namespace Hearthwood.EditorTools
{
    /// <summary>Custom inspector for <see cref="QuestData"/> with objective editing and cross-quest validation.</summary>
    [CustomEditor(typeof(QuestData))]
    public class QuestDataEditor : Editor
    {
        private bool _showObjectives = true;
        private bool _showRewards = true;
        private string _validationMessage = "";
        private bool _validationError = false;

        /// <summary>Render the custom inspector UI for QuestData.</summary>
        public override void OnInspectorGUI()
        {
            var quest = (QuestData)target;
            var so = serializedObject;
            so.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Quest", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            // Identity
            EditorGUILayout.PropertyField(so.FindProperty("_id"));
            EditorGUILayout.PropertyField(so.FindProperty("_displayName"));
            EditorGUILayout.PropertyField(so.FindProperty("_description"));

            // Gating
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Gating", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(so.FindProperty("_factionRequirement"));
            EditorGUILayout.PropertyField(so.FindProperty("_prerequisiteQuestIds"));

            // Objectives
            EditorGUILayout.Space();
            _showObjectives = EditorGUILayout.Foldout(_showObjectives, "Objectives", true);
            if (_showObjectives)
            {
                EditorGUI.indentLevel++;
                var objectivesProp = so.FindProperty("_objectives");
                EditorGUILayout.PropertyField(objectivesProp.FindPropertyRelative("Array.size"));

                for (int i = 0; i < objectivesProp.arraySize; i++)
                {
                    var obj = objectivesProp.GetArrayElementAtIndex(i);
                    EditorGUILayout.Space(2);
                    EditorGUILayout.LabelField($"Objective {i + 1}", EditorStyles.miniBoldLabel);

                    EditorGUILayout.PropertyField(obj.FindPropertyRelative("ObjectiveId"));
                    EditorGUILayout.PropertyField(obj.FindPropertyRelative("Description"));

                    var kindProp = obj.FindPropertyRelative("Kind");
                    EditorGUILayout.PropertyField(kindProp);

                    var kind = (ObjectiveKind)kindProp.enumValueIndex;

                    switch (kind)
                    {
                        case ObjectiveKind.CollectItem:
                            DrawCollectiblePicker(obj.FindPropertyRelative("TargetCollectibleId"), "Target Collectible");
                            break;
                        case ObjectiveKind.TalkToNpc:
                            DrawNpcPicker(obj.FindPropertyRelative("TargetNpcId"), "Target NPC");
                            break;
                        case ObjectiveKind.ReachArea:
                            EditorGUILayout.PropertyField(obj.FindPropertyRelative("TargetAreaId"), new GUIContent("Target Area Id"));
                            break;
                        case ObjectiveKind.KillEnemy:
                            EditorGUILayout.PropertyField(obj.FindPropertyRelative("TargetEnemyTag"), new GUIContent("Enemy Tag"));
                            break;
                        case ObjectiveKind.DeliverItem:
                            DrawCollectiblePicker(obj.FindPropertyRelative("TargetCollectibleId"), "Deliver Collectible");
                            break;
                        default:
                            // Custom — no specific target field
                            break;
                    }

                    EditorGUILayout.PropertyField(obj.FindPropertyRelative("TargetCount"), new GUIContent("Target Count"));
                }
                EditorGUI.indentLevel--;
            }

            // Rewards
            EditorGUILayout.Space();
            _showRewards = EditorGUILayout.Foldout(_showRewards, "Rewards", true);
            if (_showRewards)
            {
                EditorGUI.indentLevel++;
                var rewardsProp = so.FindProperty("_rewards");
                EditorGUILayout.PropertyField(rewardsProp.FindPropertyRelative("Array.size"));

                for (int i = 0; i < rewardsProp.arraySize; i++)
                {
                    var reward = rewardsProp.GetArrayElementAtIndex(i);
                    EditorGUILayout.Space(2);
                    EditorGUILayout.PropertyField(reward.FindPropertyRelative("FavorTokens"), new GUIContent("Favor Tokens"));
                    EditorGUILayout.PropertyField(reward.FindPropertyRelative("ItemId"), new GUIContent("Item Id"));
                    EditorGUILayout.PropertyField(reward.FindPropertyRelative("ItemCount"), new GUIContent("Item Count"));
                    EditorGUILayout.PropertyField(reward.FindPropertyRelative("UnlocksQuestId"), new GUIContent("Unlocks Quest Id"));
                }
                EditorGUI.indentLevel--;
            }

            so.ApplyIfModified();

            // Validate button
            EditorGUILayout.Space(8);
            if (GUILayout.Button("Validate Quest", GUILayout.Height(24)))
            {
                ValidateQuest(quest);
            }

            if (_validationMessage.Length > 0)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.HelpBox(_validationMessage, _validationError ? MessageType.Error : MessageType.Info);
            }
        }

        /// <summary>Draws a CollectibleData object picker backed by a string id SerializedProperty.</summary>
        private void DrawCollectiblePicker(SerializedProperty stringProp, string label)
        {
            var currentId = stringProp.stringValue;
            var currentAsset = FindAssetById<CollectibleData>(currentId);
            var newAsset = (CollectibleData)EditorGUILayout.ObjectField(label, currentAsset, typeof(CollectibleData), false);
            if (newAsset != currentAsset)
            {
                stringProp.stringValue = newAsset != null ? newAsset.Id : "";
                stringProp.serializedObject.ApplyModifiedProperties();
            }
        }

        /// <summary>Draws an NPCData object picker backed by a string id SerializedProperty.</summary>
        private void DrawNpcPicker(SerializedProperty stringProp, string label)
        {
            var currentId = stringProp.stringValue;
            var currentAsset = FindAssetById<NPCData>(currentId);
            var newAsset = (NPCData)EditorGUILayout.ObjectField(label, currentAsset, typeof(NPCData), false);
            if (newAsset != currentAsset)
            {
                stringProp.stringValue = newAsset != null ? newAsset.Id : "";
                stringProp.serializedObject.ApplyModifiedProperties();
            }
        }

        /// <summary>Finds an asset of the given ScriptableObject type by its Id field.</summary>
        private T FindAssetById<T>(string id) where T : ScriptableObject
        {
            if (string.IsNullOrEmpty(id)) return null;
            var type = typeof(T).Name;
            var guids = AssetDatabase.FindAssets($"t:{type}");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null && asset.Id == id)
                    return asset;
            }
            return null;
        }

        /// <summary>Validates prerequisite cycles and missing references across all QuestData assets.</summary>
        private void ValidateQuest(QuestData quest)
        {
            var sb = new StringBuilder();
            var hasErrors = false;

            // Gather all quest data
            var allQuests = new Dictionary<string, QuestData>();
            var guids = AssetDatabase.FindAssets("t:QuestData");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var q = AssetDatabase.LoadAssetAtPath<QuestData>(path);
                if (q != null)
                    allQuests[q.Id] = q;
            }

            // Check prerequisite cycles using DFS
            var visiting = new HashSet<string>();
            var visited = new HashSet<string>();

            bool HasCycle(string questId)
            {
                if (visiting.Contains(questId)) return true;
                if (visited.Contains(questId)) return false;
                visiting.Add(questId);
                if (allQuests.TryGetValue(questId, out var q))
                {
                    foreach (var prereq in q.PrerequisiteQuestIds)
                    {
                        if (HasCycle(prereq)) return true;
                    }
                }
                visiting.Remove(questId);
                visited.Add(questId);
                return false;
            }

            foreach (var id in allQuests.Keys)
            {
                if (HasCycle(id))
                {
                    sb.AppendLine($"Cycle detected involving quest '{id}'.");
                    hasErrors = true;
                    break;
                }
            }

            // Check this quest's prerequisites exist
            if (quest.PrerequisiteQuestIds != null)
            {
                foreach (var prereqId in quest.PrerequisiteQuestIds)
                {
                    if (!allQuests.ContainsKey(prereqId))
                    {
                        sb.AppendLine($"Missing prerequisite quest: '{prereqId}' not found.");
                        hasErrors = true;
                    }
                }
            }

            // Check objective target references
            if (quest.Objectives != null)
            {
                var collectibleIds = new HashSet<string>();
                var npcIds = new HashSet<string>();
                GatherAssetIds<CollectibleData>(collectibleIds);
                GatherAssetIds<NPCData>(npcIds);

                foreach (var obj in quest.Objectives)
                {
                    switch (obj.Kind)
                    {
                        case ObjectiveKind.CollectItem:
                        case ObjectiveKind.DeliverItem:
                            if (!string.IsNullOrEmpty(obj.TargetCollectibleId) && !collectibleIds.Contains(obj.TargetCollectibleId))
                            {
                                sb.AppendLine($"Objective '{obj.ObjectiveId}': CollectibleData with id '{obj.TargetCollectibleId}' not found.");
                                hasErrors = true;
                            }
                            break;
                        case ObjectiveKind.TalkToNpc:
                            if (!string.IsNullOrEmpty(obj.TargetNpcId) && !npcIds.Contains(obj.TargetNpcId))
                            {
                                sb.AppendLine($"Objective '{obj.ObjectiveId}': NPCData with id '{obj.TargetNpcId}' not found.");
                                hasErrors = true;
                            }
                            break;
                    }
                }
            }

            _validationMessage = hasErrors
                ? sb.ToString().TrimEnd()
                : "Quest validation passed. No cycles or missing references found.";
            _validationError = hasErrors;
        }

        /// <summary>Collects all Id values from assets of the given type into a set.</summary>
        private void GatherAssetIds<T>(HashSet<string> ids) where T : ScriptableObject
        {
            var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null)
                    ids.Add(asset.Id);
            }
        }
    }
}
