# VLive Camera Director

VLive Camera Director is a Unity 6.3+ / Cinemachine 3 package for performing
prepared live-camera shots with operator control and reliable composition
assistance.

The current implementation may still contain the legacy `VLiveCameraUnit`
folder, package, assembly, and documentation identifiers. The active migration is
defined by `Docs/roadmap.md`. Do not add compatibility wrappers for those
pre-release identifiers.

## Read first

Use this priority when instructions conflict:

1. The user's instruction for the current task.
2. This file.
3. The relevant document under `Docs/`.
4. The current implementation.

| Task | Source |
| --- | --- |
| Product direction | `Docs/product.md` |
| Ownership, dependencies, Cinemachine, folders | `Docs/architecture.md` |
| Motion Preset and camera behaviour | `Docs/motion.md` |
| Input, switching, and shot lifecycle | `Docs/operation.md` |
| Rig, Inspector, and Preset authoring | `Docs/authoring.md` |
| Current implementation stage | `Docs/roadmap.md` |
| C# and Inspector style | `Docs/code-style.md` |
| Delegation, verification, and Git | `Docs/workflow.md` |

Read only what the task needs. `Docs/README.md` defines which document owns
each decision. Do not copy the same rule into multiple documents.

## Boundaries

- Use `VLiveKit.Camera`, `VLiveKit.Camera.Editor`, and
  `VLiveKit.Camera.Tests` namespaces.
- Keep exactly one source type per C# file and match the filename to that type.
- Keep one CinemachineCamera per shot. Never reconfigure a live camera as
  another shot.
- Keep Motion Preset assets free of runtime state. Motion playback state has one
  owner, and all motion channels share one playback time.
- Treat a Motion Preset as reusable source data and a generated Shot as an
  applied scene instance. Only an explicit rebuild reapplies preset values.
- Preserve complete spline knots, tangents, tangent modes, and up data. Do not
  force every generated knot to Auto Smooth.
- Keep the first release manual-first. Do not add MIDI, runtime AI, Preview,
  compatibility wrappers, or speculative extension layers.
- Use IMGUI for current Custom Inspectors. App UI is reserved for a later live
  operation window.
- The GameObject menu creates the Rig scaffold but never creates, discovers,
  assigns, or modifies a Unity Camera. The user assigns the Program Camera.
- Keep rig authoring, Program switching, shot playback, and input in separate
  concrete responsibilities. Do not add unused interfaces, services,
  registries, or factories.
- Before the first stable release, preserve old APIs and serialized assets only
  when the current task explicitly requires it.
- Do not edit the package-root `README.md` unless the user explicitly asks.

## Working rules

- Inspect `git status --short` first and preserve unrelated work.
- Deliver implementation prompts through chat; do not store them in `Docs/`.
- Make routine code and Inspector choices autonomously using the smallest useful
  solution.
- Ask only when a decision changes product behaviour, data ownership, public
  API, or destructive scope beyond the task.
- Do not add empty folders or abstractions for roadmap features.
- During the Camera Director migration, move Unity files with their `.meta`
  files and update hard-coded asset paths in the same change.

Project skills live at `E:\Unity\Project\MMD\.agents\skills`. Every agent
and sub-agent must select applicable skills and read each `SKILL.md` in full
for itself. Use `unity-cli` for Editor and Unity asset work, and
`unity-package-management` for external UPM changes.

## Code and completion

Follow `Docs/code-style.md` for touched C# and Editor code. Every visible
serialized field requires a Tooltip, and every user-facing MonoBehaviour
requires a useful CustomEditor before it is production-ready.

Default verification is a scoped diff review, Unity import and compile, and a
quick Console check. Motion diagnostics are not visual acceptance. Do not create
a test Scene, run extended Play Mode tests, or judge camera composition unless
explicitly requested. The user performs visual and operational acceptance in
their own Scene.

Stage only approved paths. Do not branch, push, or open a pull request unless
explicitly asked. Report changed files, checks, unchecked boundaries, and the
commit ID when one exists.
