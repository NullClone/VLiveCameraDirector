# 開発・実装委譲ワークフロー

## 1. 役割

- ユーザー: 製品判断と、自身の作業用Sceneでの操作感確認
- 仕様担当Codex: 現状調査、仕様更新、実装プロンプト、diff Review
- 実装エージェントとサブエージェント: 承認された縦切り実装

通常のコード構成、命名、Inspector配置は実装エージェントが自律的に決める。製品挙動、データ所有権、公開API、破壊的な範囲変更が必要な場合だけ停止して確認する。

## 2. 実装タスク

1回の依頼は、独立してCompileできる1つの縦切りとする。密接したRuntime、Editor、Presetは1つの体験を完成させるために一括で依頼してよい。

実装プロンプトには次を含める。

```text
目的:
実装する体験:
変更してよい範囲:
変更しない範囲:
対象外:
完了条件:
既定の確認:
停止条件:
```

実装用プロンプトはチャットでユーザーへ渡し、`Docs/`へ保存しない。繰り返し適用する製品判断だけを仕様へ反映する。

実装エージェントが読むもの:

1. `AGENTS.md`
2. 今回に関係する仕様書
3. C#またはEditorを変更する場合は`Docs/code-style.md`
4. チャットで渡された実装タスク
5. 今回使用するAgent Skill

## 3. Agent Skills

Agent Skillsは`E:\Unity\Project\MMD\.agents\skills`を使用する。ユーザー指定またはdescriptionが一致するSkillを、受任したエージェント自身が全文読む。親の確認をサブエージェントへ流用しない。

Unity Editor、Scene、Prefab、Asset、Build、Testでは`unity-cli`を使用する。UPM Packageの外部変更では`unity-package-management`を使用する。Skillが要求する確認を実行できない場合は未確認として報告する。

## 4. 現在の縦切り実装

`Docs/phases.md`のStep 2は、次の内部順で一括実装できる。

1. `git status --short`と現在のA/B実装を確認する。
2. Cinemachine 3とUnity Splinesの実APIを確認する。
3. `VLiveCameraMotionPreset`と6つの初期Presetを作る。
4. SwitcherとKeyboard Inputを複数Shot、キー1〜9へ変更する。
5. `VLiveCameraSetupWindow`でTarget、Output Camera、Paletteを扱う。
6. Create / Update、Undo、重複生成防止、Scene非保存を実装する。
7. 今回触るユーザー向けMonoBehaviourへCustomEditorを追加する。
8. 旧A/B専用Scene BuilderとPlayMode Verifierが不要なら削除する。
9. Import、Compile、Console、diffを簡易確認する。

途中で専用テストSceneや汎用Editor frameworkを追加しない。

## 5. 実装規則

- 依頼範囲だけを変更する。
- Package直下の`README.md`を変更しない。
- 既存Tests Sceneを変更、削除、ステージしない。
- 1 Shotにつき1つのCinemachineCameraを使用する。
- Preset設定とShotのRuntime状態を二重管理しない。
- Live中のCameraへ別Shot設定を上書きしない。
- interface、Manager、Registry、Command Bus、DIを現在の必要性なく追加しない。
- Preview、MIDI、Runtime AI、検索、カテゴリ、サムネイルを先行実装しない。
- `Docs/code-style.md`へ従う。
- 複数エージェントが同じファイルを同時編集しない。

## 6. 既定の確認

実装エージェントは次だけを既定で行う。

- 今回のパスだけを対象にしたdiffと参照の簡易Review
- 末尾空白、namespace、asmdef、Tooltip、Editor分離の確認
- Unity Import、Domain Reload、Compile
- Consoleに新しいCompile Errorや明白な例外がないこと
- Setup WindowがMenuから開けること

次はユーザーが明示しない限り行わない。

- 自動テストの新規作成
- 専用テストSceneの作成または更新
- 長時間Play Mode
- Game Viewでの構図やカメラワーク評価
- 性能計測

実際のSetup実行、キー操作、構図、動きはユーザーが作業用Sceneで受け入れる。エージェントは未実施の項目を確認済みと報告しない。

## 7. Review

仕様担当は次をざっと確認する。

- 完成条件に必要なファイルが揃っている。
- 対象外の機能や抽象化が増えていない。
- SetupがUndo対応で、重複生成と自動保存を行わない。
- 各Shotが専用CinemachineCameraを持つ。
- PresetとRuntime状態の所有権が分かれている。
- 表示されるSerializedFieldにTooltipがある。
- ユーザー向けMonoBehaviourに有用なCustomEditorがある。
- 無効な選択でProgramを失わない。

## 8. Git

- 作業前後に`git status --short`を確認する。
- 許可されたパスだけを明示的にステージする。
- ユーザーの無関係な変更を保持する。
- Commit messageは`<type>: <変更内容>`とする。
- Branch、Push、Pull Requestは明示依頼がある場合だけ行う。

## 9. 完了報告

- 実装した体験
- 変更したファイル
- 実施した確認
- ユーザーのSceneで確認する項目
- 対象外として残した内容
- Commit ID
