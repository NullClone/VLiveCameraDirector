# 開発・検証ワークフロー

## 1. 目的

この文書は、VLiveCameraUnit の仕様、コード、Unity アセットを安全に変更し、検証結果を正確に報告する手順を定義する。

## 2. 変更前

1. ユーザーの依頼範囲と、変更してはいけない範囲を確認する。
2. [README.md](README.md) から担当仕様書を一つ選ぶ。
3. [phases.md](phases.md) で現状と前提フェーズを確認する。
4. `git status --short` で既存変更を確認し、無関係な変更を記録する。
5. 対象型、参照、Prefab、Scene、Timeline、UnityEvent を検索する。
6. SerializedProperty、GUID、公開 API に影響する場合は移行方針を先に決める。

調査だけを求められた場合は、コード、アセット、設定、文書を変更しない。

## 3. 仕様変更

- 製品仕様を変える場合は、ユーザーの承認後に担当仕様書を更新する。
- 同じ規則を複数文書へコピーせず、正本からリンクする。
- 目標仕様と現在実装の差分は [phases.md](phases.md) に記録する。
- 未決定事項は断定せず、決定が必要なフェーズと条件を書く。
- 実装都合で挙動を変更する場合も、仕様変更として扱う。
- 文書は自然で簡潔な日本語を使い、制約、数値、受け入れ条件を省略しない。

## 4. 実装

- Runtime、Editor、Tests の依存方向を守る。
- 入力源、Switcher State、Pattern、支援、Cinemachine Writer の責務を混ぜない。
- 新しい Shot のためだけの Runtime 分岐を増やさず、共通プリミティブとデータで表現する。
- namespace と命名は [AGENTS.md](../AGENTS.md) に従う。
- Unity のファイルを追加・移動・削除する場合は `.meta` と GUID を同時に扱う。
- 既存の SerializedField 名を変更する場合は `FormerlySerializedAs` 等を検討する。
- Scene、Prefab、Sample を機械的に一括更新する前に、対象パスを明示して承認範囲を確認する。
- 無関係な dirty tree の変更を編集、削除、ステージしない。

## 5. 検証の層

検証結果は次を分けて記録する。一つの成功を別の証拠として代用しない。

| 層 | 確認内容 | 代表手段 |
| --- | --- | --- |
| Text / Static | diff、リンク、命名、参照、シリアライズ契約 | `rg`、`git diff --check`、Review |
| C# Compile | assembly と API の整合 | Unity が生成した project、Editor compile |
| Unity Import | `.meta`、GUID、Package、Domain Reload | Unity Editor Import、Editor.log |
| Automated Test | 決定的な状態遷移、計算、異常処理 | EditMode / PlayMode Test |
| Scene / Game View | 構図、動き、遷移、UI | 基準 Scene の目視と録画 |
| Operation | 入力応答、誤操作、Pickup、復旧 | 操作シナリオと実デバイス |
| Performance | CPU、GPU、GC、Memory、長時間安定性 | Profiler、Frame Debugger、実機 Player |

文書だけの変更では Static 検証までを行い、Unity Import や画面品質を検証済みとは記載しない。

## 6. 主要シナリオ

実装フェーズに応じ、最低限次を反復確認する。

1. Fixed Shot を Preview し Take する。
2. Moving Shot を Take し、Entry から Main へ遷移する。
3. Rail Hold、Reverse、Resume を実行する。
4. Pan / Tilt / Zoom を操作し、解放後の復帰を確認する。
5. Program と同じ Shot を再選択する。
6. 無効 Shot、欠落 Target、未準備 Preview への Take を試す。
7. Bank を跨いで Program / Preview / Tally を確認する。
8. 入力中にフォーカス喪失または MIDI 切断を発生させる。
9. Cut、Cinemachine Blend、Video Transition を個別に確認する。
10. Multiview の品質段階を変え、Program の性能を測る。

## 7. Review 観点

- 仕様の担当範囲と実装が一致しているか。
- Program を失う失敗経路がないか。
- 同じ状態への複数 Writer がないか。
- Preview が Program 状態を変更していないか。
- 手動介入の開始・解放で不連続がないか。
- 時間源、Seed、再選択時の動作が決定的か。
- ターゲットや参照の欠落を黙って別挙動へ置換していないか。
- 論理登録台数と同時描画台数を混同していないか。
- AI 生成データが人の承認と共通検証を迂回していないか。

## 8. Git

- 作業前後に `git status --short` と diff を確認する。
- 許可されたファイルだけを明示的にステージする。
- 生成物、Library、Temp、無関係なユーザー変更をコミットしない。
- Commit message は `<type>: <変更内容>` を基本とする。
- Codex がコミットした場合は、実際に使用した Codex 名を `Co-Authored-By` trailer に記録する。
- Branch 作成、Push、Pull Request はユーザーの明示依頼がある場合だけ行う。

## 9. 完了報告

完了時は、次を簡潔に分けて報告する。

- 変更した内容と正本ファイル
- 実施した検証と結果
- Unity 上で未確認の内容
- 性能・長時間運用で未測定の内容
- 保持した無関係な既存変更
- Commit を行った場合は Commit ID

「Compile 成功」「Unity Import 成功」「Game View で正しい」「本番負荷を満たす」は、それぞれ別の結論として扱う。
