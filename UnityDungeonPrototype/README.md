# Unity Dungeon Prototype (Dragon + Crystal + Guardians)

This folder contains a ready-to-import script architecture for the dungeon gameplay loop:
- mana crystals and partial/full siphoning,
- dragon growth and stage progression,
- sacred pressure plate gate validation,
- guardian AI reaction through NavMesh,
- guardian death rewards (materials + mana return).

## Folder Layout
- `Assets/Scripts/Core` - global event hub.
- `Assets/Scripts/Dragon` - dragon progression and hunger logic.
- `Assets/Scripts/Mana` - crystals and player drain interaction.
- `Assets/Scripts/Environment` - gate puzzle, alarms, movable noise/light systems.
- `Assets/Scripts/Guardians` - guardian states, AI, niche blocking, rewards relay.
- `Assets/Scripts/Player` - player health and resources.
- `Docs` - setup and architecture docs.

## Quick Start
1. Create/open a Unity project (3D, URP or Built-in).
2. Copy this `UnityDungeonPrototype/Assets/Scripts` folder into your Unity `Assets`.
3. Open `Docs/SCENE_SETUP.md` and wire references in inspector.
4. Bake NavMesh before testing guardian behavior.
