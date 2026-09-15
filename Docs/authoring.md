# RigとPreset Authoring仕様

## 1. 目的

HierarchyとComponentを手作業で組み立てなくても、1回の操作でVLive Camera DirectorのRig骨格をSceneへ導入できるようにする。初期作成後の日常的な編集は各ComponentのInspectorとScene上のCinemachine構成で行う。

## 2. 作成入口

作成入口は`GameObject/VLiveKit/Camera Director`とする。独立したSetup Window、Step UI、Wizardは提供しない。

初期作成では次をUndo可能な1操作で作る。

- `Camera Director` Rootと`VLiveCameraRig`
- `VLiveCameraSwitcher`
- Shot、Spline、Target Groupを配置する子Container

GameObject MenuはUnity Camera、Cinemachine Brain、Shot用CinemachineCameraを生成せず、既存Cameraも探索、割り当て、変更しない。Program CameraはユーザーがRig Inspectorで明示的に指定する。

`VLiveCameraKeyboardInput`、Cinemachine Brain、Shotは最初の`Apply`で必要に応じて生成する。Rig骨格作成時点では入力やCameraを暗黙追加しない。

## 3. Actor設定

ActorのRootまたはAnimatorを含む親GameObjectへ`VLivePerformer`を追加する。Component追加時は子階層のAnimatorを自動取得し、Inspectorの`Resolve Animator From Children`で明示的に再取得できる。

- Performer Animatorは有効なHumanoid Avatarを使用する。
- Performer NameはEditor上の識別名とする。
- Head Radius、Bust Radius、Body RadiusはActor固有の構図範囲をメートル単位で設定する。
- Eyes、顔ランドマーク、Constraint、補助Proxyは要求しない。

Humanoidボーンの選択とShot SizeごとのMember構成は[motion.md](motion.md)を正本とする。

## 4. Rig Inspector

### Setup

- Program Camera
- Reference Transform
- Forward Reference Mode
- Custom Reference

Reference TransformはCamera軌道の原点と`ReferenceForward`の向きを決める。未指定時はCamera Director Rootを使用し、Actor参照とは分離する。

### Rig Performers

- 通常の全Shotに写す1人以上の`VLivePerformer`一覧

共通一覧を一度設定し、各Shot Slotは既定でこの一覧を使用する。Shotごとに被写体を変える必要がある場合だけ`Override Performers`を有効にする。

### Common Physical Camera Settings

- Sensor Size
- Gate Fit
- Lens Shift
- Near Clip Plane
- Far Clip Plane

これらはRigが唯一の正本として保持する。値の編集だけでは既存Cameraへ伝播せず、明示的な`Apply Camera Settings to All Shots`で同期する。新規ShotとRebuild対象には現在のRig値を適用する。

### Motion Settings

- Distance Scale
- Horizontal Motion Scale
- Vertical Motion Scale
- Master Playback Speed

### Shot Slots

- Slot番号
- Motion Preset参照
- `Override Performers`の有効・無効
- Override有効時だけ使用する`VLivePerformer`一覧
- 対応する生成済みShot参照
- 追加、並び替え、Slotからの除外

同じPresetを複数Slotへ設定できる。OverrideはRig共通一覧への追加ではなく完全な置き換えであり、空のOverrideは被写体なしを明示する。Slot順をShot番号とし、並び替えでShotを交換、再生成、初期化しない。無効なSlotがあっても後続番号を詰めない。

### Actions

- `Apply`
- `Rebuild`
- `Apply Camera Settings to All Shots`

選択ShotのRebuild、診断、削除はShot Inspectorへ置く。Rig InspectorはRig全体の設定と問題だけを表示する。

## 5. Inspector表示

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
| Rig | Reference、Program Camera、共通Actor、Physical Camera、Motion、Shot SlotsとActor Override | Apply、Rebuild、Camera Settings一括適用 |
| Performer | Humanoid Animator、表示名、Head / Bust / Body Radius | 子階層からのAnimator再取得 |
| Shot | Source Preset、Actor、Target Group、Cinemachine参照、適用状態 | Rebuild、Validate、Delete |
| Motion Preset | Identityと各Track | Preset編集と診断 |
| Switcher | Rig、Current Program | Program状態の確認 |
| Keyboard Input | Switcher、Key Bindings | 割り当てと競合確認 |
| Motion Player | 主要参照、Playback状態 | Play Mode中の読み取り |
| Rig Profile | Response、Recommended Constraints | Profile編集 |
| Composition Overlay | Aspect、Mask、Guides、Runtime Panel | 初期値編集とUI Document構成 |

大量Preset用の検索、カテゴリ、サムネイル、App UI Paletteは、実数と運用要件が確定するまで追加しない。

## 6. Apply

`Apply`は次だけを行う。

- Slotに不足するShotと専用CinemachineCameraを生成する。
- Motion Shotに不足する専用Splineを生成する。
- Rig共通ActorまたはShot Slot OverrideからCinemachine Target Groupを生成し、HumanoidボーンMemberを同期する。
- Shotに不足するRotation Composer、Group Framing、Motion Playerを生成する。
- Target Group、Program出力、SlotとShotの参照を修復する。
- 明示指定されたProgram Cameraに必要なCinemachine BrainがなければUndo対応で追加する。
- Cinemachine BrainをCut BlendとPhysical Lens overrideへ設定する。
- 新規生成物だけへPreset初期値、正面基準、Scaleを適用する。

既存ShotのTransform、Lens、Spline、Group Framing設定、適用済みMotionを上書きしない。Rig共通ActorまたはShot Slot Overrideから解決したActor一覧とTarget Group Memberは通常のApplyで同期する。旧SceneのSlot別Actor一覧は、データを失わないようShot固有Overrideとして解釈する。Preset、Rig Profile、Scale、正面基準、Shot Sizeの変更はRebuildで反映する。

Slotから外れたShotを自動削除しない。Inspector変更、`OnValidate`、Selection変更、Domain ReloadだけではScene構成を変更しない。

## 7. Rebuild

`Rebuild From Preset`はCamera位置、Physical Lens、Aim、Target Group、Group Framing、Spline、Timing、Curve、Activationを現在のRig設定、解決済みActor一覧、Presetから再生成する破壊的な明示操作である。

- 対象と失われるScene調整を実行前に表示する。
- SelectedとAllを分ける。
- Undo可能な1操作にする。
- Play Mode中とLive中は実行しない。
- 通常のApplyと同じボタンや暗黙処理にしない。

## 8. Camera Settings一括適用

`Apply Camera Settings to All Shots`は、Rigの共通Physical Camera設定をProgram CameraとRig所有の全CinemachineCameraへ同期する。

- Program Cameraは`usePhysicalProperties`を有効にする。
- Cinemachine Brainが存在する場合はPhysical Lens overrideを有効にする。
- 全CameraへSensor Size、Gate Fit、Lens Shift、Near / Far Clip Planeを適用する。
- Shot固有のField of View、Focal Length相当の画角、Dutch、Physical Exposure値は保持する。
- Scene変更はUndo可能にし、Sceneを自動保存しない。

Scene上のShotからMotion Presetを生成または既存Presetへ保存する操作は、現在提供しない。Scene調整をPreset Assetへ暗黙逆同期しない。

## 9. Slot除外と削除

Slotから外す操作とSceneオブジェクトの削除を分ける。生成済みShot、Camera、Spline、Target Groupを削除する場合は対象を明示し、確認とUndoを必須とする。

生成物の識別にはSlot内のShot参照とRigの親子関係を使用する。Preset参照、GameObject名、Scene内で最初に見つかったComponentだけを根拠にしない。

## 10. 安全境界

- すべてのScene変更をUndo可能にする。
- Builderが所有していないGameObjectやComponentを変更、削除しない。
- 必須参照がない場合は実行せず理由を表示する。
- 再実行でShotやCameraを増殖させない。
- 再実行でScene調整をPreset初期値へ戻さない。
- Prefab Instanceの変更はPrefab Overrideを記録する。
- AssetまたはSceneを自動保存しない。
- Validatorは既定で診断だけを行う。

## 11. 不変条件

1. 1回の操作でCameraを生成せず初期Rig骨格を作成できる。
2. Reference、Program Camera、共通Physical Camera、正面、Scale、Rig共通Actor、Shotごとの任意OverrideをRig Inspectorで編集できる。
3. Applyは不足物だけを生成し、既存のScene調整を失わない。
4. Rebuild、Camera Settings一括適用、削除は対象が明確な別操作である。
5. 同じPresetを複数Slotで使用してもShot参照が混線しない。
6. Inspector描画と値変更だけではSceneやAssetを変更しない。
7. Scene上のShotを既存Presetへ暗黙上書きしない。
8. Custom Inspectorは標準IMGUI中心の簡潔な英語UIである。
