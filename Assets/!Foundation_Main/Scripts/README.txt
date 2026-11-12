# Script assembly layers

This document describes the assembly boundaries under `Assets/!Ritualica_Main/Scripts`, the dependency rules they should follow, and the current state of the assembly definitions.

## Why assembly layers?

Assembly Definitions (`.asmdef`) let Unity compile related scripts together and make dependencies explicit. Clear boundaries help keep systems modular, reduce accidental coupling, and make it easier to test or replace one system without changing unrelated code.

A dependency should point from a higher-level system to the lower-level contracts it needs—not back from shared infrastructure into every system that uses it.

## Current assembly definitions

The following assembly names and references were found:

| Assembly name | Current references |
|---|---|
| `Ritualica.Systems` | `Ritualica.Systems.Animation`, `Ritualica.Systems.Data`, `Ritualica.Systems.Game`, `Ritualica.Systems.Load`, `Ritualica.Systems.Profiling`, `Ritualica.Systems.States` |
| `Ritualica.Systems.Animation` | `Ritualica.Systems` |
| `Ritualica.Systems.Data` | `Ritualica.Systems` |
| `Ritualica.Systems.Game` | `Ritualica.Systems.Debug` |
| `Ritualica.Systems.Load` | `Ritualica.Systems` |
| `Ritualica.Systems.Profiling` | `Ritualica.Systems` |
| `Ritualica.Systems.States` | `Ritualica.Systems` |

The assembly's identity comes from the `.asmdef` JSON `name` property, not its filename. For example, `Ritualica.Assembly.Data.asmdef` defines the assembly `Ritualica.Systems.Data`.

### Current issues to resolve

- `Ritualica.Systems` references several feature assemblies that reference it in return. Assembly references must not form cycles.
- `Ritualica.Systems.Game` references `Ritualica.Systems.Debug`, but no matching `.asmdef` was found.
- The assembly layout and namespaces should be checked together so that names communicate each layer's responsibility.

Treat the table above as a snapshot of the current definitions, not as the desired final dependency graph.

## Recommended dependency direction

Use a small shared foundation assembly and keep feature modules independent:

```text
Ritualica.Core
    ↑
    ├── Ritualica.Systems.Game
    ├── Ritualica.Systems.Data
    ├── Ritualica.Systems.Load
    ├── Ritualica.Systems.States
    ├── Ritualica.Systems.Animation
    ├── Ritualica.Systems.Audio
    ├── Ritualica.Systems.Profiling
    └── Ritualica.Systems.Debug

Ritualica.Bootstrap → Core + the systems it composes
Ritualica.Tests.*   → the assemblies under test