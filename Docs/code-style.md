# C# and Inspector Style

This document defines the default style for new or touched VLive Camera Director code.
It is based on `Assets/PrismLipSync/Runtime/PrismLipSync.cs`. Follow the intent
below rather than copying incidental inconsistencies from that file.

## Agent authority

Apply this style without asking the user about routine formatting, member order,
Tooltip wording, or Inspector layout. Choose the smallest useful Editor extension
for the component.

Ask only when the choice changes runtime behaviour, serialized data ownership,
public API, or the product workflow. Do not restyle untouched legacy files as part
of an unrelated task.

## File and naming conventions

- Use four spaces and braces for every control-flow body.
- Keep one primary type per file matching its filename. Closely coupled enums
  and small helper types may reside in the same file within the same namespace.
- Use block-scoped namespaces.
- Use `PascalCase` for types, methods, properties, constants, and public members.
- Use `_camelCase` for private fields.
- Prefer `[SerializeField] private` over public fields.
- Use `var` only when the assigned type is immediately clear.
- Keep `UnityEditor` references out of Runtime assemblies.

## Member layout

Use these section labels in this order when the section exists:

1. `// Fields`
2. `// Properties`
3. `// Methods`

Leave one blank line after a section label and between members or logical field
groups. Leave two blank lines before the next major section. Do not use repeated
blank lines elsewhere.

Serialized fields come before runtime-only fields. Group related serialized fields
with `Header`. Stack attributes one per line in this order:

```csharp
// Fields

[Header("Motion Settings")]
[Tooltip("Spline上を移動する基本速度。")]
[Min(0.0f)]
[SerializeField]
private float _speed = 0.2f;


private float _currentSpeed;


// Properties

public float CurrentSpeed => _currentSpeed;


// Methods

/// <summary>
/// 現在の移動を一時停止します。
/// </summary>
public void Hold()
{
    _isHolding = true;
}
```

Do not compress attributes and a field onto one line.

## Documentation and comments

- Write concise Japanese XML `<summary>` text for public types and public or
  protected methods. One sentence is normally enough.
- Add a summary to a private method only when its Unity callback or contract is
  not obvious from its name.
- Use inline comments only for constraints, non-obvious reasons, or measured
  behaviour. Do not narrate each branch or repeat the code.
- Prefer a short comment above the relevant member or block over many comments
  inside a method.
- Update a comment in the same change when the behaviour it describes changes.
- Put cross-file rules and product decisions in `Docs/`, not in source headers.

## Serialized fields and Tooltips

Every serialized field visible in the Inspector must have a `Tooltip`, including
fields inside nested serializable types.

- Write Tooltips in concise Japanese.
- Use English only for every visible Inspector label, Header, button, status,
  warning, and help message. Japanese is allowed in Tooltips, XML summaries,
  source comments, and authored asset content.
- State units, valid ranges, special values, and operational effect when relevant.
- Use `Header`, `Range`, `Min`, and related attributes where they prevent
  authoring mistakes.
- Keep validation in `OnValidate` when invalid serialized values could exist.
- Do not use a Tooltip to repeat only the field name.

## Inspector presentation

Custom Inspectors deliberately use IMGUI. Do not migrate them to UI Toolkit or
App UI. Runtimeの明示的なライブ操作画面にはUI ToolkitとApp UIを使用してよい。

- Use `OnInspectorGUI`, `EditorGUILayout`, `SerializedObject`, and
  `SerializedProperty` as the default path.
- Prefer built-in `EditorStyles`, standard spacing, indentation, foldouts,
  disabled scopes, and `EditorGUILayout.HelpBox`.
- Do not draw branded banners, decorative cards, custom dark backgrounds,
  emoji, coloured status badges, or a package-specific visual theme.
- Keep essential settings visible and place infrequent or diagnostic settings
  behind a clearly named foldout.
- Show a warning only when it is actionable in the current context. Do not use
  permanent instructional HelpBoxes as decoration.
- Cache every custom `GUIStyle` and reusable `GUIContent` outside
  `OnGUI`/`OnInspectorGUI`. Never construct a `GUIStyle` during a repaint.
- Use colour only when a standard Unity control cannot communicate the same
  semantic state. Prefer `MessageType` and disabled state.
- Keep component-specific actions with the component that owns the operation;
  do not turn the Rig Inspector into a dashboard for every subsystem.

## MonoBehaviour inspectors

Every user-facing MonoBehaviour must have a companion CustomEditor in the Editor
assembly and the `VLiveKit.Camera.Editor` namespace. A hidden internal
helper with no user-authored state may keep the default Inspector; the reason must
be clear from its role.

The minimum useful CustomEditor:

- uses `SerializedObject` and `SerializedProperty`;
- supports Undo and Prefab overrides;
- supports multi-object editing when its actions are safe;
- presents fields in clear operational groups;
- shows concise validation messages for missing or conflicting references; and
- shows meaningful read-only runtime state when that helps live operation.

An Editor that only calls `DrawDefaultInspector` without adding user value does
not satisfy this requirement. Keep runtime rules in Runtime code; the Editor
visualizes and edits them but does not become a second source of truth.

The agent chooses the exact Inspector layout, help messages, and whether a small
PropertyDrawer is warranted. Do not build a framework or shared Editor abstraction
until multiple real inspectors require the same behaviour.

## Runtime discipline

- Avoid `Find`, LINQ, per-frame string construction, and avoidable allocations in
  update paths.
- Treat Unity Object null semantics correctly.
- Include the cause and target in diagnostic logs and never log every frame.
- Expose only public API needed by the current feature.
- Keep state ownership singular; an Editor must not mirror authoritative runtime
  values into a second store.
