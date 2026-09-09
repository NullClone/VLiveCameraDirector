# 初期アーキテクチャ仕様

## 1. 目的

この文書は、最初の動作版に必要な構成だけを定義する。将来のMIDI、AI、推薦、完全自動化を想定した抽象層は含めない。

## 2. 初期構成

```text
VLiveCameraKeyboardInput
           |
           v
VLiveCameraSwitcher ------> VLiveCameraShot
           |                       |
           v                       v
   CinemachineBrain        CinemachineCamera
                                  + Spline
```

初期実装で新たに必要な主要型は3つとする。

| 型 | 責務 |
| --- | --- |
| `VLiveCameraShot` | 1つのShot、CinemachineCamera参照、移動設定、速度、方向、Hold状態を管理する |
| `VLiveCameraSwitcher` | Shot一覧、現在のProgram、番号指定Cutを管理する |
| `VLiveCameraKeyboardInput` | キーボード入力を読み、Switcherまたは現在のShotの公開メソッドを呼ぶ |

最初の段階ではinterface、Command Bus、Registry、Coordinator、独自Solver、Adapter階層を作らない。

## 3. VLiveCameraShot

`VLiveCameraShot`はScene上のShotを表すMonoBehaviourとする。

最初に必要な設定:

- Shot名
- CinemachineCamera参照
- FixedまたはSpline移動
- Spline参照
- 初期速度
- Loopの有無
- 初期方向

最初に必要な操作:

- 再生開始
- 速度変更
- Reverse
- Hold
- Resume

Fixed ShotにはSplineを要求せず、TransformとCinemachine設定をそのまま使用する。移動ShotだけがCinemachine 3のSpline Dollyを使用する。

## 4. VLiveCameraSwitcher

`VLiveCameraSwitcher`はScene上のShotリストと現在のProgram Shotを保持する。

- 番号からShotを選択する。
- 無効な番号や参照欠落では、現在のProgramを維持する。
- Cut時は対象のCinemachineCameraを有効なProgram状態にする。
- 選択後、対象Shotの自動移動を継続する。
- 同じShotを再選択しても、既定では再生位置をResetしない。
- Shot数を9に固定しない。ただし初期UIにBankは作らない。

Program出力は1台のUnity CameraとCinemachine Brainを使用する。既存実装のようにUnity CameraのTransformやLensを毎フレームコピーしない。

## 5. VLiveCameraKeyboardInput

初期入力はUnity標準のキーボード入力で実装する。使用する入力APIは、Unity 6.3のプロジェクト設定を確認して決定する。

- 数字キー: ShotをCut
- 増減キー: 現在のShotの速度を変更
- Reverseキー: 進行方向を反転
- Holdキー: 押下またはToggleで停止
- Resumeキー: 再開

キー割り当てはInspectorから設定可能にしてよいが、汎用Input Mappingアセットはまだ作らない。

## 6. 更新と所有権

- `VLiveCameraKeyboardInput`だけがキー入力を読む。
- `VLiveCameraSwitcher`だけがProgram Shotを変更する。
- 各`VLiveCameraShot`だけが自身のSpline進行状態を変更する。
- Cameraの最終評価はCinemachineへ任せる。
- 同じSpline位置やLensへ複数コンポーネントから書き込まない。

具体的な`Update`、`LateUpdate`、Cinemachine更新順は最小試作で確認し、必要になった設定だけを採用する。

## 7. namespaceとassembly

```text
Runtime/  -> toshi.VLiveKit.Camera
Editor/   -> toshi.VLiveKit.Camera.Editor
Tests/    -> toshi.VLiveKit.Camera.Tests
```

RuntimeとEditorのasmdefは分離する。最初の動作版に不要な追加assemblyは作らない。

## 8. 互換性

旧バージョンとのコード、Prefab、Scene、SerializedFieldの互換性は保持しない。

- 旧型を新namespaceへ転送しない。
- 旧SerializedFieldの移行処理を作らない。
- 旧API wrapperを残さない。
- 新しい基準SceneまたはPrefabを作り直してよい。

既存コードを削除するときは、実装タスクに対象パスを明記する。互換性不要を理由に、タスク外のファイルまで一括削除しない。

## 9. 抽出の条件

次のいずれかが実際に発生した場合だけ、責務の分割や共通化を検討する。

- 同じ処理が2つ以上の型に重複した。
- 1つの型が独立して検証すべき複数の状態を持ち、修正が干渉した。
- MIDI追加時に、キーボードと共通の操作入口が必要になった。
- Pattern Asset追加時に、Scene設定の複製が実害になった。
- Profilerまたは不具合調査で、現在の構成が問題だと確認できた。

「将来使うかもしれない」は抽出理由にしない。

## 10. 初期受け入れ条件

1. Unity 6.3とCinemachine 3でCompileできる。
2. Fixed ShotとSpline Shotを同じSwitcherからCutできる。
3. Spline ShotはProgram選択後も滑らかに動き続ける。
4. 速度、Reverse、Hold、Resumeが現在のProgram Shotへ作用する。
5. 無効な選択でProgramが失われない。
6. 主要型が本仕様にない抽象層へ分割されていない。
