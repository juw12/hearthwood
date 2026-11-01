# Assets

The prototype ships **without** bundled 3D / 2D assets because Unity Asset Store packs are
licensed per-developer and cannot be redistributed. This file lists what to download and how
to wire it in.

---

## 1. Asset Packs (recommended — free or one-time purchase)

| Pack | Source | Used for | License |
|---|---|---|---|
| **POLYGON Adventure Pack** | Unity Asset Store (Synty Studios) | Low-poly characters (Bron, Mara, player) | Single-project royalty-free |
| **POLYGON Village Pack** | Unity Asset Store (Synty Studios) | Houses, props, market stalls, tavern interior | Single-project royalty-free |
| **POLYGON Nature Pack** | Unity Asset Store (Synty Studios) | Trees, rocks, herb bushes | Single-project royalty-free |
| **Low-Poly Weapon Pack** | Various (e.g. Broken Vector on itch.io) | Iron sword, dagger, bow | Per-pack, usually CC0 |
| **RPG Essentials Sound Pack** | Various (e.g. Free SFX bundles) | Footsteps, interact, pickup, quest complete | Per-pack |
| **Ambient Loops** | Freesound.org (CC0 filters) | Village ambient (wind, crickets, forge clang) | CC0 |

> **Tip:** Synty's POLYGON packs share a consistent palette and are designed to mix. They
> integrate with the URP out-of-the-box after import.

---

## 2. Where to Put Imported Assets

```
Assets/
├── Art/
│   ├── Characters/      ← character prefabs, materials, textures
│   ├── Environment/     ← village buildings, terrain, props
│   ├── Icons/           ← SVG → PNG/Sprite conversions for inventory/UI
│   └── Materials/       ← URP/Lit material assets
├── Audio/
│   ├── Music/
│   ├── SFX/
│   └── Ambient/
├── Fonts/
│   └── MedievalSharp.ttf (or similar serif)
└── ScriptableObjects/   ← see Agent D's auto-generated samples
```

---

## 3. Settings to Apply After Import

1. **Material conversion**: POLYGON packs come in Standard shader. Use
   `Edit ▸ Render Pipeline ▸ Upgrade Selected Materials ▸ URP Lit` for each material folder.
2. **Texture import**: Set all textures to `Texture Type = Default`, `sRGB = ON` (color),
   `OFF` (normal/mask).
3. **Audio import**: Set music + ambient to `Load Type = Streaming`, `Compression Format = Vorbis`.
   Set SFX to `Decompress on Load`. WebGL target: keep all clips under 5 MB.
4. **Font**: Import a free serif font (MedievalSharp, IM Fell English, or similar). Place
   `*.asset` under `Assets/Fonts/`. Add to `TMP Essentials` so TextMeshPro picks it up.

---

## 4. Procedural / Free Alternatives

If you don't want to buy packs, you can ship the prototype with:

- **Unity Primitive Shapes** (Cube, Cylinder, Capsule, Sphere) for the village buildings —
  assign parchment-tinted URP/Lit materials
- **Free Synty POLYGON sample** (every pack has a free sample variant with fewer assets)
- **itch.io** — search "low poly medieval free" — many CC0 packs
- **Kenney.nl** — public-domain CC0 low-poly packs (characters, buildings, props)
- **Freesound.org** — CC0 ambient loops and SFX

---

## 5. Naming Conventions for Imported Prefabs

When you drop a Synty character prefab into the scene, rename it to match the NPC id:

| NPC | Prefab name in scene |
|---|---|
| Bron the Blacksmith | `NPC_Bron` |
| Mara the Innkeeper | `NPC_Mara` |

This keeps the Sample Content bootstrap wiring (which expects `NPC_Bron`, `NPC_Mara`) working
without renaming. Add an `NPCController` component to each prefab root and assign the matching
`NPCData` asset.

---

## 6. Asset Checklist for the Village Scene

The sample scene needs (at minimum):

- [ ] Ground plane (terrain or large cube with grass material)
- [ ] 3–5 houses (tavern, blacksmith, market, 2 generic)
- [ ] 1 well / central plaza
- [ ] 5+ trees
- [ ] 2 NPC characters (Bron, Mara)
- [ ] 1 player character
- [ ] 5 herb bushes scattered in a "forest edge" area (use small green spheres prefab as placeholder)
- [ ] 1 collectible prefab per `CollectibleData` (small cube with material tint)
- [ ] 1 directional light (sun)
- [ ] 1 ambient light + post-processing volume
- [ ] 1 camera (with `PlayerCamera` + Cinemachine optional)
- [ ] 1 `AudioManager` GameObject (with all 4 audio components)
- [ ] 1 `InputManager` GameObject (with `PlayerInput` + `HearthwoodInputActions` asset assigned)
- [ ] 1 `GameManager` GameObject
- [ ] 1 `SceneController` GameObject (with catalog populated)
- [ ] 1 `FavorTokenSystem` GameObject
- [ ] 1 `ItemDatabase` GameObject (auto-populates in Awake)
- [ ] 1 `QuestSystem` GameObject
- [ ] 1 `DialogueRunner` GameObject
- [ ] 1 UI Canvas with HUD, Dialogue, QuestLog, Pause, Inventory panels
- [ ] 1 `InteractionAudio` GameObject
- [ ] 1 `AmbientAudioManager` GameObject (with village loop assigned)

---

## 7. Critical Licensing Notes

- **MP3 audio is restricted on some WebGL hosts.** Convert all audio to `.ogg` (Vorbis) before
  importing into Unity, or use Unity's Vorbis import setting.
- **Font licensing**: prefer OFL (SIL Open Font License) fonts — IM Fell English, MedievalSharp,
  Crimson Text, Cinzel are all OFL.
- **Character packs**: most Synty assets are single-project / single-developer. If multiple
  people on your team need to use them, each must buy their own license.
- **CC0 assets** (Kenney, most Freesound with CC0 tag): unrestricted use, no attribution
  required (but appreciated).

---

**Honest limitation**: this prototype cannot be played without the user supplying assets. The
code is engine-complete; the visuals are a placeholder until the asset pipeline above is run.
The HTML preview (`preview.html`) demonstrates the gameplay loop without any of these assets.
