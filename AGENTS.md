# VLiveCameraUnit 開発ガイド

## 製品の目的

VLiveCameraUnit は、事前に用意した多数のカメラをライブ中に演奏するように切り替え、Cinemachine による追従・構図・補間で操作を支援する Unity 完結型のカメラシステムである。

最初に作るのは手動運用の核である。カメラを選ぶだけで意図した動きが始まり、必要なときだけ速度、方向、Hold、画角へ介入できる状態を優先する。MIDI、AI、推薦、完全自動化は、この核が実際に成立してから扱う。

## 仕様の優先順位

判断が衝突した場合は、次の順に優先する。

1. その作業に対するユーザーの明示指示
2. このファイル
3. 担当仕様書
4. 現在のコードの実際の状態

`Docs/` は目標を定義するが、現在実装との差分は [Docs/phases.md](Docs/phases.md) で確認する。将来構想は現在の実装要求ではない。

## 文書の読み分け

| 内容 | 正本 |
| --- | --- |
| 製品コンセプトと現在の優先事項 | [Docs/overview.md](Docs/overview.md) |
| 初期実装の型、責務、依存関係 | [Docs/architecture.md](Docs/architecture.md) |
| キーボード操作と将来のMIDI | [Docs/spec-operation.md](Docs/spec-operation.md) |
| Shot、Spline、カメラの動き | [Docs/spec-camera.md](Docs/spec-camera.md) |
| Cinemachineによる操作支援 | [Docs/spec-assistance.md](Docs/spec-assistance.md) |
| Cut、Program / Preview、Bank | [Docs/spec-switching.md](Docs/spec-switching.md) |
| 現在地と実装順序 | [Docs/phases.md](Docs/phases.md) |
| 実装委譲、検証、Git | [Docs/workflow.md](Docs/workflow.md) |

同じ規則を複数文書へ複製しない。コード内コメントには局所的な理由だけを記載する。

## 開発中の互換性

- 初回安定版までは、旧バージョンとの API、SerializedField、Prefab、Scene、Timeline の互換性を保証しない。
- 旧実装は有用な挙動を確認する参考資料であり、構造を維持する対象ではない。
- 移行用 Adapter、旧 API wrapper、`MovedFrom`、`FormerlySerializedAs` は原則として追加しない。
- 新仕様に不要な旧コードは、実装タスクの範囲を確認したうえで置き換えまたは削除できる。
- 互換性不要であっても、無関係なユーザー変更やタスク外のアセットを無断で変更しない。

初回安定版を定義するときに、以後の互換性方針を改めて決める。

## 技術方針

- Unity 6.3 以上、Cinemachine 3 を対象とする。
- Runtime のルート namespace は `toshi.VLiveKit.Camera` とする。
- Editor の namespace は `toshi.VLiveKit.Camera.Editor` とする。
- Tests の namespace は `toshi.VLiveKit.Camera.Tests` とする。
- 最初の実装は [Docs/architecture.md](Docs/architecture.md) の最小構成だけを作る。
- 固定ShotへSplineを強制しない。移動ShotだけがSplineを使用する。
- Program出力は1台のUnity CameraとCinemachine Brainを基本とする。
- Shotごとの個別コードを量産せず、同じ実装で設定値を変える。

## C#コードスタイル

- 4スペースでインデントし、波括弧を省略しない。
- 原則として1ファイルに1つの主要型を置き、ファイル名と型名を一致させる。
- 型、メソッド、プロパティ、公開メンバーは `PascalCase` とする。
- privateフィールドは `_camelCase` とする。
- Inspectorへ出すフィールドは原則 `[SerializeField] private` とし、publicフィールドを使わない。
- 不変にできるフィールドは `readonly`、定数は `const` とする。
- `var` は右辺から型が明白な場合だけ使用する。
- Runtime assemblyから`UnityEditor`を参照しない。
- Editorから値を変更するときは`SerializedObject`、`SerializedProperty`、Undoを使用する。
- `Update`など毎フレームの経路で`Find`、LINQ、不要な配列生成、文字列生成を行わない。
- Unity Objectのnull判定を通常のC#参照と同一視しない。
- Logは異常の原因と対象を含め、毎フレーム出力しない。
- コメントはコードの言い換えではなく、制約や判断理由を書く。
- 公開APIは現在必要なものだけを追加する。

## 過剰設計を避ける規則

- 利用箇所が1つしかない処理のためにinterface、抽象基底クラス、Factoryを作らない。
- 実際の要求がないCommand Bus、Service Locator、DI Container、独自Event Busを導入しない。
- 将来用の空クラス、未使用設定、互換レイヤーを追加しない。
- 同じ処理が複数箇所で必要になってから共通化を検討する。
- 新しい抽象化を追加する場合は、現在解決する重複または不具合を説明できなければならない。
- 半自動化やMIDIのためだけに、現在のキーボード実装を複雑にしない。
- 仕様にない機能を「ついでに」実装しない。

## 実装エージェントへの委譲

実装は主にAntigravityのGemini Flash 3.8 highへ委譲する。エージェントには、この文書、担当仕様書、今回の短い実装タスクだけを渡す。

- 一度に1つの縦切りタスクだけを依頼する。
- 変更可能ファイル、対象外、完了条件をタスクに明記する。
- エージェントは仕様を独自に拡張せず、不明点が実装結果を変える場合は停止して確認する。
- 依頼されていないリファクタリング、抽象化、将来機能を追加しない。
- 実装後はdiff、Compile、指定された動作確認の結果を返す。
- 複数エージェントが同じファイルを同時編集しない。

詳細は [Docs/workflow.md](Docs/workflow.md) に従う。

## 変更の原則

- 調査だけを依頼された場合は変更しない。
- 仕様変更はユーザーの承認を得てから行う。
- 無関係な既存変更を保持し、許可された範囲だけを編集・ステージする。
- Compile、Unity Import、Play Mode、見た目、性能を別の検証結果として報告する。
- 作業開始前と完了前に [Docs/workflow.md](Docs/workflow.md) を確認する。
