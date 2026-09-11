# 実装順序と現在地

## 1. この文書の役割

現在実装との差分と、次に完成させる1段階を管理する。将来像は各仕様書に記載するが、実装では現在のStepを越えて先行しない。

## 2. 現在の実装

2026-09-11時点:

- Unity 6000.3、Cinemachine 3.1.7、Input System 1.19.0を使用している。
- ProjectではUnity Splines 2.9.0が解決されている。
- `VLiveCameraRig`と順序付きShot SlotがScene Authoringの正本である。
- Rig InspectorからTarget、正面基準、Distance Scale、Motion Scale、Shot Slotを編集できる。
- Setup WindowとGameObject Menuから初期Rigを作成できる。
- `Apply / Sync`、Selected / All Rebuild、明示的な生成物削除が分かれている。
- 各Shotは別々のCinemachineCameraを持つ。
- `VLiveCameraSwitcher`、`VLiveCameraShot`、`VLiveCameraKeyboardInput`がある。
- 6つの標準Motion Presetとキーによる直接Cutがある。
- Spline ShotはSpeed、Reverse、Hold、Resumeを持つ。
- ユーザーの作業用SceneでRig作成と現在の切り替え動作が確認済みである。

現在のMotion実装には次の制限がある。

- Presetは`Vector3[]`の制御点だけを持つ。
- Builderが全KnotをAuto Smoothへ設定する。
- Spline位置を正規化位置/秒で直接進める。
- 終端付近だけ同じSmoothStepで減速する。
- Aim、Composition、Lens、Roll、Activationを独立したTrackとして持たない。
- ReverseとHoldの速度変化に機材別の応答がない。

## 3. 完了済み

### Step 0 — 仕様と開発方針

状態: 完了。

- 手動優先
- 1 Shot 1 CinemachineCamera
- Unity 6.3+、Cinemachine 3
- 初回安定版まで旧版互換なし
- Agent Skills、コード、Inspector、検証方針

### Step 1 — 独立Camera切り替え

状態: 完了。ユーザーScene確認済み。

- 1台のProgram CameraとBrain
- 複数の独立したCinemachineCamera
- 直接Cut
- Off-Air準備

### Step 2 — Motion Paletteと複数Shot

状態: 完了。ユーザーScene確認済み。

- `VLiveCameraMotionPreset` ScriptableObject
- 6つの初期Preset
- キーによる直接Cut
- Speed、Reverse、Hold、Resume
- 各Shot専用のSpline

### Step 3 — Rig Inspector Authoring

状態: 完了。ユーザーScene確認済み。

- `VLiveCameraRig`とShot Slot
- Rig Inspector主導のTarget、正面、Scale、Shot構成
- Setup Window簡略化
- GameObject Menu
- Editor専用Builder
- Apply / SyncとRebuildの分離
- Shot参照による安定した所有物識別
- 可変長Keyboard割り当てと競合表示

## 4. Step 4 — 現在の実装対象: Motion Foundation

### 目的

現在の位置Spline再生を、Body、Timing、Aim、Composition、Lens、Activationを同期評価できるCamera Performanceへ置き換える。

Preset数の量産より先に、1つのShotをプロ品質へ調整できるデータと再生基盤を完成させる。

### Runtime

- `VLiveCameraMotionPreset`の破壊的な新形式化
- 完全なSpline Knot、Tangent、Tangent Mode、Upの保存
- `VLiveCameraRigProfile`と最小限の初期Profile
- `VLiveCameraMotionEvaluator`
- `VLiveCameraMotionPlayer`
- Shotが持つ適用済みMotion設定と、Preset原本からの明示的なRebuild
- Distance単位のSpline再生
- DurationとProgress Curve
- `PreserveDuration`と`PreserveSpeed`
- Aim Offset、Screen Position、Dead Zone、Hard Limits、Damping、Lookahead
- Shot専用Aim Proxy
- Field of View / Focal LengthのLens Track
- Static / Rolling EntryとIn / Out Point
- 連続的なSpeed、Hold、Resume、Reverse
- 無効値をCinemachineへ渡さないHard Safety

### Editor

- Builderによる完全なSplineとTrackの生成、Rebuild
- SlotのPreset参照とShotの適用済みMotionが異なる場合のInspector表示
- Scene上のSelected Shotから新しいPresetを保存する明示操作
- Preset作成APIを既定Preset生成と将来のAI生成で共用
- Motion Validatorの最初の縦切り
  - 無効値
  - Progress Curve
  - 移動速度、加速度、Jerk
  - In / Out Point
  - AimのScreen Space位置
  - Lens値
- Rig、Shot、Motion PlayerのCustom Inspector更新

### 同梱Preset

現在の6 Presetを新形式へ更新する。

- Fixed Medium
- Push In
- Pull Out
- Truck Left
- Truck Right
- Arc Around

これらはMotion Foundationの代表動作確認用であり、このStepだけでGold Master認定しない。Push InのStatic版とRolling版など、Entry差の確認に必要な最小追加Presetは作成してよい。

### 対象外

- 12種類のGold Master完成
- 大量Preset生成
- Focus、Iris、Exposure Track
- 独自Jerk-Limited Trajectory Solver
- 独自Aim Solver
- RigidbodyによるCamera物理
- Runtime Occlusion Solver
- Splineの自動修正
- Pan、Tilt、Screen Position、Zoomのライブトリム
- Preview / Take / Tally
- Camera Bank、Multiview
- App UI操作Window
- MIDI
- Runtime AI、推薦、自動Take
- 専用テストScene

### エージェント完了条件

1. Unity ImportとCompileで新しいエラーがない。
2. Preset原本、Shotの適用済み設定、Motion PlayerのRuntime状態が仕様どおり分かれている。
3. 全KnotをAuto Smoothへ強制していない。
4. Distance、Duration、Progress CurveからCamera位置を評価している。
5. Body、Aim、Composition、Lensが同じPlayback Timeを使用する。
6. StaticとRollingが異なるIn Point状態から開始できる。
7. Speed、Hold、Resume、Reverseで位置と速度が不連続に飛ばない。
8. Apply、Rebuild、Save As New Presetが別の明示操作である。
9. ValidatorがAssetやSceneを暗黙変更しない。
10. Package直下README、ユーザーScene、無関係なToolsを変更していない。
11. 実施していない映像確認を完了扱いにしていない。

### ユーザー受け入れ

1. 作業用Sceneで既存6 Shotを新形式として切り替えられる。
2. StaticとRollingの開始差が自然に見える。
3. Splineの直線、弧、Tangentが意図どおりに再現される。
4. Aimが機械的な中央固定にならず、被写体を安定して捉える。
5. Lens TrackがBodyと同期して動く。
6. Speed、Hold、Resume、Reverseが機材らしい連続性を持つ。
7. Scene上で調整したShotを新しいPresetとして保存できる。
8. Validatorの警告と実映像を比較できる。

## 5. Step 5 — Rig CharacterとGold Master

Step 4をユーザーが確認した後に行う。

- Dolly、Fluid Head、Crane、Gimbal、Handheld、Robotic等のRig Profile調整
- RollとHorizon
- 選択的なCinemachine Noise
- Rig別のSpeed、Hold、Reverse応答
- Validatorの角速度、曲率、Horizon、Near Clip診断
- 12種類のGold Master候補
- Sceneでの比較とGold / Experimental / Reject判定

数値はメーカー公称値や調査値をそのまま固定せず、映像確認から調整する。

## 6. Step 6 — Palette運用

- Gold Masterからの左右、距離、Duration、Lens、Energy Variant
- AIによるPreset候補生成
- ValidatorによるCandidate確認
- 明示的な採用、調整、却下
- 実数が増えた時点で検索、カテゴリ、サムネイルを判断する

AIはUnity Editor APIから人と同じMotion Preset Assetを作成する。YAML直接編集やAI専用Runtime形式を使用しない。

## 7. Step 7 — 現場操作

- App UIによるCamera PaletteとProgram表示
- Preview / Take / Tally
- Camera Bank
- Pan、Tilt、Screen Position、Zoomのライブトリム
- MIDIとSoft Takeover
- 必要になったTransition

キーボードによるDirect Cutを維持し、MIDIを必須にしない。

## 8. 将来

- Focus / Iris / Exposure Track
- Timeline / BPM Cue
- 次Shot候補の推薦
- 明示的に許可された半自動Take
- Multiview
- 必要性が映像で確認されたJerk-Limited Solver
- 必要性が確認されたAim Response拡張

## 9. 実装順序の規則

- Step 4は独立してCompileできる1つの縦切りとして一括依頼できる。
- 密接したRuntime、Editor、同梱Presetは一緒に変更してよい。
- 実装中の通常判断はエージェントに任せる。
- 製品挙動、データ所有権、公開API、破壊的な範囲変更が仕様を越える場合だけ確認する。
- Step 5以降のAsset、UI、空interfaceを先に追加しない。
- ユーザーの既存Sceneと無関係な変更を保持する。
