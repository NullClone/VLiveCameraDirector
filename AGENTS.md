# VLiveCameraUnit 開発ガイド

## 製品の目的

VLiveCameraUnit は、事前設計した多数のカメラリグ、レーン、カメラワークパターンを演奏するように操作し、Cinemachine による追従・構図・補間がオペレーターを支援する Unity 完結型のライブカメラシステムである。

初期目標は「手動運用の最高峰」。切り替えだけでも意図のある画が成立し、必要な瞬間には人が距離、速度、向き、レンズ、構図へ自然に介入できることを優先する。将来の半自動化は、この手動運用と同じ操作・ショット・状態モデルの上へ段階的に追加する。

## 仕様の優先順位

判断が衝突した場合は、次の順に優先する。

1. その作業に対するユーザーの明示指示
2. このファイルの開発ルールと文書案内
3. 各仕様書の担当範囲
4. 現在のコードとアセットの実際の挙動

`Docs/` は目標仕様を定義する。現在実装との差分は [Docs/phases.md](Docs/phases.md) に集約し、未実装の目標を実装済みとして扱わない。

## 文書の読み分け

| 知りたい内容 | 正本 |
| --- | --- |
| 製品コンセプト、原則、対象範囲 | [Docs/overview.md](Docs/overview.md) |
| システム境界、責務、依存方向 | [Docs/architecture.md](Docs/architecture.md) |
| キーボード操作、MIDI 共通操作、手動介入 | [Docs/spec-operation.md](Docs/spec-operation.md) |
| レーン、リグ、カメラワークパターン | [Docs/spec-camera.md](Docs/spec-camera.md) |
| 構図支援、段階的な半自動化 | [Docs/spec-assistance.md](Docs/spec-assistance.md) |
| Program / Preview、Take、遷移、Tally | [Docs/spec-switching.md](Docs/spec-switching.md) |
| 現状との差分、実装順序、完了条件 | [Docs/phases.md](Docs/phases.md) |
| 変更、検証、Git の手順 | [Docs/workflow.md](Docs/workflow.md) |

同じ規則を複数文書に複製しない。変更時は正本を更新し、他文書からリンクする。

## 技術方針

- 対象は Unity 6.3 以上、Cinemachine 3 とする。
- Runtime のルート namespace は `toshi.VLiveKit.Camera`、Editor は `toshi.VLiveKit.Camera.Editor` とする。
- ファイル名と主要型名を一致させ、原則として 1 ファイル 1 主要型とする。
- 型と公開メンバーは `PascalCase`、private フィールドは `_camelCase` とする。
- ショットごとの挙動は個別 C# ではなく、検証可能な共通プリミティブとデータアセットで表現する。
- 入力デバイスをカメラロジックへ直結しない。キーボードと将来の MIDI は、同じ操作コマンドへ変換する。
- Program、Preview、Standby の状態所有者を一つにし、Transform やレンズへ複数系統から直接書き込まない。
- Unity の参照を保つ必要がある移動・改名では `.meta` と GUID を維持する。

## 変更の原則

- 仕様変更はユーザーの承認を得てから行う。実装都合だけで製品仕様を変えない。
- 調査、設計、実装、Unity 上の見た目確認、負荷測定を別の証拠として報告する。
- 無関係な既存変更は保持し、許可された範囲だけを編集・ステージする。
- コード内コメントは局所的な理由や制約に使い、製品仕様は `Docs/` に置く。
- 作業開始前と完了前に [Docs/workflow.md](Docs/workflow.md) を確認する。
