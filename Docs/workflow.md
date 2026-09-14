# 開発・実装委譲ワークフロー

## 1. 役割

- ユーザー: 製品判断と、自身の作業用Sceneでの映像・操作受け入れ
- 仕様担当: 現状調査、仕様更新、実装タスク、diff Review
- 実装エージェントとサブエージェント: 承認された縦切り実装

通常のコード構成、命名、Inspector配置はエージェントが自律的に決める。製品挙動、データ所有権、公開API、破壊的範囲が仕様を越える場合だけ停止して確認する。

## 2. 読む文書

実装エージェントは次だけを読む。

1. `AGENTS.md`
2. [roadmap.md](roadmap.md)のCurrentとNext
3. 今回に関係する機能仕様
4. C#またはEditorを変更する場合は[code-style.md](code-style.md)
5. チャットで渡された実装タスク
6. 今回使用するAgent Skill

全仕様書を毎回読む必要はない。文書の役割は[README.md](README.md)を参照する。

## 3. 実装タスク

1回の依頼は、独立してCompileできる1つの縦切りとする。密接したRuntime、Editor、Presetは、1つの体験を完成させるために一括で依頼してよい。

実装タスクには次を含める。

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

実装用プロンプトはチャットでユーザーへ渡し、`Docs/`へ保存しない。繰り返し適用する製品判断だけを仕様書へ反映する。

## 4. Agent Skills

Project Skillは`E:\Unity\Project\MMD\.agents\skills`を使用する。ユーザー指定またはdescriptionが一致するSkillを、受任した各エージェントが全文読む。親エージェントの確認をサブエージェントへ流用しない。

Unity Editor、Scene、Prefab、Asset、Build、Testでは`unity-cli`を使用する。UPM Packageの追加、削除、更新では`unity-package-management`を使用する。Skillが要求する確認を実行できない場合は未確認として報告する。

## 5. 実装規則

- 最初に`git status --short`を確認し、ユーザーの無関係な変更を保持する。
- 依頼範囲だけを変更する。
- Package直下の`README.md`を明示依頼なしに変更しない。
- ユーザーSceneを変更、保存、削除しない。
- [architecture.md](architecture.md)の状態所有権とCinemachine境界を崩さない。
- [motion.md](motion.md)のMotionデータと計算契約を崩さない。
- Apply、Rebuild、Camera Settings一括適用、削除を暗黙に統合しない。
- Inspector変更や`OnValidate`だけでScene、Spline、Assetを変更しない。
- Validatorは既定で診断だけを行う。
- 現在必要のないinterface、Manager、Registry、Command Bus、DIを追加しない。
- [roadmap.md](roadmap.md)のLaterとFutureを先行実装しない。
- C#とInspectorは[code-style.md](code-style.md)へ従う。
- 複数エージェントが同じファイルを同時編集しない。

## 6. 既定の確認

実装エージェントは次を既定で行う。

- 今回のパスに限定したdiffと参照の簡易Review
- 末尾空白、namespace、asmdef、Tooltip、Runtime / Editor分離の確認
- Unity Import、Domain Reload、Compile
- Consoleに新しいCompile Errorや明白な例外がないこと

次はユーザーが明示しない限り行わない。

- 自動テストの新規作成
- 専用テストSceneの作成または更新
- 長時間Play Mode
- Game Viewでの構図やカメラワーク評価
- 性能計測

Compile、Validatorの数値、Scene上の映像確認を同じ証拠として扱わない。実際のSetup、キー操作、構図、Motionはユーザーが作業用Sceneで受け入れる。

## 7. Review

仕様担当は今回の契約に関係する次の点だけを確認する。

- 必要なRuntime、Editor、Asset変更が揃っている。
- 対象外の機能と抽象化が増えていない。
- 状態の正本が重複していない。
- 各Shotが専用CinemachineCameraを持つ。
- Scene変更が明示操作とUndoを伴う。
- Applyが既存のScene調整を上書きしない。
- Rebuildと削除の対象が明確である。
- Scene調整がMotion Preset Assetへ暗黙逆同期されない。
- ValidatorがAssetやSceneを変更しない。
- 表示するSerializedFieldにTooltipがある。
- ユーザー向けMonoBehaviourに有用なCustomEditorがある。
- Inspectorが標準IMGUI中心の英語UIである。
- 無効な選択で現在のProgramを失わない。

## 8. Git

- 作業前後に`git status --short`を確認する。
- 許可されたパスだけを明示的にStageする。
- Branch、Push、Pull Requestは明示依頼がある場合だけ行う。
- Commit messageは`<type>: <変更内容>`とする。
- Commitを依頼された場合は、無関係な変更を含めない。

## 9. 完了報告

- 実装した体験
- 変更したファイル
- 実施した確認
- ユーザーのSceneで確認する項目
- 対象外として残した内容
- Commit ID。Commitしていない場合はその旨
