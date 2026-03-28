# Scene Setup Checklist

## 1) Dragon
- Create dragon companion object.
- Add `DragonCompanion`.
- Configure:
  - max mana,
  - stage thresholds,
  - hatchling/sacred scales,
  - essence gradient.
- Add `DragonHungerSystem` to any game manager object and assign dragon + player health.

## 2) Player
- Add `PlayerHealth` to princess.
- Add `ManaDrainInteractor` and assign dragon reference.
- Set `crystalMask` to include crystal colliders.

## 3) Crystals
- Add `ManaCrystal` to each crystal object.
- Assign crystal renderer with emissive material.
- Ensure crystal colliders are in `crystalMask` layer.
- Optional: attach `PortableCrystalLantern` when crystal is movable light source.

## 4) Alarm + Noise
- Add `CrystalDrainAlarmRelay` to scene manager object.
- Add `MovableNoiseSource` to heavy movable boxes or noisy props.

## 5) Gate Puzzle
- Place pressure plate trigger collider.
- Add `PressurePlateGate` and set required mana weight.
- Add `GateController` to gate root; assign moving mesh and slot renderers.

## 6) Guardians
- Add `NavMeshAgent` and `GuardianController` to each guardian.
- Assign princess transform and dragon reference.
- Optional: add `GuardianNicheBlocker` at spawn niche exits for "block with box" strategy.
- Bake NavMesh for dungeon.

## 7) Rewards
- Add `PlayerResourceInventory` to player.
- Add `GuardianDeathRewardRelay` on manager object, assign inventory + dragon.

## 8) Validation Playtest
- Partial crystal drain should stir/attract guardians.
- Full drain should trigger aggressive hunt.
- Dragon should grow smoothly and update stage thresholds.
- Plate should open gate only when dragon mana weight is enough.
- Stage 2 dragon should repel close guardians.
