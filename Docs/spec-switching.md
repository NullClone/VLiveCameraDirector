# スイッチング仕様

## 1. 目的

この文書は、最初に実装する直接Cutと、その後に追加するProgram / Preview運用を分けて定義する。

## 2. 初期版の状態

初期版でSwitcherが保持する状態は次の3つだけとする。

- Shot A参照
- Shot B参照
- 現在のProgram Shot

Selected、Preview、Transitioning、Tally、Bankなどの状態はまだ追加しない。

## 3. 直接Cut

1. AまたはBのCut指示を受け取る。
2. 対象Shotと専用CinemachineCameraが有効か確認する。
3. 現在と異なるCinemachineCameraをLiveにする。
4. Cinemachine BrainのCutとしてProgramを切り替える。
5. 現在のProgram Shot参照を更新する。
6. Program名をConsoleへ1回だけLogする。

無効な番号、null参照、無効なShotでは現在のProgramを維持する。

## 4. Cinemachineによる切り替え

- Program出力は1台のUnity CameraとCinemachine Brainを使用する。
- A/Bには別々のCinemachineCameraを割り当てる。
- Unity CameraへShot CameraのTransformやLensを毎フレームコピーしない。
- Live中のCinemachineCameraへ別ShotのTransform、Spline、Lens、Targetを上書きしない。
- 初期版はCutだけを実装する。
- Cinemachine Blendと映像Crossfadeは初期版へ含めない。
- Shotの有効化方法はCinemachine 3の推奨方式を確認して決める。

## 5. A/Bの準備

- AがLiveの間、BはSpline始点で移動停止状態にする。
- BへCutした後にBの移動を開始する。
- BがLiveの間、Aは安定構図を維持する。
- Aへ戻った後にだけ、Bを次の使用へ向けて始点へResetする。
- Live中のShotをResetまたは別Shot設定で再構成しない。

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

多カメラ化した後はA/B SlotへShot設定を交互にコピーせず、原則として1 Shotにつき1つのCinemachineCameraを用意して直接切り替える。A/B固定Slot方式は、大量の動的Shotを扱う必要性が実測された場合だけ再検討する。

## 9. 初期受け入れ条件

1. A/Bが別々のCinemachineCameraとして構成されている。
2. AからB、BからAへ明確なCutとして切り替えられる。
3. 無効な選択で現在のProgramを失わない。
4. 同一Shotの再選択で移動状態がResetされない。
5. BはCut後に始点から動き、終点でHoldする。
6. Aへ戻るまでBをResetしない。
7. Cut成功時にProgram Shot名が1回だけLogされる。
8. Preview、Bank、Transition、Multiviewの先行実装がない。
