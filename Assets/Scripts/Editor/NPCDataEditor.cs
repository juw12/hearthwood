using UnityEditor;
using UnityEngine;
using Hearthwood.Core;

namespace Hearthwood.EditorTools
{
    /// <summary>Custom inspector for <see cref="NPCData"/> with quick-open buttons for linked dialogue and quest assets.</summary>
    [CustomEditor(typeof(NPCData))]
    public class NPCDataEditor : Editor
    {
        /// <summary>Render the custom inspector UI for NPCData.</summary>
        public override void OnInspectorGUI()
        {
            var npc = (NPCData)target;
            var so = serializedObject;
            so.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("NPC", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            // Identity
            EditorGUILayout.PropertyField(so.FindProperty("_id"));
            EditorGUILayout.PropertyField(so.FindProperty("_displayName"));
            EditorGUILayout.PropertyField(so.FindProperty("_faction"));

            // Visuals
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Visuals", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(so.FindProperty("_portrait"), new GUIContent("Portrait"));

            // Dialogue & Quest links
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Dialogue & Quest", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(so.FindProperty("_defaultDialogue"), new GUIContent("Default Dialogue"));
            EditorGUILayout.PropertyField(so.FindProperty("_offeredQuest"), new GUIContent("Offered Quest"));

            // AI
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("AI", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(so.FindProperty("_patrolPathId"), new GUIContent("Patrol Path Id"));
            EditorGUILayout.PropertyField(so.FindProperty("_wanderRadius"), new GUIContent("Wander Radius"));

            so.ApplyIfModified();

            // Quick-open buttons
            EditorGUILayout.Space(8);
            EditorGUILayout.BeginHorizontal();

            GUI.enabled = npc.DefaultDialogue != null;
            if (GUILayout.Button("Open Dialogue"))
            {
                EditorGUIUtility.PingObject(npc.DefaultDialogue);
                Selection.activeObject = npc.DefaultDialogue;
            }

            GUI.enabled = npc.OfferedQuest != null;
            if (GUILayout.Button("Open Quest"))
            {
                EditorGUIUtility.PingObject(npc.OfferedQuest);
                Selection.activeObject = npc.OfferedQuest;
            }

            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }
    }
}
