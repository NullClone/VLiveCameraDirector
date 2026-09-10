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
- +Zを被写体正面側とする基準空間の相対制御点
- Field of View
- Rigの注視基準からの構図オフセット
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

Presetの制御点は次の基準空間で定義する。

- 原点: Performer Targetの位置
- +Z: 被写体の正面側、つまり正面カメラを置く側
- +X: 正面を見たときの右側
- +Y: World Up

Rigは次の正面基準Modeを持つ。

| Mode | 正面方向 |
| --- | --- |
| `TargetForward` | TargetのforwardをXZ平面へ射影した方向。既定値とする |
| `WorldPlusZ` | World +Z |
| `WorldMinusZ` | World -Z |
| `CustomReference` | 指定TransformのforwardをXZ平面へ射影した方向 |

Custom Referenceは位置ではなく向きだけを使用する。XZ射影した正面方向がほぼ0の場合はInspectorで警告し、勝手に別Targetを探索しない。

Scene座標への変換にはTargetの位置と選択した正面方向を使用し、Target TransformのScaleを使用しない。非一様ScaleやAvatar Import Scaleによってカメラ距離とSpline形状が変化してはならない。

### 5.1 Target Heightと構図Offset

`Target Height`はTarget原点からWorld Up方向へ加える注視基準の高さとする。PresetのTarget Offsetはその注視基準からの構図上の追加差分とする。両方に同じ既定身長を重複保存しない。

初期PresetではTarget Offsetの高さを0とし、既定の注視高さはRigのTarget Heightだけが所有する。

### 5.2 Distance ScaleとMotion Scale

最初の制御点を`p0`、後続点を`pi`とする。

- `Distance Scale`は`p0`の水平成分X/Zに適用し、Targetからカメラ開始位置までの距離を変える。Yは変えない。
- `Motion Scale`は`pi - p0`に適用し、開始位置を保ったまま移動幅を変える。
- いずれも0より大きい値とし、既定値は1とする。

この2つを同じ制御点全体へ重ねて乗算しない。Scale変更は明示的なRebuildで既存Shotへ適用し、Inspectorを動かしただけでは手動調整済みSplineを変更しない。

### 5.3 Cinemachine

- CinemachineCameraはTargetをTracking Targetとして使用する。
- PresetのField of View、RigのTarget Height、PresetのTarget Offsetを初期構図として適用する。
- FixedはSplineを要求しない。
- Spline移動にはCinemachine 3とUnity Splinesの標準機能を使用する。
- 独自の経路Solverや遮蔽回避は作らない。

Targetが大きく移動するライブへの追従方法は、初期PaletteをSceneで確認してから決める。

## 6. 初期Presetの更新

6つの同梱Presetは、正面側が+Zとなる座標へ更新する。新規Presetを作るEditor処理も同じ値を使用する。

- Package同梱の6 Assetだけを仕様変更として更新する。
- ユーザーが複製または作成したPresetを自動更新しない。
- Windowを開く、Domain Reloadする、Rigを選択するだけでPreset Assetを書き換えない。
- 既存Assetを初期化する処理と、不足Assetを作成する処理を同じ暗黙動作にしない。

## 7. 再生規則

- 移動ShotはOff Air中に始点で準備する。
- ProgramへCutされた後に自動再生を開始する。
- 終点へ近づくと減速し、到達後はHoldする。
- Off Airになってから次回用に始点へ戻す。
- Live中のShotをResetまたはTeleportしない。
- 同一Shotの再選択では既定で状態を変更しない。

Fixed Shotは選択後も設定された構図を維持する。

## 8. 手動操作

### Speed

現在のSpline進行速度を範囲内で変更し、位置を飛ばさない。

### Reverse

現在位置を保持したまま進行方向を反転する。

### Hold / Resume

Holdは現在位置でSpline進行だけを止める。Resumeは現在の方向と速度で再開する。Hold中もCinemachineのTrackingとAimは継続できる。

Fixed Shotへの移動操作は何も変更せず、安全に無視する。

## 9. 異常時

- TargetまたはSplineが無効でも例外を繰り返し発生させない。
- 無効な移動ShotをProgramへ選択しない。
- 設定不足を別Targetの自動探索で補わない。
- 失敗時は現在のProgramを維持する。

## 10. 受け入れ条件

1. 6つのPreset AssetをPaletteから選択できる。
2. 各生成Shotが専用CinemachineCameraを持つ。
3. 移動ShotがCut後に始点から終点へ動いてHoldする。
4. Speed、Reverse、Hold、Resumeで位置が飛ばない。
5. Fixed Shotが安全な戻り先になる。
6. Presetに現在不要なAI用metadataや管理機能がない。
7. TargetのTransform Scaleを変えても、同じRig Scale設定ならカメラ距離と軌道形状が変わらない。
8. Distance Scaleが開始距離だけを、Motion Scaleが開始点からの移動幅だけを変更する。
