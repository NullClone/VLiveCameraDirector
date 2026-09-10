# 開発・実装委譲ワークフロー

## 1. 役割分担

- ユーザー: 製品判断、実装範囲の承認、最終的な操作感の確認
- 仕様担当Codex: 現状調査、仕様整理、実装タスク作成、diffと仕様のReview
- Antigravity Gemini Flash 3.8 highとそのサブエージェント: 承認された縦切り実装を最後まで実行

実装エージェントに製品仕様の決定を委ねない。仕様に不足があり、選択によって挙動が変わる場合は実装を止めて確認する。

## 2. 実装タスクの単位

一度に依頼するのは、独立して完成・確認できる1つの縦切りタスクとする。調査で変更境界が確定している場合は、A/B最小版のように密接した作業を1つの一括実装として最後まで任せてよい。

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

実装エージェントへ渡す情報は、今回の縦切りに必要なものだけにする。

1. [AGENTS.md](../AGENTS.md)
2. 今回に関係する仕様書
3. C#またはEditorを変更する場合は[code-style.md](code-style.md)
4. チャットで渡す今回の実装タスク
5. 今回使用するAgent Skill名

プロンプトに完成条件と対象外を明記し、将来構想から実装範囲を推測させない。
実装用プロンプトはユーザーへチャット経由で提示し、`Docs/`へ現在のタスクとして保存しない。繰り返し適用する製品判断や開発規則だけを仕様書へ反映する。

## 3. Agent Skillsの利用

Agent SkillsはUnityプロジェクト直下の`E:\Unity\Project\MMD\.agents\skills`を使用する。

1. エージェントは作業開始前に、依頼内容と各Skillのdescriptionを照合する。
2. ユーザーが指定したSkillと、作業内容に一致するSkillの`SKILL.md`を受任したエージェント自身が全文読む。
3. サブエージェントへ委譲する場合、プロンプトに必要なSkill名を明記する。受任側は親の確認を流用せず、自分で同じ手順を行う。
4. サブエージェントがさらに委譲する場合も、必要なSkill名とこの規則を引き継ぐ。
5. Skillが要求する確認を実行できない場合、その検証を完了扱いにしない。

Unity CLI、Editor接続、Scene、Prefab、Asset、Build、Testを扱う作業では`unity-cli`を使用する。Scene、Prefab、Assetを変更する前に`unity status --project-path "E:\Unity\Project\MMD" --format json`を実行し、接続可能なEditorがある場合はSkillの手順に従ってEditor経由で操作する。0件でもプロセス確認権限に関するwarningがある場合は未接続と断定せず、`unity pipeline list --format json`でも確認する。どちらも権限制約で判定できない場合は、未接続として直接編集へ進まず、判定不能であることを報告する。UPM Packageを外部から変更する作業では`unity-package-management`を使用する。

## 4. 最初の一括実装の内部順序

[phases.md](phases.md)のStep 1は、境界監査後であれば1つの縦切りタスクとして実行できる。実装エージェントは内部で次の順に進める。

1. Cinemachine 2依存と削除・置換範囲の変更なし監査
2. Unity 6.3 / Cinemachine 3のpackage設定と最小Compile
3. 新namespaceと最小asmdefの確定
4. 別々のCinemachineCameraを持つShot A / B
5. AをFixed、BをSpline始点へ準備する`VLiveCameraShot`
6. A/BをCinemachineのCutとして切り替える`VLiveCameraSwitcher`
7. キー1 / 2だけを扱う`VLiveCameraKeyboardInput`
8. Bの始点→移動→減速→終点Hold
9. Speed、Reverse、Hold、Resume
10. Cut成功時のProgram Shot名ログ
11. 動作確認Sceneと反復操作

途中でユーザー確認を挟まず、Compile可能な状態を保ちながら一連の実装を完了する。仕様外の削除や結果を変える重大な判断が必要な場合だけ停止する。

## 5. 実装エージェントの規則

- 依頼された範囲だけを変更する。
- 仕様にないinterface、基底クラス、Manager、Service、Command Busを追加しない。
- 将来のMIDI、AI、Preview、Patternのためのコードを追加しない。
- 同じCinemachineCameraへ別ShotのTransform、Spline、Lens、Targetを上書きしない。
- A/Bを汎用Slotとして動的に再構成しない。
- ついでのリファクタリングやフォルダー再編を行わない。
- 旧版互換のためのwrapper、属性、移行処理を追加しない。
- 既存コードを削除する場合は、タスクに明記されたパスだけを対象にする。
- [code-style.md](code-style.md)のコードとInspectorスタイルに従う。
- [AGENTS.md](../AGENTS.md)のAgent Skills規則に従い、サブエージェントにも適用する。
- 完了時に変更ファイル、検証結果、未確認事項を報告する。

## 6. 変更前の確認

1. `git status --short`で既存変更を確認する。
2. 今回の変更可能ファイルを列挙する。
3. 参照元を`rg`で検索する。
4. 使用するUnityとCinemachineの実APIを現在のpackageで確認する。
5. 削除対象と残す対象を区別する。

旧実装の互換性調査は不要だが、現在の変更がタスク外ファイルを壊していないかは確認する。

## 7. Review

仕様担当は実装後に次を確認する。

- タスクの完了条件を満たしているか。
- 対象外の変更が含まれていないか。
- 将来用の抽象化が追加されていないか。
- 1つの値へ複数箇所から書き込んでいないか。
- `Update`経路に不要な検索やAllocationがないか。
- 表示されるSerializedFieldにTooltipがあり、ユーザー向けMonoBehaviourに有用なCustomEditorがあるか。
- 無効参照でProgramが失われないか。
- Unity上で未確認の内容を確認済みと報告していないか。

問題がある場合は大規模な再設計を依頼せず、問題箇所だけを修正するタスクへ分ける。

## 8. 検証の層

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

## 9. Git

- 作業前後に`git status --short`とdiffを確認する。
- 許可されたファイルだけを明示的にステージする。
- ユーザーの無関係な変更を編集・ステージしない。
- Commit messageは`<type>: <変更内容>`とする。
- Codexがコミットする場合は使用したCodex名を`Co-Authored-By`へ記録する。
- Branch、Push、Pull Requestはユーザーの明示依頼がある場合だけ行う。
- 別エージェントと連携する場合は、同じファイルを同時に編集しない。

## 10. 完了報告

- 実装した挙動
- 変更したファイル
- 実施した検証
- Unity上で未確認の内容
- 対象外として残した内容
- Commitした場合はCommit ID

推測や将来対応を、完了した実装として報告しない。
