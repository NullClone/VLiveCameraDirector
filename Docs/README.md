# VLiveCameraUnit 仕様書

このディレクトリはVLiveCameraUnitの製品方針、初期実装、実装順序を定義する。

初めて読む場合:

1. [overview.md](overview.md) — 製品コンセプトと最初に作る体験
2. [architecture.md](architecture.md) — 初期版の最小構成
3. [phases.md](phases.md) — 現在地と次の実装

機能別仕様:

- [spec-operation.md](spec-operation.md) — キーボード操作と将来のMIDI
- [spec-camera.md](spec-camera.md) — Fixed / Spline Shot
- [spec-assistance.md](spec-assistance.md) — Cinemachineによる初期支援
- [spec-switching.md](spec-switching.md) — Direct Cutと将来のPreview

開発手順:

- [workflow.md](workflow.md) — Antigravityへの実装委譲、Review、検証
- [next-agent-task.md](next-agent-task.md) — 次の作業エージェントへ渡す現在のプロンプト
- [AGENTS.md](../AGENTS.md) — コードスタイルと過剰設計防止規則

実装エージェントは全仕様書を読む必要はない。`AGENTS.md`、今回の担当仕様書1つ、短い実装タスクだけを使用する。

将来構想は現在の実装要求ではない。実装済みの範囲は必ず[phases.md](phases.md)で確認する。
