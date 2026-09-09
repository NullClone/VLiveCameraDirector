# 操作仕様

## 1. 目的

この文書は、最初の動作版で必要なキーボード操作と、後から追加する操作を分けて定義する。

## 2. 初期操作

初期版では、次の操作だけを実装する。

| 操作 | 対象 | 動作 |
| --- | --- | --- |
| Cut A | Switcher | 独立したCinemachineCameraを持つ安定Shot AへCutする |
| Cut B | Switcher | 独立したCinemachineCameraを持つ移動Shot BへCutする |
| Speed Up / Down | Live中のB | Spline進行速度を増減する |
| Reverse | Live中のB | 現在の進行方向を反転する |
| Hold | Live中のB | 現在位置でSpline進行を停止する |
| Resume | Live中のB | Holdを解除して進行を再開する |

AがLiveの場合にSpeed、Reverse、Hold、Resumeを入力しても画を変更せず、例外を発生させない。

## 3. 基本フロー

1. AがLiveの間、BをSpline始点でStandbyさせる。
2. オペレーターがキー2を押す。
3. SwitcherがBのCinemachineCameraへCutする。
4. Bが始点から終点へ移動し、終点でHoldする。
5. 必要なときだけ速度、Reverse、Hold、Resumeを操作する。
6. キー1でAへCutして安定構図へ戻る。
7. BがOff Airになってから、次の使用に向けて始点へResetする。

同じShotを再選択しても、既定では状態を変更しない。Live中のBをResetして画面上で瞬間移動させない。

## 4. キーボード実装

- キー入力を読むのは`VLiveCameraKeyboardInput`だけとする。
- `VLiveCameraKeyboardInput`は`VLiveCameraSwitcher`の公開メソッドを呼ぶ。
- カメラ制御クラスの内部で直接キー入力を読まない。
- キー割り当てはInspectorで変更可能にしてよい。
- Game Viewのフォーカスを失ったとき、押下状態が残留しないことを確認する。
- 同一フレームで複数Shotキーが押された場合の動作を決定的にする。

初期キーの具体値はUnity EditorとOSのショートカット競合を確認して決める。

## 5. 応答と失敗時

- Cut、Reverse、Hold、Resumeは入力を受けたフレームで処理を開始する。
- 存在しないShot番号を指定しても現在のProgramを維持する。
- ShotまたはCinemachineCamera参照が無効な場合も現在のProgramを維持する。
- 速度にはInspectorで設定する最小値と最大値を適用する。
- 例外や警告を毎フレーム出し続けない。

## 6. 次の操作

初期版の動作確認後、次の順で追加を検討する。

1. Preview選択とTake
2. Camera Bank
3. Pan、Tilt、Zoom、Dutchなどのライブ調整
4. Patternの強度や再生位置の操作
5. MIDI

これらのためのCommand Bus、Input Adapter階層、汎用Mapping Assetは初期版へ追加しない。

## 7. MIDI方針

MIDIはキーボード版が完成してから追加する。その時点で、既存の`VLiveCameraSwitcher`と`VLiveCameraShot`の公開操作を再利用する。

- MIDIが未接続でも全操作を継続できる。
- 絶対値Faderを使用する場合は値飛び防止を実装する。
- 特定機種の処理をShotやSwitcherへ埋め込まない。
- MIDIのための未使用コードを先に作らない。

## 8. 初期受け入れ条件

1. キーボードだけでA/Bを交互にCutできる。
2. A/Bは別々のCinemachineCameraである。
3. BはCut後に始点から終点へ移動し、終点でHoldする。
4. Speed、Reverse、Hold、ResumeがLive中のBだけへ作用する。
5. Aへの移動操作で例外が発生しない。
6. Live中のBをResetしない。
7. 無効な選択とフォーカス喪失で状態が壊れない。
