# アーキテクチャ仕様

## 1. 目的

この文書は、1 Target、複数Shot、Camera Performance形式のMotion Preset、Inspector主導のRig Authoringにおける責務と状態所有権を定義する。

現在のMotion品質を成立させる具体的な責務は分離するが、MIDI、Runtime AI、Preview、自動演出のためだけのinterface、Service、Registry、Factoryは作らない。

## 2. 全体構成

```text
GameObject Create Menu (Editor)
             |
             v
      VLiveCameraRig <--- Motion Preset Assets
             |                 |
             |                 +--> Rig Profile
             | owns ordered Shot Slots
             v
       Shot 1 ... Shot N
             | each owns one CinemachineCamera
             | each references one Motion Player
             | each owns one Spline, Aim Proxy,
             | and applied Motion configuration
             v
Motion Evaluator --> Base Motion Sample
                         |
Operator Input ----------+--> Motion Player --> Cinemachine
                                              Spline Dolly
                                              Rotation Composer
                                              Lens / Roll

Keyboard Input ---> Switcher ---> Cinemachine Brain ---> Program Camera
```

EditorではScene上のShotをMotion Presetの視覚的なAuthoring面として使用する。

```text
Rig Inspector / Shot Inspector
             |
             +--> Apply / Sync / Rebuild
             |
             +--> Save Shot As New Preset
             |
             +--> Motion Validator
             v
        explicit Editor operations
```

## 3. 主要責務

| 型 | 責務 |
| --- | --- |
| `VLiveCameraRig` | Target、Program Camera、正面基準、Scale、順序付きShot SlotをScene上の正本として保持する |
| `VLiveCameraMotionPreset` | Camera Performance Assetとしてトラック集合だけを所有する |
| `VLiveCameraMotionPresetData` | Identity、Body、Timing、Aim、Lens、Roll、Activationを単一時間軸で束ねる |
| `VLiveCameraBodyTrack`ほか各Track型 | 各チャンネルの再利用可能な値を一責務で保持する |
| `VLiveCameraRigProfile` | 機材固有の操作応答とValidator推奨制約を複数Presetで共有する |
| `VLiveCameraShot` | 専用Camera、Spline、Aim Proxy、適用済みMotion設定とOn-Air / Off-Air lifecycleを所有する |
| `VLiveCameraMotionPlayer` | 現在時刻、方向、Speed Multiplier、Hold、完了状態を所有し、自身のShotだけへ評価結果を適用する |
| `VLiveCameraMotionEvaluator` | 解決済みのMotion設定と時刻からMotion Sampleを決定的に計算する |
| `VLiveCameraSwitcher` | RigのSlot順を使用し、現在のProgramとCutだけを管理する |
| `VLiveCameraKeyboardInput` | キーボード入力をSwitcherの公開操作へ渡す |
| `VLiveCameraMotionSpace` | Preset座標へ水平・垂直の独立スケールを適用する |
| `VLiveCameraRigBuilder` | Rig初期作成とSlot単位の同期、再構築、削除操作を調整する |
| `VLiveCameraShotBuilder` | 1 Shot分のCamera、Spline、Aim Proxy、Motion Playerを生成、修復、再構築する |
| `VLiveCameraSplineBuilder` | Presetの3D Knotを軸別Scaleと正面基準でScene Splineへ具体化する |
| `VLiveCameraPresetBaker` | Scene上のShotから完全なSplineと各Trackを新しいPreset Assetへ保存する |
| `VLiveCameraMotionValidator` | PresetまたはShotの運動値と構図値を診断し、結果をEditorへ返す |
| `VLiveCameraRigEditor` | Rig設定、Shot Slot、同期、全Shot再構築を簡潔なInspectorへ提示する |

`VLiveCameraMotionEvaluator`はRuntime再生とEditor診断で同じ結果を得る必要があるため共通化する。汎用Animation frameworkにはせず、VLiveCameraUnitのPresetだけを評価する具体型とする。

## 4. 状態の正本

### RigとShot Slot

`VLiveCameraRig`の順序付きShot Slot一覧を、使用するShotと番号順の唯一の正本とする。各Slotは次だけを参照する。

- `VLiveCameraMotionPreset`
- 対応する`VLiveCameraShot`

同じPresetを複数Slotで使用でき、それぞれが別のShotとCinemachineCameraを参照する。リストの並び替えでShotの実体を交換、再生成、初期化しない。

Switcherに別のPreset一覧やShot一覧を正本として重複保持しない。

### Motion Preset

Motion Preset Assetは人またはAIが作るCamera Performanceの正本である。

Preset本体へ全フィールドを平坦に置かず、Identity、Body、Timing、Aim、Lens、Roll、ActivationのSerializable型を1ファイル1型で保持する。各Trackは再利用可能な値だけを持ち、Runtime再生状態を持たない。

- 完全なSpline定義
- Timing
- AimとComposition
- Lens
- Roll
- Activation
- Rig Profile参照

現在時刻、方向、Speed Multiplier、Hold、Live状態、生成済みShot参照は保存しない。

Presetは再利用可能な原本であり、生成済みShotをRuntime中に直接駆動する可変状態ではない。SlotのPreset参照を変更しても、既存Shotへ暗黙適用しない。現在のPresetをShotへ反映する操作はRebuildだけとする。

### Shot

ShotはScene上の実体として次を所有する。

- 専用CinemachineCamera
- 専用SplineContainer
- 専用Aim Proxy
- 適用済みMotion設定
- Motion Player参照
- On-Air / Off-Air lifecycle

生成されたCamera、Spline、Aim、Lensと適用済みMotion設定は、PresetからSceneへ具体化した派生結果である。適用済みMotion設定はTiming、Curve、Activationなど、Scene Componentだけでは表せない再生設定を保持する。これはRuntime再生状態ではない。

Motion PlayerはPreset Assetを毎フレーム直接評価せず、Shotに適用済みの設定を評価する。これによりPreset Assetの編集やSlot参照の変更が、Live中または調整済みShotへ暗黙伝播しない。

適用済みMotion設定は、公開frameworkにせず`VLiveCameraShot`が所有する具体的なSerializableデータとして実装する。保存場所は次のとおり分ける。

| データ | 再利用元 | 生成済みShotでの所有先 |
| --- | --- | --- |
| Body geometry | Motion Preset | `SplineContainer` |
| Timing、Progress、Entry、Activation | Motion Preset | Shotの適用済みMotion設定 |
| Aim、Composition、Lens、RollのCurve | Motion Preset | Shotの適用済みMotion設定 |
| Rig応答 | Rig Profile | Rebuild時に解決したShotの適用済みMotion設定 |
| 現在時刻、速度、方向、Hold | なし | Motion Playerだけ |

PresetとRig Profileは再利用元、ShotはSceneへ適用したインスタンスであり、同じ用途の正本を競合させる二重管理ではない。Scene上のSplineやCameraを変更してもPresetへ自動逆同期しない。

### Motion Player

Motion Playerだけが次のRuntime再生状態を所有する。

- Current Time
- Current / Target Speed Multiplier
- Direction
- HoldまたはReverseの遷移状態
- Playing / Completed

Shot、Switcher、Presetへ同じ値を複製しない。Shotはlifecycle操作をMotion Playerへ渡す。

### Aim Proxy

Aim Proxyは現在のTarget Poseと、Shotへ適用済みのTarget Height、基準向き、Aim Trackから導出されるTransformであり、独立した設定の正本ではない。ユーザーが直接編集する対象にしない。Target Heightや基準向きの設定変更はRebuildでShotへ反映する。

## 5. Motion評価パイプライン

```text
Resolved Shot Motion + Playback Time
            |
            v
   VLiveCameraMotionEvaluator
            |
            v
       Base Sample
       - Distance
       - Aim Offset
       - Screen Position
       - Lens
       - Roll
            |
            v
       Operator Trim
            |
            v
  finite-value / hard safety check
            |
            v
        Cinemachine
```

- Motion評価は同じ解決済み入力に対して同じ結果を返す。
- Distance Scale、Horizontal Motion Scale、Vertical Motion Scale、正面基準、Rig ProfileはRebuild時に解決する。Master Playback Speedだけはライブ中にRig全体へ掛ける非破壊倍率とし、PresetやSplineを書き換えない。
- Motion EvaluatorはUnity Input、Program状態、Scene生成を扱わない。
- Operator TrimはAssetを書き換えない。
- RuntimeのHard SafetyはNaN、Infinity、無効参照などの破綻だけを拒否する。
- 映像表現上の推奨上限はEditor Validatorで警告し、Runtimeで別の演出へ暗黙変更しない。
- Cameraの最終評価と出力はCinemachineへ任せる。

## 6. Runtime責務

### Shot lifecycle

`VLiveCameraShot`は次の操作を提供する。

- Off-Air中のPrepare
- Programへ入るTake
- Programから外れるRelease
- Motion PlayerへのSpeed、Hold、Resume、Reverse

PrepareはShotに適用済みのIn TimeへCamera、Aim、Lensを準備する。Rolling EntryではIn Timeが非0速度を持てる。Take後にMotion Playerがその状態から継続する。

### Motion Player

Motion PlayerはShotに適用済みの時間を進め、Motion Evaluatorの結果を自身のCinemachineCamera、Spline Dolly、Aim Proxy、Rotation Composer、Lens、Rollへ適用する。

- UpdateでSceneやAssetを生成、削除しない。
- 他ShotのCameraやSplineへ書き込まない。
- Speed、Hold、Resume、Reverseによる速度変化を、Rebuild時にRig Profileから適用した応答値で連続化する。
- Live中のRebuild、Reset、Teleportを行わない。

### SwitcherとInput

- Keyboard Inputだけがキー入力を読み、Switcherの公開操作だけを呼ぶ。
- SwitcherだけがProgram Shotを変更し、Motion操作を現在のShotへ委譲する。
- SwitcherはPresetの時刻、Spline位置、Lensを直接変更しない。
- InputはSwitcherと公開されたMotion操作だけを呼ぶ。

## 7. Editor責務

### Create

`GameObject/VLiveKit/Virtual Camera`は、Cameraを含まない新しいRig骨格をUndo可能な1操作で作成し、生成したRigを選択する。独立したSetup WindowやStep UIは持たない。Camera、Cinemachine Brain、既存Switcherを探索、生成、割り当て、変更しない。

### Inspector

Custom InspectorはIMGUIで実装する。標準の`SerializedProperty`、`EditorGUILayout`、`EditorStyles`を中心にし、Inspectorの固定表示は英語、Tooltipは日本語とする。独自テーマやダッシュボードを作らず、各Componentが所有する設定、操作、状態だけを表示する。

- RigはScene全体の設定、Slot、Apply / Sync、Rebuild Allを扱う。
- ShotはShot固有のRebuild、Preset保存、診断、削除を扱う。
- SwitcherはProgram、Inputはキー、Motion Playerは再生状態を扱う。
- App UIは将来のライブ操作Window専用とし、Custom Inspectorへ使用しない。

### Apply / Sync

Rig Inspectorの`Apply / Sync`は次だけを行う。

- 不足するShot、CinemachineCamera、Spline、Aim Proxy、Motion Playerの生成
- 壊れた参照の修復
- TargetとProgram出力参照の反映
- Slot順の反映
- 新規生成物へのPreset初期値適用

既存ShotのTransform、Lens、Spline、Aim Track、適用済みMotion設定をPreset値へ戻さない。SlotのPreset参照を変更した場合も、既存Shotは以前に適用された設定のまま維持し、InspectorでRebuildが必要であることを表示する。Slotから外れたShotを自動削除しない。

### Rebuild From Preset

選択Shotまたは全ShotをPresetから再構築する明示操作とする。

- 上書きするCamera、Spline、Aim、Lens、適用済みMotion設定を表示する。
- Live中とPlay Mode中は実行しない。
- Undo可能な1操作とする。
- 通常のApplyと同じボタンや暗黙処理にしない。

### Save Shot As New Preset

Scene上で調整したShotから新しいPreset Assetを作成する。

- Source Shotと保存先を明示する。
- 現在のCamera、Spline、Aim、LensとShotの適用済みMotion設定から、完全なKnot、Tangent、Up、Timing、Composition、Activationを保存する。
- 既存Presetを暗黙に上書きしない。
- Asset作成と参照変更をUndo可能な範囲で記録する。
- SceneやProjectを自動保存しない。

AIによる生成も同じEditor APIを使用し、YAMLを直接書き換えない。

### Motion Validator

ValidatorはMotion Evaluatorを用いてPresetまたはShotを一定間隔でサンプリングし、診断結果を返す。

- ValidatorはProgram状態やRuntime再生状態を変更しない。
- 既定ではAsset、Spline、Durationを修正しない。
- 警告値を映像品質の合格証明にしない。

### Remove

Slotから外す操作とSceneオブジェクトの削除を分ける。生成済みShotを削除する場合は対象を明示し、確認とUndoを必須とする。

## 8. Camera出力

Program出力は1台のUnity Cameraと、そのCameraに付属するCinemachine Brainを使用する。ShotのCinemachineCameraを切り替え、Unity CameraのTransformやLensを毎フレームコピーしない。

GameObject Menuによる初期作成はProgram Cameraを生成せず、`Camera.main`を自動割り当てせず、CameraやTagを変更しない。Program CameraはユーザーがRig Inspectorで明示的に指定する。`Apply / Sync`は明示指定されたCameraにCinemachine BrainがなければUndo対応で追加できる。

現在はCutだけを扱う。Preview、Take、Blend、映像CrossfadeをMotion Foundationへ含めない。

## 9. AuthoringデータとScene Override

Preset Assetは再利用可能な原本、生成済みShotはScene固有の派生結果とする。

- 通常のApplyはScene Overrideを保持する。
- RebuildはPreset値をScene Componentと適用済みMotion設定へ具体化し、Scene Overrideを上書きする。
- Scene Overrideを再利用したい場合はSave Shot As New Presetを使用する。
- Presetの変更を全Shotへ暗黙伝播しない。
- Preset参照、GameObject名、Hierarchy順だけで生成物を識別しない。

## 10. 拡張境界

将来のMIDI、App UI、Camera Bank、Preview、AIは、Switcher、Motion Player、Preset作成APIを利用する。

現在追加しないもの:

- 入力機器別の空Adapter
- Command Bus
- Service Locator
- DI Container
- 汎用Node Graph
- 剛体カメラ物理
- 独自Aim Solver
- Runtime Occlusion Solver
- Runtime AI
- 自動Take

責務分離は、Runtime状態の所有者が異なる、EditorとRuntimeのassembly境界が異なる、またはRuntimeとEditorが同じMotion評価を必要とする場合だけ新しい型にする。

## 11. namespaceとassembly

```text
Runtime/ -> VLiveKit.Camera
Editor/  -> VLiveKit.Camera.Editor
Tests/   -> VLiveKit.Camera.Tests
```

Runtime:

- Rig
- Motion Preset
- Rig Profile
- Shot
- Motion Player
- Motion Evaluator
- Switcher
- Keyboard Input

Editor:

- Builder
- Preset Baker
- Motion Validator表示
- IMGUI Custom Editors

## 12. 互換性

初回安定版までは旧Motion PresetのSerializedField、生成済みSpline、Shot再生APIとの互換性を保持しない。移行wrapperや旧式Playerを並存させない。

実装時は、Package同梱PresetとBuilderを新形式へ更新する。ユーザーが独自作成したAssetやSceneを暗黙変換、保存、削除しない。

## 13. 受け入れ条件

1. Rig、Switcher、Shot、Motion Player、Evaluator、Input、Builderの責務が重複していない。
2. RigのSlot一覧以外にShot順の正本がない。
3. Motion Player以外に再生状態の正本がない。
4. 各Shotが別々のCinemachineCamera、Spline、Aim Proxyを持つ。
5. PresetがRuntime状態やScene参照を持たない。
6. 通常のApplyでScene上の手動調整を失わない。
7. RuntimeとValidatorが同じMotion Evaluatorを使用する。
8. 無効な選択でProgramを失わない。
9. 現在不要な汎用frameworkや将来用interfaceが追加されていない。
10. Preset Assetの変更が生成済みShotまたはLive中のMotionへ暗黙伝播しない。
11. C#ソースはenum、struct、Serializable helperを含め1ファイル1型である。
