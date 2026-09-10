# 操作仕様

## 1. 目的

この文書は、複数Shotをキーボードだけで切り替え、現在の移動Shotへ必要な介入を行う初期操作を定義する。

## 2. 初期操作

| 操作 | 対象 | 動作 |
| --- | --- | --- |
| 設定済みCutキー | 指定番号のShot | 対応する専用CinemachineCameraへCutする |
| Speed Up / Down | 現在の移動Shot | Spline進行速度を増減する |
| Reverse | 現在の移動Shot | 現在位置を保って進行方向を反転する |
| Hold | 現在の移動Shot | 現在位置でSpline進行を停止する |
| Resume | 現在の移動Shot | 現在の方向と速度で再開する |

初期Paletteではキー1〜6を使用する。既定のCutキーは1〜9とし、Inspectorから可変長のキー一覧を確認、変更できる。

## 3. 基本フロー

1. Rig InspectorのShot Slot順をShot番号とする。
2. オペレーターが数字キーを押す。
3. Switcherが対応するShotへCutする。
4. 移動Shotなら始点から自動再生する。
5. 必要なときだけSpeed、Reverse、Hold、Resumeを操作する。
6. 別ShotへCutすると、前の移動ShotはOff Airになってから次回用に準備される。

切り替えだけで動きが成立することを基本とし、手動操作を必須にしない。

## 4. 入力の責務

- キー入力を読むのは`VLiveCameraKeyboardInput`だけとする。
- InputはSwitcherの公開操作だけを呼ぶ。
- ShotやCinemachineCameraの内部でキー入力を読まない。
- Shot番号はRigの順序付きShot Slotと一致させる。
- 同一フレームの複数Cut入力は小さい番号を優先するなど、結果を決定的にする。
- キー割り当てはInspectorから確認・変更できる。
- Cut判定は固定値9ではなく、キー一覧とShot Slot数の範囲で行う。
- Numpad 1〜9は対応するShot番号への補助入力として維持できる。
- キー重複と、Speed、Reverse、Hold、Resumeとの競合をInspectorで警告する。

QWERTY、記号キー、テンキーをShot数に応じて自動割り当てしない。9を超えるShotへキーを割り当てる場合はユーザーが明示的に設定する。既存の割り当てを自動補完処理で上書きしない。

汎用Input Mapping Asset、Command Bus、MIDI Adapterはまだ作らない。

## 5. 失敗時

- 存在しないShot番号は無視し、現在のProgramを維持する。
- 同一Shotの再選択では再生位置をResetしない。
- Fixed Shotへの移動操作は無視する。
- 参照欠落時も現在のProgramを維持する。
- 警告を毎フレーム出さない。

## 6. 次の操作

初期PaletteがユーザーのSceneで成立した後、必要性を確認して次を検討する。

1. Pan、Tilt、Zoomのライブトリム
2. PreviewとTake
3. Camera Bank
4. MIDI

## 7. 受け入れ条件

1. キー1〜6で初期Shotを直接Cutできる。
2. 移動ShotはCut後に自動再生される。
3. Speed、Reverse、Hold、Resumeは現在の移動Shotだけへ作用する。
4. 同一Shot選択と無効番号で状態が壊れない。
5. キーボード以外の入力基盤が先行実装されていない。
6. 9固定のループに依存せず、設定されたキー数の範囲でCutできる。
7. キー競合がInspectorで分かり、暗黙のQWERTY割り当てがない。
