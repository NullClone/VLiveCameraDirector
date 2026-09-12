# カメラとMotion Preset仕様

## 1. 目的

この文書は、Shot、Motion Preset、機材特性、再生計算、構図、Lens、初期Paletteを定義する。

VLiveCameraUnitのMotion Presetは単なる位置Splineではない。位置、時間、Aim、構図、Lens、Roll、開始状態を同期評価し、オペレーターの非破壊な手動操作を受け付ける再利用可能なCamera Performance Assetとする。

## 2. 基本原則

- 1 Shotにつき1つの`VLiveCameraShot`と専用CinemachineCameraを使用する。
- Live中の同じCinemachineCameraへ別ShotのPresetを上書きしない。
- `VLiveCameraMotionPreset`は再利用可能な設定だけを持ち、現在時刻、速度、方向、Hold、Live状態を持たない。
- ShotはScene上のCamera、Spline、Aim Proxy、適用済みMotion設定とOn-Air lifecycleを持つ。
- Motion PlayerだけがCamera PerformanceのRuntime再生状態を持つ。
- AIと人は同じMotion Preset形式を作成する。AI専用Runtime経路は作らない。
- 実機材の制約は動きの語彙と検証基準に使用し、剛体物理シミュレーターは作らない。
- 固定画を安全な基準として残し、すべてのShotへ無目的な微動を加えない。

## 3. Motion Preset

`VLiveCameraMotionPreset`はScriptableObjectを正本とし、`VLiveCameraMotionPresetData`が次の具体的なTrackを束ねる。Identity、Body、Timing、Aim、Lens、Roll、Activationはそれぞれ独立したSerializable型とし、1ファイル1型で実装する。

### Identity and Intent

- 表示名
- FixedまたはMotion
- Motion Family
- Shot Size
- Energy
- 想定用途の短い説明

Motion Family、Shot Size、Energyは初期Paletteの識別とAI生成時の入力に使用する。生成者、評価履歴、自然言語全文、バージョン管理情報は現在保存しない。

### Rig Profile

- `VLiveCameraRigProfile`参照

Rig Profileは共有する機材応答と推奨範囲を持つ。Aim、Lens、TimingなどPresetが保存する具体値と同じ値を二重に持たない。ProfileのRuntime応答値はRebuild時にShotへ適用し、Profile Assetの編集を生成済みShotへ暗黙伝播しない。

### Body Track

- Target相対のSpline定義
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

- Target基準からのAim Offset
- Aim Offset Curve
- Screen Position
- Screen Position Curve
- Dead Zone
- Hard Limits
- Horizontal / Vertical Damping
- LookaheadとSmoothing
- Center On Activate

### Lens Track

- Field of ViewまたはFocal LengthのMode
- 開始値と終了値、またはLens Curve
- Physical Mode時のSensor SizeとGate Fit

Focus Distance、Iris、ExposureはMotion Foundationの対象外とする。必要性を確認した後、Lens Trackの任意チャンネルとして追加する。

### Roll Track

- Horizon Mode
- Roll Curve

HumanizationとCinemachine Noiseは現在のPresetデータへ含めない。必要性を映像で確認した後、Locked、Dolly、Pedestal、Roboticへ一律適用せず追加する。

### Activation

- Entry Mode
- Clip内のIn Time
- Clip内のOut Time
- 終了時にHoldするか、Post-rollを継続するか

Presetは再利用可能な原本である。Rebuild時にRig Scale、正面基準、Rig Profileを解決し、Camera、Spline、Cinemachine設定とShotの適用済みMotion設定へ具体化する。RuntimeのMotion Playerはこの解決済み設定を評価する。Preset、Rig Profile、Rig Scale、Slot参照の変更を生成済みShotへ暗黙伝播しない。

## 4. Spline定義

Presetへ`Vector3[]`だけを保存せず、各Knotについて次を保持する。

- Position
- Tangent In
- Tangent Out
- Tangent Mode
- RotationまたはUp
- Auto Smooth Tension

Tangent Modeは`Linear`、`Mirrored`、`Continuous`、`Broken`、`AutoSmooth`を使用できる。BuilderはPresetの設定をそのまま生成Splineへ反映し、全Knotを一律にAuto Smoothへ変更しない。

### 基準空間

PresetのSplineとCamera位置は次の基準空間で定義する。

- 原点: Performer Targetの位置
- +Z: 被写体の正面側、つまり正面カメラを置く側
- +X: 正面を見たときの右側
- +Y: World Up

Scene座標への変換にはTarget位置とRigの正面基準を使用し、Target TransformのScaleを使用しない。Custom Referenceは位置ではなく向きだけを使用する。

### Distance Scaleと軸別Motion Scale

最初のKnotを`p0`、後続Knotを`pi`とする。

- `Distance Scale`は`p0`の水平成分X/Zへ適用する。
- `Horizontal Motion Scale`は`pi - p0`とTangentのX/Zへ適用する。
- `Vertical Motion Scale`は`pi - p0`とTangentのYへ適用する。
- `Vertical Motion Scale = 0`では始点の高さを保ち、軌道内の上下差分だけを無効にする。
- UpとRotationへScaleを適用しない。

Scale変更は明示的なRebuildで生成済みShotへ適用する。通常のApplyとRuntime評価では手動調整済みSplineを変更しない。

## 5. 再生計算

RuntimeではCinemachine Spline Dollyを`Distance`単位で操作する。オペレーター向けの移動速度はm/sとして扱い、Spline長に依存する正規化位置/秒を公開速度にしない。

基本評価は次とする。

```text
phase = clipTime / clipDuration
progress = ProgressCurve(phase)
distance = startDistance + travelDistance * progress
clipTime += deltaTime * shotSpeedMultiplier * rigMasterPlaybackSpeed * direction
```

- PhaseとProgressは原則0から1の範囲とする。
- Clip Timeは0からClip Durationまでとし、Programへ出る区間はIn TimeからOut Timeまでとする。
- Progress Curveは原則単調とし、停止区間は水平区間で表す。
- Position、Aim、Composition、Lens、Rollは同じ時刻から別々に評価する。
- Master Playback SpeedはRig全体へのライブ倍率であり、Preset Duration、Progress Curve、Splineを変更しない。
- Curveの接線や値が範囲を超える場合はEditorで警告する。
- 単一のSmoothStepを全Presetへ共用しない。

### Scale Timing Mode

| Mode | 規則 |
| --- | --- |
| `PreserveDuration` | Scale後もDurationを維持する。音楽やCueに時間を合わせる場合に使用する |
| `PreserveSpeed` | 基準Spline長との比率でDurationを調整し、移動速度感を維持する |

Jerk制限の専用Trajectory Solverは初期必須としない。まずCurveから速度、加速度、Jerkを計測し、映像比較で必要性が確認された場合だけ追加する。

## 6. Entryと終了

Motion Presetは、Clip Duration内のIn TimeとOut Timeによって、映像へ出る範囲の前後にHandleを持てる。

In PointはIn Timeで評価したCamera Performanceの状態、Out PointはOut Timeで評価した状態を指す。

```text
Pre-roll Handle | In Point | Program Motion | Out Point | Post-roll Handle
```

| Entry Mode | 動作 |
| --- | --- |
| `Static` | In Timeで速度0から開始する |
| `Rolling` | In Timeですでに非0の速度を持ち、その速度から継続する |
| `Continuous` | Off-Air中も継続する特殊Shot。初期Paletteでは使用しない |

直接CutではShotをIn Timeの位置、Aim、Lensへ準備する。RollingはCut後の最初の表示区間から非0速度で進み、必ず停止状態から発進する見え方を避ける。

将来Preview / Takeを追加した場合は、Preview選択後にPre-roll Handleを実時間で再生できる。同じPreset形式とMotion Playerを使用し、Rolling専用の別Runtime経路は作らない。

Out Time到達後はPreset設定に従ってHoldまたはPost-rollを継続する。別ShotへCutされた後にだけ次回用のIn Timeへ準備する。Live中のShotをResetまたはTeleportしない。

## 7. Aimと構図

各Shotは専用のAim Proxy Transformを持つ。

```text
Performer Target
      -> Applied Target Height + Aim Offset
      -> Aim Proxy
      -> Cinemachine Rotation Composer
```

Aim Proxyは保存状態の正本ではなく、現在のTarget PoseとShotへ適用済みのRig設定、Aim評価から導出する。これによりCamera位置とAim位置を分離し、Target原点を機械的に画面中央へ固定しない。

Cinemachine Rotation Composerへ、PresetのScreen Position、Dead Zone、Hard Limits、Damping、Lookaheadを適用する。有人感は最初にこれらの標準機能で作り、独自の反応遅延、Overshoot、構図SolverはCinemachineだけでは不足すると確認されるまで追加しない。

Aim Offset CurveとScreen Position Curveは各軸の基準値へ加算する差分として評価する。Curveを設定しただけで基準構図を置き換えない。

Target Heightの入力値はRigが所有し、Rebuild時にShotへ適用する。PresetのAim Offsetはその基準からの追加差分とし、両方に同じ身長を保存しない。

## 8. LensとRoll

### Lens

一般的なPresetはField of Viewを使用できる。映画レンズを意識するPresetはFocal LengthとSensor Sizeを使用できる。Physical Cameraを全Presetへ強制しない。

Lens値はBody Trackと独立して動かせる。DollyとZoomを同じ操作として扱わず、意図的なDolly Zoomだけが両方を同期させる。

### Roll

- 通常のLocked、Dolly、Pedestalは水平維持を既定とする。
- Jib、Crane、Handheld、Robotic、VirtualはPresetが明示した場合だけRollを使用する。
- Spline UpとRoll CurveのどちらがRollを所有するかをPreset内で明確にし、二重適用しない。

## 9. Rig Profile

`VLiveCameraRigProfile`は機材らしさを複数Presetで共有するScriptableObjectとする。

対象候補:

- Locked Tripod
- Fluid Head
- Dolly
- Pedestal
- Jib / Crane
- Gimbal / Steadicam
- Handheld
- Cable
- Robotic
- Virtual

現在のProfileが持つもの:

- 推奨速度、角速度、加速度、Jerkの範囲
- 手動Speed変更、Hold、Resume、Reverseへの応答

Rig Character段階で映像上の必要性を確認してから追加を判断するもの:

- 推奨Damping
- 使用する移動軸
- Horizon特性
- Noise Profile

Profile値はメーカー公称最高速度をそのまま映像品質の上限にしない。最初は検証開始値として警告に使用し、ユーザーのSceneで調整する。

## 10. 手動操作との合成

手動操作はPreset Assetを書き換えず、Motion Evaluatorの出力へ一時的なTrimとして合成する。

- SpeedはTarget Speed Multiplierを変更し、Rig Profileの応答時間で追従する。
- Holdは設定された減速で停止する。
- Resumeは現在位置から設定された加速で復帰する。
- Reverseは減速、停止、逆方向への再加速を行い、速度符号を瞬時に反転しない。
- 即時停止が必要な場合はHoldと分けたFreeze操作として扱う。

操作中も位置を飛ばさない。MIDIは将来、Keyboardと同じSwitcherの公開操作を呼ぶ。

## 11. Preset Authoring

Motion Presetの正本はScriptableObjectとする。Scene上のShot、CinemachineCamera、SplineContainerを視覚的なAuthoring面として使用する。

Editorは次の明示操作を提供する。

- `Rebuild Shot From Preset`: PresetからShotを再構築する。
- `Save Shot As New Preset`: Scene上のCamera、Spline、Aim、LensとShotの適用済みTiming / Activationを新しいPresetへ保存する。
- 既存Presetの更新が必要な場合は対象Assetと上書き内容を確認し、別操作として実行する。

保存処理はKnot、Tangent、Up、Curveを完全に取得する。Asset YAMLを人やAIが直接編集することを標準手順にせず、Unity Editor APIと専用Creatorを使用する。

通常の`Apply / Sync`は生成済みShotの手動調整をPreset値へ戻さない。

SlotのPreset参照を変更した既存Shotは、明示的にRebuildするまで以前の適用済みMotionを維持する。ApplyだけでCamera Performanceを途中から差し替えない。

## 12. Motion Validator

EditorのValidatorはPresetまたは生成済みShotをサンプリングし、次を診断する。

- 無効値、NaN、Infinity
- Progress Curveの範囲、単調性、接線
- 移動速度、加速度、Jerk
- 角速度、角加速度
- Splineの曲率と急変
- In / Out Pointの速度
- Lens変化速度
- AimのScreen Space位置と移動速度
- Horizon Roll
- Targetとの最短距離
- Near Clip侵入
- Duration、Spline長、Rig Scaleの不整合

閾値はRig Profileごとの検証開始値とし、根拠のない業界標準値として固定しない。Validatorは既定で診断だけを行い、Spline、Duration、Aimを暗黙に修正しない。

明示的な`Conform Duration To Limits`のように、変更内容と対象が分かりUndoできる処理だけを後から追加できる。Occlusionが意図的なForeground Revealか事故かは自動判定しない。

## 13. 初期3D PaletteとGold Master

現在は次の10種をUnity Editor APIから生成し、3D Motion Paletteの評価候補とする。

1. Fixed Medium
2. Push In Rise
3. Pull Out Reveal
4. Truck Left Float
5. Truck Right Float
6. Arc Around Lift
7. Orbit Push
8. Crane Rise
9. Crane Drop
10. Pedestal Rise

すべてのSpline候補はY差分を持ち、Body、Timing、Lens、Rolling Entryを同じ時間軸で評価する。これはGold Master認定ではなく、ユーザーの作業用SceneでGold / Experimental / Rejectを判断するための初期候補である。

その後、次の12系統をGold Master候補として揃える。

1. Fixed Wide
2. Fixed Medium
3. Fixed Close
4. Slow Dolly Push
5. Rolling Dolly Push
6. Pull Reveal
7. Parallax Truck Left
8. Parallax Truck Right
9. Low Rising Arc
10. Crane Descend
11. Gimbal Float
12. Fluid Reframe

Preset数を先に増やさず、各候補についてEntry、Body、Aim、Lens、終了、手動介入を確認する。確認後に左右、距離、Duration、Lens、EnergyのVariantをAIが生成する。

Gold Master認定はユーザーのScene確認後にだけ行う。

## 14. 異常時

- Target、Spline、Duration、Curveが無効なMotion ShotをProgramへ選択しない。
- 選択失敗時は現在のProgramを維持する。
- 警告を毎フレーム出さない。
- Lens、Aim、DistanceへNaNまたはInfinityを適用しない。
- RuntimeでAsset、Spline、Sceneオブジェクトを生成、削除、保存しない。
- Runtimeの安全処理でPresetの演出意図を別の動きへ自動変更しない。

## 15. 受け入れ条件

1. Presetが完全なSpline Knot、Timing、Aim、Composition、Lens、Activationを保持できる。
2. 各生成Shotが専用CinemachineCamera、Spline、Aim Proxyを持つ。
3. Distance単位とDuration / Progress Curveで動きを再生できる。
4. StaticとRollingの開始差が映像上明確である。
5. Speed、Hold、Resume、Reverseで位置と速度が不連続に飛ばない。
6. 全Knotが暗黙にAuto Smoothへ変更されない。
7. Fixed Shotが安全な戻り先として維持される。
8. Preset原本、Shotの適用済み設定、Motion PlayerのRuntime状態が混同されない。
9. Scene上の調整を明示操作で新しいPresetへ保存できる。
10. Validatorの警告値と映像上の合否を混同しない。
11. Preset AssetまたはSlot参照の変更が、生成済みShotへ暗黙適用されない。
12. Spline PresetのY差分とVertical Motion ScaleがScene上の上下移動へ反映される。
13. Master Playback Speedが全Shotの時間進行へ掛かり、Assetを変更しない。
