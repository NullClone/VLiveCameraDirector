# Motion仕様

## 1. 目的

Motion Presetを、位置SplineだけでなくBody、Timing、Aim、Composition、Lens、Roll、Activationを同期評価する再利用可能なCamera Performance Assetとして定義する。

Motionはオペレーターが選んだ演出を安定して再生し、必要な手動介入を非破壊に受け付ける。次Shotの判断、映像意図の推測、別軌道への自動変更は行わない。

## 2. 基本原則

- 1 Shotにつき1つの`VLiveCameraShot`と専用CinemachineCameraを使用する。
- Presetは再利用可能な設定だけを持ち、Runtime再生状態とScene参照を持たない。
- ShotはScene上のCamera、Spline、Cinemachine Target Group、Group Framing、適用済みMotionを持つ。
- Motion Playerだけが現在時刻、速度、方向、Hold、完了状態を持つ。
- Body、Aim、Composition、Lens、Rollは同じPlayback Timeを使用する。
- Motion Presetは作成者によらず同じAsset形式を使用する。
- Fixed Shotを安全な基準として残し、すべてのShotへ微動を加えない。
- Cinemachine 3とUnity Splinesの標準機能を優先する。

状態所有権とCinemachine境界は[architecture.md](architecture.md)を正本とする。

## 3. Motion Preset

`VLiveCameraMotionPreset`はScriptableObjectを正本とし、`VLiveCameraMotionPresetData`が次のTrackを束ねる。各Trackは独立したSerializable型とし、1ファイル1型で実装する。

### Identity

- 表示名
- FixedまたはMotion
- Motion Family
- Shot Size
- Energy
- 想定用途の短い説明

Family、Shot Size、EnergyはPaletteの検索とAI生成入力に使用する。分類をAssetフォルダ階層の正本にしない。

Shot Sizeは`Wide`、`Full`、`BustUp`、`CloseUp`、`FaceUp`とし、既定値は`BustUp`とする。これは焦点距離の別名ではなく、Target Groupへ含めるHumanoidボーンとGroup Framingの画面占有率を決める。

### Rig Profile

Presetは`VLiveCameraRigProfile`を参照できる。Profileは複数Presetで共有する機材応答とValidator推奨値だけを持ち、Aim、Lens、Timingなどの具体値をPresetと重複所有しない。

### Body Track

- Reference Transform相対の完全なSpline定義
- 開始距離と終了距離
- 初期方向
- OpenまたはClosed
- 基準Spline長

Fixed ShotはSplineを要求せず、初期Camera位置だけを使用する。

### Timing Track

- Clip Duration
- 単調なProgress Curve
- Scale Timing Mode
- 手動Speed Multiplierの許容範囲

### Aim and Composition Track

- Target Group基準のAim OffsetとCurve
- Screen PositionとCurve
- Dead ZoneとHard Limits
- Horizontal / Vertical Damping
- LookaheadとSmoothing
- Center On Activate

### Lens Track

- Field of ViewまたはFocal Length
- 開始値と終了値、またはLens Curve

Sensor Size、Gate Fit、Lens Shift、Near / Far Clip PlaneはMotion Presetへ保存せず、Rigの共通Physical Camera設定だけが所有する。Focus Distance、Iris、Exposureは現在含めない。

### Roll Track

- Horizon Mode
- Roll Curve

Spline UpとRoll CurveのどちらがRollを所有するかを明確にし、二重適用しない。HumanizationとCinemachine Noiseは現在のPresetへ一律追加しない。

### Activation Track

- Entry Mode
- In Time
- Out Time
- HoldまたはPost-rollの終了動作

## 4. Presetの具体化

Presetは原本であり、Rebuild時にRig Scale、正面基準、Rig Profileを解決してCamera、Spline、Cinemachine設定とShotの適用済みMotionへ具体化する。

Preset、Rig Profile、Rig Scaleの変更を生成済みShotへ暗黙伝播しない。Actor一覧はApply、Preset由来の値はRebuildで反映する。Runtime PlayerはPreset Assetを毎フレーム直接評価せず、Shotに適用済みの設定を評価する。Apply、Rebuild、Camera Settings一括適用の詳細は[authoring.md](authoring.md)を正本とする。

## 5. Spline定義

各Knotについて次を保存する。

- Position
- Tangent In
- Tangent Out
- Tangent Mode
- RotationまたはUp
- Auto Smooth Tension

Tangent Modeは`Linear`、`Mirrored`、`Continuous`、`Broken`、`AutoSmooth`を使用できる。Builderは設定をそのままScene Splineへ反映し、全Knotを一律にAuto Smoothへ変更しない。

### 基準空間

- 原点: RigのReference Transform位置。未指定時はCamera Director Root位置
- +Z: 被写体の正面側
- +X: 正面から見た右側
- +Y: World Up

Scene座標への変換にはReference Transform位置とRigの正面基準を使用する。Reference TransformのScaleは使用せず、Custom Referenceは位置ではなく向きだけを使用する。Camera軌道の基準と被写体Actorは独立させる。

### Scale

最初のKnotを`p0`、後続Knotを`pi`とする。

- Distance Scaleは`p0`の水平成分X/Zへ適用する。
- Horizontal Motion Scaleは`pi - p0`とTangentのX/Zへ適用する。
- Vertical Motion Scaleは`pi - p0`とTangentのYへ適用する。
- Vertical Motion Scaleが0の場合は始点の高さを保ち、軌道内の上下差分だけを無効にする。
- UpとRotationへScaleを適用しない。

Scale変更はRebuildで既存Shotへ反映する。通常のApplyとRuntime評価ではScene上のSplineを変更しない。

## 6. 再生計算

RuntimeではCinemachine Spline DollyをDistance単位で操作し、公開する移動速度はm/sとして扱う。

```text
phase = clipTime / clipDuration
progress = ProgressCurve(phase)
distance = startDistance + travelDistance * progress
clipTime += deltaTime
          * shotSpeedMultiplier
          * rigMasterPlaybackSpeed
          * direction
```

- PhaseとProgressは原則0から1とする。
- Clip Timeは0からClip Durationまでとする。
- Program区間はIn TimeからOut Timeまでとする。
- Progress Curveは原則単調とし、停止区間は水平区間で表す。
- Master Playback SpeedはPreset、Curve、Splineを書き換えない。
- Curveの値、接線、単調性の異常はEditorで警告する。
- 単一のSmoothStepを全Presetへ共用しない。

| Scale Timing Mode | 規則 |
| --- | --- |
| `PreserveDuration` | Scale後もDurationを維持する |
| `PreserveSpeed` | 基準Spline長との比率でDurationを調整する |

専用のJerk-Limited Trajectory Solverは、現在のCurve方式で不足すると映像比較から確認されるまで追加しない。

## 7. Entryと終了

```text
Pre-roll Handle | In Point | Program Motion | Out Point | Post-roll Handle
```

| Entry Mode | 動作 |
| --- | --- |
| `Static` | In Timeの状態から速度0で開始する |
| `Rolling` | In Timeの非0速度を保って開始する |
| `Continuous` | Off-Air中も継続する。現在のPaletteでは使用しない |

RollingはCut後の最初の表示区間から運動を継続する。Out Time到達後はPreset設定に従ってHoldまたはPost-rollを継続する。Live中のShotをResetまたはTeleportしない。切り替えとの関係は[operation.md](operation.md)を正本とする。

## 8. Aimと構図

```text
Rig Performers
      +-- Shot Slot Override (optional)
      -> Resolved VLivePerformer List
      -> Humanoid Bones
      -> Cinemachine Target Group
             |--> Rotation Composer Target Offset
             +--> Group Framing Center Offset and Dolly
```

`VLivePerformer`はHumanoid Animatorを参照し、次の簡潔な規則でTarget Group Memberを構築する。Eyes、顔ランドマーク、補助Proxy、Constraintは使用しない。

通常はRig共通のPerformer一覧を全Shotで使用する。Shot Slotの`Override Performers`が有効な場合だけ、そのShotでは共通一覧を使わずOverride一覧へ完全に置き換える。空のOverrideは被写体なしを明示する設定であり、Rig共通一覧へフォールバックしない。

| Shot Size | Target Group Member、Weight、Radius | Group Framing Size |
| --- | --- | --- |
| `Wide` | Head: 1.2 / Head Radius x 1.1、Bust: 1.0 / Bust Radius、Hips: 0.9 / Body Radius | 0.55 |
| `Full` | Head: 1.2 / Head Radius x 1.1、Bust: 1.0 / Bust Radius、Hips: 0.9 / Body Radius | 0.72 |
| `BustUp` | Head: 1.2 / Head Radius x 1.1、Bust: 1.0 / Bust Radius | 0.80 |
| `CloseUp` | Head: 1.2 / Head Radius x 1.1、Bust: 1.0 / Bust Radius x 0.6 | 0.90 |
| `FaceUp` | Head: 1.2 / Head Radius | 1.00 |

表中のBustはUpperChest、存在しない場合はChestを表す。Member表記は`Weight / Radius`とする。

- ActorごとにHead Radius、Bust Radius、Body Radiusを持ち、既定値は0.18m、0.35m、0.65mとする。
- UpperChestがないHumanoid AvatarではChestを使用する。
- 複数Actorの場合は全Actorの有効なMemberを同じTarget Groupへ登録する。
- 有効なHumanoidボーンMemberを1つも取得できないShotは無効とし、別Actorへ暗黙フォールバックしない。
- Target GroupはGroup Center、Manual Rotation、Late Updateを使用し、Manual RotationをRigの正面基準へ合わせる。

Aim OffsetはTarget Groupのローカル空間で`CinemachineRotationComposer.TargetOffset`へ適用する。Dead Zone、Hard Limits、Damping、Lookahead、Center On ActivateもRotation Composerへ適用する。

Screen Positionは`CinemachineGroupFraming.CenterOffset`へ適用し、Rotation ComposerのScreen Positionは中央に保つ。AimとGroup Framingで同じ画面オフセットを二重適用しない。

Group FramingはHorizontal and Vertical、Damping 1、Dolly Only、Change Rotation、Dolly Range -5mから+5mを既定とする。Actorの人数や間隔が変わってもTarget Group全体を収めるが、Field of ViewまたはFocal Lengthは変更しない。

Aim Offset CurveとScreen Position Curveは基準値への差分として評価する。Targetを常に中央へ固定せず、構図値はPresetの意図を使用する。

独自の反応遅延、Overshoot、構図Solverは、Cinemachine標準機能だけでは不足すると確認されるまで追加しない。

## 9. LensとRoll

全ShotとProgram CameraはPhysical Cameraを使用する。一般的なPresetはField of View、映画レンズを意識するPresetはFocal Lengthを保持できる。Focal Lengthは評価時点のRig共通Sensor Heightを用いて垂直Field of Viewへ変換し、Cinemachine Lensへ渡す。

RigはSensor Size、Gate Fit、Lens Shift、Near / Far Clip Planeを一括管理する。既定値は次とする。

| Setting | Default |
| --- | --- |
| Sensor Size | 36 x 24mm |
| Gate Fit | Horizontal |
| Lens Shift | (0, 0) |
| Near Clip Plane | 0.1m |
| Far Clip Plane | 1000m |

これらの共通値は明示的なCamera Settings適用操作でProgram Cameraと全Shotへ同期し、Shot固有のField of View、Focal Length相当の画角、Dutchを上書きしない。

LensはBodyと独立して評価できる。DollyとZoomを同じ操作として扱わず、意図的なDolly Zoomだけが同期させる。

Locked、Dolly、Pedestalは水平維持を既定とする。Jib、Crane、Handheld、Robotic、VirtualはPresetが明示した場合だけRollを使用する。

## 10. Rig Profileと手動介入

Rig Profileは次へ使用する。

- Preset作成時の初期値
- Rebuild時に適用するSpeed、Hold、Resume、Reverseの応答
- Validatorの推奨範囲

メーカー公称最高速度を映像品質の上限にせず、値は検証開始点として扱う。Profile変更はRebuildでのみ既存Shotへ反映する。

手動操作はPreset Assetを書き換えず、適用済みMotionへ非破壊に合成する。

- SpeedはPlayback Timeの進み方だけを変える。
- Holdは設定された減速で停止する。
- Resumeは現在位置から連続的に再開する。
- Reverseは減速、停止、逆方向への加速として処理する。
- Freezeだけが緊急時の即時停止を許容する。

## 11. Motion Validator

Validatorは映像品質の採点器ではなく、破綻候補を見つけるEditor診断である。Runtimeと同じMotion Evaluatorを使用し、Preset診断時は必要な設定をメモリ上で解決してSceneを変更しない。

自動診断する項目:

- NaN、Infinity、参照欠落
- Progress Curveの範囲、単調性、接線
- 移動速度、加速度、Jerk
- 角速度、角加速度
- 曲率とTangentの急変
- In / Out Pointの速度
- Lens変化速度
- AimのScreen Space位置
- Horizon Roll
- Target GroupのBounding Sphereとの最短距離
- Near Clip侵入
- Duration、Spline長、Rig Scaleの不整合

人が判断する項目:

- 動きの動機
- 楽曲、振付、照明との相性
- 機材らしさと有人感
- 前後Shotとのつながり
- Foreground Occlusionの意図
- 長時間視聴時の疲労

Validatorの閾値は検証開始値であり、警告を合否へ変換しない。Asset、Spline、Duration、Aim、Lensを暗黙修正しない。

## 12. Runtime安全境界

- 無効なMotion ShotをProgramへ選択しない。
- 選択失敗時は現在のProgramを維持する。
- NaNまたはInfinityをCinemachineへ適用しない。
- Target Groupが空、または有効なHumanoidボーンがないShotは無効として扱う。
- Spline欠落時はMotion Shotを無効として扱う。
- 異常時に別Target、別Shot、別軌道へ自動切り替えしない。
- RuntimeでScene、Spline、Assetを生成、削除、保存しない。

## 13. 初期Palette

現在の10 Assetを3D Motion Paletteの候補とする。

1. FixedMedium
2. PushIn
3. PullOut
4. TruckLeft
5. TruckRight
6. ArcAround
7. PushInRolling
8. CraneRise
9. CraneDrop
10. PedestalRise

これらはGold Masterではない。ユーザーの作業用SceneでGold、Experimental、Rejectを判断してから、左右、距離、Duration、Lens、EnergyのVariantを増やす。Preset数より先にEntry、Body、Aim、Lens、終了、手動介入を確認する。

## 14. 不変条件

1. Presetが完全なSpline、Timing、Aim、Composition、Lens、Roll、Activationを保持できる。
2. Distance、Duration、Progress CurveからCamera位置を評価する。
3. 全Motionチャンネルが同じPlayback Timeを使用する。
4. StaticとRollingが異なるIn Point状態から開始できる。
5. Speed、Hold、Resume、Reverseで位置と速度が不連続に飛ばない。
6. 全Knotを暗黙にAuto Smoothへ変更しない。
7. Preset原本、適用済みMotion、Runtime再生状態を混同しない。
8. Vertical Motion ScaleとMaster Playback SpeedがAssetを変更しない。
9. Validatorの警告値と映像上の合否を混同しない。
10. Sensor SizeをMotion Presetへ保存せず、Rig共通値からFocal Lengthを評価する。
11. Actor構図にAim Proxyを使用せず、HumanoidボーンをCinemachine Target Groupへ直接登録する。
