# Rig作成とInspector Authoring仕様

## 1. 目的

Hierarchyと制御Componentを手作業で組み立てなくても、1回の操作でVLiveCameraUnitのRig骨格を現在のSceneへ導入できるようにする。初期作成後の日常的な編集は各ComponentのInspectorで行う。

## 2. 作成入口

`GameObject/VLiveKit/Virtual Camera`だけを作成入口とする。独立したSetup Window、Step UI、Wizardは提供しない。作成後の設定はRig Inspectorへ集約する。

## 3. 初期作成

初期作成では次をUndo可能な1操作で作る。

- `VLive Camera Rig` Rootと`VLiveCameraRig`
- `VLiveCameraSwitcher`
- `VLiveCameraKeyboardInput`
- 10種の標準3D Motion Presetを参照するShot Slot
- Shot、Spline、Aim Proxyを配置する子Container

GameObject Menuによる初期作成はUnity Camera、Cinemachine Brain、Shot用CinemachineCameraを生成せず、既存Cameraも探索、割り当て、変更しない。作成後はRigを選択し、ユーザーがInspectorでTargetとProgram Cameraを明示的に割り当てる。`Apply / Sync`は割り当てられたProgram Cameraに必要なCinemachine BrainをUndo対応で追加し、不足するShot用CinemachineCameraを生成できる。

## 4. Rig Inspector

Rig Inspectorは次の区分を持つ。

### Setup and Framing

- Performer Target
- Program Camera参照
- 正面基準Mode
- Custom Reference
- Target Height
- Distance Scale
- Motion Scale
- Vertical Motion Scale
- Master Playback Speed

### Shot Slots

- Slot番号
- Motion Preset参照
- 対応する生成済みShot参照
- 追加、並び替え、Slotからの除外

同じPresetを複数Slotへ設定できる。Slot順をShot番号とする。Slotの並び替えで生成済みShotを交換、再生成、初期化しない。Slotが無効でも後続Slotの番号を詰めない。

### Actions

- `Apply / Sync`
- `Rebuild All From Presets`

選択ShotのRebuild、Preset保存、診断、削除は`VLiveCameraShot`のInspectorへ置く。Rig Inspectorは不足参照、無効なScale、キー不足などRig全体の問題だけを簡潔に表示する。

## 5. Inspector表示仕様

- 既存のIMGUIを使用し、UI ToolkitやApp UIへ移行しない。
- UnityとCinemachineの標準Componentに近い、装飾を抑えた外観にする。
- Inspectorに表示する固定文言は英語だけとし、Tooltipは日本語でよい。
- 独自Header Banner、暗色背景、絵文字、色付きBadge、常設の説明Boxを使用しない。
- 標準のPropertyField、EditorStyles、Foldout、HelpBox、DisabledScopeを優先する。
- 基本設定は常時表示し、詳細設定、診断、Runtime状態は必要に応じてFoldoutへ置く。
- Custom GUIStyleが本当に必要な場合はキャッシュし、`OnInspectorGUI`内で生成しない。

各Inspectorの責務は次とする。

| Inspector | 常時表示する主な内容 | 所有する操作 |
| --- | --- | --- |
| Rig | Target、Program Camera、Framing、Shot Slots | Apply / Sync、Rebuild All |
| Shot | Source Preset、主要参照、適用状態 | Rebuild、Save As New Preset、Validate、Delete |
| Motion Preset | Identityと主要分類 | Body、Timing、Aim、Lens、Roll、Activationの編集と診断 |
| Switcher | Rig、Current Program | Program切り替え状態の確認 |
| Keyboard Input | Switcher、Key Bindings | Auto Assignと競合確認 |
| Motion Player | 必要最小限の参照 | Play Mode中の読み取り専用Playback状態 |
| Rig Profile | Response、Recommended Constraints | Profile編集 |

検索、カテゴリ、画像サムネイル、Preset専用管理画面は現在作らない。

## 6. Apply / Sync

`Apply / Sync`は次だけを行う。

- Slotに不足するShotと専用CinemachineCameraを生成する。
- Spline Shotに不足する専用Splineを生成する。
- Shotに不足するAim ProxyとMotion Playerを生成する。
- Target、Program出力、SlotとShot間の参照を修復する。
- 新規生成物だけへPreset初期値、正面基準、スケールを適用する。

既存ShotのTransform、Lens、Spline形状、Aim、適用済みMotion設定を上書きしない。Preset、Rig Profile、Target Height、Rig Scale、正面基準、Slot参照の変更を既存Shotへ反映するにはRebuildが必要であることを表示し、Applyだけでは差し替えない。Slotから外れたShotを自動削除しない。Inspectorの値変更、`OnValidate`、Selection変更、Domain ReloadだけではScene構成を変更しない。

## 7. Rebuildと削除

`Rebuild From Preset`は、対象ShotのCamera位置、Lens、Aim、Splineと、Timing、Curve、Activationを含む適用済みMotion設定を現在のRig設定とPresetから再生成する破壊的操作である。

- 対象と失われる手動調整を実行前に表示する。
- SelectedとAllを分ける。
- Undo可能な1操作として実行する。
- Live中およびPlay Mode中は実行しない。

Slotから外す操作とSceneオブジェクトの削除を分ける。生成済みShotやSplineを削除する場合は対象を明示し、確認とUndoを必須とする。

## 8. SceneからPresetへの保存

`Save Selected Shot As New Preset`は、Scene上で調整したShotを再利用可能な新しいMotion Presetへ保存する明示操作とする。

- Source Shotと保存先を実行前に表示する。
- 現在のCamera、Spline、Aim、LensとShotの適用済みMotion設定から、Knot、Tangent、Up、Timing、Composition、Activationを保存する。
- 既存Presetを暗黙に上書きしない。
- Asset YAMLを直接編集せず、Unity Editor APIで作成する。
- 保存後に元のSlotのPreset参照を自動で差し替えない。差し替える場合は別の明示選択とする。
- SceneやProjectを自動保存しない。

AIによるPreset生成も同じAsset作成処理を利用する。AI専用形式やPrefab形式のMotion Presetを追加しない。

## 9. 安全性

- すべてのScene変更をUndoできる。
- GameObject MenuはCameraを生成、探索、割り当て、変更しない。
- Program CameraはユーザーがInspectorで明示的に割り当てる。
- Builderが所有していないGameObjectやComponentを変更、削除しない。
- Sceneを自動保存しない。
- 必須参照がない場合は同期せず、Inspectorに理由を表示する。
- 再実行で同じShotやCameraを増殖させない。
- 再実行でユーザー調整値をPreset初期値へ戻さない。
- Prefab Instanceを編集する場合はPrefab Overrideを正しく記録する。

## 10. 所有権

GameObject Menuは初期導入、Rig Inspectorは設定、BuilderはScene変更を担当する。設定の正本は`VLiveCameraRig`だけとし、CustomEditorとBuilderが設定値のコピーを保持しない。

生成物の識別にはSlot内のShot参照とRigの親子関係を使用する。Preset参照、GameObject名、Scene内で最初に見つかったSwitcherだけを識別根拠にしない。

## 11. Agent確認

実装エージェントは次だけを既定確認とする。

- GameObject Menuと作成処理がCompileされる。
- SerializedProperty、Undo、参照設定に明白な問題がない。
- 全Inspectorの固定表示が英語で、Tooltipが必要なSerializedFieldに存在する。
- IMGUI内にRepaintごとのGUIStyle生成や不要な装飾がない。
- 差分全体に不要な生成、削除、抽象化がない。

専用テストSceneの作成、ユーザーSceneの保存、Game Viewでの構図評価、長時間試験は行わない。実際のRig作成、Apply、Rebuild結果とカメラワークはユーザーが作業用Sceneで確認する。

## 12. 受け入れ条件

1. 1回の操作でCameraを生成せずに初期Rig骨格を作成できる。
2. Target、正面、スケール、Shot SlotsをRig Inspectorで編集できる。
3. `Apply / Sync`で不足Shotだけを生成し、通常の同期で手動調整を失わない。
4. 同じPresetを複数Slotで使用してもShot参照が混線しない。
5. Rebuildと削除が明示操作で、Undoできる。
6. Inspector編集だけではSceneオブジェクトを生成、削除、再配置しない。
7. Sceneを自動保存せず、初期PaletteのShotをキーで切り替えられる。
8. Scene上のShotを既存Presetへ暗黙上書きせず、新しいPresetとして保存できる。
9. Motion診断がShot、Asset、Sceneを暗黙変更しない。
10. SlotのPreset参照変更がApplyだけで生成済みShotのCamera Performanceを差し替えない。
11. Inspectorが標準IMGUI中心の簡潔な英語UIになり、Shot固有操作がRig Inspectorへ集中していない。
