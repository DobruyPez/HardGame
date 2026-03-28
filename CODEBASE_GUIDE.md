# HardGame/UnityDungeonPrototype - Codebase Guide for Developers

**Last Updated:** March 2026  
**Status:** Active Development (Pre-Release)

---

## 📋 Quick Overview

HardGame is an event-driven dungeon prototype featuring:
- **Player** (Princess): Escapes dungeon by draining mana crystals
- **Dragon Companion**: Grows stronger with stolen mana, unlocks gates at high stages
- **Guardians**: AI enemies with multi-state threat detection (Dormant → Hunt → Dead)
- **Crystal Mana System**: Draining crystals empowers dragon but alerts enemies

**Core Loop:** Drain crystals → Dragon grows → Repel enemies → Reach exit to win

---

## 🏛️ Architecture: Event-Driven System

### Central Hub: `GameEvents` (Core/GameEvents.cs)

All major systems communicate via static events. **No direct dependencies between systems.**

```
Crystal Drain → GameEvents.RaiseCrystalDrainProgress
              ↓
         [Listeners: Dragon, LightManager, CrystalDrainAlarmRelay]
              ↓
         Dragon updates mana → GameEvents.RaiseDragonManaChanged
              ↓
         [Listeners: GateController, UI, GuardianController]
```

**✅ BEST PRACTICE:** Always unsubscribe from events in `OnDisable()` to prevent memory leaks.

```csharp
private void OnEnable()
{
    GameEvents.SomeEvent += MyHandler;
}

private void OnDisable()
{
    GameEvents.SomeEvent -= MyHandler;  // CRITICAL: Remove listener
}
```

---

## 🎯 Core Systems

### 1. **Dragon Companion** (Dragon/DragonCompanion.cs)
**Responsibility:** Mana storage, growth stages, visual representation  
**Three Stages Based on Mana:**
- **Hatchling** (0-34 mana): Small, weak, damages player from starvation
- **Companion** (35-74 mana): Medium, repels guardians within 5m
- **Sacred** (75+ mana): Large, full power, opens high-level gates

**Key Methods:**
- `AddMana(amount)` - Called by `ManaDrainInteractor` after crystal drain
- `RemoveMana(amount)` - Called by `DragonHungerSystem` for starvation
- `IsAtLeastStage(stage)` - Check if dragon can activate certain abilities

**Optimization Note:** All stage models pre-instantiated in `Awake()`, not every frame.

---

### 2. **Mana Crystals & Draining** (Mana/)

#### ManaCrystal.cs
- Stores mana amount, handles depletion
- Broadcasts via `GameEvents.CrystalDepleted` when empty
- Emits visual feedback (light fading, color changes)

#### ManaDrainInteractor.cs **[OPTIMIZED]**
- Player holds **E** to drain nearby crystal
- **Optimization:** Caches nearby crystals per drain attempt, not every frame
  - `RefreshCrystalCache()` called once on E-press
  - Avoids expensive `Physics.OverlapSphere()` spam
- Transfers drained mana to dragon

---

### 3. **Guardian AI** (Guardians/GuardianController.cs) **[HEAVILY DOCUMENTED]**

**7 States (Threat-Based State Machine):**

```
Dormant ──[noise heard]──→ Stirring ──[investigate]──→ Investigating
                                           ↓
                              [high threat or player found]
                                           ↓
                                        Hunting ──[defeated]──→ Dead
                                           ↑
                                           └─[dragon nearby] Repelled

Special: Trapped = niche exit blocked, cannot leave spawn area
```

**Threat Levels (0-1 scale):**
- `stirThreat` (0.25): Minimal noise → Wake up
- `huntThreat` (0.85): Full threat → Active hunt

**Input Channels:**
1. **Noise Events** - `GameEvents.NoiseEmitted` from crystals/gates
2. **Crystal Depletion** - Highest threat, immediate hunt
3. **Proximity Aggro** - Line-of-sight + distance detection
4. **Hitbox Relays** - GuardianAggroHitbox and GuardianAttackHitbox

**Key Optimization:** 
- Distance checks before raycasts (early exit)
- `TryAttachToNavMesh()` called every frame but cheap (only re-attaches if needed)
- Multiple safety checks prevent NavMesh errors on startup

---

### 4. **Player Health** (Player/PlayerHealth.cs)
- Damaged by: Guardian attacks, Dragon starvation
- Healed by: Guardian defeats via `GuardianDeathRewardRelay`
- **On death:** Level immediately fails

---

### 5. **UI System**
#### PlayerHPSlider.cs **[OPTIMIZED]**
- Animates health bar with smooth lerp
- Animation speed is now configurable (was hardcoded 5f)
- Threshold constant prevents floating-point precision errors

#### SliderMana.cs
- Displays dragon mana percentage

---

## 📊 Data Flow Diagram

```
┌─────────────────┐
│     Player      │
└────────┬────────┘
         │ E-key
         ↓
┌──────────────────────────────┐
│ ManaDrainInteractor          │
│ (Find nearby crystal)        │
└────────┬─────────────────────┘
         │
         ↓ Drain(amount)
┌──────────────────────────────┐
│ ManaCrystal                  │
│ (Deplete, broadcast events)  │
└────────┬─────────────────────┘
         │ GameEvents.CrystalDepleted
         ↓
    ┌────┴────┐
    ↓         ↓
┌──────────┐  ┌─────────────────────────┐
│ Dragon   │  │ GuardianController      │
│ (AddMana)│  │ (StartHunt instantly)   │
└────┬─────┘  └─────────────────────────┘
     │ ManaChanged
     ↓
┌─────────────────┐
│ GateController  │
│ (Check weight)  │
└─────────────────┘
```

---

## 🔧 Configuration Best Practices

### Store Magic Numbers in Constants
```csharp
private const float LERP_COMPLETION_THRESHOLD = 0.01f;
private const float DAMAGE_THRESHOLD = 0.01f;
```

### Use [Header] to Organize Inspector
```csharp
[Header("Awareness")]
[SerializeField] private float stirThreat = 0.25f;

[Header("Combat")]
[SerializeField] private float maxHealth = 60f;
```

### Use XML Documentation
```csharp
/// <summary>Adds mana to the dragon from crystal drain.</summary>
/// <param name="amount">Mana to add. Clamped to max capacity.</param>
/// <returns>Actual mana added (may be less if already at max).</returns>
public float AddMana(float amount) { ... }
```

---

## 🐛 Common Pitfalls & Solutions

### ❌ **Pitfall 1: Subscribing Without Unsubscribing**
```csharp
// BAD: Memory leak - listener stays even after object destroyed
private void OnEnable() => GameEvents.NoiseEmitted += Handler;
```

✅ **Solution:**
```csharp
private void OnEnable() => GameEvents.NoiseEmitted += Handler;
private void OnDisable() => GameEvents.NoiseEmitted -= Handler;
```

---

### ❌ **Pitfall 2: Expensive Physics Queries in Update()**
```csharp
// BAD: Physics.OverlapSphere called 60 times/second
private void Update()
{
    FindNearestCrystal();  // Expensive!
}
```

✅ **Solution:** Cache results and reuse
```csharp
private int _lastCacheFrame = -999;

private void Update()
{
    if (Time.frameCount != _lastCacheFrame)
    {
        RefreshCache();
        _lastCacheFrame = Time.frameCount;
    }
}
```

---

### ❌ **Pitfall 3: Null References from FindObjectByType**
```csharp
// May fail if scene loading incomplete
DragonCompanion dragon = FindObjectOfType<DragonCompanion>();
if (dragon == null) Debug.LogError("Dragon not found!");  // Crashes
```

✅ **Solution:** Use GetComponent from cached references
```csharp
[SerializeField] private DragonCompanion dragon;  // Inspector assignment
if (dragon == null) return;  // Safe fallback
```

---

## ✨ Optimization Tips

### 1. **Use `sqrMagnitude` Instead of `magnitude`**
```csharp
// Slow - calculates square root
float distance = Vector3.Distance(a, b);
if (distance < threshold) { ... }

// Fast - skips square root
float sqrDistance = (a - b).sqrMagnitude;
if (sqrDistance < threshold * threshold) { ... }
```

### 2. **Early Exit Patterns**
```csharp
// Check quick conditions first, expensive operations last
if (_state == Dead) return;  // Fast
if (player == null) return;  // Fast
if (Physics.Raycast(...)) { ... }  // Expensive - only if needed
```

### 3. **Cache Component Lookups**
```csharp
// Bad - GetComponent every time
for (int i = 0; i < 100; i++)
{
    hit[i].GetComponent<Guardian>().TakeDamage(1);
}

// Good - cache once
Guardian guardian = hit[0].GetComponent<Guardian>();
for (int i = 0; i < 100; i++)
{
    guardian.TakeDamage(1);
}
```

### 4. **Use Object Pooling for Frequently Created/Destroyed Objects**
**Future Enhancement:** Pool guardians, projectiles, effects instead of Instantiate/Destroy

---

## 🧪 Testing Guidelines

### When Adding New Features, Test:
1. **Memory Leaks:** Subscribe/unsubscribe patterns
2. **NavMesh Initialization:** Test on cold start before NavMesh bakes
3. **Null References:** Disable components in inspector and ensure graceful fallback
4. **Performance:** Profile Physics queries, animation updates

---

## 📚 File Structure

```
HardGame/Assets/Scenes/Assets/Scripts/
├── Core/
│   └── GameEvents.cs          ← Central event hub (READ THIS FIRST)
│
├── Dragon/
│   ├── DragonCompanion.cs     ← Main dragon logic
│   ├── DragonStage.cs         ← Enum definition
│   ├── DragonHungerSystem.cs  ← Starvation mechanic
│   └── HungerVisualEffects.cs
│
├── Mana/
│   ├── ManaCrystal.cs         ← Harvestable resources
│   └── ManaDrainInteractor.cs ← Player E-key interaction [OPTIMIZED]
│
├── Guardians/
│   ├── GuardianController.cs  ← Complete AI system [FULLY DOCUMENTED]
│   ├── GuardianState.cs       ← State enum
│   ├── GuardianDeathRewardRelay.cs
│   ├── GuardianAggroHitboxRelay.cs
│   └── GuardianNicheBlocker.cs
│
├── Player/
│   ├── PlayerHealth.cs        ← Health management
│   └── PlayerResourceInventory.cs
│
├── Environment/
│   ├── GateController.cs      ← Animated gates
│   ├── PressurePlateGate.cs   ← Dragon weight triggers  
│   ├── LightManager.cs        ← Crystal light fading
│   ├── CrystalDrainAlarmRelay.cs
│   └── ...
│
├── CanvasUI/
│   ├── PlayerHPSlider.cs      ← Health bar [OPTIMIZED]
│   └── SliderMana.cs
│
├── Editor/
│   └── Scene builders (procedural level generation)
```

---

## 🚀 Next Steps for Development

1. **Extract Configuration to ScriptableObjects**
   - Move all SerializeField parameters to SO configs
   - Allows easy tuning without code changes

2. **Implement Object Pooling**
   - Pool guardians, crystals, effects
   - Reuse instead of Instantiate/Destroy

3. **Add State Transition Logging**
   - Debug guardian state changes
   - Help diagnose AI behavior issues

4. **Replace SimpleDictionary Searches with Caching**
   - `stageModels.Find()` in DragonCompanion could be cached

5. **Consider Behavior Tree for Advanced Guardian AI**
   - Current enum-based state machine works but is hard to extend
   - Behavior trees allow modular, reusable AI components

---

## 📞 Questions?

- Check XML documentation comments in your IDE (hover over methods)
- Read the event definitions in `GameEvents.cs` to understand data flow
- Check the state machine diagram in `GuardianController.cs` header

**Good luck, team! 🚀**
