# カメラ仕様

## 1. 目的

この文書は、初期版のFixed ShotとSpline Shot、その移動と手動操作を定義する。汎用Motion PatternやAI生成形式はまだ定義しない。

## 2. Shotの種類

初期版はA/Bの2 Shotだけを実装する。AとBはそれぞれ専用のCinemachineCameraを持つ。

### Fixed Shot

- CinemachineCameraの位置、回転、Lens、追従設定を使用する。
- Programに選ばれても移動しない。
- Spline参照を要求しない。
- 意図した固定画として扱い、無理に微動を加えない。
- 初期版ではShot Aへ割り当て、正面ミドル等の安全な戻り先にする。

### Spline Shot

- Cinemachine 3のSpline Dollyを使用する。
- 初期版ではShot Bへ割り当てる。
- AがLiveの間にSpline始点へ準備する。
- BへCutした後、設定された速度と方向で終点まで動く。
- 終点で減速して構図を決め、LoopせずHoldする。
- Speed、Reverse、Hold、Resumeを受け付ける。

Orbit、Crane、Handheldなどの分類は、実際のShotを複数制作して共通差分が分かってから追加する。

## 3. VLiveCameraShotの設定

最初に必要なSerializedFieldは次の範囲に留める。

- Shot名
- CinemachineCamera
- Shot種類
- SplineまたはSpline Dolly参照
- 初期速度
- 最小速度
- 最大速度
- 初期方向
- 始点と終点
- 移動の加減速または進行Curve

項目名と正確な型はCinemachine 3の実APIを確認して決める。未使用の将来設定を追加しない。

## 4. 再生状態

Spline Shotが保持する状態は次のとおり。

- 現在の進行方向
- 現在速度
- Hold中か
- Programに選ばれているか
- 始点準備済みか

Spline上の現在位置はCinemachineコンポーネントを正本とし、同じ値を別フィールドへ複製しない。Cinemachine API上で直接保持できない場合だけ、必要な状態を1か所に持つ。

## 5. Program選択時

- Aは現在の固定構図をそのまま使用する。
- Bは準備済みの始点から移動を開始する。
- 同じShotの再選択では、既定で再生位置を変更しない。
- Live中のBをSpline先頭へ戻さない。
- AへCutしてBがOff Airになった後にだけ、Bを次の使用へ向けて始点へ戻す。

## 6. 移動操作

### Speed

- 現在速度を最小値と最大値の範囲で変更する。
- 方向は速度の符号と別状態にしてもよいが、二重管理にならない方法を選ぶ。
- 値変更でSpline位置を飛ばさない。

### Reverse

- 現在位置を保持したまま進行方向だけを反転する。
- 連打しても位置をResetしない。

### Hold / Resume

- Holdは現在位置で進行を停止する。
- ResumeはHold前の速度と方向で再開する。
- Hold中もCinemachineの追従とAimは継続してよい。

### 終点

- 終点へ近づくと速度を収束させる。
- 終点到達後は構図を保持する。
- 自動で始点へLoopまたはTeleportしない。

## 7. 構図と追従

初期版ではCinemachine 3標準コンポーネントのTracking Target、Composer、Dampingを使用する。独自の構図Solverを作らない。

- Target参照が欠落しても例外を発生させない。
- Target消失時に別の対象を自動探索しない。
- Shotごとの構図はCinemachineCamera側で調整する。
- Lens、Focus、Noiseの独自制御は初期版へ含めない。

## 8. Pattern Asset

初期版ではPattern用ScriptableObjectを作らず、Shotの設定をSceneまたはPrefabへ直接保持する。

同じ移動設定を複数Shotへ複製する実害が確認できた時点で、再利用可能なPattern Assetを設計する。その際も、最初に実際に使われた設定だけをデータ化する。

## 9. AI生成

AIによるCamera Pattern生成は将来構想とする。初期版ではスキーマ、Importer、生成metadata、承認状態を実装しない。

Pattern Assetの形式が人の手作業で安定した後に、AIが同じ形式を生成できるようにする。AI専用のRuntimeコードは作らない。

## 10. 初期受け入れ条件

1. A/Bが別々のCinemachineCameraを持つ。
2. AはSplineなしで安定構図を維持する。
3. BはOff Air中に始点へ準備され、Cut後に終点まで動く。
4. Bは終点で収束してHoldし、LoopやTeleportを行わない。
5. Speed、Reverse、Hold、Resumeで位置が飛ばない。
6. Live中のBをResetしない。
7. TargetやSpline参照が無効でもProgram全体が停止しない。
8. Pattern Assetや独自Solverなど、初期版に不要な仕組みが追加されていない。
