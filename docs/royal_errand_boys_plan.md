# Royal Errand Boys — Project Development Plan
> FNA/XNA · 3D · Entity-Component-System · Co-op | Version 1.0

---

## Overview

Royal Errand Boys is a 3D co-op dungeon-extraction game built on FNA/XNA with a strict Entity-Component-System architecture. Players are hired by a perpetually dissatisfied King to retrieve a princess from a procedurally generated tower. They loot treasure along the way, physically carry the princess back out, and face the King's theatrical judgment at the end of each run before spending their reward at the Tavern to upgrade for the next job.

This document defines the high-level epics and user stories that form the full development plan from concept to shipped game. Epics are organized by system domain and tagged with priority and development phase.

**Priority Tags:**
- `P0` — Must ship. Blocking. The game does not function without this.
- `P1` — Should ship. Core experience is incomplete without it, but the game is technically playable.
- `P2` — Nice to have. Can be cut or deferred to post-launch.

---

## Development Phases

| Phase | Name | Goal |
|-------|------|------|
| 1 | **Foundation** | ECS engine core, FNA setup, rendering pipeline. No gameplay yet — just a solid technical base. |
| 2 | **Vertical Slice** | One floor, one session, players can move, carry princess, grab loot, and return to a placeholder King. Proves the core loop. |
| 3 | **Alpha** | Full run loop functional end-to-end. Multiple floors, tavern upgrades, enemies, basic King review. Playable but rough. |
| 4 | **Beta** | Content expansion. All epics implemented. Princess AI, boss fights, negotiation system, audio, full upgrade tree. |
| 5 | **Ship** | QA, polish, performance, platform prep. Content-complete and release-ready. |

---

## Epics & User Stories

---

### EPIC 01 — Project Foundation & ECS Architecture

> Establish the core technical foundation: project structure, FNA/XNA integration, and the Entity-Component-System (ECS) framework that all gameplay systems will be built upon. This epic gates everything else.

#### Story 1.1 — FNA Project Scaffold
`P0 | Phase 1`

As a developer, I need a working FNA project scaffold so I can start building game systems.

- FNA/XNA project setup with build pipeline
- Solution structure (Engine, Game, Content, Tests)
- CI/CD pipeline (GitHub Actions or similar)
- Coding standards & contribution guide

#### Story 1.2 — Core ECS Framework
`P0 | Phase 1`

As a developer, I need a core ECS framework so all game logic is decoupled from rendering.

- World, Entity, Component, System base classes
- Component registration & pooling system
- System execution ordering & dependency graph
- Entity lifecycle (create, destroy, tags, groups)

#### Story 1.3 — 3D Rendering System
`P0 | Phase 1`

As a developer, I need a 3D rendering system integrated with ECS so entities can be drawn.

- TransformComponent (position, rotation, scale)
- MeshRendererComponent
- Basic forward renderer with camera system
- Debug draw utilities (AABB, ray, grid)

#### Story 1.4 — Core Engine Services
`P0 | Phase 1`

As a developer, I need core engine services (input, audio, content) wired to ECS.

- InputSystem (keyboard, gamepad, mouse)
- AudioSystem with positional audio
- ContentManager integration for assets
- SettingsComponent & persistence layer

---

### EPIC 02 — 3D World & Level Generation

> Build the tower dungeon environment: procedural floor generation, lighting, collision, and the systems that make the world feel alive. This epic defines the physical space players will inhabit.

#### Story 2.1 — Procedural Tower Floors
`P0 | Phase 2`

As a player, I want a multi-floor tower that feels different each run so the game stays fresh.

- ProceduralFloorGeneratorSystem
- Room templates & connector logic
- Floor theme variations (crypt, treasure vault, etc.)
- Seed-based generation for reproducible runs

#### Story 2.2 — Physics & Collision
`P0 | Phase 2`

As a developer, I need a physics & collision system so characters and objects interact correctly.

- ColliderComponent (box, sphere, capsule)
- PhysicsSystem with gravity
- Trigger volumes for doors/events
- Layer-based collision filtering

#### Story 2.3 — Lighting & Atmosphere
`P1 | Phase 3`

As a player, I want atmospheric lighting and visual polish in the tower so it feels immersive.

- Dynamic lighting system (point, spot, ambient)
- LightComponent for ECS entities
- Shadow mapping (basic)
- Fog / depth cue system

#### Story 2.4 — Spatial Partitioning & Performance
`P1 | Phase 2`

As a developer, I need a scene graph and spatial partitioning so the game runs performantly.

- Octree / BVH spatial query system
- Culling system (frustum + basic occlusion)
- LOD component & distance culling
- Performance profiling overlay

---

### EPIC 03 — Player & Multiplayer Systems

> The crew. All systems relating to player characters, multiplayer session management, co-op interaction, and the role system that makes each run a collaborative negotiation.

#### Story 3.1 — Player Controller
`P0 | Phase 2`

As a player, I want to control my character in the 3D tower with responsive movement.

- PlayerControllerSystem (movement, jump, camera)
- CharacterControllerComponent
- AnimationComponent & state machine
- First/third person camera toggle

#### Story 3.2 — Multiplayer Session
`P0 | Phase 2`

As a group of players, we want to play together in the same tower session.

- Multiplayer session manager (LAN + online)
- NetworkSyncComponent & interpolation
- Player connection/disconnection handling
- Lobby system with role selection

#### Story 3.3 — Role System
`P1 | Phase 2`

As a player, I want to choose a role each run that changes how I interact with the world.

- RoleComponent (Carrier, Scout, Treasurer, Negotiator)
- Role-specific ability systems per role
- Role assignment UI in lobby
- Role swap mechanic between runs

#### Story 3.4 — Princess Carry Mechanics
`P0 | Phase 2`

As a Carrier, I need to physically hold and manage the princess so her state affects the run.

- CarryComponent & HandoffSystem
- PrincessStateComponent (health, mood, struggle)
- Drop detection & penalty trigger
- Handoff interaction between players

---

### EPIC 04 — Loot, Treasure & Inventory

> Everything that goes in the bag. The loot system, item definitions, inventory management, valuation, and the tension between grabbing treasure and keeping the princess safe.

#### Story 4.1 — Item Pickup & Carry
`P0 | Phase 2`

As a player, I want to pick up and carry treasure items with physical weight and encumbrance.

- ItemComponent (type, weight, value, rarity)
- InventorySystem with weight limits
- PickupInteractionSystem
- Item throw / drop mechanics

#### Story 4.2 — Loot Valuation
`P1 | Phase 2`

As a Treasurer, I want to track and assign value to loot so I can report to the King accurately.

- LootValuationSystem
- TreasureLedger component per session
- Item rarity tiers (Common, Rare, Legendary)
- Cursed / trap item types

#### Story 4.3 — Procedural Loot Placement
`P1 | Phase 2`

As a player, I want procedurally placed loot in the tower so each run has unique rewards.

- LootSpawnSystem with weighted tables
- Floor-difficulty scaling for loot quality
- Secret room detection & bonus loot
- Loot container types (chest, corpse, shrine)

#### Story 4.4 — Usable Items
`P1 | Phase 3`

As a player, I want to use items during the run that create interesting risk/reward decisions.

- Consumable item system
- UseItemSystem with cooldowns
- Item interaction events (broadcast to ECS)
- Active vs passive item distinction

---

### EPIC 05 — Princess AI & Behavior

> The cargo that fights back. The princess is a semi-autonomous character with mood, traits, and behaviors that change the difficulty and flavor of every run. She is not passive.

#### Story 5.1 — Personality Traits
`P1 | Phase 3`

As a player, I want the princess to have a personality trait each run that changes how she behaves.

- PrincessTraitComponent (Cooperative, Stubborn, Excited, Scared)
- TraitBehaviorSystem affecting carry difficulty
- Trait selection on run start (random/seeded)
- Trait effect communication to players (UI hints)

#### Story 5.2 — Mood System
`P1 | Phase 3`

As a player, I want the princess to react to how I treat her so my choices have consequences.

- MoodSystem (goodwill/trust score)
- MoodReactionSystem (help vs hinder behaviors)
- Princess dialogue system (bark lines)
- Mood persistence across a run & end report

#### Story 5.3 — Princess AI & Netcode Safety
`P0 | Phase 3`

As a developer, I need the princess AI to integrate cleanly with the ECS without breaking netcode.

- PrincessAISystem with deterministic behavior
- Network authority for princess state
- AI interrupt / override when carried
- Pathfinding component (NavMesh or A*)

---

### EPIC 06 — Enemies, Hazards & Combat

> Everything trying to stop the crew. Enemy AI, combat systems, environmental hazards, and the trap-filled gauntlet that becomes doubly dangerous on the way back down with cargo.

#### Story 6.1 — Combat System
`P0 | Phase 3`

As a player, I want to fight enemies in the tower using satisfying melee and ranged combat.

- CombatSystem (melee, ranged, collision-based)
- DamageComponent & health tracking
- HitReactionSystem & knockback
- Death & despawn handling

#### Story 6.2 — Enemy AI
`P0 | Phase 3`

As a developer, I need an enemy AI system that is extensible and ECS-friendly.

- EnemyAIComponent & BehaviorTreeSystem
- Enemy archetypes (Guard, Archer, Brute)
- AggroSystem with line-of-sight detection
- Patrol / idle / chase / attack state machine

#### Story 6.3 — Environmental Hazards
`P1 | Phase 3`

As a player, I want environmental hazards that punish careless movement, especially while carrying.

- HazardComponent (spike trap, pit, swinging blade)
- TrapTriggerSystem (player vs princess damage)
- Environmental hazard placement in proc gen
- Hazard state machine (armed, triggered, reset)

#### Story 6.4 — Boss Encounters
`P1 | Phase 4`

As a player, I want boss encounters at key tower floors to create memorable moments.

- BossComponent with phase system
- Boss arena room template
- Boss loot table (high-value rewards)
- Boss defeated event & floor unlock

---

### EPIC 07 — The King's Court & Reward System

> The end-of-run experience. The King reviews your haul, complains, docks pay, and reluctantly rewards the crew. The Negotiator earns their keep here. This epic drives the entire loot motivation loop.

#### Story 7.1 — King's Court Scene
`P0 | Phase 3`

As a player, I want a dramatic end-of-run review scene with the King judging our performance.

- KingsCourtSceneSystem
- RunSummaryComponent (loot, princess condition, time)
- King dialogue system with randomized complaints
- Animated King character with reaction states

#### Story 7.2 — Negotiation Minigame
`P1 | Phase 4`

As a Negotiator, I want to make choices during the King's review to maximize our payout.

- NegotiationMinigameSystem
- DialogueChoiceComponent with outcome weights
- King disposition modifier system
- Advisor bribe mechanic (from tavern upgrades)

#### Story 7.3 — Payout Calculation
`P0 | Phase 3`

As a player, I want the payout to feel fair-but-punishing so the King's dissatisfaction is funny.

- PayoutCalculationSystem (base + modifiers)
- Princess condition penalty table
- Loot condition modifier (broken items, etc.)
- Payout breakdown UI with King's commentary

#### Story 7.4 — King Relationship Memory
`P2 | Phase 4`

As a player, I want the King to have persistent memory of past runs so failure compounds.

- KingRelationshipComponent (per crew save)
- Persistent run history log
- Relationship tier effects on base payout
- Relationship-unlocked King dialogue branches

---

### EPIC 08 — Tavern & Upgrade Systems

> The between-run loop. Spending gold at the tavern to upgrade gear, unlock abilities, bribe NPCs, and prepare for the next run. The upgrade tree is the long-term progression hook.

#### Story 8.1 — Tavern Scene & Upgrade Tree
`P0 | Phase 3`

As a player, I want to visit the tavern between runs to spend my gold on meaningful upgrades.

- TavernSceneSystem & UI
- UpgradeComponent & UpgradeTreeSystem
- GoldCurrencySystem (persistent per save)
- Upgrade category tabs (Gear, Abilities, Bribes, Unlocks)

#### Story 8.2 — Gear Upgrades
`P1 | Phase 3`

As a player, I want gear upgrades that directly change how I play in the tower.

- Carrying harness upgrades (speed, drop resistance)
- Weapon upgrade tree
- Armor & survivability upgrades
- Tool upgrades (lockpicks, grapple, etc.)

#### Story 8.3 — Tavernkeeper NPC
`P2 | Phase 4`

As a player, I want the tavern keeper to remember us and react to our run history.

- TavernkeeperNPCComponent
- Run history dialogue system
- Tavern keeper tip system (hints for next run)
- Unlockable tavern services (medic, fence, scout)

#### Story 8.4 — Save & Persistence
`P0 | Phase 3`

As a developer, I need the save/progression system to persist correctly across sessions.

- SaveDataComponent & SerializationSystem
- Per-slot save with run history
- Upgrade state persistence
- Cloud save / local backup support

---

### EPIC 09 — UI, HUD & Game Feel

> Everything the player sees and hears that makes the game feel polished and readable. HUD, menus, feedback systems, audio, and all the juice that turns a prototype into a game.

#### Story 9.1 — In-Run HUD
`P1 | Phase 3`

As a player, I want a clear HUD that shows critical info without cluttering the screen.

- HUDSystem with ECS data binding
- Princess condition indicator (per carrier)
- Loot weight & inventory bar
- Role indicator & cooldown display

#### Story 9.2 — Menus & Navigation
`P1 | Phase 3`

As a player, I want menus that are functional, readable, and match the game's tone.

- Main menu, pause menu, settings
- Lobby UI & role select screen
- Run summary / King's court UI
- Tavern shop UI with upgrade tree visualization

#### Story 9.3 — Audio Systems
`P1 | Phase 4`

As a player, I want audio that reacts to gameplay and creates atmosphere.

- Dynamic music system (explore / combat / escape)
- Spatial SFX for footsteps, combat, hazards
- King voice lines & princess barks
- Tavern ambient audio system

#### Story 9.4 — Game Feel & Juice
`P2 | Phase 4`

As a player, I want visual and audio feedback that makes actions satisfying.

- Hit particles & screenshake system
- Loot pickup fanfare effects
- Princess drop impact feedback
- Camera shake & rumble on gamepad

---

### EPIC 10 — QA, Polish & Ship Readiness

> The final push. Performance profiling, bug fixing, content completion, platform validation, and all the unglamorous work that turns a nearly-done game into a shipped one.

#### Story 10.1 — QA Process & Automation
`P0 | Phase 5`

As a developer, I need automated and manual QA processes to catch regressions before ship.

- Unit tests for core ECS systems
- Integration tests for run flow
- Playtesting log & issue tracking process
- Regression suite for critical paths

#### Story 10.2 — Performance
`P0 | Phase 5`

As a player, I want the game to run at a stable framerate on target hardware.

- Performance profiling pass (CPU & GPU)
- ECS system bottleneck audit
- Asset optimization & atlas packing
- Target platform frame budget documentation

#### Story 10.3 — Content Completion
`P0 | Phase 5`

As a developer, I need a content-complete build with all runs, floors, and enemies implemented.

- All tower floors & themes implemented
- Full enemy roster & boss encounters
- Complete upgrade tree & all items
- All King/Tavern dialogue written & recorded

#### Story 10.4 — Release Pipeline
`P0 | Phase 5`

As a team, we need a release build pipeline and store presence ready for launch.

- Steam / platform build pipeline
- Store page assets (screenshots, trailer)
- EULA, credits, & legal requirements
- Day-1 patch process & hotfix pipeline

---

## Technical Notes

### ECS Architecture Guidance

All game logic must live in Systems, never in Components. Components are pure data bags. Entities are IDs only. The World object owns all component storage and system registration. This keeps the codebase testable, the netcode predictable, and the team unblocked from each other's work.

Recommended ECS approach: sparse component arrays keyed by entity ID, with system execution order defined declaratively via dependency attributes. Avoid any `Update()` methods on components.

### FNA/XNA Considerations

FNA is the recommended runtime for cross-platform support (Windows, Linux, macOS) while maintaining XNA API compatibility. The 3D rendering pipeline should use `BasicEffect` as a starting point with custom shaders introduced in Phase 3+. The content pipeline should use the standard XNA `ContentManager` extended to support hot-reload during development.

### Multiplayer Architecture

Multiplayer authority model: server-authoritative for all gameplay state (physics, loot, princess condition, enemy AI). Client-side prediction only for local player movement. Princess state is owned by the server exclusively. Netcode should be implemented as an ECS system layer, not baked into component logic, so it can be swapped or disabled for single-player mode.
