using UnityEditor;
using UnityEngine;
using Hearthwood.Core;

namespace Hearthwood.EditorTools
{
    /// <summary>Generates a working medieval-village sample dataset under Assets/ScriptableObjects/.</summary>
    public static class HearthwoodSampleContent
    {
        private const string TargetDir = "Assets/ScriptableObjects";

        /// <summary>Generates all sample content assets. Idempotent — skips assets that already exist.</summary>
        [MenuItem("Hearthwood/Generate Sample Content")]
        public static void GenerateSampleContent()
        {
            if (!AssetDatabase.IsValidFolder(TargetDir))
            {
                AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
            }

            // --- Items ---
            var itemCoin = CreateItem("Item_Coin", "item_coin", "Coin", "A shiny copper coin. The universal currency of Hearthwood.", ItemType.Currency, 99, 0);
            var itemPotion = CreateItem("Item_HealthPotion", "item_health_potion", "Health Potion", "A restorative draught brewed by the village herbalist. Restores 30 HP.", ItemType.Consumable, 5, 8);
            var itemSword = CreateItem("Item_IronSword", "item_iron_sword", "Iron Sword", "A well-forged blade from the village blacksmith. Reliable in combat.", ItemType.Weapon, 1, 25);
            var itemWood = CreateItem("Item_Wood", "item_wood", "Wood", "A cut branch of seasoned oak. Useful for crafting and building.", ItemType.Material, 20, 1);
            var itemHerb = CreateItem("Item_Herb", "item_herb", "Herb", "A fragrant medicinal herb found in the meadows around the village.", ItemType.Material, 20, 2);

            // --- Dialogues ---
            var dialogueBlacksmith = CreateDialogueBlacksmith();
            var dialogueInnkeeper = CreateDialogueInnkeeper();

            // --- Quests ---
            var questGatherHerbs = CreateQuestGatherHerbs();
            var questDeliverMessage = CreateQuestDeliverMessage();
            var questBanditProblem = CreateQuestBanditProblem();

            // --- Collectibles ---
            var colHerb = CreateCollectibleHerb(itemHerb, questGatherHerbs);
            var colWood = CreateCollectibleWood(itemWood);
            var colCoin = CreateCollectibleCoin(itemCoin);

            // --- NPCs ---
            CreateNpcBlacksmith(dialogueBlacksmith, questGatherHerbs);
            CreateNpcInnkeeper(dialogueInnkeeper, questDeliverMessage);

            // --- Scene ---
            CreateSceneVillage();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Hearthwood] Sample content generated under Assets/ScriptableObjects/");
        }

        // ===================== Items =====================

        private static ItemData CreateItem(string fileName, string id, string displayName, string description, ItemType type, int maxStack, int basePrice)
        {
            if (AssetExists<ItemData>(fileName)) return null;
            var path = AssetDatabase.GenerateUniqueAssetPath($"{TargetDir}/{fileName}.asset");
            var item = CreateAsset<ItemData>(path);
            var so = new SerializedObject(item);
            so.FindProperty("_id").stringValue = id;
            so.FindProperty("_displayName").stringValue = displayName;
            so.FindProperty("_description").stringValue = description;
            so.FindProperty("_type").enumValueIndex = (int)type;
            so.FindProperty("_maxStack").intValue = maxStack;
            so.FindProperty("_basePrice").intValue = basePrice;
            so.ApplyModifiedProperties();
            return item;
        }

        // ===================== Collectibles =====================

        private static CollectibleData CreateCollectibleHerb(ItemData herbItem, QuestData quest)
        {
            if (AssetExists<CollectibleData>("Collectible_Herb")) return null;
            var path = AssetDatabase.GenerateUniqueAssetPath($"{TargetDir}/Collectible_Herb.asset");
            var c = CreateAsset<CollectibleData>(path);
            var so = new SerializedObject(c);
            so.FindProperty("_id").stringValue = "collectible_herb";
            so.FindProperty("_displayName").stringValue = "Herb";
            so.FindProperty("_collectPrompt").stringValue = "Press E to pick up the herb";
            so.FindProperty("_rewardItem").objectReferenceValue = herbItem;
            so.FindProperty("_rewardCount").intValue = 1;
            if (quest != null)
            {
                so.FindProperty("_questId").stringValue = quest.Id;
                so.FindProperty("_objectiveId").stringValue = "obj_gather_herbs";
            }
            so.FindProperty("_respawnable").boolValue = true;
            so.FindProperty("_respawnDelaySeconds").floatValue = 120f;
            so.ApplyModifiedProperties();
            return c;
        }

        private static CollectibleData CreateCollectibleWood(ItemData woodItem)
        {
            if (AssetExists<CollectibleData>("Collectible_Wood")) return null;
            var path = AssetDatabase.GenerateUniqueAssetPath($"{TargetDir}/Collectible_Wood.asset");
            var c = CreateAsset<CollectibleData>(path);
            var so = new SerializedObject(c);
            so.FindProperty("_id").stringValue = "collectible_wood";
            so.FindProperty("_displayName").stringValue = "Wood";
            so.FindProperty("_collectPrompt").stringValue = "Press E to pick up the wood";
            so.FindProperty("_rewardItem").objectReferenceValue = woodItem;
            so.FindProperty("_rewardCount").intValue = 1;
            so.FindProperty("_respawnable").boolValue = true;
            so.FindProperty("_respawnDelaySeconds").floatValue = 300f;
            so.ApplyModifiedProperties();
            return c;
        }

        private static CollectibleData CreateCollectibleCoin(ItemData coinItem)
        {
            if (AssetExists<CollectibleData>("Collectible_Coin")) return null;
            var path = AssetDatabase.GenerateUniqueAssetPath($"{TargetDir}/Collectible_Coin.asset");
            var c = CreateAsset<CollectibleData>(path);
            var so = new SerializedObject(c);
            so.FindProperty("_id").stringValue = "collectible_coin";
            so.FindProperty("_displayName").stringValue = "Coin Pile";
            so.FindProperty("_collectPrompt").stringValue = "Press E to collect coins";
            so.FindProperty("_rewardItem").objectReferenceValue = coinItem;
            so.FindProperty("_rewardCount").intValue = 5;
            so.FindProperty("_respawnable").boolValue = false;
            so.ApplyModifiedProperties();
            return c;
        }

        // ===================== Quests =====================

        private static QuestData CreateQuestGatherHerbs()
        {
            if (AssetExists<QuestData>("Quest_GatherHerbs")) return null;
            var path = AssetDatabase.GenerateUniqueAssetPath($"{TargetDir}/Quest_GatherHerbs.asset");
            var q = CreateAsset<QuestData>(path);
            var so = new SerializedObject(q);
            so.FindProperty("_id").stringValue = "quest_gather_herbs";
            so.FindProperty("_displayName").stringValue = "Gather Herbs";
            so.FindProperty("_description").stringValue = "The village herbalist is running low on fresh herbs. Gather 5 from the meadows.";
            so.FindProperty("_factionRequirement").stringValue = "Villager";

            // One objective: Collect 5 herbs
            var objProp = so.FindProperty("_objectives");
            objProp.arraySize = 1;
            var obj = objProp.GetArrayElementAtIndex(0);
            obj.FindPropertyRelative("ObjectiveId").stringValue = "obj_gather_herbs";
            obj.FindPropertyRelative("Description").stringValue = "Gather 5 wild herbs";
            obj.FindPropertyRelative("Kind").enumValueIndex = (int)ObjectiveKind.CollectItem;
            obj.FindPropertyRelative("TargetCount").intValue = 5;
            obj.FindPropertyRelative("TargetCollectibleId").stringValue = "collectible_herb";

            // Rewards: 5 favor + Wood x2
            var rewProp = so.FindProperty("_rewards");
            rewProp.arraySize = 2;
            var r0 = rewProp.GetArrayElementAtIndex(0);
            r0.FindPropertyRelative("FavorTokens").intValue = 5;
            r0.FindPropertyRelative("ItemCount").intValue = 1;
            var r1 = rewProp.GetArrayElementAtIndex(1);
            r1.FindPropertyRelative("ItemId").stringValue = "item_wood";
            r1.FindPropertyRelative("ItemCount").intValue = 2;

            so.ApplyModifiedProperties();
            return q;
        }

        private static QuestData CreateQuestDeliverMessage()
        {
            if (AssetExists<QuestData>("Quest_DeliverMessage")) return null;
            var path = AssetDatabase.GenerateUniqueAssetPath($"{TargetDir}/Quest_DeliverMessage.asset");
            var q = CreateAsset<QuestData>(path);
            var so = new SerializedObject(q);
            so.FindProperty("_id").stringValue = "quest_deliver_message";
            so.FindProperty("_displayName").stringValue = "Deliver a Message";
            so.FindProperty("_description").stringValue = "The innkeeper has a message that needs to be delivered in person. Talk to him.";
            so.FindProperty("_factionRequirement").stringValue = "Villager";

            // One objective: Talk to Innkeeper
            var objProp = so.FindProperty("_objectives");
            objProp.arraySize = 1;
            var obj = objProp.GetArrayElementAtIndex(0);
            obj.FindPropertyRelative("ObjectiveId").stringValue = "obj_deliver_message";
            obj.FindPropertyRelative("Description").stringValue = "Deliver the message to the Innkeeper";
            obj.FindPropertyRelative("Kind").enumValueIndex = (int)ObjectiveKind.TalkToNpc;
            obj.FindPropertyRelative("TargetCount").intValue = 1;
            obj.FindPropertyRelative("TargetNpcId").stringValue = "npc_innkeeper";

            // Rewards: 3 favor
            var rewProp = so.FindProperty("_rewards");
            rewProp.arraySize = 1;
            var r0 = rewProp.GetArrayElementAtIndex(0);
            r0.FindPropertyRelative("FavorTokens").intValue = 3;
            r0.FindPropertyRelative("ItemCount").intValue = 1;

            so.ApplyModifiedProperties();
            return q;
        }

        private static QuestData CreateQuestBanditProblem()
        {
            if (AssetExists<QuestData>("Quest_BanditProblem")) return null;
            var path = AssetDatabase.GenerateUniqueAssetPath($"{TargetDir}/Quest_BanditProblem.asset");
            var q = CreateAsset<QuestData>(path);
            var so = new SerializedObject(q);
            so.FindProperty("_id").stringValue = "quest_bandit_problem";
            so.FindProperty("_displayName").stringValue = "Bandit Problem";
            so.FindProperty("_description").stringValue = "Wolves have been spotted near the trading roads. Thin their numbers to 3.";
            so.FindProperty("_factionRequirement").stringValue = "Villager";

            // One objective: Kill 3 wolves
            var objProp = so.FindProperty("_objectives");
            objProp.arraySize = 1;
            var obj = objProp.GetArrayElementAtIndex(0);
            obj.FindPropertyRelative("ObjectiveId").stringValue = "obj_kill_wolves";
            obj.FindPropertyRelative("Description").stringValue = "Slay 3 wolves";
            obj.FindPropertyRelative("Kind").enumValueIndex = (int)ObjectiveKind.KillEnemy;
            obj.FindPropertyRelative("TargetCount").intValue = 3;
            obj.FindPropertyRelative("TargetEnemyTag").stringValue = "Wolf";

            // Rewards: 10 favor + IronSword
            var rewProp = so.FindProperty("_rewards");
            rewProp.arraySize = 2;
            var r0 = rewProp.GetArrayElementAtIndex(0);
            r0.FindPropertyRelative("FavorTokens").intValue = 10;
            r0.FindPropertyRelative("ItemCount").intValue = 1;
            var r1 = rewProp.GetArrayElementAtIndex(1);
            r1.FindPropertyRelative("ItemId").stringValue = "item_iron_sword";
            r1.FindPropertyRelative("ItemCount").intValue = 1;

            so.ApplyModifiedProperties();
            return q;
        }

        // ===================== Dialogues =====================

        private static DialogueData CreateDialogueBlacksmith()
        {
            if (AssetExists<DialogueData>("Dialogue_Blacksmith")) return null;
            var path = AssetDatabase.GenerateUniqueAssetPath($"{TargetDir}/Dialogue_Blacksmith.asset");
            var d = CreateAsset<DialogueData>(path);
            var so = new SerializedObject(d);
            so.FindProperty("_id").stringValue = "dialogue_blacksmith";
            so.FindProperty("_rootNodeId").stringValue = "root";
            so.FindProperty("_defaultSpeakerId").stringValue = "npc_blacksmith";

            var nodesProp = so.FindProperty("_nodes");
            nodesProp.arraySize = 3;

            // Node 1: Greeting
            var n0 = nodesProp.GetArrayElementAtIndex(0);
            n0.FindPropertyRelative("NodeId").stringValue = "root";
            n0.FindPropertyRelative("SpeakerId").stringValue = "npc_blacksmith";
            n0.FindPropertyRelative("SpeakerName").stringValue = "Blacksmith";
            n0.FindPropertyRelative("Text").stringValue = "Ah, {playerName}. The forge is hot and the day is young. Can I help you with something?";
            n0.FindPropertyRelative("NextNodeId").stringValue = "quest_offer";

            // Node 2: Quest offer
            var n1 = nodesProp.GetArrayElementAtIndex(1);
            n1.FindPropertyRelative("NodeId").stringValue = "quest_offer";
            n1.FindPropertyRelative("SpeakerId").stringValue = "npc_blacksmith";
            n1.FindPropertyRelative("SpeakerName").stringValue = "Blacksmith";
            n1.FindPropertyRelative("Text").stringValue = "You know, the herbalist has been asking about fresh herbs for her tinctures. If you could gather a few from the meadows, she would owe you a favor.";
            n1.FindPropertyRelative("NextNodeId").stringValue = "farewell";

            // Node 3: Farewell
            var n2 = nodesProp.GetArrayElementAtIndex(2);
            n2.FindPropertyRelative("NodeId").stringValue = "farewell";
            n2.FindPropertyRelative("SpeakerId").stringValue = "npc_blacksmith";
            n2.FindPropertyRelative("SpeakerName").stringValue = "Blacksmith";
            n2.FindPropertyRelative("Text").stringValue = "Take care out there. The roads aren't always safe these days.";
            n2.FindPropertyRelative("NextNodeId").stringValue = "";

            so.ApplyModifiedProperties();
            return d;
        }

        private static DialogueData CreateDialogueInnkeeper()
        {
            if (AssetExists<DialogueData>("Dialogue_Innkeeper")) return null;
            var path = AssetDatabase.GenerateUniqueAssetPath($"{TargetDir}/Dialogue_Innkeeper.asset");
            var d = CreateAsset<DialogueData>(path);
            var so = new SerializedObject(d);
            so.FindProperty("_id").stringValue = "dialogue_innkeeper";
            so.FindProperty("_rootNodeId").stringValue = "root";
            so.FindProperty("_defaultSpeakerId").stringValue = "npc_innkeeper";

            var nodesProp = so.FindProperty("_nodes");
            nodesProp.arraySize = 3;

            // Node 1: Greeting
            var n0 = nodesProp.GetArrayElementAtIndex(0);
            n0.FindPropertyRelative("NodeId").stringValue = "root";
            n0.FindPropertyRelative("SpeakerId").stringValue = "npc_innkeeper";
            n0.FindPropertyRelative("SpeakerName").stringValue = "Innkeeper";
            n0.FindPropertyRelative("Text").stringValue = "Well, well. Welcome to the Hearthwood Inn. Have a seat by the fire, {playerName}.";
            n0.FindPropertyRelative("NextNodeId").stringValue = "rumor";

            // Node 2: Rumor
            var n1 = nodesProp.GetArrayElementAtIndex(1);
            n1.FindPropertyRelative("NodeId").stringValue = "rumor";
            n1.FindPropertyRelative("SpeakerId").stringValue = "npc_innkeeper";
            n1.FindPropertyRelative("SpeakerName").stringValue = "Innkeeper";
            n1.FindPropertyRelative("Text").stringValue = "Heard some travelers say there are wolves circling the eastern roads. The merchant guild is worried about their caravans.";
            n1.FindPropertyRelative("NextNodeId").stringValue = "farewell";

            // Node 3: Farewell
            var n2 = nodesProp.GetArrayElementAtIndex(2);
            n2.FindPropertyRelative("NodeId").stringValue = "farewell";
            n2.FindPropertyRelative("SpeakerId").stringValue = "npc_innkeeper";
            n2.FindPropertyRelative("SpeakerName").stringValue = "Innkeeper";
            n2.FindPropertyRelative("Text").stringValue = "Keep your coins safe and your blade sharp. We'll be here if you need us.";
            n2.FindPropertyRelative("NextNodeId").stringValue = "";

            so.ApplyModifiedProperties();
            return d;
        }

        // ===================== NPCs =====================

        private static void CreateNpcBlacksmith(DialogueData dialogue, QuestData quest)
        {
            if (AssetExists<NPCData>("NPC_Blacksmith")) return;
            var path = AssetDatabase.GenerateUniqueAssetPath($"{TargetDir}/NPC_Blacksmith.asset");
            var npc = CreateAsset<NPCData>(path);
            var so = new SerializedObject(npc);
            so.FindProperty("_id").stringValue = "npc_blacksmith";
            so.FindProperty("_displayName").stringValue = "Blacksmith";
            so.FindProperty("_faction").stringValue = "Villager";
            if (dialogue != null)
                so.FindProperty("_defaultDialogue").objectReferenceValue = dialogue;
            if (quest != null)
                so.FindProperty("_offeredQuest").objectReferenceValue = quest;
            so.ApplyModifiedProperties();
        }

        private static void CreateNpcInnkeeper(DialogueData dialogue, QuestData quest)
        {
            if (AssetExists<NPCData>("NPC_Innkeeper")) return;
            var path = AssetDatabase.GenerateUniqueAssetPath($"{TargetDir}/NPC_Innkeeper.asset");
            var npc = CreateAsset<NPCData>(path);
            var so = new SerializedObject(npc);
            so.FindProperty("_id").stringValue = "npc_innkeeper";
            so.FindProperty("_displayName").stringValue = "Innkeeper";
            so.FindProperty("_faction").stringValue = "Villager";
            if (dialogue != null)
                so.FindProperty("_defaultDialogue").objectReferenceValue = dialogue;
            if (quest != null)
                so.FindProperty("_offeredQuest").objectReferenceValue = quest;
            so.ApplyModifiedProperties();
        }

        // ===================== Scene =====================

        private static void CreateSceneVillage()
        {
            if (AssetExists<SceneData>("Scene_Village")) return;
            var path = AssetDatabase.GenerateUniqueAssetPath($"{TargetDir}/Scene_Village.asset");
            var s = CreateAsset<SceneData>(path);
            var so = new SerializedObject(s);
            so.FindProperty("_id").stringValue = "scene_village";
            so.FindProperty("_displayName").stringValue = "Village";
            so.FindProperty("_scenePath").stringValue = "Assets/Scenes/Village.unity";
            so.FindProperty("_buildName").stringValue = "Village";
            so.ApplyModifiedProperties();
        }

        // ===================== Helpers =====================

        /// <summary>Creates a ScriptableObject asset at the given path and registers it.</summary>
        private static T CreateAsset<T>(string path) where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        /// <summary>Returns true if an asset of the given type with the given filename already exists in the target directory.</summary>
        private static bool AssetExists<T>(string fileName) where T : ScriptableObject
        {
            var path = $"{TargetDir}/{fileName}.asset";
            return AssetDatabase.LoadAssetAtPath<T>(path) != null;
        }
    }
}
