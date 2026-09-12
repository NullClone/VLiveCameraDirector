# スイッチング仕様

## 1. 目的

この文書は、複数の独立したCinemachineCameraを番号指定で直接Cutし、Motion PresetのEntry状態から再生する運用を定義する。

## 2. Switcherの状態

Switcherが持つ状態は次だけとする。

- 使用する`VLiveCameraRig`参照
- 現在のProgram Shot

Preview、Selected、Transitioning、Tally、BankはMotion Foundationへ追加しない。

Program Camera参照はRigが所有し、Cinemachine BrainはそのCameraのComponentから取得する。Switcherへ同じCameraまたはBrain参照を重複保存しない。

順序付きShot SlotはRigだけが所有する。Switcherへ別のPreset一覧やShot一覧を正本として持たせず、番号指定時にRigのSlot順から対象Shotを取得する。Slotがnullまたは無効でも後続番号を詰めない。

## 3. Shotの準備状態

Off-Airの各Shotは、適用済みMotionのEntry Modeに従って準備する。

| Entry Mode | Off-Air準備 |
| --- | --- |
| `Static` | In Pointの位置、Aim、Lensへ置き、速度0で待機する |
| `Rolling` | In Pointの位置、Aim、Lensへ置き、Take後に非0速度から継続できる状態で待機する |
| `Continuous` | Off-Air中も進行する。初期Paletteでは使用しない |

PrepareはOff-Air中だけ実行する。Live中のShotをIn Pointへ戻したり、別Presetまたは別の適用済みMotionで上書きしたりしない。

RollingのDirect Cutは、実時間のPreview再生を必須にしない。適用済みMotionのIn Point自体が移動途中の状態を表し、最初の表示区間から運動を継続する。

## 4. 直接Cut

1. Shot番号を受け取る。
2. Rigの同じ番号のSlotから対象Shotを取得する。
3. 対象Shot、Motion Player、専用CinemachineCamera、適用済みMotionの必須値を確認する。
4. 現在と異なるCinemachineCameraをProgramにする。
5. Cinemachine BrainのCutとして切り替える。
6. Program Shot参照を更新する。
7. 対象ShotへTakeを通知する。
8. Cut成功時だけProgram Shot名を1回Logする。

無効な番号、参照、Spline、Duration、Curveでは現在のProgramを維持する。Program参照を先に失ってから検証しない。

## 5. TakeとMotion

SwitcherはMotionの時刻やSpline位置を直接変更しない。

- ShotがTakeを受け取る。
- Shotが自身のMotion Playerを開始する。
- Motion Playerが適用済みMotionのIn Pointから進行する。
- Staticは速度0から、Rollingは適用済みMotionが定めた非0速度から開始する。
- Body、Aim、Composition、Lens、Rollは同じPlayback Timeを使用する。

CutとMotion開始の責務を分けるが、ユーザー操作としては1回のCutキーで完結する。

## 6. Programから外れたShot

別ShotへのCutが成功した後、以前のProgram ShotへReleaseを通知する。

- Live状態を解除する。
- Live中の位置を先にResetしない。
- Release後に次回用のIn PointへPrepareする。
- AssetやSceneを保存しない。
- PresetのRuntime状態をAssetへ書き戻さない。

Continuous EntryはPresetの規則に従ってOff-Air再生を継続できる。初期実装とGold Masterでは使用せず、通常Shotを無意味に常時更新しない。

## 7. Cinemachineとの関係

- Program出力は1台のUnity CameraとCinemachine Brainを使用する。
- 各Shotは別々のCinemachineCameraを持つ。
- Unity CameraへTransformやLensを毎フレームコピーしない。
- Live中のCinemachineCameraへ別Shotの位置、Spline、Lens、Targetを上書きしない。
- Motion FoundationはCutだけとし、Blendと映像Crossfadeを含めない。
- Aim、Lens、Rollは各ShotのCinemachine構成へMotion Playerが適用する。

## 8. 同一Shotの選択

現在のProgramと同じShotを選択しても何もしない。Playback Time、方向、Speed Multiplier、Hold、Reverse状態を変更しない。

Retrigger、Restart、Jump To In Pointは必要になった時点で別操作として定義する。

## 9. A/Bとの関係

すべてのShotをA/B Slotへ交互にコピーする方式は使用しない。各Shotが専用CameraとMotion Playerを持ち、SwitcherはそのCameraへ直接Cutする。

A/Bは映像Transitionや外部送出経路を実装する場合の別概念であり、Motion Presetの実体切り替えには使用しない。

## 10. 将来のPreview / Take

将来Previewを追加する場合は、SwitcherにPreview Shotを1つ追加し、次の流れを使用する。

1. Preview Shotを選択する。
2. 必要ならPre-roll Handleを再生する。
3. Takeで同じShotをProgramへ切り替える。

Preview専用のMotion Asset、Camera複製、Rolling専用Playerは作らない。Direct Cutは引き続き利用できる。

## 11. Program確認

Cut成功時のConsole LogとCustom Inspectorの読み取り専用状態で現在のProgramを確認できるようにする。Runtime UIとMultiviewはMotion Foundationへ含めない。

## 12. 受け入れ条件

1. キー番号とRigの順序付きShot Slotが一致する。
2. 各Shotが別々のCinemachineCameraとして明確にCutされる。
3. StaticとRollingが適用済みMotionのIn Pointから正しく開始する。
4. 無効な選択で現在のProgramを失わない。
5. 同一Shot選択でMotion状態がResetされない。
6. Off-AirになったShotだけが次回用にPrepareされる。
7. SwitcherがMotion再生状態を重複所有しない。
8. Preview、Bank、Transition、Multiviewが先行実装されていない。
