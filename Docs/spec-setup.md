# Rig作成とInspector Authoring仕様

## 1. 目的

HierarchyとComponentを手作業で組み立てなくても、1回の操作でVLiveCameraUnitを現在のSceneへ導入できるようにする。初期作成後の日常的な編集は`VLiveCameraRig`のInspectorで完結させる。

## 2. 開き方

次の入口を提供する。

- `GameObject/VLiveKit/Camera Rig`: 現在のSceneへ新しいRigを作成する。
- `Tools/VLive Camera/VLive Camera Setup`: 初期Rig作成だけを行う小さなWindowを開く。

どちらも同じ`VLiveCameraRigBuilder`の作成処理を呼び、生成結果に差を作らない。WindowにTarget、Preset、スケールの編集状態を保持しない。

## 3. 初期作成

初期作成では次をUndo可能な1操作で作る。

- `VLive Camera Rig` Rootと`VLiveCameraRig`
- `VLiveCameraSwitcher`
- `VLiveCameraKeyboardInput`
- Program CameraとCinemachine Brain
- 6つの標準Presetを参照するShot Slot
- Shot、Spline、Aim Proxyを配置する子Container

作成後はRigを選択し、ユーザーがInspectorでTargetを割り当てて`Apply / Sync`できる状態にする。新しいProgram Cameraへ`MainCamera`タグを付けるのはSceneに既存のMain Cameraがない場合だけとする。既存CameraやSwitcherを自動探索して変更しない。

## 4. Rig Inspector

Inspectorは次の3区分を持つ。

### Setup and Framing

- Performer Target
- Program Camera参照
- 正面基準Mode
- Custom Reference
- Target Height
- Distance Scale
- Motion Scale

### Shot Slots

- Slot番号
- Motion Preset参照
- 対応する生成済みShot参照
- 追加、並び替え、Slotからの除外

同じPresetを複数Slotへ設定できる。Slot順をShot番号とする。Slotの並び替えで生成済みShotを交換、再生成、初期化しない。Slotが無効でも後続Slotの番号を詰めない。

### Operations

- `Apply / Sync`
- `Rebuild Selected From Preset`
- `Rebuild All From Presets`
- `Save Selected Shot As New Preset`
- 選択ShotまたはPresetのMotion診断
- 生成済みShotの明示的な削除

Inspectorは不足参照、無効なScale、キー不足、Motion診断結果などを簡潔に表示する。検索、カテゴリ、画像サムネイル、Preset専用管理画面は現在作らない。

## 5. Apply / Sync

`Apply / Sync`は次だけを行う。

- Slotに不足するShotと専用CinemachineCameraを生成する。
- Spline Shotに不足する専用Splineを生成する。
- Shotに不足するAim ProxyとMotion Playerを生成する。
- Target、Program出力、SlotとShot間の参照を修復する。
- 新規生成物だけへPreset初期値、正面基準、スケールを適用する。

既存ShotのTransform、Lens、Spline形状、Aim、適用済みMotion設定を上書きしない。Preset、Rig Profile、Target Height、Rig Scale、正面基準、Slot参照の変更を既存Shotへ反映するにはRebuildが必要であることを表示し、Applyだけでは差し替えない。Slotから外れたShotを自動削除しない。Inspectorの値変更、`OnValidate`、Selection変更、Domain ReloadだけではScene構成を変更しない。

## 6. Rebuildと削除

`Rebuild From Preset`は、対象ShotのCamera位置、Lens、Aim、Splineと、Timing、Curve、Activationを含む適用済みMotion設定を現在のRig設定とPresetから再生成する破壊的操作である。

- 対象と失われる手動調整を実行前に表示する。
- SelectedとAllを分ける。
- Undo可能な1操作として実行する。
- Live中およびPlay Mode中は実行しない。

Slotから外す操作とSceneオブジェクトの削除を分ける。生成済みShotやSplineを削除する場合は対象を明示し、確認とUndoを必須とする。

## 7. SceneからPresetへの保存

`Save Selected Shot As New Preset`は、Scene上で調整したShotを再利用可能な新しいMotion Presetへ保存する明示操作とする。

- Source Shotと保存先を実行前に表示する。
- 現在のCamera、Spline、Aim、LensとShotの適用済みMotion設定から、Knot、Tangent、Up、Timing、Composition、Activationを保存する。
- 既存Presetを暗黙に上書きしない。
- Asset YAMLを直接編集せず、Unity Editor APIで作成する。
- 保存後に元のSlotのPreset参照を自動で差し替えない。差し替える場合は別の明示選択とする。
- SceneやProjectを自動保存しない。

AIによるPreset生成も同じAsset作成処理を利用する。AI専用形式やPrefab形式のMotion Presetを追加しない。

## 8. 安全性

- すべてのScene変更をUndoできる。
- 既存Cameraを使用する場合は、ユーザーがInspectorで明示的に割り当てる。
- Builderが所有していないGameObjectやComponentを変更、削除しない。
- Sceneを自動保存しない。
- 必須参照がない場合は同期せず、Inspectorに理由を表示する。
- 再実行で同じShotやCameraを増殖させない。
- 再実行でユーザー調整値をPreset初期値へ戻さない。
- Prefab Instanceを編集する場合はPrefab Overrideを正しく記録する。

## 9. 所有権

Windowとメニューは初期導入、Rig Inspectorは設定、BuilderはScene変更を担当する。設定の正本は`VLiveCameraRig`だけとし、Window、CustomEditor、Builderが設定値のコピーを保持しない。

生成物の識別にはSlot内のShot参照とRigの親子関係を使用する。Preset参照、GameObject名、Scene内で最初に見つかったSwitcherだけを識別根拠にしない。

## 10. Agent確認

実装エージェントは次だけを既定確認とする。

- Windowと作成MenuがCompileされる。
- SerializedProperty、Undo、参照設定に明白な問題がない。
- 差分全体に不要な生成、削除、抽象化がない。

専用テストSceneの作成、ユーザーSceneの保存、Game Viewでの構図評価、長時間試験は行わない。実際のRig作成、Apply、Rebuild結果とカメラワークはユーザーが作業用Sceneで確認する。

## 11. 受け入れ条件

1. 1回の操作で初期Rigを作成できる。
2. Target、正面、スケール、Shot SlotsをRig Inspectorで編集できる。
3. `Apply / Sync`で不足Shotだけを生成し、通常の同期で手動調整を失わない。
4. 同じPresetを複数Slotで使用してもShot参照が混線しない。
5. Rebuildと削除が明示操作で、Undoできる。
6. Inspector編集だけではSceneオブジェクトを生成、削除、再配置しない。
7. Sceneを自動保存せず、初期PaletteのShotをキーで切り替えられる。
8. Scene上のShotを既存Presetへ暗黙上書きせず、新しいPresetとして保存できる。
9. Motion診断がShot、Asset、Sceneを暗黙変更しない。
10. SlotのPreset参照変更がApplyだけで生成済みShotのCamera Performanceを差し替えない。
