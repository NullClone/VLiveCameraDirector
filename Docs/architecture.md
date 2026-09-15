# アーキテクチャ仕様

## 1. 目的

この文書は、VLive Camera Directorの責務、状態所有権、依存方向、Cinemachine統合、RuntimeとEditorの境界を定義する。Motionの値と計算は[motion.md](motion.md)、操作状態は[operation.md](operation.md)、Editor操作は[authoring.md](authoring.md)を正本とする。

## 2. 全体構成

```text
Motion Preset Assets ---> Rig ---> ordered Shot Slots
                           |             |
                           |             +--> Shot 1 ... Shot N
                           |                    each owns one CinemachineCamera
                           |                    Spline, Target Group, Group Framing
                           |                    Motion Player
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
| 基準Transform、Program Camera、共通Actor一覧、共通Physical Camera設定、正面基準、Scale、Shot順 | `VLiveCameraRig` |
| SlotのPreset参照、任意のActor Override、生成済みShot参照 | `VLiveCameraShotSlot` |
| Humanoid AnimatorとActorごとの構図半径 | `VLivePerformer` |
| 再利用可能なCamera Performance | `VLiveCameraMotionPreset` |
| Sceneへ適用済みのMotion設定 | `VLiveCameraShot` |
| 現在時刻、速度、方向、Hold、完了状態 | `VLiveCameraMotionPlayer` |
| 現在のProgram Shot | `VLiveCameraSwitcher` |
| キー割り当て | `VLiveCameraKeyboardInput` |
| 構図Overlayの初期値とPlay Mode中の表示状態 | `SplitLines` |

Switcherへ別のShot一覧を持たせず、Motion Player以外へ再生状態を複製しない。PresetはRuntime状態とScene参照を持たない。

## 4. 主要責務

| 型 | 責務 |
| --- | --- |
| `VLiveCameraRig` | Scene全体の基準、共通Actor、共通Physical Camera設定、順序付きShot Slotを保持する |
| `VLivePerformer` | ActorのHumanoid AnimatorとHead、Bust、Bodyの構図半径を保持する |
| `VLiveCameraMotionPreset` | Track集合からなる再利用可能なCamera Performanceを保持する |
| `VLiveCameraRigProfile` | 機材固有の操作応答とValidator推奨値を共有する |
| `VLiveCameraShot` | 専用Camera、Spline、Target Group、Group Framing、適用済みMotionとlifecycleを所有する |
| `VLiveCameraMotionPlayer` | 1 Shotの再生状態を進め、評価結果を自身のCinemachine構成へ適用する |
| `VLiveCameraMotionEvaluator` | 解決済みMotionと時刻からMotion Sampleを決定的に計算する |
| `VLiveCameraSwitcher` | RigのSlot順を使用してProgram Cutだけを管理する |
| `VLiveCameraKeyboardInput` | Keyboard入力をSwitcherの公開操作へ渡す |
| `SplitLines` | Game Viewのアスペクトマスク、構図ガイド、App UIテーマと設定パネルを管理する |
| `SplitLinesElement` | `SplitLines`から受け取ったマスク、外周フレーム、Split LineをPainter2Dで描画する |
| Editor Builder | 明示操作としてRig、Shot、Spline、Target Groupの生成、修復、再構築を行う |
| Motion Validator | AssetやSceneを変更せずMotionを診断する |

## 5. Preset、Shot、Playerの境界

Presetは原本、ShotはSceneへ適用したインスタンス、PlayerはRuntime状態である。

| データ | 再利用元 | 生成済みShotでの所有先 |
| --- | --- | --- |
| Body geometry | Motion Preset | `SplineContainer` |
| Timing、Progress、Entry、Activation | Motion Preset | Shotの適用済みMotion |
| Aim、Composition、Lens、Roll | Motion Preset | Shotの適用済みMotion |
| 解決済みActor一覧 | Rig共通ActorまたはShot Slot Override | ShotとCinemachine Target Group |
| ActorのHumanoidボーンと構図半径 | `VLivePerformer` | Cinemachine Target GroupのMember |
| Sensor Size、Gate Fit、Lens Shift、Near / Far Clip Plane | Rig | Program Cameraと各ShotのPhysical Lens |
| Rig応答 | Rig Profile | Rebuild時に解決した適用済みMotion |
| 現在時刻、速度、方向、Hold | なし | Motion Playerだけ |

通常のApplyはScene Overrideを保持しつつ、Rig共通ActorまたはShot Slot Overrideから解決したActor一覧とTarget Group Memberを同期する。Preset、Rig Profile、Scale、正面基準の変更はRebuildでのみ既存Shotへ反映する。Scene変更をPresetへ自動逆同期しない。

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
- `CinemachineTargetGroup`による単独・複数Actorの被写体範囲
- `CinemachineRotationComposer`による追従と構図補間
- `CinemachineGroupFraming`による被写体Groupの画面内維持
- Physical Lensと最終Camera Pipeline

### 統合規則

- 1 Shotにつき1つのCinemachineCameraを使用し、Live中のCameraを別Shotへ再構成しない。
- Program出力は1台のUnity CameraとCinemachine Brainを使用する。
- Cinemachine BrainはPhysical Lens overrideを有効にする。
- Unity CameraへTransformやLensを毎フレームコピーしない。
- ActorはRig共通一覧を通常値とし、Shot Slotで明示的にOverrideできる。`VLivePerformer`がHumanoid Animatorから直接取得したボーンをTarget Groupへ登録し、補助Proxyは作らない。
- Aim OffsetはRotation ComposerのTarget Offset、Screen PositionはGroup FramingのCenter Offsetへ適用する。
- Group Framingは画角を変更せずDollyで収まりを調整し、PresetのField of ViewまたはFocal Lengthを保持する。
- Aim、Damping、Lookahead、Group Framing、LensなどはCinemachine標準機能を優先する。
- Cinemachineと競合する独自Brain、独自Aim Solver、独自Camera Pipelineを作らない。
- 標準Pipelineで不足するとScene比較から確認できた処理だけを、Cinemachineの正式な拡張点へ追加する。
- RuntimeのHard SafetyはNaN、Infinity、無効参照を拒否するが、Presetの演出意図を別の動きへ置き換えない。

## 7. RuntimeとEditorの境界

Runtimeは再生、状態、決定的なMotion評価だけを扱う。Update中にScene、Spline、Assetを生成、削除、保存しない。

EditorはRig作成、Apply、Rebuild、Camera Settings一括適用、Validation、Custom Inspectorを扱う。Scene変更は明示操作、確認、Undoを伴い、Inspector描画や`OnValidate`だけでは実行しない。

RuntimeとEditor Validatorは同じMotion Evaluatorを使用する。EditorはRuntime状態を第二の正本として保持しない。

## 8. 目標フォルダとassembly

```text
Runtime/
  (主要撮影・リグ・操作コンポーネント: Rig, Profile, Shot, Slot, Switcher, KeyboardInput, Performer)
  Composition/
  Motion/Preset/
  Motion/Playback/
  Timeline/
  UI/
  VLiveKit.Camera.Runtime.asmdef

Editor/
  Inspectors/
  Authoring/
  Validation/
  VLiveKit.Camera.Editor.asmdef

Presets/
  Motion/
  DefaultRigProfile.asset
```

- Runtime namespaceは`VLiveKit.Camera`とする。
- Editor namespaceは`VLiveKit.Camera.Editor`とする。
- Testsを追加する場合は`VLiveKit.Camera.Tests`とする。
- フォルダ階層をnamespaceへ反映しない。
- RuntimeとEditorのasmdefを機能フォルダごとに細分化しない。
- `Core`、`Common`、`Utilities`、`Managers`、`Enums`、`Tools`のような投棄先フォルダを作らない。
- enumや密接に関連する補助型は、所有する主要機能のファイル（同一namespace）に同居させる。
- MIDI、AIなど未実装機能の空フォルダを先に作らない。

製品表示名は`VLive Camera Director`、Package IDは`com.toshi.vlivekit.camera-director`とする。Runtime asmdefは`VLiveKit.Camera.Runtime`、Editor asmdefは`VLiveKit.Camera.Editor`とし、コード型の`VLiveCamera`接頭辞は維持する。旧名の互換wrapperを並存させない。

## 9. 拡張境界

将来のMIDI、Camera Palette全体を扱うApp UI、Camera Bank、Preview、AIは、Switcher、Motion Player、Motion Presetの現行データ契約を利用する。現在の基盤へ次を先行追加しない。

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
9. C#ソースは主要型ごとに1ファイルとし、密接なenumや小さな補助型は同一ファイル・同一namespaceに同居させる。
10. 現在不要な汎用frameworkや将来用interfaceが存在しない。
11. ActorのHumanoidボーンを独自Proxyへ複製せず、Cinemachine Target Groupが直接参照する。
12. 共通Physical Camera設定はRigだけが所有し、Motion Presetへ重複保存しない。
