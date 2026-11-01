using System.Collections.Generic;
using UnityEngine;
using Hearthwood.Core;

namespace Hearthwood.Quest
{
    /// <summary>
    /// Scene-scoped catalog of all loaded ScriptableObject assets. Populated once in
    /// <c>Awake</c> via <c>Resources.FindObjectsOfTypeAll</c> so that any SO instance
    /// (loaded from disk or created at runtime) is discoverable by stable id.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ItemDatabase : MonoBehaviour
    {
        /// <summary>The single live instance. Null until first Awake.</summary>
        public static ItemDatabase Instance { get; private set; }

        private readonly List<ItemData> _allItems = new();
        private readonly List<QuestData> _allQuests = new();
        private readonly List<NPCData> _allNpcs = new();
        private readonly List<CollectibleData> _allCollectibles = new();
        private readonly List<DialogueData> _allDialogues = new();
        private readonly List<SceneData> _allScenes = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            PopulateCatalogue();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Look up an item by its stable id. Returns null if not found.</summary>
        /// <param name="id">The item's stable id.</param>
        public ItemData GetById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < _allItems.Count; i++)
                if (_allItems[i].Id == id) return _allItems[i];
            return null;
        }

        /// <summary>Look up a quest by its stable id. Returns null if not found.</summary>
        /// <param name="id">The quest's stable id.</param>
        public QuestData GetQuestById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < _allQuests.Count; i++)
                if (_allQuests[i].Id == id) return _allQuests[i];
            return null;
        }

        /// <summary>Look up an NPC by its stable id. Returns null if not found.</summary>
        /// <param name="id">The NPC's stable id.</param>
        public NPCData GetNpcById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < _allNpcs.Count; i++)
                if (_allNpcs[i].Id == id) return _allNpcs[i];
            return null;
        }

        /// <summary>Look up a collectible by its stable id. Returns null if not found.</summary>
        /// <param name="id">The collectible's stable id.</param>
        public CollectibleData GetCollectibleById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < _allCollectibles.Count; i++)
                if (_allCollectibles[i].Id == id) return _allCollectibles[i];
            return null;
        }

        /// <summary>Look up a dialogue by its stable id. Returns null if not found.</summary>
        /// <param name="id">The dialogue's stable id.</param>
        public DialogueData GetDialogueById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < _allDialogues.Count; i++)
                if (_allDialogues[i].Id == id) return _allDialogues[i];
            return null;
        }

        /// <summary>Look up a scene by its stable id. Returns null if not found.</summary>
        /// <param name="id">The scene's stable id.</param>
        public SceneData GetSceneById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < _allScenes.Count; i++)
                if (_allScenes[i].Id == id) return _allScenes[i];
            return null;
        }

        private void PopulateCatalogue()
        {
            _allItems.Clear();
            _allQuests.Clear();
            _allNpcs.Clear();
            _allCollectibles.Clear();
            _allDialogues.Clear();
            _allScenes.Clear();

            CollectInto<ItemData>(_allItems);
            CollectInto<QuestData>(_allQuests);
            CollectInto<NPCData>(_allNpcs);
            CollectInto<CollectibleData>(_allCollectibles);
            CollectInto<DialogueData>(_allDialogues);
            CollectInto<SceneData>(_allScenes);

            Debug.Log($"[ItemDatabase] Catalogue populated: {_allItems.Count} items, {_allQuests.Count} quests, " +
                      $"{_allNpcs.Count} NPCs, {_allCollectibles.Count} collectibles, " +
                      $"{_allDialogues.Count} dialogues, {_allScenes.Count} scenes.");
        }

        private static void CollectInto<T>(List<T> target) where T : Object
        {
            var all = Resources.FindObjectsOfTypeAll<T>();
            for (int i = 0; i < all.Length; i++)
                target.Add(all[i]);
        }
    }
}
