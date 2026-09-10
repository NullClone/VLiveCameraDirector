# カメラとMotion Palette仕様

## 1. 目的

この文書は、Shot、Motion Preset、初期Palette、移動中の手動操作を定義する。

## 2. ShotとPreset

- 1 Shotにつき1つの`VLiveCameraShot`と専用CinemachineCameraを使用する。
- ShotはScene上の構図とRuntime状態を持つ。
- `VLiveCameraMotionPreset`は再利用可能なFixedまたはSpline移動設定を持つ。
- Live中の同じCinemachineCameraへ別ShotのPresetを上書きしない。
- AIは将来、人と同じPreset Assetを作成する。AI専用Runtime経路は作らない。

## 3. Preset v0

最初のPreset Assetは次だけを持つ。

- 表示名
- FixedまたはSpline
- Targetローカル基準の相対制御点
- Field of View
- Tracking Targetのオフセット
- 初期進行速度
- 終端への減速距離または進行Curve

検索用カテゴリ、タグ、サムネイル、生成者、評価、バージョン、AI metadataは追加しない。現在速度、方向、Hold、Live状態も保存しない。

## 4. 初期Palette

最初は次の6 Presetを用意する。

| 番号 | Preset | 動き |
| --- | --- | --- |
| 1 | Fixed Medium | 正面ミドルの安定した戻り先 |
| 2 | Push In | 正面からTargetへ近づく |
| 3 | Pull Out | Targetから後方へ引く |
| 4 | Truck Left | Targetを捉えたまま右から左へ移動する |
| 5 | Truck Right | Targetを捉えたまま左から右へ移動する |
| 6 | Arc Around | Targetの周囲を短い弧で回り込む |

値はプロ水準の完成値ではなく、ユーザーのSceneで調整を始められる安全で分かりやすい初期値とする。

## 5. 座標と構図

- 相対制御点はSetup実行時のTarget Transformを基準にScene座標へ変換する。
- CinemachineCameraはTargetをTracking Targetとして使用する。
- PresetのField of ViewとTarget offsetを初期構図として適用する。
- FixedはSplineを要求しない。
- Spline移動にはCinemachine 3とUnity Splinesの標準機能を使用する。
- 独自の経路Solverや遮蔽回避は作らない。

Targetが大きく移動するライブへの追従方法は、初期PaletteをSceneで確認してから決める。

## 6. 再生規則

- 移動ShotはOff Air中に始点で準備する。
- ProgramへCutされた後に自動再生を開始する。
- 終点へ近づくと減速し、到達後はHoldする。
- Off Airになってから次回用に始点へ戻す。
- Live中のShotをResetまたはTeleportしない。
- 同一Shotの再選択では既定で状態を変更しない。

Fixed Shotは選択後も設定された構図を維持する。

## 7. 手動操作

### Speed

現在のSpline進行速度を範囲内で変更し、位置を飛ばさない。

### Reverse

現在位置を保持したまま進行方向を反転する。

### Hold / Resume

Holdは現在位置でSpline進行だけを止める。Resumeは現在の方向と速度で再開する。Hold中もCinemachineのTrackingとAimは継続できる。

Fixed Shotへの移動操作は何も変更せず、安全に無視する。

## 8. 異常時

- TargetまたはSplineが無効でも例外を繰り返し発生させない。
- 無効な移動ShotをProgramへ選択しない。
- 設定不足を別Targetの自動探索で補わない。
- 失敗時は現在のProgramを維持する。

## 9. 受け入れ条件

1. 6つのPreset AssetをPaletteから選択できる。
2. 各生成Shotが専用CinemachineCameraを持つ。
3. 移動ShotがCut後に始点から終点へ動いてHoldする。
4. Speed、Reverse、Hold、Resumeで位置が飛ばない。
5. Fixed Shotが安全な戻り先になる。
6. Presetに現在不要なAI用metadataや管理機能がない。
