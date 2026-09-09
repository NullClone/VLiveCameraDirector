# 実装順序と現在地

## 1. この文書の役割

この文書は、現在実装との差分と、次に実装する1段階を管理する。将来フェーズの詳細設計は先に行わず、直前の段階で得られた結果を見て更新する。

## 2. 現在実装のスナップショット

2026-09-09の確認時点では次の状態である。

- `package.json`はUnity 2022.3、Cinemachine 2.9.7を指定している。
- RuntimeではCinemachine 2の`CinemachineVirtualCamera`などを使用している。
- namespaceは`toshi.VLiveKit.Photography`、`toshi.VLiveKit`、`toshi.VLiveKit.VLiveCameraUnit`、global namespaceが混在している。
- 既存`VLiveCamera`にはLook / Follow、Dolly、Lens、Noise、Presetなどの実装がある。
- 既存`VLiveCameraSwitcher`はUnity CameraへTransformとLensを毎フレーム転記する。
- 数字キーによる直接選択は最大9Shotである。
- ランダムAuto Cutが既定で有効である。
- Preview、Take、Bank、MIDIは未実装である。

旧実装との互換性は保持しない。有用な挙動は参考にできるが、旧型、旧SerializedField、旧Prefabを維持するためのコードは追加しない。

## 3. Step 0 — 仕様の簡素化

状態: 完了。コード変更とUnity実機確認は未実施。

実施内容:

- 初期版と将来構想を分離
- 最小3型のアーキテクチャへ縮小
- 旧版互換を不要とする方針を明記
- C#コードスタイルと過剰設計防止規則を追加
- Antigravity向けの実装委譲手順を追加

## 4. Step 1 — 最小動作版

### 目的

Unity 6.3とCinemachine 3で、複数ShotをキーボードCutし、移動Shotを手動調整できる状態を作る。

### 実装範囲

- package設定をUnity 6.3 / Cinemachine 3へ更新
- Runtime namespaceを`toshi.VLiveKit.Camera`へ統一
- `VLiveCameraShot`
- `VLiveCameraSwitcher`
- `VLiveCameraKeyboardInput`
- 1台のProgram CameraとCinemachine Brain
- Fixed Shot
- Spline Shot
- 数字キーCut
- Speed、Reverse、Hold、Resume
- Cut成功時のProgram Shot名ログ
- 動作確認用SceneまたはPrefab

### 対象外

- 旧API、Prefab、Sceneとの互換処理
- Preview / Take / Tally
- Camera Bank
- Motion Pattern Asset
- MIDI
- AI
- Recommendation
- Video Transition
- Multiview
- Command Bus、独自Solver、DI、汎用Adapter階層

### 完了条件

1. Unity 6.3とCinemachine 3でImportとCompileが成功する。
2. 3台以上のFixed / Spline Shotを数字キーでCutできる。
3. Spline ShotはCut後も自動で動き続ける。
4. Speed、Reverse、Hold、Resumeで位置が飛ばない。
5. 同一Shotの再選択で再生位置がResetされない。
6. 無効な番号や参照欠落でProgramを失わない。
7. Game Viewで切り替えと動きを目視確認する。
8. 30分の反復操作で例外と入力残留がない。

## 5. Step 2 — 現場向け手動スイッチング

Step 1の操作感をユーザーが確認してから、実装範囲を確定する。

候補:

- Preview / Take
- Program / Preview Tally
- Camera Bank
- Cinemachine Blend
- Pan、Tilt、Zoomなどのライブ調整
- 操作状態のConsole

この段階でも、MIDIと半自動化は実装しない。Step 1で不足した操作だけを仕様へ追加する。

## 6. Step 3 — 再利用とMIDI

Step 2で複数Shotを制作し、設定複製や入力差し替えの実害が確認できてから着手する。

候補:

- 再利用可能なMotion Pattern Asset
- Entry / Main / Exitまたは必要になった再生規則
- MIDI Input
- MIDI Mapping
- Soft Takeover / Pickup
- Tally LED Feedback

キーボード操作を維持し、MIDIを必須にしない。

## 7. 将来 — 制作支援と半自動化

次は方向性だけを保持し、現時点で実装構造を決めない。

- AIによるPattern制作支援
- Timeline / BPM Cue
- 次Shot候補の推薦
- 明示的に許可された区間での半自動Take
- Multiviewと大規模Patternライブラリ

手動運用で得られたShot、操作履歴、失敗例を基に、必要な段階で設計する。

## 8. 実装順序のルール

- 実装エージェントへはStep全体ではなく、さらに小さな1タスクを渡す。
- 現在のStepがGame Viewで成立するまで次へ進まない。
- 将来機能のための空コードや拡張ポイントを作らない。
- 実装中に必要性が判明した仕様だけを、ユーザー承認後に追加する。
- 旧版互換のために実装を複雑化しない。
- 削除対象はタスクごとに具体的なパスを指定する。
