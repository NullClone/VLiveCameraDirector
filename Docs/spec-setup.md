# Setup Window仕様

## 1. 目的

`VLive Camera Setup`は、ユーザーの現在のSceneへVLiveCameraUnitを導入する唯一の初期セットアップ画面とする。HierarchyとComponentを手作業で組み立てなくても、1つのWindowと1回の実行で開始できる状態を作る。

## 2. 開き方

Unityの`Tools/VLive Camera/VLive Camera Setup`からEditorWindowを開く。

## 3. 入力

最初のWindowに必要な入力は次だけとする。

- Performer Target
- 使用する既存Output Camera、または新規作成
- 使用するMotion Presetの選択と順序

Targetは必須とする。初期状態では6つの標準Presetを選択済みにしてよい。

## 4. Palette

Windowは利用可能な`VLiveCameraMotionPreset`を名前で一覧表示する。

- 使用するPresetを選択できる。
- Shot番号となる順序を確認できる。
- 最初は検索、カテゴリ、画像サムネイルを実装しない。
- Preset編集専用の大規模ツールは作らない。

PaletteはAssetの一覧であり、Runtime中にShotを動的再構成する仕組みではない。

## 5. Create / Update

`Create / Update Camera Rig`を押すと、現在のSceneへ次を作成または更新する。

- 1台のProgram CameraとCinemachine Brain
- `VLiveCameraSwitcher`
- `VLiveCameraKeyboardInput`
- 選択Presetごとの`VLiveCameraShot`
- Shotごとの専用CinemachineCamera
- Spline Shotごとの専用Spline
- Target、Preset、Shot一覧、キー順序の参照

生成RootはWindowが識別できる固定名または専用Componentを持つ。既存Rootがある場合は重複生成せず、Windowが所有する範囲だけを更新する。

- 初回CreateではPresetの初期値からCameraとSplineを作る。
- Updateでは参照、Target、Shot順を更新し、不足するShotだけを追加する。
- 既存Shotへユーザーが加えた構図、Lens、Spline調整は上書きしない。
- 既存Shotの削除やPreset初期値へのResetは、初期Windowへ含めない。

## 6. 安全性

- すべてのScene変更をUndoできる。
- 既存Cameraを選択した場合は、必要なComponentと設定だけを変更する。
- Windowが作成していない無関係なGameObjectやComponentを削除しない。
- Sceneを自動保存しない。
- 必須参照がない場合は生成せず、Window内に理由を表示する。
- 再実行で同じShotやCameraを増殖させない。
- 再実行でユーザー調整値をPreset初期値へ戻さない。
- Prefab Instanceを編集する場合はPrefab Overrideを正しく記録する。

## 7. Inspectorとの関係

Windowは初期導入を担当し、生成後の細かな値調整は各CustomEditorで行う。WindowとCustomEditorが別々の設定値を所有しない。

## 8. Agent確認

実装エージェントは次だけを既定確認とする。

- WindowがCompileされ、Menuから開ける。
- SerializedProperty、Undo、参照設定に明白な問題がない。
- 差分全体に不要な生成、削除、抽象化がない。

専用テストSceneの作成、ユーザーSceneの保存、Game Viewでの構図評価、長時間試験は行わない。実際のCreate / Update結果とカメラワークはユーザーが作業用Sceneで確認する。

## 9. 受け入れ条件

1. 1つのWindowでTarget、Output Camera、Presetを指定できる。
2. 1回の操作で必要なRigと複数Shotを作成できる。
3. 再実行しても重複生成しない。
4. Undoでき、Sceneを自動保存しない。
5. 初期PaletteのShotをキーで切り替えられる状態になる。
