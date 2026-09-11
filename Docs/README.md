# VLiveCameraUnit 仕様書

このディレクトリはVLiveCameraUnitの製品方針、初期実装、実装順序を定義する。

初めて読む場合:

1. [overview.md](overview.md) — 製品コンセプトと最初に作る体験
2. [architecture.md](architecture.md) — Rig、Shot、Motion Player、Evaluator、Switcher、Editorの責務と所有権
3. [phases.md](phases.md) — 現在地と次の実装

機能別仕様:

- [spec-operation.md](spec-operation.md) — キーボード操作と将来のMIDI
- [spec-camera.md](spec-camera.md) — Camera Performance、Spline、Timing、Aim、Lens、初期Palette
- [spec-setup.md](spec-setup.md) — Rig作成、Inspector Authoring、Scene同期
- [spec-assistance.md](spec-assistance.md) — Cinemachineによる初期支援
- [spec-switching.md](spec-switching.md) — 複数ShotのDirect Cut

開発手順:

- [workflow.md](workflow.md) — Antigravityへの実装委譲、Review、検証
- [code-style.md](code-style.md) — C#、Tooltip、コメント、Custom Inspectorの規則
- [AGENTS.md](../AGENTS.md) — エージェント向け入口と変更境界

実装エージェントは全仕様書を読む必要はない。`AGENTS.md`、今回の担当仕様書、チャットで渡された短い実装タスク、該当するAgent Skillだけを使用する。実装用プロンプトは一時的な依頼であり、仕様書としてこのディレクトリへ保存しない。

将来構想は現在の実装要求ではない。実装済みの範囲は必ず[phases.md](phases.md)で確認する。
