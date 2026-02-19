# AGENTS.md — Unity Game Development Agent Instructions
## JetBrains Rider | Unity 6000.1.17f1

---

## Overview

You are a coding agent operating inside **JetBrains Rider** to assist in the design and development of a **Unity 6000.1.17f1** project. Your role is to write, refactor, debug, and scaffold Unity C# code and project assets in a clean, maintainable, and performant manner.

Always prefer Unity 6-idiomatic patterns and APIs. Do not suggest deprecated Unity APIs unless explicitly asked.

---

## Environment

| Property | Value |
|---|---|
| IDE | JetBrains Rider (latest) |
| Unity Version | 6000.1.17f1 (Unity 6) |
| Language | C# 10+ |
| Target Platform | As specified per task (default: Standalone/PC) |
| Render Pipeline | Assume **URP** unless told otherwise |
| Scripting Backend | IL2CPP (Release) / Mono (Development) |

---

## Project Conventions

### Naming Conventions

- **Classes / Structs / Enums:** `PascalCase` — e.g., `PlayerController`, `EnemyState`
- **Methods:** `PascalCase` — e.g., `TakeDamage()`, `OnPlayerDeath()`
- **Private fields:** `camelCase` — e.g., `health`, `rigidbody`
- **Public properties:** `PascalCase` — e.g., `Health`, `IsGrounded`
- **Constants:** `ALL_CAPS_SNAKE_CASE` — e.g., `MAX_HEALTH`
- **Interfaces:** Prefix with `I` — e.g., `IDamageable`, `IInteractable`
- **ScriptableObjects:** Suffix with `SO` or `Data` — e.g., `WeaponDataSO`
- **Scenes:** `PascalCase`, descriptive — e.g., `MainMenu`, `MountainFields`
- **Prefabs:** Match their primary script name — e.g., `PlayerCharacter.prefab`

---

## Coding Standards

### General Rules

- All MonoBehaviours must be placed on GameObjects; use ScriptableObjects for pure data.
- Do **not** use `GameObject.Find()` or `FindObjectOfType()` in Update loops. Cache references in `Awake()` or `Start()`.
- Avoid `string`-based API calls (e.g., `Invoke("MethodName", ...)`) — prefer coroutines, `UnityEvents`, or delegates.
- Null-check all external references fetched via `GetComponent<>()`.
- Use `[SerializeField] private` instead of `public` for Inspector-exposed fields.
- Prefer `TryGetComponent<T>()` over `GetComponent<T>()` where null is a valid outcome.

### Unity 6 Specifics

- Use the **Input System** package (new) — do **not** use `Input.GetKey()` / legacy input.
- Use **UnityEngine.Pool** (`ObjectPool<T>`) for object pooling — do not write custom pools from scratch.
- Prefer **Addressables** for asset loading over `Resources.Load()`.
- Use `Awaitable` (Unity 6 native async) or `UniTask` for async operations — avoid raw `Task`/`async void` on MonoBehaviours.
- Physics queries should use the non-allocating variants (e.g., `Physics.RaycastNonAlloc()`).
- For UI, use **UI Toolkit** for new screens; use Unity UI (uGUI) only if maintaining existing components.

### Performance Guidelines

- Never allocate in `Update()`, `FixedUpdate()`, or `LateUpdate()` — no LINQ, no `new`, no string concatenation.
- Use `[System.Serializable]` structs over classes for small value-type data passed frequently.
- Profile before optimizing; note any suspected hot paths with a `// PERF:` comment.
- Prefer `CompareTag()` over `tag ==` string comparison.

---

## Architecture Patterns

Use the following patterns consistently:

- **Game Manager:** A persistent singleton (`DontDestroyOnLoad`) that owns global game state (current level, score, game phase) if needed.
- **Event System:** Use a central `GameEventsSO` (ScriptableObject-based events) or C# `Action`/`event` delegates for decoupled communication between systems.
- **State Machines:** Implement character/enemy logic as explicit state machines (enum-driven or class-driven). Do not embed complex branching logic directly in `Update()`.
- **ScriptableObject-Driven Data:** All tunable game data (stats, item configs, level configs) should live in ScriptableObjects, not hardcoded in MonoBehaviours.
- **Command Pattern:** Use for player input handling and anything that needs undo/replay support.

---

## Scene & Prefab Workflow

- Every scene must have a **Bootstrap** or **Initializer** object that sets up services before any gameplay logic runs.
- Prefabs should be **self-contained** — a prefab should not depend on a specific scene hierarchy to function.
- Use **Prefab Variants** for shared-base objects with minor differences (e.g., different enemy types).
- Scenes should be kept lean; heavy data belongs in ScriptableObjects or loaded via Addressables.

---

## Testing & Debugging

- Write **Unity Test Framework** (EditMode/PlayMode) tests for core logic where feasible — place them in `Assets/StickWarfare3D/Tests/`.
- Use `Debug.Log()` sparingly in production code; wrap logs in a custom `GameLogger` utility that can be toggled off in builds.
- Use `[ContextMenu("Test Action")]` on MonoBehaviours for quick in-editor manual testing of methods.
- Gizmos (`OnDrawGizmos`) should be used to visualize spatial logic (attack ranges, patrol paths, etc.).

---

## What to Do When Asked a Task

- **Check existing patterns** — match the style and architecture already present in the project.
- **Write complete, compilable code** — do not leave placeholder `// TODO` stubs unless asked to scaffold.
- **Flag concerns** — if a requested approach conflicts with performance, Unity best practices, or these guidelines, note it before proceeding.
- **One responsibility per class** — if a script is growing beyond a single clear purpose, suggest a split.

---

## What to Avoid

- Do **not** modify files outside `Assets/` unless explicitly instructed.
- Do **not** change `.meta` files manually.
- Do **not** add packages to `Packages/manifest.json` without confirming with the user.
- Do **not** use `Thread` or raw `Task.Run` for anything touching Unity objects — Unity APIs are not thread-safe.
- Do **not** generate AI art, audio, or binary asset files — only scripts, scene configs, and text-based assets.

---

## Useful References

- Unity 6 Manual: https://docs.unity3d.com/6000.1/Documentation/Manual/
- Unity 6 Script API: https://docs.unity3d.com/6000.1/Documentation/ScriptReference/
- URP Documentation: https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.0/manual/
- Input System: https://docs.unity3d.com/Packages/com.unity.inputsystem@1.8/manual/
- Addressables: https://docs.unity3d.com/Packages/com.unity.addressables@2.2/manual/

---

*Last updated: 2026-02-19 | Unity 6000.1.17f1*
