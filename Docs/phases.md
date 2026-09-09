# 実装フェーズと現在地

## 1. この文書の役割

この文書だけが、目標仕様と現在実装の差分、実装順序、各段階の完了条件を管理する。仕様書に記載された機能を、ここで完了と確認する前に実装済みとして扱わない。

## 2. 現在実装のスナップショット

2026-09-09 の文書作成時点で確認できた状態は次のとおり。

- `package.json` は Unity 2022.3、Cinemachine 2.9.7 を指定している。
- Runtime では Cinemachine 2 の `CinemachineVirtualCamera`、Transposer、Composer 等を使用している。
- namespace は `toshi.VLiveKit.Photography`、`toshi.VLiveKit`、`toshi.VLiveKit.VLiveCameraUnit`、global namespace が混在している。
- 既存 `VLiveCamera` には Look / Follow、Dolly、Lens、Noise、Impulse、Preset 等の有用な要素がある。
- 既存 `VLiveCameraSwitcher` は Unity Camera の状態を Program Camera へ毎フレーム転記する方式である。
- 数字キーによる直接選択は最大 9 Shot で、Preview / Take / Bank の状態モデルはまだない。
- ランダム Auto Cut が既定で有効になっている。
- 論理 64 台以上の Bank、低負荷 Multiview、共通操作コマンド、MIDI Adapter はまだ目標仕様である。

これは移行前の観測結果であり、既存機能の品質評価や削除判断ではない。Prefab、Scene、Timeline、外部利用箇所を監査するまで、破壊的な置換を行わない。

## 3. Phase 0 — 仕様の基準化

状態: 文書初版を作成。コード変更と Unity 実機検証は未実施。

成果物:

- 製品コンセプトと初期スコープ
- 操作、Camera Pattern、支援、Switching の目標仕様
- 技術基盤、namespace、責務、実装順序
- 文書の正本と検証ワークフロー

完了条件:

1. ユーザーが製品思想と初期スコープを承認する。
2. 用語と文書の担当範囲に重大な重複がない。
3. 未決定事項を実装済みの事実として記載していない。

## 4. Phase 1 — 現状監査と移行設計

目的: 既存資産を失わずに Unity 6.3 / Cinemachine 3 へ移行する計画を確定する。

主な作業:

- Runtime、Editor、Tests、Develop、Prefab、Scene、Timeline の利用関係を棚卸しする。
- 公開 API、SerializedProperty、UnityEvent、Animation Clip、Timeline Binding を検索する。
- 既存機能を Keep、Adapt、Replace、Retire に分類する。
- Unity 6.3 と使用する Cinemachine 3 の正確なバージョンを固定する。
- namespace と asmdef の移行方法、`MovedFrom` 等の互換策を決める。
- package metadata、README、Sample、依存関係の更新範囲を決める。
- 基準 Scene と操作シナリオを保存し、移行前の挙動を記録する。

完了条件:

1. 既存 Scene / Prefab を壊す可能性のある変更点が一覧化されている。
2. Cinemachine 2 から 3 への型・機能対応表がある。
3. 最初の実装単位と Rollback 方法が承認されている。
4. 旧機能を削除する場合は個別に承認されている。

## 5. Phase 2 — Cinemachine 3 基盤

目的: 単一 Shot を新しい責務分割と Cinemachine 3 で安全に動かす。

主な作業:

- Unity 6.3 / Cinemachine 3 対応と asmdef 整理
- namespace を `toshi.VLiveKit.Camera` 系へ統一
- Camera Registry、Shot ID、Switcher State の最小モデル
- CinemachineCamera と Spline Dolly を用いた基準リグ
- Camera State Coordinator と単一 Writer
- 既存 Look / Follow / Lens / Noise の移植
- Edit Mode / Play Mode の基礎テスト

完了条件:

1. 基準 Scene が Import、Compile、Play できる。
2. Fixed と Dolly の 2 種類を Program へ Cut できる。
3. Transform、Lens、Spline Position に競合 Writer がない。
4. 既存参照の移行結果がログと手動確認で追跡できる。

## 6. Phase 3 — Guided Manual の核

目的: 切り替えだけで動き、必要なら手動で演奏できる最小完成形を作る。

主な作業:

- Shot、Lane、Motion Pattern のデータモデル
- Entry / Main / Exit / Hold と `RestartOnTake`
- Keyboard Input Adapter と共通操作コマンド
- Rail Drive / Reverse / Hold、Pan / Tilt / Zoom 等の手動トリム
- チャンネル別合成、制限、滑らかな復帰
- Target Resolver と異常時 Hold
- 操作状態モニター

完了条件:

1. 操作なしでも Fixed、Dolly、Orbit 等の代表 Shot が成立する。
2. キーボード介入の開始・解放で画が飛ばない。
3. ターゲット消失と入力フォーカス喪失で暴走しない。
4. 30 分以上の反復切り替えで状態破損がない。

## 7. Phase 4 — ライブスイッチャー

目的: 大量 Shot を現場運用できる Program / Preview 系へ発展させる。

主な作業:

- Camera Bank / Page と論理 64 Shot 以上の登録
- Program / Preview / Selected / Tally
- Preview / Take と Direct Cut
- Cinemachine Blend と Video Transition の分離
- Preview Prewarm
- 品質階層を持つ Multiview
- Bank、状態、警告を表示する Operator UI

完了条件:

1. 64 Shot 以上の登録・検索・Bank 操作ができる。
2. 無効 Shot への Take で Program が失われない。
3. Preview と Take 後の開始画が一致する。
4. Multiview 有無の性能差を計測し、Program 優先設定を確認する。

## 8. Phase 5 — Pattern 制作と AI 量産基盤

目的: 大量のプロ水準 Camera Pattern を安全に制作・分類・再利用する。

主な作業:

- Camera Grammar とデータスキーマの確定
- Pattern Authoring Window、Preview Scene、Validator
- Variation、Seed、タグ、サムネイル、作者・生成元 metadata
- Loop、速度、角速度、Lens、参照、範囲の自動検査
- AI が出力できる交換形式と Importer
- 人による Review / Approved 状態

完了条件:

1. C# 追加なしで新しい Pattern を登録できる。
2. AI 生成と手作業の Pattern が同じ検証を通る。
3. 代表対象とステージで目視 Review できる。
4. 大規模ライブラリでも重複、互換性、品質状態を追跡できる。

## 9. Phase 6 — MIDI 運用

目的: キーボードで完成した操作体系を、現場向け物理コントローラーへ拡張する。

主な作業:

- MIDI Input Adapter と Mapping Profile
- Note、CC、相対 Encoder、複数デバイス
- Soft Takeover / Pickup
- Tally、選択、Pickup の LED Feedback
- Hot Plug、切断、再接続
- 機種別 Profile をコア Runtime から分離

完了条件:

1. キーボードと MIDI が同じ操作結果を生む。
2. 接続、Bank 変更、Take 後に絶対値 Fader で値が飛ばない。
3. MIDI を外しても Program と Pattern が継続する。
4. 代表的な長時間本番操作で入力欠落と残留を測定する。

## 10. Phase 7 — Cue 支援と Recommendation

目的: 手動運用を維持しながら、楽曲同期と次 Shot 選択の負担を減らす。

主な作業:

- Show Time / Beat Time / Timeline 同期
- Pattern Cue と Entry / Exit の事前準備
- 次 Shot の候補提示と反復回避
- 出演者、曲セクション、Shot 履歴の利用
- 候補理由、禁止 Shot、優先 Shot の表示
- Manual Override と Master Hold

完了条件:

1. Cue 支援が無効でも Phase 4 の全運用が成立する。
2. 推薦は Preview へ提示され、無断で Program を変更しない。
3. BPM 変更、Seek、Pause、再開で Pattern が破綻しない。
4. 候補の理由と除外条件をオペレーターが確認できる。

## 11. Phase 8 — Production Validation

目的: 実ステージ、本番時間、実オペレーターでプロ運用の成立を検証する。

検証項目:

- 代表楽曲を通した構図、切り替え、操作感
- 初見運用者と熟練運用者の誤操作・習熟時間
- CPU、GPU、GC、メモリ、RenderTexture、入力遅延
- 60 分以上の連続運用、Scene 切り替え、デバイス再接続
- Build Player、解像度、フレームレート、レンダーパイプライン
- Pattern ライブラリの品質と検索性
- 障害時の Program Hold、Cut、復旧

完了条件は対象公演の制作条件とハードウェアを固定して数値化する。Editor の Compile 成功だけで本番品質を証明したことにしない。

## 12. フェーズ運用ルール

- 次フェーズの試作は可能だが、前提条件を省略して完了扱いにしない。
- 実装により仕様変更が必要になった場合は、先にユーザーへ判断材料を提示する。
- 各フェーズ完了時に、実装済み、未実装、目視未確認、性能未測定を分けて更新する。
- 既存機能の削除や Scene / Prefab の一括移行は、個別承認を得る。
