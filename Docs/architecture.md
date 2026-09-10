# アーキテクチャ仕様

## 1. 目的

この文書は、1 Target、複数Shot、Motion Palette、Setup Windowからなる現在の最小構成を定義する。MIDI、Runtime AI、Preview、自動化のための抽象層は含めない。

## 2. 構成

```text
VLiveCameraSetupWindow (Editor)
      | creates / updates
      v
Motion Presets ---> Shot 1 ... Shot N
                         |
Keyboard Input ---> Switcher
                         |
           independent CinemachineCameras
                         |
                Cinemachine Brain
                         |
                   Unity Camera
```

現在必要な主要型は次の5つとする。

| 型 | 責務 |
| --- | --- |
| `VLiveCameraMotionPreset` | FixedまたはSpline移動を再利用可能なAssetとして保持する |
| `VLiveCameraShot` | 専用CinemachineCamera、Preset参照、Live中の再生状態を持つ |
| `VLiveCameraSwitcher` | Shot一覧と現在のProgramを管理し、番号指定でCutする |
| `VLiveCameraKeyboardInput` | キーボード入力をSwitcherへ渡す |
| `VLiveCameraSetupWindow` | TargetとPresetから現在のSceneへ必要な構成を生成・更新する |

専用Manager、Registry、Command Bus、DI、独自Solverは追加しない。

## 3. データとSceneの責務

### Motion Preset

Preset Assetは人または将来のAIが作る、再利用可能な動きの正本である。

- 表示名
- FixedまたはSpline
- Targetローカル基準の相対制御点
- Field of View
- Tracking Targetのオフセット
- 初期進行速度
- 終端への減速距離または進行Curve

現在速度、方向、Hold、Live状態はAssetへ保存しない。

### Shot

ShotはScene上の実体であり、Presetと専用CinemachineCameraを参照する。Spline ShotはSetup Windowが生成した専用Splineを使用する。

Presetの設定をRuntime状態へ重複保存しない。Cinemachine API上で必要なSceneデータはPresetから生成した派生結果として扱う。初回生成後にユーザーが調整したCinemachineCameraやSplineの値は、通常のUpdateで上書きしない。

### Switcher

Switcherは順序付きShot一覧と現在のProgramだけを持つ。A/B固定フィールドは複数Shot対応へ置き換えるが、Shotを動的なSlotとして再構成しない。

## 4. Setup Window

WindowはEditor専用とし、Runtime assemblyから参照しない。

- Targetは必須。
- Output Cameraは既存選択または新規作成を許可する。
- 選択されたPresetごとに独立したShotとCinemachineCameraを作る。
- Shot順をキー番号として使用する。
- 既存の生成物を識別し、同じ操作で重複を増やさず更新できる。
- 初回CreateはPreset値を適用し、Updateは参照修復、順序更新、不足Shotの追加を基本とする。
- Updateで既存Shotの構図、Spline、LensをPreset初期値へ戻さない。
- Undo、Prefab Override、Scene Dirtyを正しく扱う。
- Sceneを自動保存しない。
- 関係のないCamera、GameObject、Assetを変更しない。

最初は名前一覧の簡易Paletteでよい。検索、カテゴリ、画像サムネイルは作らない。

## 5. Runtime所有権

- `VLiveCameraKeyboardInput`だけがキー入力を読む。
- `VLiveCameraSwitcher`だけがProgram Shotを変更する。
- 各`VLiveCameraShot`だけが自身の移動状態を変更する。
- 各Shotは自身のCinemachineCameraとSplineだけを使用する。
- Motion Presetは設定データ、Shotは実行状態を所有する。
- Cameraの最終評価はCinemachineへ任せる。

同じSpline位置、Lens、Program状態へ複数箇所から書き込まない。

## 6. Camera出力

Program出力は1台のUnity CameraとCinemachine Brainを使用する。ShotのCinemachineCameraを切り替え、Unity CameraのTransformやLensを毎フレームコピーしない。現在はCutだけを扱う。

## 7. namespaceとassembly

```text
Runtime/ -> toshi.VLiveKit.Camera
Editor/  -> toshi.VLiveKit.Camera.Editor
Tests/   -> toshi.VLiveKit.Camera.Tests
```

RuntimeとEditorのasmdefを分離する。PresetはRuntime、Setup WindowとCustomEditorはEditorへ置く。

## 8. 互換性

初回安定版までは旧API、SerializedField、Prefab、Sceneとの互換性を保持しない。移行wrapperや将来用interfaceは作らない。無関係な既存コードやユーザー変更は保持する。

## 9. 受け入れ条件

1. 5つの主要型の責務が重複していない。
2. 各Shotが別々のCinemachineCameraを持つ。
3. Preset設定とShotのRuntime状態が分離されている。
4. Setup Windowが現在のSceneだけを安全に生成・更新する。
5. 無効な選択でProgramを失わない。
6. 現在不要な抽象層が追加されていない。
