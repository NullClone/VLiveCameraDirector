# VLiveCameraUnit

VLiveCameraUnit is a Unity 6.3+ / Cinemachine 3 package for performing prepared
live-camera shots with operator control and reliable composition assistance. The
current goal is the manual A/B core: one output Camera and Brain, a stable fixed
shot, and an independently moving spline shot. MIDI, recommendation, and
automation come after this workflow is proven.

## Read first

Use this priority when instructions conflict:

1. The user's instruction for the current task.
2. This file.
3. The relevant document under `Docs/`.
4. The current implementation.

| Task | Source |
| --- | --- |
| Product direction | `Docs/overview.md` |
| Runtime design and camera behaviour | `Docs/architecture.md`, `Docs/spec-camera.md` |
| Input and switching | `Docs/spec-operation.md`, `Docs/spec-switching.md` |
| Assistance rules | `Docs/spec-assistance.md` |
| Current implementation phase | `Docs/phases.md` |
| C# and Inspector style | `Docs/code-style.md` |
| Delegation, verification, and Git | `Docs/workflow.md` |

Read only what the task needs. Code comments own local reasons; `Docs/` owns
cross-file contracts and product decisions; commit messages own why a change was
made. Do not duplicate the same explanation.

## Boundaries

- Use `toshi.VLiveKit.Camera`, `.Editor`, and `.Tests` namespaces.
- Keep one CinemachineCamera per shot. Never reconfigure a live camera as another
  shot.
- Keep the first release manual-first. Do not add MIDI, AI, generic slots,
  compatibility wrappers, or speculative extension layers.
- Before the first stable release, preserve old APIs and serialized assets only
  when the current task explicitly requires it.
- Do not edit the package-root `README.md` unless the user explicitly asks.

## Working rules

- Inspect `git status --short` first and preserve unrelated work.
- Deliver implementation prompts through chat; do not store them in `Docs/`.
- Make routine code and Inspector choices autonomously using the smallest useful
  solution. Ask only when a decision changes product behaviour, data ownership,
  public API, or destructive scope.
- Do not add unused abstractions or refactor unrelated legacy code.

Project skills live at `E:\Unity\Project\MMD\.agents\skills`. Every agent and
sub-agent must select applicable skills and read each `SKILL.md` in full for
itself; a parent's review is not inherited. Use `unity-cli` for Editor and Unity
asset work, and `unity-package-management` for external UPM changes. Report any
verification that the environment prevents instead of claiming it passed.

## Code and completion

Follow `Docs/code-style.md` for all new or touched C# and Editor code. Match the
layout of `Assets/PrismLipSync/Runtime/PrismLipSync.cs`: explicit member
sections, deliberate spacing, concise Japanese XML summaries, and Tooltips on
all visible serialized fields. Every user-facing MonoBehaviour requires a useful
CustomEditor before it is production-ready.

Validate only relevant layers and distinguish static review, compile, Unity
import, Play Mode, Game View, long-run, and performance evidence. Stage only
approved paths. Do not branch, push, or open a pull request unless explicitly
asked. Report changed files, checks, unchecked boundaries, and the commit ID.
