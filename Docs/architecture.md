# アーキテクチャ仕様

## 1. 目的

この文書は、VLive Camera Directorの責務、状態所有権、依存方向、Cinemachine統合、RuntimeとEditorの境界を定義する。Motionの値と計算は[motion.md](motion.md)、操作状態は[operation.md](operation.md)、Editor操作は[authoring.md](authoring.md)を正本とする。

## 2. 全体構成

```text
Motion Preset Assets ---> Rig ---> ordered Shot Slots
                           |             |
                           |             +--> Shot 1 ... Shot N
                           |                    each owns one CinemachineCamera
                           |                    Spline, Aim Proxy, Motion Player
                           v
Operator Input --------> Switcher --------> Cinemachine Brain
                           |                       |
                           v                       v
                     Program Shot ----------> Program Camera

Resolved Shot Motion + Playback Time
                |
                v
        Motion Evaluator
                |
                v
        Base Motion Sample
                |
                v
         Operator Trim
                |
                v
           Cinemachine
```

依存はEditorからRuntime、入力から公開操作、VLiveの演出意図からCinemachine出力へ向ける。Cinemachine Component、Custom Inspector、BuilderからPresetやRigへ状態を逆輸入しない。

## 3. 状態の正本

| 状態 | 唯一の所有者 |
| --- | --- |
| Target、Program Camera、正面基準、Scale、Shot順 | `VLiveCameraRig` |
| SlotのPreset参照と生成済みShot参照 | `VLiveCameraShotSlot` |
| 再利用可能なCamera Performance | `VLiveCameraMotionPreset` |
| Sceneへ適用済みのMotion設定 | `VLiveCameraShot` |
| 現在時刻、速度、方向、Hold、完了状態 | `VLiveCameraMotionPlayer` |
| 現在のProgram Shot | `VLiveCameraSwitcher` |
| キー割り当て | `VLiveCameraKeyboardInput` |

Switcherへ別のShot一覧を持たせず、Motion Player以外へ再生状態を複製しない。PresetはRuntime状態とScene参照を持たない。

## 4. 主要責務

| 型 | 責務 |
| --- | --- |
| `VLiveCameraRig` | Scene全体の設定と順序付きShot Slotを保持する |
| `VLiveCameraMotionPreset` | Track集合からなる再利用可能なCamera Performanceを保持する |
| `VLiveCameraRigProfile` | 機材固有の操作応答とValidator推奨値を共有する |
| `VLiveCameraShot` | 専用Camera、Spline、Aim Proxy、適用済みMotionとlifecycleを所有する |
| `VLiveCameraMotionPlayer` | 1 Shotの再生状態を進め、評価結果を自身のCinemachine構成へ適用する |
| `VLiveCameraMotionEvaluator` | 解決済みMotionと時刻からMotion Sampleを決定的に計算する |
| `VLiveCameraSwitcher` | RigのSlot順を使用してProgram Cutだけを管理する |
| `VLiveCameraKeyboardInput` | Keyboard入力をSwitcherの公開操作へ渡す |
| Editor Builder | 明示操作としてRig、Shot、Splineの生成、修復、再構築を行う |
| Preset Baker | Scene上のShotから新しいPreset Assetを保存する |
| Motion Validator | AssetやSceneを変更せずMotionを診断する |

## 5. Preset、Shot、Playerの境界

Presetは原本、ShotはSceneへ適用したインスタンス、PlayerはRuntime状態である。

| データ | 再利用元 | 生成済みShotでの所有先 |
| --- | --- | --- |
| Body geometry | Motion Preset | `SplineContainer` |
| Timing、Progress、Entry、Activation | Motion Preset | Shotの適用済みMotion |
| Aim、Composition、Lens、Roll | Motion Preset | Shotの適用済みMotion |
| Rig応答 | Rig Profile | Rebuild時に解決した適用済みMotion |
| 現在時刻、速度、方向、Hold | なし | Motion Playerだけ |

通常のApplyはScene Overrideを保持する。Preset、Rig Profile、Scale、正面基準、Slot参照の変更はRebuildでのみ既存Shotへ反映する。Scene変更をPresetへ自動逆同期しない。

## 6. Cinemachine統合契約

### VLive Camera Directorが所有するもの

- Shotの選択とlifecycle
- Motion PresetとRig Profile
- Playback Timeと手動介入
- Aim、Composition、Lens、Rollの演出値
- Editor AuthoringとValidation

### Cinemachineが所有するもの

- 1 Shotごとの`CinemachineCamera`
- Program Camera上の`CinemachineBrain`
- `CinemachineSplineDolly`によるSpline評価
- `CinemachineRotationComposer`による追従と構図補間
- Lensと最終Camera Pipeline

### 統合規則

- 1 Shotにつき1つのCinemachineCameraを使用し、Live中のCameraを別Shotへ再構成しない。
- Program出力は1台のUnity CameraとCinemachine Brainを使用する。
- Unity CameraへTransformやLensを毎フレームコピーしない。
- Aim、Damping、Lookahead、LensなどはCinemachine標準機能を優先する。
- Cinemachineと競合する独自Brain、独自Aim Solver、独自Camera Pipelineを作らない。
- 標準Pipelineで不足するとScene比較から確認できた処理だけを、Cinemachineの正式な拡張点へ追加する。
- RuntimeのHard SafetyはNaN、Infinity、無効参照を拒否するが、Presetの演出意図を別の動きへ置き換えない。

## 7. RuntimeとEditorの境界

Runtimeは再生、状態、決定的なMotion評価だけを扱う。Update中にScene、Spline、Assetを生成、削除、保存しない。

EditorはRig作成、Apply、Rebuild、Preset保存、Validation、Custom Inspectorを扱う。Scene変更は明示操作、確認、Undoを伴い、Inspector描画や`OnValidate`だけでは実行しない。

RuntimeとEditor Validatorは同じMotion Evaluatorを使用する。EditorはRuntime状態を第二の正本として保持しない。

## 8. 目標フォルダとassembly

```text
Runtime/
  Rig/
  Motion/Preset/
  Motion/Playback/
  Switching/
  Input/
  Composition/
  Performer/
  Timeline/
  VLiveKit.Camera.Runtime.asmdef

Editor/
  Inspectors/
  Authoring/
  Validation/
  VLiveKit.Camera.Editor.asmdef

Presets/
  Motion/
  RigProfiles/
```

- Runtime namespaceは`VLiveKit.Camera`とする。
- Editor namespaceは`VLiveKit.Camera.Editor`とする。
- Testsを追加する場合は`VLiveKit.Camera.Tests`とする。
- フォルダ階層をnamespaceへ反映しない。
- RuntimeとEditorのasmdefを機能フォルダごとに細分化しない。
- `Core`、`Common`、`Utilities`、`Managers`、`Enums`、`Tools`のような投棄先フォルダを作らない。
- enumは所有する機能の近くへ置く。
- App UI、MIDI、AIなど未実装機能の空フォルダを先に作らない。

製品表示名は`VLive Camera Director`、移行後のPackage IDは`com.toshi.vlivekit.camera-director`とする。コード型の`VLiveCamera`接頭辞は維持する。旧`VLiveCameraUnit`のフォルダ、Package ID、asmdef名は[roadmap.md](roadmap.md)の破壊的移行で置き換え、互換wrapperを並存させない。

## 9. 拡張境界

将来のMIDI、App UI、Camera Bank、Preview、AIは、Switcher、Motion Player、Preset作成APIを利用する。現在の基盤へ次を先行追加しない。

- 入力機器別の空Adapter
- Command Bus、Service Locator、DI Container
- 汎用Node Graph
- Rigidbody Camera物理
- Runtime Occlusion Solver
- Runtime AIと自動Take

新しい型は、状態所有者、assembly境界、または決定的評価の再利用が実際に異なる場合だけ追加する。

## 10. 不変条件

1. RigのSlot一覧以外にShot順の正本がない。
2. Motion Player以外に再生状態の正本がない。
3. 各Shotが別々のCinemachineCameraを持つ。
4. PresetがRuntime状態やScene参照を持たない。
5. 通常のApplyでScene上の手動調整を失わない。
6. RuntimeとValidatorが同じMotion Evaluatorを使用する。
7. 無効な選択で現在のProgramを失わない。
8. Preset変更が生成済みShotまたはLive中のMotionへ暗黙伝播しない。
9. C#ソースはenum、struct、Serializable helperを含め1ファイル1型である。
10. 現在不要な汎用frameworkや将来用interfaceが存在しない。
