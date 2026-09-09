# アーキテクチャ仕様

## 1. 目的

この文書は、VLiveCameraUnit の責務、データの流れ、依存方向、所有権を定義する。クラス名は設計上の役割を示す仮称を含み、実装開始時に既存アセットとの互換性を調査して確定する。

## 2. 基本構成

```text
Keyboard / Mouse / MIDI
          |
     Input Adapter
          |
   Operation Commands <--- UI / Timeline / Future Cue Assist
          |
    Control Router
       /       \
Shot Selection  Live Control Channels
       \       /
   Camera State Coordinator
          |
 Pattern Player + Assistance + Manual Trim
          |
 Cinemachine 3 Camera Rigs
          |
 Program / Preview Brains and Transition Output
```

入力デバイス、ショット選択、運動計算、Cinemachine、送出状態を分離する。デバイス追加や半自動化によって、カメラロジックそのものを分岐させない。

## 3. 論理モジュール

| モジュール | 責務 | 持たない責務 |
| --- | --- | --- |
| Input Adapter | 物理入力を共通操作コマンドへ変換 | カメラ選択、Transform 更新 |
| Operation Command Bus | フレーム内の操作を順序付きで伝達 | デバイス固有状態 |
| Camera Registry / Bank | ショット ID、Bank、表示順、可用性の管理 | Program の確定 |
| Switcher State | Program、Preview、Tally、Take 状態の唯一の所有 | カメラ運動の生成 |
| Pattern Player | Entry / Main / Exit、レーン進行、時間同期 | 入力デバイス判定 |
| Assistance Solver | 追従、構図、制限、補間の支援値を生成 | 無断で Program を切り替える判断 |
| Manual Trim Mixer | 操作チャンネルごとに人の介入を合成 | ベースパターンの破壊的変更 |
| Cinemachine Rig Adapter | 論理状態を Cinemachine 3 へ適用 | ショット選択 UI |
| Transition Output | Cinemachine Blend と映像遷移を実行 | Camera Bank 管理 |
| Operator UI | Multiview、状態、警告、入力可視化 | Runtime 状態の重複所有 |

## 4. 状態の所有権

### 4.1 一つの状態に一人の所有者

- Program / Preview / Tally は `Switcher State` だけが確定する。
- パターンの再生位置は `Pattern Player` だけが進める。
- 手動入力の保持値は `Manual Trim Mixer` だけが管理する。
- 最終的な論理 Camera State は `Camera State Coordinator` が 1 回だけ合成する。
- Cinemachine への書き込みは `Cinemachine Rig Adapter` に集約する。

同じ Transform、Lens、Spline Position へ複数コンポーネントが独立に書き込む構成は禁止する。

### 4.2 フレーム更新順序

標準順序は次のとおり。

1. 入力を読み、操作コマンドへ変換する。
2. Bank 選択、Preview、Take などの離散状態を更新する。
3. 使用する時間源を決定し、パターン再生位置を更新する。
4. ターゲットと構図支援を評価する。
5. 手動トリムと支援値をチャンネル単位で合成する。
6. Cinemachine 3 のリグ状態へ反映する。
7. Program / Preview の遷移と Tally を確定する。
8. UI と診断情報へ読み取り専用スナップショットを公開する。

具体的な Unity の更新フェーズは、Cinemachine Brain と入力システムの評価順を実機検証して決める。

## 5. Cinemachine 3 構成

- ショットの基本単位は `CinemachineCamera` とする。
- レーン移動には `Spline Dolly` 系コンポーネントを使用する。
- 注視・構図には Tracking Target と Position / Rotation Composer 系を使用する。
- 手持ち感や衝撃は Noise / Impulse 系を用途別に使用する。
- Program は原則として 1 台の Unity Camera と Cinemachine Brain から出力する。
- Preview は Program と独立した Brain または Output Channel で評価し、Program 状態を変更しない。
- Cinemachine Blend は仮想カメラ状態の補間であり、RenderTexture を用いた映像クロスフェード、フェード、ワイプとは別機能として扱う。

Cinemachine の詳細 API は Unity 6.3 対応バージョンを固定した時点で確定し、旧 Cinemachine 2 API を新規設計へ持ち込まない。

## 6. Program / Preview / Standby の評価

| 状態 | 用途 | 評価方針 |
| --- | --- | --- |
| Program | 現在の送出 | 完全なカメラ、支援、ポスト処理を評価 |
| Preview | 次の候補確認 | Take と同等の構図・タイミングを事前評価 |
| Standby | Bank 内のその他 | 必要最小限の状態保持。高コスト描画は行わない |
| Multiview | 一覧監視 | 低解像度、低頻度など品質を段階化 |

論理上の登録台数と同時に高品質描画する台数を分離する。64 台以上の登録を目標にするが、同時描画性能は実ステージで測定して上限を決める。

## 7. 時間モデル

パターンは次の時間源を選択できる。

- Shot Local Time: Take または選択からの経過時間
- Show Time: ライブ全体で共有する時間
- Beat Time: BPM と拍位置に同期した時間
- External Time: Timeline または将来のキューシステム

時間源の切り替え、Seek、Pause、再開、フレーム落ちで飛躍が起きた場合の方針を明示する。乱数を使うパターンは Seed を保存し、Preview と Program で再現可能にする。

## 8. 入力抽象化

すべての入力は [spec-operation.md](spec-operation.md) の操作コマンドへ変換する。Keyboard Adapter と将来の MIDI Adapter は同時利用でき、同じコマンドに対する優先順位と合成規則を `Control Router` が決める。

絶対値フェーダーは Pickup、相対エンコーダーは差分入力として扱い、デバイス接続時や Bank 切り替え時の値飛びを防ぐ。

## 9. データと Runtime の境界

- Camera Shot、Motion Pattern、Transition Preset、Input Mapping は ScriptableObject 等のシリアライズ可能なデータとする。
- Runtime はデータを解釈する少数の共通プリミティブで構成する。
- AI 生成物も手作業の生成物も同じスキーマ、検証、プレビューを通す。
- Scene 固有参照と再利用可能なパターンを分離する。パターンアセットが Scene オブジェクトを直接恒久参照しない。
- 保存形式の変更では GUID、SerializedProperty 名、移行処理を検討し、既存 Prefab と Scene を無断で破壊しない。

## 10. namespace と assembly

目標構成は次のとおり。

```text
Runtime/  -> toshi.VLiveKit.Camera
Editor/   -> toshi.VLiveKit.Camera.Editor
Tests/    -> toshi.VLiveKit.Camera.Tests
```

必要に応じて Runtime、Editor、Tests の asmdef を分離し、Runtime から Editor API を参照しない。外部公開 API は最小化し、内部実装は `internal` を優先する。

## 11. エラーと診断

ターゲット消失、無効なレーン、未ロードのショット、遷移不能、入力競合は、例外で送出を停止させる前に安全な Hold と明確な状態表示へ移行する。エラー時の具体的な画の扱いは各機能仕様で定義する。

診断スナップショットには少なくとも Program、Preview、現在 Bank、パターン位相、時間源、手動介入中のチャンネル、Pickup 待ち、遷移状態、警告理由を含める。
