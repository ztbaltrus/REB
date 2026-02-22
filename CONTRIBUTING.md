# Contributing to Royal Errand Boys

## Architecture

All game logic lives in **Systems**. Components are pure data bags. Entities are integer IDs.
The `World` owns all component storage and system registration.

```
World
├── Entity management (create / destroy / version)
├── ComponentPool<T>  (sparse set per component type)
├── Tag system        (string tags per entity)
└── Systems           (topologically sorted, Update then Draw)
```

## Coding Standards

### General

- C# 12, `net8.0`, `Nullable enable`, `ImplicitUsings enable`.
- No logic in components. No XNA types in the ECS core (`REB.Engine/ECS/`).
- Keep `GameSystem` subclasses focused — one responsibility per system.
- Prefer `ref T GetComponent<T>()` for in-place mutation (avoids copies on large structs).

### Naming

| Concept | Convention | Example |
|---------|------------|---------|
| Component | `PascalCase` + `Component` suffix | `TransformComponent` |
| System | `PascalCase` + `System` suffix | `RenderSystem` |
| ECS attribute | `PascalCase` + `Attribute` suffix | `RunAfterAttribute` |
| Private field | `_camelCase` | `_world` |
| Constant | `PascalCase` | `MaxEntities` |

### System ordering

Declare dependencies with `[RunAfter(typeof(OtherSystem))]` on your system class.
The `World` performs a topological sort on startup.

```csharp
[RunAfter(typeof(InputSystem))]
[RunAfter(typeof(PhysicsSystem))]
public class PlayerControllerSystem : GameSystem { ... }
```

### Components

Components must be `struct` and implement `IComponent`. No methods except field helpers.

```csharp
public struct HealthComponent : IComponent
{
    public int Current;
    public int Max;

    public readonly bool IsDead => Current <= 0;
}
```

### Tests

Every ECS system and core utility should have a corresponding test in `tests/REB.Tests/`.
Tests use xUnit. Keep test files mirroring the source tree:

```
src/REB.Engine/ECS/World.cs  →  tests/REB.Tests/ECS/WorldTests.cs
```

## Workflow

1. Branch from `develop` — name: `feature/short-description` or `fix/short-description`.
2. Write tests before (or alongside) implementation.
3. Run `dotnet test REB.sln` locally before pushing.
4. Open a PR targeting `develop`; `main` is protected and receives merges from `develop` only.
5. PRs require at least one review and a green CI run.

## Project structure

```
REB/
├── FNA/                    FNA library (submodule, do not modify)
├── docs/                   Design documents
├── src/
│   ├── REB.Engine/         ECS core + all engine systems
│   └── REB.Game/           Game bootstrap and scenes
├── tests/
│   └── REB.Tests/          xUnit test project
├── .github/workflows/      CI pipeline
├── CONTRIBUTING.md         This file
└── REB.sln
```
