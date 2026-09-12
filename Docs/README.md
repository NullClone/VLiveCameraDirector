# VLive Camera Director Documentation

このディレクトリは、VLive Camera Directorの製品判断、技術契約、現在の開発範囲を定義する。

## 読む順番

初めて読む場合は次の順とする。

1. [product.md](product.md) — 製品コンセプト、原則、対象範囲
2. [architecture.md](architecture.md) — 責務、状態所有権、Cinemachine統合
3. [roadmap.md](roadmap.md) — 現在地、次、将来

## 機能仕様

| 関心 | 正本 |
| --- | --- |
| Motion Preset、Spline、Timing、Aim、Lens、Roll、支援 | [motion.md](motion.md) |
| Direct Cut、Shot lifecycle、Keyboard、手動介入 | [operation.md](operation.md) |
| Rig作成、Inspector、Apply、Rebuild、Preset保存 | [authoring.md](authoring.md) |

## 開発規則

| 関心 | 正本 |
| --- | --- |
| C#、Tooltip、コメント、Custom Inspector | [code-style.md](code-style.md) |
| エージェント委譲、確認、Review、Git | [workflow.md](workflow.md) |
| エージェント向け入口と変更境界 | [AGENTS.md](../AGENTS.md) |

## 文書所有の規則

- 製品として何を目指すかは`product.md`だけが所有する。
- 状態の正本、依存方向、Cinemachineとの境界は`architecture.md`だけが所有する。
- 数式、データ契約、機能固有の異常時動作は該当する機能仕様が所有する。
- 実装済み・次・将来の区別は`roadmap.md`だけが所有する。
- 完了済み作業の履歴はGitとCHANGELOGが所有し、RoadmapへStep履歴を蓄積しない。
- コードコメントは局所的な理由、仕様書は複数ファイルにまたがる契約、Commit messageは変更理由を所有する。
- 同じ規則を複数文書へコピーせず、正本へリンクする。

実装エージェントは全仕様書を読む必要はない。`AGENTS.md`、担当仕様書、C#またはEditorを触る場合の`code-style.md`、チャットで渡された実装タスク、該当するAgent Skillだけを読む。実装用プロンプトは`Docs/`へ保存しない。
