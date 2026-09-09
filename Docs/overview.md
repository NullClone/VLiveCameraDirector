# 製品概要

## 1. コンセプト

VLiveCameraUnit は、事前に配置した多数のカメラをライブ中に演奏するように切り替え、Cinemachineによる追従・構図・補間でオペレーターを支援するUnity完結型のライブカメラシステムである。

カメラを選択した時点で、そのカメラ固有の動きが始まり、操作しなくてもライブ映像として意図のある画が続く。オペレーターは必要な場面だけ速度、方向、Hold、画角などへ介入する。

## 2. 最初に実現する体験

最初の完成目標は次の操作である。

1. 1台のUnity CameraとCinemachine Brainを用意する。
2. AとBに、別々のCinemachineCameraを割り当てる。
3. Aは正面ミドル等の安定した固定Shotとする。
4. BはSpline上を始点から終点まで移動し、終点でHoldする。
5. キーでA/BをCutし、オペレーターはBの速度変更、Reverse、Hold、Resumeを行える。

同じCinemachineCameraへA/Bの設定を上書きしない。AがLiveの間にBを始点へ準備し、BがLiveの間はAを安全な戻り先として維持する。この体験が実際に気持ちよく動くまでは、Preview、MIDI、AI、推薦、汎用Pattern基盤を実装しない。

## 3. 製品原則

### 3.1 切り替えだけで成立する

各Shotは、選ばれた後に操作がなくても意図した動きまたは固定画を維持する。無目的に揺れ続けることを要求しない。

### 3.2 操作すると深化する

手動操作は生のTransformを制御することではない。オペレーターはタイミングや強さを決め、追従、構図、補間はCinemachineへ任せる。

### 3.3 現在必要なものだけを作る

最初から半自動化に適した汎用基盤を作らない。キーボード運用で実際に必要になった共通点だけを、後のフェーズで抽出する。

### 3.4 旧版互換より完成度を優先する

開発中は旧バージョンとの互換性を保持しない。既存コードから有用な挙動は学ぶが、新仕様に不要な構造は引き継がない。

## 4. 対象環境

- Unity 6.3以上
- Cinemachine 3
- 初期入力はキーボードとマウス
- 将来入力としてMIDIを検討する

## 5. 現在の対象

- 独立したCinemachineCameraを持つA/Bの2 Shot
- Aは固定の安定枠、BはSpline移動枠
- 1台のProgram CameraとCinemachine Brain
- キーボードによるA/Bの直接Cut
- 移動速度、Reverse、Hold、Resume
- Bの始点準備、終点への収束、終点Hold
- 切り替え時に不自然な停止や値飛びがないこと
- Cut成功時にProgram Shot名をConsoleへ1回表示

## 6. 次の対象

最初の体験が成立した後、必要性を確認しながら次を追加する。

- Program / Preview / Take / Tally
- Camera Bank
- Pan、Tilt、Zoomなどのライブ調整
- 再利用可能なMotion Pattern
- MIDI入力とSoft Takeover

## 7. 将来構想

以下は製品の方向性であり、現在の実装要求ではない。

- AIによるCamera Patternの制作支援
- 楽曲やTimelineに同期したCue
- 次Shotの推薦
- 明示的に許可された範囲での半自動Take
- 大規模なPatternライブラリとMultiview

将来構想のために、現在の実装へ空のinterface、未使用設定、拡張ポイントを先に追加しない。

## 8. 最初の成功条件

1. A/Bが別々のCinemachineCameraとして構成されている。
2. AからB、BからAへ明確なCutとして安定して切り替えられる。
3. Bは始点から滑らかに動き、終点で収束してHoldする。
4. Forward、Reverse、Hold、Resumeで位置が飛ばない。
5. Live中のCinemachineCameraへ別Shot設定を上書きしていない。
6. 入力がない状態でもA/Bそれぞれの構図が成立する。
7. 30分の反復操作で例外、入力残留、状態破損が起きない。
