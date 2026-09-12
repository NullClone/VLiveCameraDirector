# RigとPreset Authoring仕様

## 1. 目的

HierarchyとComponentを手作業で組み立てなくても、1回の操作でVLive Camera DirectorのRig骨格をSceneへ導入できるようにする。初期作成後の日常的な編集は各ComponentのInspectorとScene上のCinemachine構成で行う。

## 2. 作成入口

移行後の作成入口は`GameObject/VLiveKit/Camera Director Rig`とする。独立したSetup Window、Step UI、Wizardは提供しない。

初期作成では次をUndo可能な1操作で作る。

- `VLive Camera Rig` Rootと`VLiveCameraRig`
- `VLiveCameraSwitcher`
- `VLiveCameraKeyboardInput`
- 標準Motion Presetを参照するShot Slot
- Shot、Spline、Aim Proxyを配置する子Container

GameObject MenuはUnity Camera、Cinemachine Brain、Shot用CinemachineCameraを生成せず、既存Cameraも探索、割り当て、変更しない。Program CameraはユーザーがRig Inspectorで明示的に指定する。

## 3. Rig Inspector

### Setup and Framing

- Performer Target
- Program Camera
- Forward Reference Mode
- Custom Reference
- Target Height
- Distance Scale
- Horizontal Motion Scale
- Vertical Motion Scale
- Master Playback Speed

### Shot Slots

- Slot番号
- Motion Preset参照
- 対応する生成済みShot参照
- 追加、並び替え、Slotからの除外

同じPresetを複数Slotへ設定できる。Slot順をShot番号とし、並び替えでShotを交換、再生成、初期化しない。無効なSlotがあっても後続番号を詰めない。

### Actions

- `Apply / Sync`
- `Rebuild All From Presets`

選択ShotのRebuild、Preset保存、診断、削除はShot Inspectorへ置く。Rig InspectorはRig全体の設定と問題だけを表示する。

## 4. Inspector表示

- IMGUIを使用し、Custom InspectorへUI ToolkitやApp UIを使用しない。
- UnityとCinemachineの標準Componentに近い外観にする。
- 固定表示は英語、Tooltipは日本語とする。
- 独自Banner、絵文字、色付きBadge、装飾目的のBoxを使用しない。
- PropertyField、EditorStyles、Foldout、HelpBox、DisabledScopeを優先する。
- 基本設定は常時表示し、詳細、診断、Runtime状態はFoldoutへ置く。
- Actionableでない常設HelpBoxを表示しない。
- Custom GUIStyleとGUIContentはRepaint外でキャッシュする。

| Inspector | 主な表示 | 所有する操作 |
| --- | --- | --- |
| Rig | Target、Program Camera、Framing、Shot Slots | Apply / Sync、Rebuild All |
| Shot | Source Preset、Cinemachine参照、適用状態 | Rebuild、Save As New Preset、Validate、Delete |
| Motion Preset | Identityと各Track | Preset編集と診断 |
| Switcher | Rig、Current Program | Program状態の確認 |
| Keyboard Input | Switcher、Key Bindings | 割り当てと競合確認 |
| Motion Player | 主要参照、Playback状態 | Play Mode中の読み取り |
| Rig Profile | Response、Recommended Constraints | Profile編集 |

大量Preset用の検索、カテゴリ、サムネイル、App UI Paletteは、実数と運用要件が確定するまで追加しない。

## 5. Apply / Sync

`Apply / Sync`は次だけを行う。

- Slotに不足するShotと専用CinemachineCameraを生成する。
- Motion Shotに不足する専用Splineを生成する。
- Shotに不足するAim ProxyとMotion Playerを生成する。
- Target、Program出力、SlotとShotの参照を修復する。
- 明示指定されたProgram Cameraに必要なCinemachine BrainがなければUndo対応で追加する。
- 新規生成物だけへPreset初期値、正面基準、Scaleを適用する。

既存ShotのTransform、Lens、Spline、Aim、適用済みMotionを上書きしない。Preset、Rig Profile、Target Height、Scale、正面基準、Slot参照の変更はRebuildで反映する。

Slotから外れたShotを自動削除しない。Inspector変更、`OnValidate`、Selection変更、Domain ReloadだけではScene構成を変更しない。

## 6. Rebuild

`Rebuild From Preset`はCamera位置、Lens、Aim、Spline、Timing、Curve、Activationを現在のRig設定とPresetから再生成する破壊的な明示操作である。

- 対象と失われるScene調整を実行前に表示する。
- SelectedとAllを分ける。
- Undo可能な1操作にする。
- Play Mode中とLive中は実行しない。
- 通常のApplyと同じボタンや暗黙処理にしない。

## 7. SceneからPresetへの保存

`Save Selected Shot As New Preset`は、Sceneで調整したShotを新しいMotion Presetへ保存する。

- Source Shotと保存先を実行前に表示する。
- Camera、Spline、Aim、Lens、適用済みTimingとActivationを保存する。
- Knot、Tangent、Tangent Mode、Up、Curveを欠落させない。
- 既存Presetを暗黙に上書きしない。
- SlotのPreset参照を自動差し替えしない。
- Asset YAMLを直接編集せずUnity Editor APIを使用する。
- SceneやProjectを自動保存しない。

AIによるPreset生成も同じAsset作成APIを使用する。

## 8. Slot除外と削除

Slotから外す操作とSceneオブジェクトの削除を分ける。生成済みShot、Camera、Splineを削除する場合は対象を明示し、確認とUndoを必須とする。

生成物の識別にはSlot内のShot参照とRigの親子関係を使用する。Preset参照、GameObject名、Scene内で最初に見つかったComponentだけを根拠にしない。

## 9. 安全境界

- すべてのScene変更をUndo可能にする。
- Builderが所有していないGameObjectやComponentを変更、削除しない。
- 必須参照がない場合は実行せず理由を表示する。
- 再実行でShotやCameraを増殖させない。
- 再実行でScene調整をPreset初期値へ戻さない。
- Prefab Instanceの変更はPrefab Overrideを記録する。
- AssetまたはSceneを自動保存しない。
- Validatorは既定で診断だけを行う。

## 10. 不変条件

1. 1回の操作でCameraを生成せず初期Rig骨格を作成できる。
2. Target、Program Camera、正面、Scale、Shot SlotsをRig Inspectorで編集できる。
3. Applyは不足物だけを生成し、既存のScene調整を失わない。
4. Rebuild、削除、Preset保存は対象が明確な別操作である。
5. 同じPresetを複数Slotで使用してもShot参照が混線しない。
6. Inspector描画と値変更だけではSceneやAssetを変更しない。
7. Scene上のShotを既存Presetへ暗黙上書きしない。
8. Custom Inspectorは標準IMGUI中心の簡潔な英語UIである。
