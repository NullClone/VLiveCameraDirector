# 操作仕様

## 1. 目的

この文書は、複数Shotをキーボードだけで切り替え、現在のMotionへ必要な介入を行う操作を定義する。

切り替えだけでPresetの意図した映像が成立することを基本とし、手動操作を必須にしない。

## 2. 操作

| 操作 | 対象 | 動作 |
| --- | --- | --- |
| 設定済みCutキー | 指定番号のShot | 対応する専用CinemachineCameraへ直接Cutする |
| Speed Up / Down | 現在のMotion Shot | Target Speed Multiplierを増減する |
| Reverse | 現在のMotion Shot | 減速、停止後に進行方向を反転する |
| Hold | 現在のMotion Shot | 設定された減速で停止する |
| Resume | 現在のMotion Shot | 現在位置から設定された加速で再開する |
| Freeze | 現在のMotion Shot | 緊急時に即時停止する。既定キーは割り当てない |

初期Paletteではキー1〜6を使用する。既定のCutキーは1〜9とし、Inspectorから可変長のキー一覧を確認、変更できる。

## 3. Direct Cutフロー

1. Rig InspectorのShot Slot順をShot番号とする。
2. Off-AirのShotは適用済みMotionのIn Pointへ準備されている。
3. オペレーターがCutキーを押す。
4. Switcherが対応するShotへ即時Cutする。
5. ShotがMotion PlayerへTakeを通知する。
6. Static Entryは速度0から、Rolling EntryはIn Pointの非0速度から進行する。
7. 必要なときだけSpeed、Reverse、Hold、Resumeを操作する。
8. 別ShotへCutすると、前のShotはOff-Airになってから次回用に準備される。

直接CutではPreview選択やTakeの二段階操作を要求しない。将来Preview / Takeを追加しても、同じShot、Preset、Motion Playerを使用する。

## 4. Speed

Speed Up / DownはPresetのDurationやCurveを変更せず、現在のMotion PlayerのTarget Speed Multiplierだけを変更する。

- 現在値からTarget値へRig Profileの応答時間で追従する。
- 位置、Aim、Lens、Rollは同じPlayback Timeを使用し、同期を失わない。
- 倍率変更でCamera位置を飛ばさない。
- Inspectorに現在値とTarget値を区別して表示する。
- 許容範囲外の入力はPresetまたはRig Profileの範囲へ制限する。

## 5. Hold、Resume、Freeze

### Hold

HoldはCameraを現在フレームで瞬間停止させる操作ではない。Rig Profileの停止応答に従ってSpeed Multiplierを0へ収束させる。停止までにわずかに進行できる。

### Resume

ResumeはHold開始前の方向とTarget Speed Multiplierへ連続的に復帰する。再開時に位置、Aim、LensをResetしない。

### Freeze

安全上すぐ停止する必要がある場合だけFreezeを使用する。Freezeは即時停止による速度不連続を許容する緊急操作であり、通常の演出操作として使用しない。

Freezeの既定キーは設定しない。必要なユーザーだけがInspectorから明示的に割り当てる。

## 6. Reverse

Reverseは現在位置を保ったまま方向の符号を瞬時に反転しない。

1. 現在方向の速度を0へ減速する。
2. 停止した位置を反転点とする。
3. 逆方向へ設定された応答で加速する。

Reverse中にHoldされた場合はその場で停止へ移行する。Reverseを再入力した場合の再反転は、現在の遷移を安全に完了または取消できる一意な規則にし、フレームごとに方向が振動しないようにする。

## 7. Fixed Shot

Fixed Shotは設定された位置、Aim、Composition、Lensを維持する。

- Speed、Reverse、Hold、Resumeは安全に無視する。
- Freezeは状態を変更しない。
- Target追従とRotation ComposerによるAimは継続できる。

## 8. 入力の責務

- キー入力を読むのは`VLiveCameraKeyboardInput`だけとする。
- InputはSwitcherの公開操作だけを呼び、Motion Playerを直接参照しない。
- Shot、Motion Player、CinemachineCameraの内部でキー入力を読まない。
- Shot番号はRigの順序付きShot Slotと一致させる。
- 同一フレームの複数Cut入力は小さい番号を優先するなど、結果を決定的にする。
- キー割り当てはInspectorから確認、変更できる。
- Cut判定は固定値9ではなく、キー一覧とShot Slot数の範囲で行う。
- Numpad 1〜9は対応するShot番号への補助入力として維持できる。
- Cut、Speed、Reverse、Hold、Resume、Freezeのキー競合をInspectorで警告する。

QWERTY、記号キー、テンキーをShot数に応じて自動割り当てしない。9を超えるShotへキーを割り当てる場合はユーザーが明示的に設定する。既存割り当てを自動補完処理で上書きしない。

汎用Input Mapping Asset、Command Bus、MIDI Adapterはまだ作らない。

## 9. 手動Trimの境界

現在のKeyboard操作はPlayback Timeだけを変更する。Pan、Tilt、Screen Position、ZoomのライブトリムはMotion Foundationの対象外とする。

将来のライブトリムはShotの適用済みMotion評価結果へ一時的なOffsetとして合成し、Preset Assetを書き換えない。MIDIもKeyboardと同じ公開操作を使用する。

## 10. 失敗時

- 存在しないShot番号は無視し、現在のProgramを維持する。
- 同一Shotの再選択では再生位置をResetしない。
- Fixed ShotへのMotion操作は無視する。
- Target、Spline、Duration、Curveの参照欠落時も現在のProgramを維持する。
- 警告を毎フレーム出さない。
- 入力値がNaNまたはInfinityの場合は適用しない。

## 11. 受け入れ条件

1. キー1〜6で初期Shotを直接Cutできる。
2. StaticとRollingがそれぞれのIn Pointから開始する。
3. Speedは現在のMotion Shotだけへ作用し、Body、Aim、Lensの同期を失わない。
4. HoldとResumeが連続的に停止、再開する。
5. Reverseが減速、停止、逆方向への加速として動作する。
6. Freezeと通常のHoldが区別される。
7. 同一Shot選択と無効番号で状態が壊れない。
8. 9固定のループに依存せず、設定されたキー数の範囲でCutできる。
9. Keyboard以外の入力基盤が先行実装されていない。
