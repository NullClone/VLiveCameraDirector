# スイッチング・送出仕様

## 1. 目的

本仕様は、Camera Bank、Program、Preview、Take、Cut、Transition、Tally、Multiview の状態と動作を定義する。

## 2. 状態モデル

| 状態 | 意味 |
| --- | --- |
| Selected | 現在 Bank で操作候補として選択されている Shot |
| Preview | 次の送出候補として評価・表示される Shot |
| Program | 現在送出中の Shot |
| Transitioning | 旧 Program から新 Program へ遷移中 |
| Standby | 登録済みだが Program / Preview ではない Shot |
| Unavailable | 参照不足、未ロード、無効化等で使用できない Shot |

Program と Preview は一意とする。Tally はこの正本状態から派生させ、UI、MIDI LED、外部連携が個別に状態を持たない。

## 3. 選択方式

### 3.1 Preview / Take

1. `SelectCamera` で Preview 候補を選ぶ。
2. Preview 側で Shot の開始状態を評価・表示する。
3. `Take` で選択済み Transition を用いて Program へ送出する。

### 3.2 Direct Cut

`Cut` は指定 Shot を最短経路で Program へ送出する。緊急操作として Bank 内の直接番号選択と組み合わせられる。

Direct Cut の有効・無効は運用モードで明示し、誤操作を防ぐ。自動 Cut を既定にしない。

## 4. Take の処理順序

Take は論理的に次の順で行う。

1. Preview Shot の可用性を再確認する。
2. ターゲット、Lane、Pattern、Lens の準備完了を確認する。
3. 開始ポリシーから Take 時点の Camera State を確定する。
4. 旧 Program と新 Program の Transition を解決する。
5. Program 状態と Tally の遷移を開始する。
6. Cinemachine Blend または映像遷移を実行する。
7. 完了後、旧 Program を Standby / Hold / Exit の設定へ移す。
8. 次の Preview 候補を運用設定に従って選ぶ。

途中で検証に失敗した場合は Program を維持し、失敗理由を表示する。

## 5. 同一 Shot の再選択

- Program と同じ Shot を Preview に選択することは許可するが、状態を明示する。
- 同一 Shot への Take は既定では No-op とし、意図しない Pattern Restart を防ぐ。
- `Retrigger` を明示した場合だけ、開始ポリシーに従って Pattern を再開できる。
- Direct Cut で同一 Shot が指定された場合も、既定では Program 状態を維持する。

## 6. Transition

Transition は次の二層に分ける。

### 6.1 Camera State Transition

Cinemachine Brain が位置、回転、Lens 等を補間する Cut / Blend。Blend Definition、Duration、Curve を保持する。

### 6.2 Video Transition

複数の描画結果を用いる Crossfade、Fade、Wipe 等。RenderTexture、合成、色空間、解像度、遅延を別途管理する。

二つを同じ `Blend` 名だけで表現しない。Shot の推奨値、運用者の選択、緊急 Cut の優先順位を明確にする。Cut は常に利用可能とする。

## 7. Preview と Prewarm

Preview は Take した瞬間の画を信頼できるよう、Program と同じ Shot 計算を用いる。

- ターゲット解決、Spline 評価、Lens、Focus、Noise Seed を準備する。
- `RestartOnTake` では Entry の開始画を確認できる Preview モードを用意する。
- `ShowTimeSync` / `BeatSync` では Take 予測時刻との差を表示する。
- Preview だけで Program の Pattern 時間や手動トリムを変更しない。
- Prewarm 未完了時は状態を表示し、設定に応じて Take 拒否または Cut 継続を選べる。

## 8. Tally

最低限、次の表示を統一する。

- Program: 赤
- Preview: 緑
- Selected だが Preview 未確定: 運用 UI のアクセント色
- Transitioning: 旧・新 Shot の遷移状態が分かる表示
- Unavailable / Degraded: 警告色と理由

色だけに依存せず、ラベル、枠、アイコンを併用する。将来の MIDI LED と外部 Tally は同じ状態スナップショットから出力する。

## 9. Multiview

Multiview は大量 Shot の監視手段であり、すべてを Program 品質で常時描画しない。

- Program と Preview は優先的に高品質・高頻度で更新する。
- その他は解像度、更新頻度、ポスト処理、表示台数を段階化する。
- 表示外 Bank はサムネイルまたは状態だけにできる。
- 遅延した Multiview 画像と実際の Shot 状態を混同しないよう更新状態を示す。
- 負荷超過時も Program のフレーム時間と入力応答を優先する。

具体的な同時描画数と解像度は、実ステージ相当 Scene で GPU / CPU / メモリを測定して決定する。

## 10. 自動化の境界

初期版は `SelectCamera`、`Take`、`Cut` を人が実行する。次候補の自動 Preview は Recommendation 段階で追加できるが、Program を自動変更しない。

将来の半自動 Take は、対象区間、許可 Shot、遷移、停止条件が明示的に設定され、オペレーターが有効化した場合だけ動作する。手動 Take、Cut、Master Hold は常に上位の緊急操作として扱う。

## 11. 受け入れ条件

1. Program、Preview、Selected、Unavailable を常に識別できる。
2. 無効な Preview への Take で現在の Program が失われない。
3. Preview と Take 後の開始画が、開始ポリシーの範囲で一致する。
4. Cut、Cinemachine Blend、Video Transition を独立して選択・検証できる。
5. 同一 Shot 操作で意図しない Pattern Restart が起きない。
6. Multiview 負荷が Program の画と入力応答を阻害しない設定を選べる。
7. 論理 Camera Bank が 64 台以上を扱えても、同時高品質描画を要求しない。
