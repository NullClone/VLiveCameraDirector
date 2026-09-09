# スイッチング仕様

## 1. 目的

この文書は、最初に実装する直接Cutと、その後に追加するProgram / Preview運用を分けて定義する。

## 2. 初期版の状態

初期版でSwitcherが保持する状態は次の2つだけとする。

- 登録されたShot一覧
- 現在のProgram Shot

Selected、Preview、Transitioning、Tally、Bankなどの状態はまだ追加しない。

## 3. 直接Cut

1. 数字キーに対応するShot番号を受け取る。
2. 対象ShotとCinemachineCameraが有効か確認する。
3. CinemachineのProgram対象を切り替える。
4. 現在のProgram Shot参照を更新する。
5. Program名をConsoleへ1回だけLogする。

無効な番号、null参照、無効なShotでは現在のProgramを維持する。

## 4. Cinemachineによる切り替え

- Program出力は1台のUnity CameraとCinemachine Brainを使用する。
- Unity CameraへShot CameraのTransformやLensを毎フレームコピーしない。
- 初期版はCutだけを実装する。
- Cinemachine Blendと映像Crossfadeは初期版へ含めない。
- Shotの有効化方法はCinemachine 3の推奨方式を確認して決める。

## 5. Shot一覧

- Shot数を9に固定しない。
- 数字キーで直接選べる範囲を超えたShotの操作方法は、Camera Bank実装時に決める。
- 初期版のために64台対応を検証しない。
- 登録上限と同時描画性能を同じ問題として扱わない。

## 6. 同一Shotの選択

- 現在のProgramと同じShotを選択しても、既定では何もしない。
- Spline位置、速度、方向、Hold状態をResetしない。
- Retriggerや先頭再生は必要になった時点で別操作として追加する。

## 7. Program確認

初期版では、Cut成功時に現在のProgram Shot名をConsoleへ1回だけ表示する。Runtime UI、専用Inspector、Multiviewは作らない。

Log出力が送出処理を停止させないようにする。実用的なProgram / Preview表示は次の段階で追加する。

## 8. 次の段階

直接Cutと移動操作が成立した後、次の順で追加する。

1. Preview Shot
2. Take
3. Program / Preview Tally
4. Camera Bank
5. Cinemachine Blend
6. 必要性を確認した場合だけMultiview

Video TransitionはCinemachine Blendとは別機能として扱うが、必要になるまで設計しない。

## 9. 初期受け入れ条件

1. 3台以上のShotを数字キーでCutできる。
2. 無効な選択で現在のProgramを失わない。
3. 同一Shotの再選択で移動状態がResetされない。
4. Cut後のSpline Shotが動き続ける。
5. Cut成功時にProgram Shot名が1回だけLogされる。
6. Preview、Bank、Transition、Multiviewの先行実装がない。
