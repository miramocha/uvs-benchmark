# Enhanced fork changelog

Changes in this repository **on top of** Unity Visual Scripting **1.9.11** (baseline: `original-backup` branch, commit `70c928f`).

The upstream Unity release history remains in [CHANGELOG.md](CHANGELOG.md).

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

## [Unreleased]

### Changed

- **Monorepo:** package moved to [uvs-benchmark](https://github.com/miramocha/uvs-benchmark) at `Packages/com.unity.visualscripting/`. Git UPM installs use `?path=/Packages/com.unity.visualscripting` with `#stable`, `#enhanced`, or release tags.

## [1.9.11-enhanced.2] - 2026-05-20

Correctness fixes, C# Generator fixes, and repository/docs updates since [1.9.11-enhanced.1](https://github.com/miramocha/uvs-community-performance-optimization/releases/tag/v1.9.11-enhanced.1).

### Changed

- Repository renamed to **uvs-community-performance-optimization**; [README.md](README.md) and [package.json](package.json) Git URLs updated.
- [README.md](README.md): link to [uvs-benchmark](https://github.com/miramocha/uvs-benchmark) Play Mode performance suite.
- **ParameterValue**: constructor-based API; static pool reset on domain reload (`SubsystemRegistration`) per [#16](https://github.com/miramocha/uvs-community-performance-optimization/pull/16).

### Fixed

- **Issue #9 correctness** ([#16](https://github.com/miramocha/uvs-community-performance-optimization/pull/16)): `SetMember` chainable target, graph lifecycle manager listener registration leaks, teardown `Unregister` path, `ForEach` type-mismatch warnings, `Machine.UseCompiledGraph` edge cases.
- **C# Generator**: `CodeGeneratorValueUtility` and Humility value root compile errors ([#17](https://github.com/miramocha/uvs-community-performance-optimization/pull/17)).
- **UPM import**: `CHANGELOG.enhanced.md.meta` — removes immutable-folder “no meta file” warning ([#18](https://github.com/miramocha/uvs-community-performance-optimization/pull/18)).

### Removed

- **ParameterValue** `Create*` helper methods (replaced by constructors; [#16](https://github.com/miramocha/uvs-community-performance-optimization/pull/16)).

---

## [1.9.11-enhanced.1] - 2026-05-18

First tagged release of the enhanced fork on branch `enhanced`.

### Added

- **C# Generator** (editor): compile script graphs and enum assets to C#, with live preview window, asset compilers, and per-node generators for control flow, collections, math, logic, events, members, variables, time, NCalc, and related nodes.
- **On Awake** lifecycle event unit (*Events/Lifecycle*).
- **Graph lifecycle managers** (`GraphUpdateManager`, `GraphFixedUpdateManager`, `GraphLateUpdateManager`) to batch `Update`, `FixedUpdate`, and `LateUpdate` for registered event machines.
- **Struct-optimized reflection invokers** and accessors (`StructInstance*` paths, 0–5 arity) alongside existing instance/static optimized paths.
- **Shared editor utilities**: graph traversal, stack-trace handling, extended machine inspection.
- **Repository docs**: [README.md](README.md) fork install guide (`#enhanced`, `#stable`, embedded package); [CONTRIBUTING.md](CONTRIBUTING.md) branch model; GitHub ruleset payloads for PR-only `enhanced` and `stable` branches.
- **Runtime plugin**: `System.Runtime.CompilerServices.Unsafe.dll` under `Runtime/Plugins/` (see [Third Party Notices.md](Third%20Party%20Notices.md)).

### Changed

- **Optimized reflection pipeline**: `ParameterValue`, `OptimizedAccessorBase`, `OptimizedInvokerBase`, and related instance/static invokers refactored for fewer allocations and better struct support.
- **Flow runtime hot paths**: `Flow`, `ForEach`, `SetMember`, `GetVariable` / `SetVariable`, member units (`GetMember`, `SetMember`, `InvokeMember`, `Expose`), and graph pointer/reference pooling.
- **Event system**: `EventBus`, `EventHook`, `EventMachine`, and `IEventMachine` extended for centralized lifecycle registration and triggering.
- **Graph / machine internals**: `Graph`, `GraphData`, `GraphPointer`, `GraphReference`, `GraphStack`, `Machine` updated for new event and pooling behavior.
- **Platform stub writers** aligned with optimized reflection (`AccessorInfoStubWriter`, `MethodInfoStubWriter`, etc.).
- **Variable declarations** collections updated for performance and consistency.
- **Assembly definitions** (`Unity.VisualScripting.Core`, `Unity.VisualScripting.Flow`) updated for shared/generator and plugin references.

### Fixed

- Compile errors and obsolete warnings related to `Unsafe` usage and generator utilities.
- **Type → Object** conversion on optimized reflection invoke paths.
- Reduced unnecessary `Unsafe` intrinsics in `Member`, `ParameterValue`, and `Flow` while retaining the bundled DLL where needed.
- Minor editor widget fix (`ValueConnectionWidget`).

### Removed

- `FlowMachineEditor` (superseded by shared `MachineEditor` / inspection path).

---

## Baseline

| Item | Value |
|------|--------|
| Upstream package | `com.unity.visualscripting` **1.9.11** |
| Fork baseline branch | `original-backup` (`70c928f`, 2026-05-13) |
| Integration branch | `enhanced` |

Install tagged releases via Git URL hash (see [README.md](README.md)), e.g. `#v1.9.11-enhanced.2` or `#v1.9.11-enhanced.1`.
