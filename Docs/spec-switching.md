# スイッチング仕様

## 1. 目的

この文書は、複数の独立したCinemachineCameraを番号指定で直接Cutする初期運用を定義する。

## 2. Switcherの状態

Switcherが持つ状態は次だけとする。

- 使用する`VLiveCameraRig`参照
- 現在のProgram Shot

Preview、Selected、Transitioning、Tally、Bankは追加しない。

Program Camera参照はRigが所有し、Cinemachine BrainはそのCameraのComponentから取得する。Switcherへ同じCameraまたはBrain参照を重複保存しない。

順序付きShot SlotはRigだけが所有する。Switcherへ別のPreset一覧やShot一覧を正本として持たせず、番号指定時にRigのSlot順から対象Shotを取得する。Slotがnullまたは無効でも後続番号を詰めない。

## 3. 直接Cut

1. Shot番号を受け取る。
2. Rigの同じ番号のSlotから対象Shotを取得する。
3. 対象Shotと専用CinemachineCameraが有効か確認する。
4. 現在と異なるCinemachineCameraをProgramにする。
5. Cinemachine BrainのCutとして切り替える。
6. Program Shot参照を更新する。
7. 移動Shotの自動再生を開始する。
8. Cut成功時だけProgram Shot名を1回Logする。

無効な番号や参照では現在のProgramを維持する。

## 4. Cinemachineとの関係

- Program出力は1台のUnity CameraとCinemachine Brainを使用する。
- 各Shotは別々のCinemachineCameraを持つ。
- Unity CameraへTransformやLensを毎フレームコピーしない。
- Live中のCinemachineCameraへ別Shotの位置、Spline、Lens、Targetを上書きしない。
- 初期版はCutだけとし、Blendと映像Crossfadeを含めない。

## 5. Shotの準備

- Off Air中の移動Shotは始点で待機する。
- ProgramへCutされた後に移動を開始する。
- 別ShotへCutするまでLive中の位置をResetしない。
- Off Airになった後にだけ次回用の始点へ戻す。
- Fixed Shotは設定された構図を維持する。

すべてのShotをA/B Slotへ交互にコピーする方式は使用しない。

## 6. 同一Shotの選択

現在のProgramと同じShotを選択しても何もしない。Spline位置、速度、方向、Hold状態を変更しない。Retriggerは必要になった時点で別操作として定義する。

## 7. Program確認

Cut成功時のConsole LogとCustom Inspectorの読み取り専用状態で現在のProgramを確認できるようにする。Runtime UIとMultiviewは作らない。

## 8. 受け入れ条件

1. キー番号とRigの順序付きShot Slotが一致する。
2. 各Shotが別々のCinemachineCameraとして明確にCutされる。
3. 無効な選択で現在のProgramを失わない。
4. 同一Shot選択で移動状態がResetされない。
5. 移動ShotがCut後に再生され、Off Air後に準備される。
6. Preview、Bank、Transition、Multiviewが先行実装されていない。
