# GameFoundation

A modular, tested foundation for Unity games. It provides the runtime infrastructure so game design can start on solid ground.

> **Status:** pre-alpha. The API may change between minor versions until `v1.0`.

## Requirements

- Unity **6000.3.20f1**
- Universal Render Pipeline (URP)

## What is implemented

| System | Summary |
|---|---|
| Bootstrap | `GameEntryPoint` creates a persistent `GameServicesHost` before the first scene loads. |
| Service registry | `GameServiceRegistry` with explicit initialisation order and reverse-order shutdown. |
| Game loop | `GameController` runs `ControlledBehaviour` instances deterministically through the `IBehaviourScheduler`. |
| Pause | `PauseController` hands out `PauseToken`s instead of touching `Time.timeScale` directly. |
| Diagnostics | `DebugController`, in-game `DebugConsole`, `DebugCommandRegistry` and `PerformanceTracker`. |
| Tests | EditMode and PlayMode test assemblies. |

## Getting started

1. Clone the repository and open it with Unity 6000.3.20f1.
2. Open `Assets/Scenes/SampleScene.unity` and press Play. The services start automatically.
3. Run the tests from **Window > General > Test Runner**.

## Project rules

- **Architecture:** systems are modular and decoupled, and communicate through events rather than direct references. Gameplay logic runs through `ControlledBehaviour` and the `GameController` loop.
- **Pausing:** never stop the game ad hoc. Request a `PauseToken` from `PauseController` and release it when done.
- **Diagnostics:** log through `DebugController` with the right category and severity, not `Debug.Log`.
- **Code style:** XML doc comments on public members, `#region` sections as in existing systems, and no runtime dependency on `UnityEditor`.
- **Data:** definitions (ScriptableObjects) are immutable. Runtime state is kept separate and saved through the save system.
- **Unity hygiene:** always commit `.meta` files, and never commit `Library/`, `Temp/` or `Logs/`. Avoid two branches editing the same scene or prefab.
- **Work tracking:** every change starts from an issue and references it in the PR (`Closes #N`).

## Branch structure

| Branch | Purpose |
|---|---|
| `main` | Stable and playable. Tagged releases only (e.g. `v0.1.0`). |
| `develop` | Integration branch and default. All work lands here first. |
| `feature/<issue#>-<slug>` | New systems and features. |
| `fix/<issue#>-<slug>` | Bug fixes. |
| `refactor/<slug>` | Restructuring without behaviour change. |
| `chore/<slug>` | CI, tooling, project settings. |
| `docs/<slug>` | Documentation. |
| `hotfix/<slug>` | Urgent fix branched from `main`, merged into `main` and `develop`. |
| `epic/<slug>` | Optional integration branch for large milestones (e.g. `epic/m4-modular-control`). |

### Workflow

1. Branch from `develop` (`hotfix/*` from `main`).
2. Open a PR into `develop` with `Closes #N`.
3. **Squash-merge** into `develop`. Branches are deleted after merge.
4. At each milestone, merge `develop` into `main` with a **merge commit** and tag it.
5. Rebase on `develop` regularly to limit scene and prefab conflicts.

### AI usage

The architecture and all C# runtime and test code in this project are written by me. I use AI assistants (GitHub Copilot) as a support tool for:

- **Project management:** roadmap structuring, milestone and issue planning.

- **Code review:** feedback on commits, design and edge cases, which I then evaluate and apply myself.

- **Documentation and repository setup:** outlines, drafts, and configuration files such as `.editorconfig`, `LICENSE` and the base README.

Design decisions and their trade-offs are mine, and I'm responsible for everything that is merged. The objective of this project is to
learn, improve and enable others to come along with me in this process. 

## License

Released under the [MIT License](LICENSE).
