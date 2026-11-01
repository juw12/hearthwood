using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Text;
using Hearthwood.Core;

namespace Hearthwood.EditorTools
{
    /// <summary>Custom inspector for <see cref="DialogueData"/> with node/choice editing and tree validation.</summary>
    [CustomEditor(typeof(DialogueData))]
    public class DialogueDataEditor : Editor
    {
        private bool _showNodes = true;
        private string _validationMessage = "";
        private bool _validationError = false;

        /// <summary>Render the custom inspector UI for DialogueData.</summary>
        public override void OnInspectorGUI()
        {
            var dialogue = (DialogueData)target;
            var so = serializedObject;
            so.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Dialogue", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            // Identity
            EditorGUILayout.PropertyField(so.FindProperty("_id"));
            EditorGUILayout.PropertyField(so.FindProperty("_rootNodeId"));
            EditorGUILayout.PropertyField(so.FindProperty("_defaultSpeakerId"));

            // Nodes
            EditorGUILayout.Space();
            _showNodes = EditorGUILayout.Foldout(_showNodes, "Nodes", true);
            if (_showNodes)
            {
                EditorGUI.indentLevel++;
                var nodesProp = so.FindProperty("_nodes");
                EditorGUILayout.PropertyField(nodesProp.FindPropertyRelative("Array.size"));

                for (int i = 0; i < nodesProp.arraySize; i++)
                {
                    var node = nodesProp.GetArrayElementAtIndex(i);
                    EditorGUILayout.Space(4);
                    EditorGUILayout.LabelField($"Node {i + 1}", EditorStyles.miniBoldLabel);

                    EditorGUILayout.PropertyField(node.FindPropertyRelative("NodeId"), new GUIContent("Node Id"));
                    EditorGUILayout.PropertyField(node.FindPropertyRelative("SpeakerId"), new GUIContent("Speaker Id"));
                    EditorGUILayout.PropertyField(node.FindPropertyRelative("SpeakerName"), new GUIContent("Speaker Name"));
                    EditorGUILayout.PropertyField(node.FindPropertyRelative("Text"), new GUIContent("Text"));
                    EditorGUILayout.PropertyField(node.FindPropertyRelative("NextNodeId"), new GUIContent("Next Node Id"));
                    EditorGUILayout.PropertyField(node.FindPropertyRelative("TriggerEventId"), new GUIContent("Trigger Event Id"));

                    // Choices
                    var choicesProp = node.FindPropertyRelative("Choices");
                    EditorGUILayout.Space(2);
                    EditorGUILayout.LabelField("Choices", EditorStyles.miniBoldLabel);
                    EditorGUILayout.PropertyField(choicesProp.FindPropertyRelative("Array.size"));

                    for (int c = 0; c < choicesProp.arraySize; c++)
                    {
                        var choice = choicesProp.GetArrayElementAtIndex(c);
                        EditorGUILayout.Space(2);
                        EditorGUILayout.LabelField($"Choice {c + 1}", EditorStyles.miniLabel);

                        EditorGUILayout.PropertyField(choice.FindPropertyRelative("Text"), new GUIContent("Text"));
                        EditorGUILayout.PropertyField(choice.FindPropertyRelative("NextNodeId"), new GUIContent("Next Node Id"));
                        DrawQuestPicker(choice.FindPropertyRelative("RequiredQuestId"), "Required Quest");
                        EditorGUILayout.PropertyField(choice.FindPropertyRelative("RequiredQuestStatus"), new GUIContent("Required Status"));
                        EditorGUILayout.PropertyField(choice.FindPropertyRelative("EndsDialogue"), new GUIContent("Ends Dialogue"));
                        EditorGUILayout.PropertyField(choice.FindPropertyRelative("AdvancesQuestObjective"), new GUIContent("Advances Quest Objective"));
                        EditorGUILayout.PropertyField(choice.FindPropertyRelative("AdvancesObjectiveId"), new GUIContent("Advances Objective Id"));
                    }
                }
                EditorGUI.indentLevel--;
            }

            so.ApplyIfModified();

            // Validate button
            EditorGUILayout.Space(8);
            if (GUILayout.Button("Validate", GUILayout.Height(24)))
            {
                ValidateDialogue(dialogue);
            }

            if (_validationMessage.Length > 0)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.HelpBox(_validationMessage, _validationError ? MessageType.Error : MessageType.Info);
            }
        }

        /// <summary>Draws a QuestData object picker backed by a string id SerializedProperty.</summary>
        private void DrawQuestPicker(SerializedProperty stringProp, string label)
        {
            var currentId = stringProp.stringValue;
            var currentAsset = FindQuestById(currentId);
            var newAsset = (QuestData)EditorGUILayout.ObjectField(label, currentAsset, typeof(QuestData), false);
            if (newAsset != currentAsset)
            {
                stringProp.stringValue = newAsset != null ? newAsset.Id : "";
                stringProp.serializedObject.ApplyModifiedProperties();
            }
        }

        /// <summary>Finds a QuestData asset by its Id field.</summary>
        private QuestData FindQuestById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            var guids = AssetDatabase.FindAssets("t:QuestData");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<QuestData>(path);
                if (asset != null && asset.Id == id)
                    return asset;
            }
            return null;
        }

        /// <summary>Validates rootNodeId exists, no orphan nextNodeId references, no duplicate nodeIds.</summary>
        private void ValidateDialogue(DialogueData dialogue)
        {
            var sb = new StringBuilder();
            var hasErrors = false;
            var nodes = dialogue.Nodes;

            if (nodes == null || nodes.Length == 0)
            {
                _validationMessage = "No nodes defined.";
                _validationError = true;
                return;
            }

            // Build set of existing node ids
            var nodeIds = new HashSet<string>();
            var duplicates = new HashSet<string>();
            foreach (var node in nodes)
            {
                if (!string.IsNullOrEmpty(node.NodeId))
                {
                    if (!nodeIds.Add(node.NodeId))
                        duplicates.Add(node.NodeId);
                }
            }

            // Check for duplicates
            foreach (var dup in duplicates)
            {
                sb.AppendLine($"Duplicate node id: '{dup}'.");
                hasErrors = true;
            }

            // Check rootNodeId exists
            if (string.IsNullOrEmpty(dialogue.RootNodeId))
            {
                sb.AppendLine("RootNodeId is empty.");
                hasErrors = true;
            }
            else if (!nodeIds.Contains(dialogue.RootNodeId))
            {
                sb.AppendLine($"RootNodeId '{dialogue.RootNodeId}' does not match any node.");
                hasErrors = true;
            }

            // Check for orphan nextNodeId references
            foreach (var node in nodes)
            {
                if (!string.IsNullOrEmpty(node.NextNodeId) && !nodeIds.Contains(node.NextNodeId))
                {
                    sb.AppendLine($"Node '{node.NodeId}': NextNodeId '{node.NextNodeId}' does not exist.");
                    hasErrors = true;
                }

                if (node.Choices != null)
                {
                    foreach (var choice in node.Choices)
                    {
                        if (!string.IsNullOrEmpty(choice.NextNodeId) && !nodeIds.Contains(choice.NextNodeId))
                        {
                            sb.AppendLine($"Node '{node.NodeId}': Choice nextNodeId '{choice.NextNodeId}' does not exist.");
                            hasErrors = true;
                        }
                    }
                }
            }

            _validationMessage = hasErrors
                ? sb.ToString().TrimEnd()
                : "Dialogue validation passed. No orphan references or duplicates found.";
            _validationError = hasErrors;
        }
    }
}
