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

`Docs/phases.md`のStep 4 Motion Foundationは、次の内部順で一括実装できる。

1. `git status --short`と現在のPreset、Builder、Shot再生、Switcher、Inputを確認する。
2. 現在解決されているCinemachine 3.1.7とUnity Splinesの実APIを確認する。
3. `VLiveCameraMotionPreset`を完全なSpline、Timing、Aim、Composition、Lens、Activationを持つ形式へ更新する。
4. Preset原本、Shotの適用済み設定、Runtime状態を分け、`VLiveCameraMotionEvaluator`と`VLiveCameraMotionPlayer`を追加する。
5. Spline DollyをDistance単位へ変更し、DurationとProgress Curveから位置を評価する。
6. Shot専用Aim ProxyとRotation Composer設定、Lens Trackを同じPlayback Timeへ接続する。
7. Static / Rolling Entryと連続的なSpeed、Hold、Resume、Reverseを実装する。
8. BuilderのCreate、Apply / Sync、Rebuildを新形式へ対応させる。
9. Scene上のSelected Shotから新しいPresetを保存する明示操作を追加する。
10. 無効値、Curve、速度、加速度、Jerk、In / Out、Aim、Lensの簡易Validatorを追加する。
11. 6つの同梱Presetを新形式へ更新する。
12. Import、Compile、Console、diffを簡易確認する。

途中でGold Master量産、Preview、App UI、MIDI、専用テストScene、汎用Editor frameworkを追加しない。

## 5. 実装規則

- 依頼範囲だけを変更する。
- Package直下の`README.md`を変更しない。
- 既存Tests Sceneを変更、削除、ステージしない。
- 1 Shotにつき1つのCinemachineCameraを使用する。
- Preset原本、Shotへ具体化した設定、Motion PlayerのRuntime状態を混同しない。
- PresetやSlot参照の変更を、Applyだけで生成済みShotへ暗黙伝播しない。
- Shot順とSlotの正本は`VLiveCameraRig`だけに置く。
- Motion再生状態の正本は`VLiveCameraMotionPlayer`だけに置く。
- Live中のCameraへ別Shot設定を上書きしない。
- `OnValidate`やInspector変更だけでSceneオブジェクトを生成、削除、再配置しない。
- 通常のApplyと、手動調整を上書きするRebuildを分ける。
- Scene上のShotからPresetを作る場合は新規保存を既定とし、既存Assetを暗黙に上書きしない。
- Validatorは既定で診断だけを行い、Spline、Aim、Lens、Durationを変更しない。
- interface、Manager、Registry、Command Bus、DIを現在の必要性なく追加しない。
- Gold Master量産、Preview、App UI、MIDI、Runtime AI、検索、カテゴリ、サムネイルを先行実装しない。
- `Docs/code-style.md`へ従う。
- 複数エージェントが同じファイルを同時編集しない。

## 6. 既定の確認

実装エージェントは次だけを既定で行う。

- 今回のパスだけを対象にしたdiffと参照の簡易Review
- 末尾空白、namespace、asmdef、Tooltip、Editor分離の確認
- Unity Import、Domain Reload、Compile
- Consoleに新しいCompile Errorや明白な例外がないこと
- Motion Preset、Rig Profile、Evaluator、Player、Editor codeがImport、Compileされること

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
- Rig作成とBuilder操作がUndo対応で、重複生成と自動保存を行わない。
- Applyが既存のLens、Spline、Camera調整を上書きしない。
- Rebuildと生成物削除が明示的で、対象外のSceneオブジェクトを変更しない。
- Save Shot As New Presetが既存Assetを暗黙に上書きしない。
- 同じPresetを複数Slotで使用してもShot参照が混線しない。
- 各Shotが専用CinemachineCameraを持つ。
- 各Motion Shotが専用Spline、Aim Proxy、Motion Playerを持つ。
- Preset原本、Shotの適用済み設定、Motion PlayerのRuntime状態の所有権が分かれている。
- Body、Aim、Composition、Lensが同じPlayback Timeを使用する。
- ValidatorがAssetやSceneを暗黙変更しない。
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
