# 操作とスイッチング仕様

## 1. 目的

複数の独立したCinemachineCameraをKeyboardから直接Cutし、Motion PresetのEntry状態から再生する操作を定義する。

切り替えだけでPresetの意図した映像が成立することを基本とし、手動介入を必須にしない。

## 2. 状態所有

Switcherが持つ状態は次だけとする。

- 使用する`VLiveCameraRig`参照
- 現在のProgram Shot

Program CameraとShot Slot順はRigが所有し、Motion再生状態はMotion Playerが所有する。SwitcherへCamera、Brain、Preset一覧、Shot一覧、Playback Timeを重複保存しない。

Preview、Selected、Transitioning、Tally、Bankは現在追加しない。

## 3. Shot番号と準備

Rigの順序付きShot SlotをShot番号の唯一の正本とする。無効なSlotがあっても後続番号を詰めない。

Off-AirのShotは適用済みMotionのEntry Modeに従って準備する。

| Entry Mode | Off-Air準備 |
| --- | --- |
| `Static` | In Pointの位置、Aim、Lensへ置き、速度0で待機する |
| `Rolling` | In Pointの状態へ置き、Take後に非0速度から継続できるようにする |
| `Continuous` | Off-Air中も進行する。現在のPaletteでは使用しない |

PrepareはOff-Air中だけ実行する。Live中のShotをIn Pointへ戻したり、別Presetで上書きしたりしない。

## 4. Direct Cut

1. 入力からShot番号を受け取る。
2. Rigの同じ番号のSlotから対象Shotを取得する。
3. Shot、Motion Player、専用CinemachineCamera、適用済みMotionを検証する。
4. 現在と異なるCinemachineCameraをProgramにする。
5. Cinemachine BrainのCutとして切り替える。
6. Cut成功後にProgram Shot参照を更新する。
7. 対象ShotへTakeを通知する。
8. 以前のProgram ShotへReleaseを通知し、Off-Airになってから次回用にPrepareする。

無効な対象では現在のProgramを維持する。Program参照を失ってから検証しない。Cut成功時だけProgram Shot名を1回Logできる。

Direct CutはPreview選択やTakeの二段階操作を要求しない。

## 5. TakeとRelease

Takeを受けたShotが自身のMotion Playerを開始する。SwitcherはMotion時刻、Spline位置、Aim、Lensを直接変更しない。

- StaticはIn Pointの速度0から開始する。
- RollingはIn Pointの非0速度から継続する。
- Body、Aim、Composition、Lens、Rollは同じPlayback Timeを使用する。

別ShotへのCutが成功した後だけ、以前のShotをReleaseする。

- Live状態を解除する。
- Live中の位置を先にResetしない。
- Release後に次回用のIn PointへPrepareする。
- AssetやSceneへRuntime状態を書き戻さない。

## 6. 操作

| 操作 | 対象 | 動作 |
| --- | --- | --- |
| 設定済みCutキー | 指定番号のShot | 対応する専用CinemachineCameraへ直接Cutする |
| Speed Up / Down | 現在のMotion Shot | Target Speed Multiplierを増減する |
| Reverse | 現在のMotion Shot | 減速、停止後に進行方向を反転する |
| Hold | 現在のMotion Shot | 設定された減速で停止する |
| Resume | 現在のMotion Shot | 現在位置から設定された加速で再開する |
| Freeze | 現在のMotion Shot | 緊急時に即時停止する。既定キーは割り当てない |

### Speed

SpeedはPresetのDurationやCurveを変更せず、現在のPlayerのTarget Speed Multiplierだけを変える。Rig Profileの応答時間で追従し、Camera位置を飛ばさない。InspectorではCurrentとTargetを区別して表示する。

### Hold、Resume、Freeze

HoldはSpeedを0へ連続的に収束させる。ResumeはHold前の方向とTarget Speedへ現在位置から復帰する。

Freezeは速度不連続を許容する緊急停止であり、通常操作として使用しない。必要なユーザーだけがキーを明示設定する。

### Reverse

Reverseは速度符号を瞬時に反転しない。

1. 現在方向の速度を0へ減速する。
2. 停止した位置を反転点とする。
3. 逆方向へ加速する。

遷移中の再入力で方向がフレームごとに振動しない決定的な規則を持つ。

### Fixed Shot

Fixed Shotは位置、Aim、Composition、Lensを維持する。Motion操作は安全に無視し、Target Group追従、Rotation Composer、Group Framingによる構図維持は継続できる。

## 7. Keyboard Input

- 入力を読むのは`VLiveCameraKeyboardInput`だけとする。
- InputはSwitcherの公開操作だけを呼び、ShotやMotion Playerを直接参照しない。
- Shot、Motion Player、CinemachineCameraの内部でキー入力を読まない。
- Cut判定は9固定ではなく、キー一覧とShot Slot数の範囲で行う。
- 既定のCutキーは1から9とし、Inspectorから可変長で変更できる。
- Numpad 1から9は対応番号への補助入力として使用できる。
- 9を超えるShotのキーはユーザーが明示設定する。
- 既存割り当てを自動補完で上書きしない。
- キー競合をInspectorで警告する。
- 同一フレームの複数Cut入力は小さい番号を優先するなど、結果を決定的にする。

汎用Input Mapping Asset、Command Bus、MIDI Adapterは現在作らない。

## 8. Cinemachineとの関係

- Program出力は1台のUnity CameraとCinemachine Brainを使用する。
- 各Shotは別々のCinemachineCameraを持つ。
- Unity CameraへTransformやLensを毎フレームコピーしない。
- Live中のCameraへ別Shotの位置、Spline、Lens、Target Groupを上書きしない。
- 現在はCutだけを扱い、Blendと映像Crossfadeを含めない。

詳細な所有境界は[architecture.md](architecture.md)を正本とする。

## 9. 同一ShotとA/B

現在のProgramと同じShotを選択しても何もしない。Playback Time、方向、Speed、Hold、Reverseを変更しない。RetriggerやRestartは必要になった時点で別操作として定義する。

すべてのShotをA/Bへ交互にコピーする方式は使用しない。各Shotが専用CameraとMotion Playerを持ち、そのCameraへ直接Cutする。A/Bは将来の映像Transitionや外部送出経路の概念であり、Motion Presetの実体切り替えには使用しない。

## 10. 将来のPreview / Take

Previewを追加する場合も同じShot、Preset、Motion Playerを使用する。

1. Preview Shotを選択する。
2. 必要ならPre-roll Handleを再生する。
3. Takeで同じShotをProgramへ切り替える。

Preview専用Motion Asset、Camera複製、Rolling専用Playerを作らない。Direct Cutは維持する。

## 11. 失敗時

- 存在しない番号、無効な参照、Spline、Duration、Curveでは現在のProgramを維持する。
- 同一Shotの再選択では状態をResetしない。
- Fixed ShotへのMotion操作は無視する。
- NaNまたはInfinityの入力を適用しない。
- 警告を毎フレーム出さない。
- 無効なCut後に以前のShotをReleaseしない。

## 12. 不変条件

1. キー番号とRigのShot Slot順が一致する。
2. 各Shotが別々のCinemachineCameraとしてCutされる。
3. StaticとRollingが適用済みMotionのIn Pointから開始する。
4. Speed、Hold、Resume、ReverseでMotionの同期を失わない。
5. 無効な選択と同一Shot選択で状態を壊さない。
6. Off-AirになったShotだけを次回用にPrepareする。
7. SwitcherとInputがMotion再生状態を所有しない。
8. Keyboard以外の入力基盤、Preview、Bank、Transitionを先行実装しない。
