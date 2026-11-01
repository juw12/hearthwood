# Hearthwood — Architecture Contract

> **Read this document before writing any code.** Every subsystem agent must conform to the
> public APIs, namespace conventions, and event patterns defined here. Code that violates
> this contract will be rejected during integration verification.

---

## 1. Project Identity

| Property | Value |
|---|---|
| Game name | **Hearthwood** |
| Theme | Low-poly medieval village |
| Engine | **Unity 6** (6000.x LTS) |
| Render pipeline | **URP** (17.x) |
| Input | **Input System** (1.x, not legacy) |
| Platforms | PC (Standalone) + WebGL |
| Language | C# 9+ |
| Namespace root | `Hearthwood.<Subsystem>` |
| Default scene name | `Village.unity` |
| Save location | `PlayerPrefs` (JSON serialized) — **WebGL safe** |

---

## 2. Subsystem Map

```
Hearthwood.Core          — Interfaces, GameManager, SaveSystem, utilities (no MonoBehaviour deps)
Hearthwood.Input         — InputManager (wraps PlayerInput)
Hearthwood.Audio         — AudioManager, AmbientAudioManager, FootstepPlayer, InteractionAudio
Hearthwood.Scene         — SceneController
Hearthwood.Player        — PlayerController3D, CombatController, InventoryComponent, HealthComponent
Hearthwood.NPC           — NPCController
Hearthwood.Collectible   — Collectible
Hearthwood.Quest         — QuestSystem, QuestGiver, IQuestTarget implementations
Hearthwood.Dialogue      — DialogueRunner, DialogueChoice
Hearthwood.UI            — HUDController, DialogueUI, QuestLogUI, PauseMenuUI, InventoryUI
Hearthwood.EditorTools   — Custom inspectors, asset menu items (Editor-only)
```

### Assembly dependency rules (acyclic)

```
Core ◀── Input, Audio, Scene
Core ◀── Player, NPC, Collectible, Quest, Dialogue
Core ◀── UI
UI ◀── (depends on Player + Quest + Dialogue for read-only state)
EditorTools ◀── Core + Quest + Dialogue + NPC  (editor-only, separate platform define)
```

**Forbidden**: any subsystem referencing a higher-numbered sibling. Forward references break the
compilation order and are an automatic integration failure.

---

## 3. Public Interfaces (Core)

These are defined in `Hearthwood.Core` and consumed by **every** subsystem. **Do not duplicate them**.

### `IInteractable`

```csharp
public interface IInteractable
{
    string InteractPrompt { get; }                 // e.g. "Press E to Talk"
    bool CanInteract(PlayerContext ctx);          // ctx = player who is interacting
    void Interact(PlayerContext ctx);             // raises events, may pause game
}
```

### `IDialogueSpeaker`

```csharp
public interface IDialogueSpeaker
{
    string SpeakerName { get; }
    Sprite Portrait { get; }                      // nullable in HTML preview, optional in Unity
    DialogueData GetDefaultDialogue();            // ScriptableObject reference
}
```

### `IQuestTarget`

```csharp
public interface IQuestTarget
{
    string QuestId { get; }                       // matches QuestData.QuestId
    QuestState GetState(string questId);
    void Notify(string questId, QuestEvent evt, object payload = null);
}
```

### `ICollectible`

```csharp
public interface ICollectible
{
    CollectibleData Data { get; }
    void OnPicked(InventoryComponent inventory);
}
```

### `IPlayerContextProvider`

```csharp
public interface IPlayerContextProvider
{
    PlayerContext Current { get; }                // null if no player active in scene
}
```

`PlayerContext` is a small POCO (struct or sealed class) — position, health, faction, faction tags.
Defined in `Hearthwood.Core.PlayerContext`.

---

## 4. Cross-System APIs (must be public on these classes)

These are **contracts**. Other subsystems read from them. Subagents writing these classes must
expose exactly these members with exactly these names.

### `PlayerController3D` (Hearthwood.Player)

```csharp
public bool IsGrounded { get; }                   // for FootstepPlayer
public bool IsSprinting { get; }                  // for FootstepPlayer + audio
public bool IsRunning { get; }                    // any movement (walk+sprint)
public Vector3 Velocity { get; }
public Transform Transform { get; }               // for camera follow + interact ray
public void TakeDamage(float amount, DamageSource source);
public event Action<float, float> OnHealthChanged;     // (current, max)
public event Action OnDeath;
public event Action<IInteractable> OnInteractableFocusChanged;
public event Action OnInteractPressed;            // raised when Interact input fires
public event Action<CollectibleData> OnItemCollected; // for HUD + QuestSystem
```

### `InventoryComponent` (Hearthwood.Player)

```csharp
public int Capacity { get; }
public IReadOnlyList<InventorySlot> Slots { get; }
public bool HasItem(ItemData item, int count = 1);
public bool TryAdd(ItemData item, int count = 1);
public bool TryRemove(ItemData item, int count = 1);
public int GetCount(ItemData item);
public event Action OnInventoryChanged;
```

### `HealthComponent` (Hearthwood.Player) — also reused on NPCs

```csharp
public float Current { get; }
public float Max { get; }
public bool IsDead { get; }
public void TakeDamage(float amount, DamageSource source = DamageSource.Generic);
public void Heal(float amount);
public event Action<float, float> OnChanged;
public event Action OnDeath;
```

### `QuestSystem` (Hearthwood.Quest)

```csharp
public static QuestSystem Instance { get; }       // singleton
public event Action<QuestData> OnQuestStarted;
public event Action<QuestData> OnQuestCompleted;
public event Action<QuestData, QuestObjective> OnObjectiveCompleted;
public event Action<QuestData> OnQuestFailed;
public IReadOnlyList<QuestData> ActiveQuests { get; }
public IReadOnlyList<QuestData> CompletedQuests { get; }
public void StartQuest(QuestData quest);
public void CompleteObjective(string questId, string objectiveId);
public void NotifyCollectible(CollectibleData c);
public void NotifyDialogueEnded(string npcId);
public QuestState GetQuestState(string questId);
```

### `DialogueRunner` (Hearthwood.Dialogue)

```csharp
public static DialogueRunner Instance { get; }
public bool IsRunning { get; }
public void StartDialogue(IDialogueSpeaker speaker, DialogueData dialogue, PlayerContext ctx);
public void Choose(int choiceIndex);
public void Advance();
public void EndDialogue();
public event Action<DialogueLine> OnLineShown;
public event Action<DialogueChoice[]> OnChoicesShown;
public event Action OnDialogueEnded;
```

### `GameManager` (Hearthwood.Core)

```csharp
public static GameManager Instance { get; }
public GameState State { get; }
public void SetState(GameState newState);
public event Action<GameState, GameState> OnStateChanged;  // (from, to)
public void PauseGame();
public void ResumeGame();
public void Save();
public void Load();
```

### `AudioManager` (Hearthwood.Audio)

```csharp
public static AudioManager Instance { get; }
public void PlaySFX(AudioClip clip, Vector3 position = default);
public void PlaySFX2D(AudioClip clip);
public void SetMasterVolume(float v01);
public void SetSFXVolume(float v01);
public void SetMusicVolume(float v01);
```

---

## 5. Coding Conventions (binding)

- **One public class per file.** File name = class name.
- **No `public` fields on MonoBehaviours.** Always `[SerializeField] private` + `public` getter.
- **`event Action<T>` for all decoupling** (no UnityEvents on runtime code).
- **XML doc comments on every public member** (`<summary>` minimum, `<param>` if any).
- **Header / Tooltip / Range attributes** on every serialized field.
- **Namespace: `Hearthwood.<Subsystem>`** exactly.
- **Singletons**: `public static T Instance { get; private set; }` + `Awake()` assignment.
  - Only `GameManager.Instance` uses `DontDestroyOnLoad`.
  - `QuestSystem.Instance`, `DialogueRunner.Instance`, `AudioManager.Instance` are **scene-scoped**
    (reset on scene change). Their `Awake` does NOT call `DontDestroyOnLoad`.
  - The intent: `GameManager` survives, the rest rebuild per scene.
- **No `FindObjectOfType` in hot paths.** Use `Instance` properties or cached references.
- **No `async void`** except for event handlers. Use `async Task` or coroutines.
- **WebGL-safe I/O**: avoid `System.IO.File`. Use `Application.persistentDataPath` only if WebGL
  build target verified, otherwise `PlayerPrefs` (which uses IndexedDB on WebGL).

---

## 6. ScriptableObject Conventions

Every data asset:

- Inherits a `[CreateAssetMenu]` attribute with a unique menu path
  (`Hearthwood/Quests/<Name>`, `Hearthwood/Dialogues/<Name>`, etc.)
- Has a stable string ID field (`string Id`) — never use `name` for save data
- Has `[Header]`, `[Tooltip]` on every public field
- Has `[CreateAssetMenu]` even if a custom editor also exists
- Assets are created at runtime by `SOAssetBootstrap.cs` if the asset DB is empty
  (see PHASE 3e deliverable)

### Canonical SO schemas (defined in PHASE 2, but listed here so dependents know):

- `ItemData` (id, displayName, icon, maxStack, type)
- `CollectibleData : ItemData` (collectPrompt, questId, objectiveId)
- `QuestData` (id, displayName, description, objectives[], rewards[])
- `QuestObjective` (id, description, kind, targetCount, currentCount)
- `DialogueData` (id, rootNodeId, nodes[])
- `DialogueNode` (id, speakerId, text, nextNodeId, choices[])
- `DialogueChoice` (text, nextNodeId, condition)
- `NPCData` (id, displayName, prefabKey, defaultDialogueId, faction)
- `SceneData` (id, sceneName, additiveScenes[])

---

## 7. Input Action Map Contract

The `Input Actions` asset must contain three action maps:

| Map | Bindings |
|---|---|
| **Player** | Move (Vector2), Look (Vector2), Jump (Button), Sprint (Button), Interact (Button), Attack (Button), Dodge (Button), Inventory (Button), Pause (Button) |
| **UI** | Navigate (Vector2), Submit (Button), Cancel (Button), Point (Vector2), Click (Button), QuestLog (Button), PauseBack (Button) |
| **Dialogue** | Advance (Button), Choice1..Choice4 (Button), Skip (Button) |

`InputManager` switches maps via `PlayerInput.SwitchCurrentActionMap("UI")` etc.
Map transitions:
- `GameState.Exploring` → Player
- `GameState.Dialogue` → Dialogue
- `GameState.Inventory` / `GameState.QuestLog` / `GameState.Paused` → UI

---

## 8. Event Flow (canonical wiring)

```
Player presses Interact
  └─▶ PlayerController3D.OnInteractPressed event fires
        └─▶ PlayerController3D resolves IInteractable under focus raycast
              └─▶ Calls .Interact(ctx) on the focused target
                    ├─ NPCController → DialogueRunner.StartDialogue(...)
                    │     └─ DialogueRunner raises OnLineShown → DialogueUI listens
                    │     └─ DialogueRunner raises OnChoicesShown → DialogueUI shows choices
                    │     └─ OnDialogueEnded → GameManager.SetState(Exploring)
                    └─ Collectible → InventoryComponent.TryAdd + Collectible.OnPicked
                          └─ QuestSystem.NotifyCollectible → updates objectives
                                └─ OnObjectiveCompleted → HUD listens
```

---

## 9. WebGL Hard Constraints

These compile-verify but won't run on WebGL if violated:

| Constraint | Why |
|---|---|
| No `System.IO.File.*` | WebGL has no real filesystem |
| No threads (`new Thread(...)`) | WebGL is single-threaded |
| No `Application.persistentDataPath` heavy writes | Use `PlayerPrefs` |
| No third-party DLLs (System.Data, etc.) | Stripped during WebGL build |
| TextMeshPro essential fonts must be in `Resources/` | WebGL can't lazy-load fonts from AssetBundles |
| All audio as `.ogg` or compressed `.wav` | MP3 has licensing issues on some WebGL hosts |
| URP renderer (not Built-in) | Built-in renderer is deprecated |

---

## 10. Integration Verification Checklist (Phase 4)

The orchestrator will run these greps post-delegation:

```bash
# All namespace declarations match their folder
grep -rn "^namespace " Assets/Scripts/

# No subsystem reaches into another subsystem's privates
grep -rn "\.private\|\._internal" Assets/Scripts/  # should be empty

# Event signatures match the contract above
grep -rn "event Action" Assets/Scripts/

# No legacy input
grep -rn "Input\.GetKey\|Input\.GetButton" Assets/Scripts/  # must be empty
```

Any failure → fix in the offending subsystem before integration.

---

**Last updated**: Phase 1 sign-off. Subagents must re-read this if the orchestrator revises it.
