# アーキテクチャ仕様

## 1. 目的

この文書は、1 Target、複数Shot、Motion Palette、Inspector主導のRig Authoringからなる現在の構成と所有権を定義する。責務は明確に分けるが、MIDI、Runtime AI、Preview、自動化のためだけの抽象層は含めない。

## 2. 構成

```text
Create Menu / Setup Window (Editor)
             |
             | creates one rig
             v
      VLiveCameraRig <--- Motion Presets
             | owns ordered Shot Slots
             | each slot references one generated Shot
             v
       Shot 1 ... Shot N
             | each owns one CinemachineCamera
             |
Keyboard Input ---> Switcher ---> Cinemachine Brain ---> Unity Camera

Rig Inspector ---> VLiveCameraRigBuilder (Editor only)
                      | explicit Apply / Sync / Rebuild
                      v
                 current Scene
```

現在必要な主要責務は次のとおりとする。

| 型 | 責務 |
| --- | --- |
| `VLiveCameraRig` | Target、Program Camera参照、正面基準、構図スケール、順序付きShot SlotをScene上の正本として保持する |
| `VLiveCameraMotionPreset` | FixedまたはSpline移動の再利用可能な初期値を保持する |
| `VLiveCameraShot` | 専用CinemachineCamera、Spline参照、Live中の再生状態を持つ |
| `VLiveCameraSwitcher` | Rigの順序付きShotを使用し、現在のProgramとCutだけを管理する |
| `VLiveCameraKeyboardInput` | キーボード入力をSwitcherへ渡す |
| `VLiveCameraRigBuilder` | Editor上でRig、Shot、Camera、Splineを明示的に生成、同期、再構築する |
| `VLiveCameraRigEditor` | Rig設定、検証結果、同期操作、Live状態をInspectorへ提示する |
| `VLiveCameraSetupWindow` | 初期Rig作成だけを行う小さな入口を提供する |

`VLiveCameraRigBuilder`はWindowとInspectorから共通利用されるEditor専用の具体的な処理であり、汎用frameworkやRuntime Managerにはしない。

## 3. 状態の正本

### RigとShot Slot

`VLiveCameraRig`の順序付きShot Slot一覧を、使用するShotと番号順の唯一の正本とする。各Slotが保持する値は現在必要な次だけとする。

- `VLiveCameraMotionPreset`参照
- 対応する`VLiveCameraShot`参照

Preset参照、リスト番号、GameObject名だけで生成済みShotを識別しない。同じPresetを複数Slotで使用でき、それぞれが別のShotとCinemachineCameraを参照する。リストの並び替えでShotの実体を交換、再生成、初期化しない。

Switcherに別のPreset一覧やShot一覧を正本として重複保持しない。SwitcherはRigを参照し、RigのSlotを番号どおりに扱う。無効なSlotを選択した場合は現在のProgramを維持する。

### Motion Preset

Preset Assetは人または将来のAIが作る、再利用可能な動きの初期値である。

- 表示名
- FixedまたはSpline
- 基準空間の相対制御点
- Field of View
- 注視基準からの構図オフセット
- 初期進行速度
- 終端への減速距離または進行Curve

現在速度、方向、Hold、Live状態、生成済みShot参照はAssetへ保存しない。

### Shot

ShotはScene上の実体であり、専用CinemachineCameraを参照する。Spline Shotは専用Splineを使用する。現在速度、方向、Hold、Live状態はShotだけが所有する。生成元PresetはRigのSlotだけが参照し、Shotへ重複保存しない。

Presetの設定をRuntime状態へ重複保存しない。Cinemachine API上で必要なSceneデータはPresetから生成した派生結果として扱う。ただし初回生成後のCamera、Lens、Splineはユーザーが調整できるため、通常の同期ではPreset値へ戻さない。

## 4. Runtime責務

- `VLiveCameraRig`だけがTarget、正面基準、スケール、Shot Slot順を所有する。
- `VLiveCameraKeyboardInput`だけがキー入力を読む。
- `VLiveCameraSwitcher`だけがProgram Shotを変更する。
- 各`VLiveCameraShot`だけが自身の移動状態を変更する。
- 各Shotは自身のCinemachineCameraとSplineだけを使用する。
- Motion Presetは初期設定データ、Shotは生成結果と実行状態を所有する。
- Cameraの最終評価はCinemachineへ任せる。

同じSpline位置、Lens、Program状態へ複数箇所から書き込まない。

RuntimeコードはSceneオブジェクトやAssetを生成、削除しない。Editorからの同期処理を`OnValidate`、`Update`、Property setterから呼ばない。

## 5. Editor責務

### Create

メニューまたはSetup Windowは、新しいRig一式をUndo可能な1操作で作成し、生成したRigを選択する。既存の任意のSwitcherを探索して流用しない。既存Rigがある場合も、名前だけを根拠に所有物を変更しない。

### Apply / Sync

Rig Inspectorの`Apply / Sync`は次だけを行う。

- 不足するShot、CinemachineCamera、Splineの生成
- 壊れた参照の修復
- TargetとProgram出力参照の反映
- Slot順の反映
- 新規生成物へのPreset初期値適用

既存ShotのTransform、Lens、Spline形状、速度調整をPreset値へ戻さない。Slotから外れたShotを自動削除しない。

### Rebuild From Preset

選択Shotまたは全ShotをPreset初期値から再構築する明示操作とする。上書き対象をInspectorに表示し、確認後にUndo可能な1操作として実行する。通常のApplyと同じボタンや暗黙処理にしない。

### Remove

Slotから外す操作とSceneオブジェクトの削除を分ける。生成済みShotを削除する場合は対象を明示し、確認とUndoを必須とする。無関係なCamera、Spline、GameObjectを削除しない。

## 6. Camera出力

Program出力は1台のUnity Cameraと、そのCameraに付属するCinemachine Brainを使用する。RigがProgram Camera参照を所有し、BrainはそのComponentから取得する。ShotのCinemachineCameraを切り替え、Unity CameraのTransformやLensを毎フレームコピーしない。現在はCutだけを扱う。

新しいProgram Cameraへ`MainCamera`タグを付けるのは、Sceneに既存のMain Cameraがない場合だけとする。既存Cameraを使用または変更する場合はユーザーが明示的に指定する。

## 7. 拡張境界

将来のMIDI、Bank、Preview、AIは、Switcherの公開された操作またはMotion Presetを利用する。現在の段階では、それらのためのinterface、Adapter、Command Bus、Service Locator、Factory、空設定を追加しない。

責務分離は、Runtime状態の所有者が異なる、EditorとRuntimeのassembly境界が異なる、または複数の入口から同じScene変更処理を安全に共有する、という現在の理由がある場合だけ新しい型にする。

## 8. namespaceとassembly

```text
Runtime/ -> VLiveKit.Camera
Editor/  -> VLiveKit.Camera.Editor
Tests/   -> VLiveKit.Camera.Tests
```

RuntimeとEditorのasmdefを分離する。PresetとRig、Shot、Switcher、InputはRuntime、Builder、Setup Window、CustomEditorはEditorへ置く。

## 9. 互換性

初回安定版までは旧API、SerializedField、Prefab、Sceneとの互換性を保持しない。移行wrapperや将来用interfaceは作らない。無関係な既存コードやユーザー変更は保持する。

## 10. 受け入れ条件

1. Rig、Switcher、Shot、Input、Preset、Editor Builderの責務が重複していない。
2. RigのSlot一覧以外にShot順の正本がない。
3. 各Shotが別々のCinemachineCameraを持つ。
4. 同じPresetを複数Slotで使用できる。
5. 通常の同期でユーザー調整値を失わない。
6. Scene変更が明示操作、Undo、Prefab Override、Scene Dirtyへ正しく反映される。
7. 無効な選択でProgramを失わない。
8. 現在不要な抽象層が追加されていない。
