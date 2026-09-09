# 開発・実装委譲ワークフロー

## 1. 役割分担

- ユーザー: 製品判断、実装範囲の承認、最終的な操作感の確認
- 仕様担当Codex: 現状調査、仕様整理、実装タスク作成、diffと仕様のReview
- Antigravity Gemini Flash 3.8 high: 指定された小さな実装タスクの実行

実装エージェントに製品仕様の決定を委ねない。仕様に不足があり、選択によって挙動が変わる場合は実装を止めて確認する。

## 2. 実装タスクの単位

一度に依頼するのは、独立して確認できる1つの縦切りタスクとする。`Step 1を全部実装`のような大きな依頼を避ける。

各タスクには次を必ず含める。

```text
目的:
実装する挙動:
変更してよいファイル:
追加してよいファイル:
対象外:
禁止する抽象化・変更:
完了条件:
実施する検証:
判断できない場合の停止条件:
```

実装エージェントへ渡す文書は、原則として次の3つだけにする。

1. [AGENTS.md](../AGENTS.md)
2. 今回に関係する仕様書1つ
3. 今回の実装タスク

全仕様書から実装範囲を推測させない。

## 3. 最初の実装タスク分割

[phases.md](phases.md)のStep 1は、少なくとも次の順へ分割する。

1. Unity 6.3 / Cinemachine 3のpackage設定と最小Compile
2. 新namespaceと最小asmdefの確定
3. Fixed ShotとSpline Shotを表す`VLiveCameraShot`
4. 2〜3台をCutする`VLiveCameraSwitcher`
5. 数字キーだけを扱う`VLiveCameraKeyboardInput`
6. Speed、Reverse、Hold、Resume
7. Cut成功時のProgram Shot名ログ
8. 動作確認Sceneと反復操作

各タスク完了後にdiffとUnity結果を確認し、次のタスク内容を調整する。

## 4. 実装エージェントの規則

- 依頼された範囲だけを変更する。
- 仕様にないinterface、基底クラス、Manager、Service、Command Busを追加しない。
- 将来のMIDI、AI、Preview、Patternのためのコードを追加しない。
- ついでのリファクタリングやフォルダー再編を行わない。
- 旧版互換のためのwrapper、属性、移行処理を追加しない。
- 既存コードを削除する場合は、タスクに明記されたパスだけを対象にする。
- [AGENTS.md](../AGENTS.md)のコードスタイルに従う。
- 完了時に変更ファイル、検証結果、未確認事項を報告する。

## 5. 変更前の確認

1. `git status --short`で既存変更を確認する。
2. 今回の変更可能ファイルを列挙する。
3. 参照元を`rg`で検索する。
4. 使用するUnityとCinemachineの実APIを現在のpackageで確認する。
5. 削除対象と残す対象を区別する。

旧実装の互換性調査は不要だが、現在の変更がタスク外ファイルを壊していないかは確認する。

## 6. Review

仕様担当は実装後に次を確認する。

- タスクの完了条件を満たしているか。
- 対象外の変更が含まれていないか。
- 将来用の抽象化が追加されていないか。
- 1つの値へ複数箇所から書き込んでいないか。
- `Update`経路に不要な検索やAllocationがないか。
- 無効参照でProgramが失われないか。
- Unity上で未確認の内容を確認済みと報告していないか。

問題がある場合は大規模な再設計を依頼せず、問題箇所だけを修正するタスクへ分ける。

## 7. 検証の層

| 層 | 確認内容 |
| --- | --- |
| Static | diff、namespace、参照、末尾空白、不要な抽象化 |
| Compile | C#とCinemachine APIの整合 |
| Unity Import | asmdef、`.meta`、GUID、Domain Reload |
| Play Mode | Cut、移動、入力、無効参照 |
| Game View | 構図、動き、ジャンプ、操作感 |
| Long Run | 入力残留、例外、GC、状態破損 |
| Performance | CPU、GPU、GC、Memory |

Compile成功だけでGame Viewの正しさを証明したことにしない。初期タスクでは必要な層だけを指定し、毎回すべての検証を要求しない。

## 8. Git

- 作業前後に`git status --short`とdiffを確認する。
- 許可されたファイルだけを明示的にステージする。
- ユーザーの無関係な変更を編集・ステージしない。
- Commit messageは`<type>: <変更内容>`とする。
- Codexがコミットする場合は使用したCodex名を`Co-Authored-By`へ記録する。
- Branch、Push、Pull Requestはユーザーの明示依頼がある場合だけ行う。
- 別エージェントと連携する場合は、同じファイルを同時に編集しない。

## 9. 完了報告

- 実装した挙動
- 変更したファイル
- 実施した検証
- Unity上で未確認の内容
- 対象外として残した内容
- Commitした場合はCommit ID

推測や将来対応を、完了した実装として報告しない。
