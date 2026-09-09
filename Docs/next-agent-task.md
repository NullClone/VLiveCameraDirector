# 次作業エージェント向けプロンプト

以下をそのままAntigravityのGemini Flash 3.8 highへ渡す。

---

あなたはUnityパッケージ`VLiveCameraUnit`の実装前監査を担当します。今回はコード、文書、package、Prefab、Scene、`.meta`を一切変更しないでください。調査結果だけを報告してください。

## 作業場所

`E:\Unity\Project\MMD\Assets\toshi.VLiveKit\VLiveCameraUnit`

## 最初に読む文書

1. `AGENTS.md`
2. `Docs/architecture.md`
3. `Docs/phases.md`

他の仕様書は、調査対象の確認に必要な箇所だけ読んでください。文書中の将来構想を今回の実装範囲として扱わないでください。

## 背景

目標環境はUnity 6.3以上、Cinemachine 3です。旧バージョンとのAPI、SerializedField、Prefab、Scene互換性は保持しません。ただし、互換性不要を理由に、範囲未確認のコードやアセットを削除してはいけません。

最初に実装する完成形は次です。

- 出力用Unity CameraとCinemachine Brainは1台
- Shot AとShot Bは別々のCinemachineCameraを持つ
- Aは安定したFixed Shot
- BはSplineの始点から終点へ動き、終点でHoldするShot
- キー1 / 2でA/BをCinemachineのCutとして切り替える
- Live中の同じCinemachineCameraへ別ShotのTransform、Spline、Lens、Targetを上書きしない
- A/Bを汎用Slotとして動的に再構成しない

## 今回の目的

現在のCinemachine 2.9.7依存をCinemachine 3へ置き換える前に、次の実装タスクで変更・削除を許可すべき正確なファイル一覧を作成してください。

## 調査内容

1. `package.json`、asmdef、Runtime、Editor、Testsから、Cinemachine 2 APIへ依存するファイルを列挙する。
2. 各ファイルを次のいずれかへ分類する。
   - A/B最小実装で置き換える
   - Cinemachine 3へ小さく移植する
   - 今回は削除候補
   - Cinemachineと無関係なので保持する
3. Cinemachine 2から3で変更される型、namespace、主要APIを、現在利用可能なUnity公式ドキュメントまたはインストール済みPackage sourceで確認する。
4. 現在のRuntime / Editor asmdef参照を調べ、package更新後にCompileを妨げる箇所を特定する。
5. Prefab、Scene、Timeline、UnityEventから旧カメラ型が参照されている箇所を検索する。互換対応は不要だが、破損対象を把握する。
6. A/B最小実装のために、新規作成が必要なファイルを最大5個まで提案する。
7. 実装を1回で大きく行わず、Compile可能な小タスクへ分割する。

## 禁止事項

- ファイルの作成、編集、削除、移動、改名
- `package.json`やasmdefの変更
- Unity SceneやPrefabの保存
- Import、Reimport、アップグレード処理の実行
- 自動修正、formatter、migration toolの実行
- Branch作成、Commit、Push
- Command Bus、Manager階層、DI、独自Solver等の提案
- Preview、Bank、MIDI、AI、Pattern Assetの実装提案
- 旧版互換レイヤーの提案

読み取り専用の`rg`、`git status`、`git diff`、ファイル閲覧は使用できます。現在の作業ツリーに既存変更がある場合は、内容を変更せず最初に報告してください。

## 報告形式

### 1. 現在状態

- Unity指定バージョン
- Cinemachine指定バージョン
- asmdef構成
- 作業ツリー状態

### 2. Cinemachine 2依存一覧

| ファイル | 使用中の旧API | 分類 | 次タスクでの扱い | 理由 |
| --- | --- | --- | --- | --- |

### 3. 参照が壊れるアセット

Prefab、Scene、Timeline、UnityEventごとに正確なパスを列挙してください。見つからない場合は、検索範囲と「未検出」を明記してください。

### 4. A/B最小実装の新規ファイル

最大5個。各ファイルの責務を1文で説明してください。

### 5. 推奨実装順序

各タスクについて、変更可能パス、対象外、完了条件、検証方法を記載してください。

### 6. ユーザー判断が必要な点

削除候補、既存アセット破損、複数の実装方法がある箇所だけを記載してください。判断不要なら「なし」としてください。

### 7. 検証境界

今回確認した内容と、Unity Import、Compile、Play Mode、Game Viewで未確認の内容を分けてください。

## 完了条件

- 変更が1件もない。
- Cinemachine 2依存ファイルと参照アセットが正確なパスで示されている。
- 次の実装タスクで許可する変更・削除範囲をユーザーが判断できる。
- 初期A/B以外の将来設計を追加していない。

---
