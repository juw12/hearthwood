using System;
using UnityEngine;

namespace Hearthwood.Core
{
    /// <summary>
    /// Static utility around <c>PlayerPrefs</c> + <c>JsonUtility</c>. WebGL-safe
    /// (PlayerPrefs uses IndexedDB on WebGL; never touches <c>System.IO.File</c>).
    /// <see cref="Snapshot"/> is the persisted shape — every field must be a
    /// JsonUtility-serializable type (primitives, strings, arrays, lists of serializables).
    /// </summary>
    public static class SaveSystem
    {
        /// <summary>Snapshot of all persisted state. Add fields with care; bump _version on breaking changes.</summary>
        [Serializable]
        public sealed class Snapshot
        {
            public int _version = 1;
            public string sceneName;
            public float playerX, playerY, playerZ;
            public float playerHealth, playerMaxHealth;
            public string playerFaction;
            public int favorTokens;
            public string[] activeQuestIds;
            public string[] completedQuestIds;
            public ObjectiveProgress[] objectiveProgress;
            public InventoryEntry[] inventory;
            public string[] killedEnemyIds;
            public string[] unlockedDialogueIds;
            public float playTimeSeconds;
        }

        [Serializable] public struct ObjectiveProgress { public string questId; public string objectiveId; public int currentCount; }
        [Serializable] public struct InventoryEntry { public string itemId; public int count; }

        /// <summary>Capture the current world state. Called by <c>GameManager.Save()</c>.</summary>
        public static Snapshot CaptureSnapshot()
        {
            var snap = new Snapshot
            {
                sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                playTimeSeconds = Time.realtimeSinceStartup,
            };
            // Player position/health/faction filled by Hearthwood.Player.PlayerPersistence (see Player asmdef).
            // QuestSystem fills activeQuestIds/completedQuestIds/objectiveProgress.
            // InventoryComponent fills inventory array.
            // Favors tracked by FavorTokenSystem.
            SnapShotFillerRegistry.FillInto(snap);
            return snap;
        }

        /// <summary>Apply a snapshot back to runtime systems.</summary>
        public static void ApplySnapshot(Snapshot snap)
        {
            if (snap == null) return;
            SnapShotFillerRegistry.ApplyFrom(snap);
        }

        /// <summary>Write snapshot to PlayerPrefs as JSON.</summary>
        public static void Save(string key, Snapshot snap)
        {
            if (snap == null) return;
            try
            {
                var json = JsonUtility.ToJson(snap, prettyPrint: false);
                PlayerPrefs.SetString(key, json);
                PlayerPrefs.Save();
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Save failed: {e.Message}");
            }
        }

        /// <summary>Read snapshot from PlayerPrefs. Returns null if no save exists or JSON is corrupt.</summary>
        public static Snapshot Load(string key)
        {
            if (!PlayerPrefs.HasKey(key)) return null;
            try
            {
                var json = PlayerPrefs.GetString(key);
                if (string.IsNullOrEmpty(json)) return null;
                return JsonUtility.FromJson<Snapshot>(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveSystem] Load failed (returning null): {e.Message}");
                return null;
            }
        }

        /// <summary>Erase the save. Used by "New Game" button.</summary>
        public static void Delete(string key)
        {
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }
    }
}
