# Unity Dungeon Prototype Architecture

## Goal
Build a loosely-coupled gameplay architecture for:
- crystal mana draining with partial/full outcomes,
- dragon growth and stage progression,
- sacred pressure plate + gate validation,
- guardian awakening, hunt, and reward loop.

## Design Principles
- Event-driven communication through `GameEvents` (Actions).
- Minimal direct references across systems.
- Runtime-extensible: can replace action events with ScriptableObject channels later.
- Scene wiring through inspector references.

## Core Runtime Flow
1. Player holds `E` near `ManaCrystal` (`ManaDrainInteractor`).
2. Crystal emits drain events and noise pulses.
3. Dragon receives mana, scales smoothly (`DragonCompanion`).
4. `CrystalDrainAlarmRelay` and crystal noise wake/stir guardians.
5. Dragon stands on `PressurePlateGate`; if `ManaWeight >= required`, `GateController` opens and lights slots with dragon essence color.
6. If guardians die, `GuardianDeathRewardRelay` grants materials and returns part of mana to dragon.

## Stage Rules
- Stage 1: Hatchling
  - Dragon needs feeding.
  - `DragonHungerSystem` drains princess HP if not fed.
  - Guardians react strongly to crystal drain and full depletion.
- Stage 2: Companion
  - Dragon can repel nearby guardians (`GuardianController`).
- Stage 3: Sacred
  - Highest mana weight for late gate checks and advanced access.

## Suggested Extensions
- Replace debug combat with hitbox + damage interfaces.
- Add animation hooks for guardian state transitions.
- Add stealth modifiers for sound radius and threat.
- Add craft recipes consuming `GuardianShardMaterials`.
