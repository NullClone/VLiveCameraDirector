# VLiveCameraUnit

VLiveCameraUnit is a Unity 6.3+ / Cinemachine 3 package for performing prepared
live-camera shots with operator control and reliable composition assistance.

The current goal is the Motion Foundation on top of the proven manual rig:
replace position-only spline playback with a Camera Performance preset that
coordinates body, timing, aim, composition, lens, and entry state. Multiple
independent shot cameras remain keyboard-selectable. Gold-master expansion,
App UI, MIDI, recommendation, and automation come after this foundation is
proven in the user's Scene.

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
| Rig creation and Inspector authoring | `Docs/spec-setup.md` |
| Input and switching | `Docs/spec-operation.md`, `Docs/spec-switching.md` |
| Assistance rules | `Docs/spec-assistance.md` |
| Current implementation phase | `Docs/phases.md` |
| C# and Inspector style | `Docs/code-style.md` |
| Delegation, verification, and Git | `Docs/workflow.md` |

Read only what the task needs. Code comments own local reasons; `Docs/` owns
cross-file contracts and product decisions; commit messages own why a change was
made. Do not duplicate the same explanation.

## Boundaries

- Use `VLiveKit.Camera`, `VLiveKit.Camera.Editor`, and
  `VLiveKit.Camera.Tests` namespaces.
- Keep one CinemachineCamera per shot. Never reconfigure a live camera as another
  shot.
- Keep Motion Preset assets free of runtime state. Motion playback state has one
  owner, and all motion channels share one playback time.
- Treat a Motion Preset as reusable source data and a generated Shot as an
  applied scene instance. Only an explicit rebuild reapplies preset values to an
  existing Shot.
- Preserve full spline knots, tangents, tangent modes, and up data. Do not force
  every generated knot to Auto Smooth.
- Keep the first release manual-first. Do not add MIDI, runtime AI, Preview,
  compatibility wrappers, or speculative extension layers.
- Keep rig authoring, Program switching, shot playback, and input in separate
  concrete responsibilities. Do not use this rule to add unused interfaces,
  services, registries, or factories.
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
asset work, and `unity-package-management` for external UPM changes.

## Code and completion

Follow `Docs/code-style.md` for all new or touched C# and Editor code. Every
visible serialized field requires a Tooltip, and every user-facing MonoBehaviour
requires a useful CustomEditor before it is production-ready.

Default agent verification is a scoped diff review, Unity import and compile, and
a quick Console check. Motion validation may report numeric diagnostics but must
not be presented as visual acceptance. Do not create a test Scene, run extended
Play Mode tests, or judge camera composition unless the task explicitly requests
it. The user performs visual and operational acceptance in their own Scene.

Stage only approved paths. Do not branch, push, or open a pull request unless
explicitly asked. Report changed files, checks, unchecked boundaries, and the
commit ID.
