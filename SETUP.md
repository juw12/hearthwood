# Setup — Hearthwood Unity 6 Walkthrough

> The prototype ships without the Unity project bootstrapped. This file walks through
> what you do in the editor once you point Unity at `Games/Hearthwood/`.

---

## 0. Prerequisites

| Tool | Version |
|---|---|
| Unity Hub | latest |
| Unity Editor | **6 LTS (6000.x)** — must match the package versions in `Packages/manifest.json` |
| WebGL Build Support module | optional, only for WebGL targets |
| Visual Studio / Rider | for C# editing (VSCode also works) |

If you don't have Unity installed yet: this prototype is also viewable without Unity via
`preview.html` (double-click in any browser). Skip to the end of this doc for the
preview-only flow.

---

## 1. Open the Project

1. Launch Unity Hub → **Add project from disk** → select `Games/Hearthwood/`.
2. Open with Unity 6 LTS. First import will fetch packages from `manifest.json` (~2-5 min).
3. If prompted, accept package warnings for URP / Input System / Cinemachine.

---

## 2. Generate Sample Content

1. Wait for compile to finish (no red errors in the Console).
2. Menu bar: **Hearthwood ▸ Generate Sample Content**.
3. This creates (under `Assets/ScriptableObjects/`):
   - `Items/`: 5 items (Coin, Health Potion, Iron Sword, Wood, Herb)
   - `Collectibles/`: 3 collectibles (Herb, Wood, Coin) — each linked to a quest
   - `NPCs/`: Bron the Blacksmith, Mara the Innkeeper — each with default dialogue + offered quest
   - `Quests/`: Gather Wild Herbs, Deliver a Message, The Bandit Problem
   - `Dialogues/`: Bron's 3-node tree, Mara's 3-node tree
   - `Scenes/`: `Scene_Village` reference (still need to author the actual scene below)
4. Re-run is idempotent: existing assets are skipped, no duplicates.

---

## 3. Create the Village Scene

1. **File ▸ New Scene** → Basic (URP). Save as `Assets/Scenes/Village.unity`.
2. Open **SceneDataEditor** on `Scene_Village.asset` → click **Find Scene Asset** then
   **Add to Build Settings** to register the scene.

### Required GameObjects (drop these in)

```
Scene Root
├── Main Camera                      ← add PlayerCamera.cs
├── EventSystem                      ← (built-in) needed for UI hover/click
├── Directional Light
│
├── ─── System Singletons ───
├── GameManager                      ← GameManager.cs
├── InputManager                     ← InputManager.cs + PlayerInput component
│       └─ PlayerInput Actions Asset = Assets/Settings/HearthwoodInputActions.inputactions
│       └─ Default Map = "Player"
├── SceneController                  ← SceneController.cs (drop SceneData assets into Catalog)
├── AudioManager                     ← AudioManager.cs
├── FavorTokenSystem                 ← FavorTokenSystem.cs
├── ItemDatabase                     ← ItemDatabase.cs (auto-populates from Resources / FindObjectsOfTypeAll)
├── QuestSystem                      ← QuestSystem.cs
├── DialogueRunner                   ← DialogueRunner.cs
├── AmbientAudioManager              ← AmbientAudioManager.cs (assign ambient clips)
├── InteractionAudio                 ← InteractionAudio.cs (assign SFX clips)
│
├── ─── World ───
├── Terrain / Ground Plane
├── Buildings (5+): Tavern, Blacksmith, Market, House A, House B
├── Props: trees, well, market stalls
├── NavMesh Surface                  ← bake before play
│
├── ─── Player ───
├── Player                           ← CharacterController + PlayerController3D.cs + HealthComponent + InventoryComponent + PlayerPersistence (auto-registers)
│       └─ Children: CombatController (sword hitbox), FootstepPlayer
│
├── ─── NPCs ───
├── NPC_Bron                         ← NPCController.cs + NavMeshAgent; assign NPCData = NPCs/NPC_Bron
├── NPC_Mara                         ← NPCController.cs + NavMeshAgent; assign NPCData = NPCs/NPC_Mara
│
├── ─── Collectibles ───
├── Herb_1..Herb_5                   ← Collectible.cs + Collider (trigger); assign CollectibleData per
├── Wolf_1..Wolf_3                   ← enemy placeholder (cube + HealthComponent); add to "Enemy" tag
│
└── ─── UI ───
    └── Canvas (Screen Space - Overlay)
        ├── HUDController            ← HUDController.cs (assign all TMP fields + HP bar fill)
        ├── DialoguePanel            ← DialogueUI.cs (portrait + name + line + choices)
        ├── QuestLogPanel            ← QuestLogUI.cs (active + completed scroll content)
        ├── PausePanel               ← PauseMenuUI.cs (4 buttons)
        ├── InventoryPanel           ← InventoryUI.cs (slot template + grid + tooltip)
        ├── InteractPromptUI         ← InteractPromptUI.cs (World Space canvas child of Camera)
        └── UIInputBinder            ← UIInputBinder.cs (assign all 5 modal refs)
```

### Required Component References (inspector)

| Component | Assign |
|---|---|
| `GameManager` | nothing |
| `InputManager.PlayerInput` | Actions = `HearthwoodInputActions.inputactions`; Default Map = "Player" |
| `SceneController` | Drag every `SceneData` asset into the **Catalog** list |
| `AmbientAudioManager` | `Layers[]` (e.g. Village Day, Night, Storm), `Initial Layer Id` |
| `InteractionAudio` | Each `[SerializeField] AudioClip` field (use placeholders for now) |
| `HUDController` | 5 TMP fields + optional `HP Bar Fill` (Image with Fill Method = Horizontal) |
| `DialogueUI` | Panel, Portrait (Image), Name (TMP), Line (TMP), Choices Container (Vertical Layout Group), Hint (TMP), **Choice Button Template** (Button with TMP child) |
| `QuestLogUI` | Panel, Active Content, Completed Content, **Quest Entry Template** + **Objective Template** |
| `InventoryUI` | Grid (Grid Layout Group), **Slot Template** (Image + child Icon + Count TMP), Tooltip Panel + TMPs |
| `PauseMenuUI` | Panel + 4 Buttons (assign onClick in inspector or rely on `[SerializeField] Button _resumeButton` etc.) |
| `UIInputBinder` | Assign the 4 modal MonoBehaviours + Dialogue UI reference |
| `InteractPromptUI` | World-space canvas child, assign CanvasGroup + prompt TMP |
| `PlayerController3D` | Camera Transform reference (assigned to `_interactionCamera`); `HealthComponent`, `InventoryComponent`, `CombatController`, `FootstepPlayer` on children |
| `NPCController` | `NavMeshAgent` auto-added via RequireComponent; `NPCData` asset; `Interact Range` default 3 |

---

## 4. Bake the NavMesh

1. **Window ▸ AI ▸ Navigation** (requires `com.unity.ai.navigation` package — already
   in `manifest.json`).
2. Mark walkable surfaces as **Navigation Static**.
3. Add a **NavMeshSurface** component to a parent GameObject, click **Bake**.
4. NPCs can now wander.

---

## 5. Camera & Post-Processing

1. On Main Camera: assign `PlayerCamera.cs`; drag `Player` into `Target` field.
2. Optional but recommended: replace `PlayerCamera.cs` with a Cinemachine 3rd-person rig
   (the contract is read-only — any camera that follows the player works).

---

## 6. Test Play

1. Press **Play**.
2. Move with WASD; sprint with Shift; jump with Space.
3. Walk up to NPC_Bron — interaction prompt appears — press E — dialogue opens.
4. Press 1 or click to pick "I'll gather them" — quest starts.
5. Walk to a herb — press E — counter ticks up.
6. At 5 herbs → quest completes, +5 Favor Tokens, next quest starts automatically.
7. Press Tab to see the quest log. Press Esc to pause. Press I for inventory.

If something doesn't work, check the Console for errors. The most common issues:

- **"PlayerInput actions asset not assigned"** → assign `Assets/Settings/HearthwoodInputActions.inputactions`
- **NPCs don't move** → NavMesh not baked
- **Dialogue opens but no choices appear** → dialogue SO has empty `Choices[]` on the last node — edit via the **DialogueDataEditor** inspector
- **HUD shows nothing** → serialized TMP fields not assigned

---

## 7. Build for WebGL

1. **File ▸ Build Settings** → **WebGL** → **Switch Platform** (first time only — may take a few minutes).
2. **Player Settings**:
   - **Resolution**: 1280×720 default, allow downscale
   - **Publishing Settings ▸ Compression Format**: Brotli (or Gzip if your host blocks Brotli)
   - **Memory Size**: 256 MB minimum (URP uses more than Built-in)
3. **Build And Run** → uploads to `Builds/WebGL/`.
4. Open the generated `index.html` from any static host (GitHub Pages, itch.io, etc.).

WebGL-specific gotchas already handled by this prototype:

- `SaveSystem` uses `PlayerPrefs` (IndexedDB on WebGL), no `System.IO.File`
- No threads
- No DLLs
- Audio Vorbis/MP3 (no licensing-restricted formats)
- `Application.persistentDataPath` only used as fallback in `SceneController`

---

## 8. Asset Pipeline

See [`ASSETS.md`](./ASSETS.md) for the recommended pack list and import settings. The
prototype will run with primitive cubes if no assets are imported, but the visual quality
is a placeholder until you drop in real meshes/materials.

---

## 9. Project Validation Checklist

Before merging changes:

- [ ] Console shows 0 errors
- [ ] All UI panels toggle on their assigned keys (Tab/I/Esc)
- [ ] At least one full quest flow plays end-to-end
- [ ] `Application.persistentDataPath` is not used in any hot path
- [ ] No `Input.GetKey` / legacy input anywhere (grep)
- [ ] `ARCHITECTURE.md` §10 integration greps pass (see the file)
- [ ] No new asmdef files added (one per subsystem)
- [ ] All `[SerializeField] private` (no public fields on MonoBehaviours)
- [ ] Sample Content regenerates without errors

---

## 10. Preview-Only Flow (no Unity)

If you just want to see what the prototype plays like without installing Unity:

1. Open `Games/Hearthwood/preview.html` in any browser (double-click works).
2. Click **Begin Adventure**.
3. The preview demonstrates: dialogue, quest progression, inventory, save/load, combat
   (wolves), favor rewards, the full 3-quest chain ending in Iron Sword.

The preview is a vanilla-JS port of the gameplay systems. It does NOT need the Unity
Editor, asset packs, or any build step.
