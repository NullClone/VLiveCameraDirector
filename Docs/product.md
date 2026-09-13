# VLive Camera Director 製品仕様

## 1. コンセプト

VLive Camera Directorは、プロの現場水準を満たす直感的な手動操作を中心に、破綻しない計算とCinemachine 3による追従、構図、Lens、補間でカメラ演出を支援するUnity完結型のライブカメラシステムである。

事前に用意した多数のShotを、楽器を演奏するように選択する。切り替えるだけで固定画または移動画として成立し、必要な場面だけ速度、方向、Holdなどへオペレーターが介入できる。

製品名は`VLive Camera Director`とする。`Cinemachine`は依存関係を正確に説明する場合に使用し、製品名、Package ID、namespaceへ含めない。

## 2. 目指す操作体験

1. GameObject MenuからCameraを含まないRig骨格を作成する。
2. Rig InspectorでPerformer Target、Program Camera、正面基準、Scale、使用するMotion Presetと順序を設定する。
3. `Apply / Sync`で不足するShotを生成し、参照と順序を同期する。
4. キーでShotへ直接Cutする。
5. ShotはPresetから適用されたIn Pointから成立し、Rolling Shotは最初の表示区間から動いている。
6. 必要なときだけSpeed、Reverse、Hold、Resumeを操作する。
7. Scene上でCamera、Spline、Aim、Lensを調整し、必要なら新しいPresetとして明示保存する。

初期導入後の日常的な編集は、UnityとCinemachineの標準Componentに近いInspectorで完結させる。専用の確認Sceneは配布せず、構図と操作感はユーザーが自身の作業用Sceneで判断する。

## 3. 製品原則

### 3.1 切り替えだけで成立する

各Shotは追加操作がなくても意図した映像を作る。Fixed Shotは安全な戻り先として残し、すべてのShotへ無目的な微動を加えない。

Motion Shotは必ず停止状態から始めず、StaticまたはRollingのEntryをPresetごとに選ぶ。

### 3.2 操作すると深化する

手動操作は生のTransformやPreset Assetを直接書き換えない。オペレーターがタイミングと強さを決め、Motion評価、機材応答、構図補間、最終Camera出力はVLive Camera DirectorとCinemachineへ任せる。

Keyboardは常に利用できる基礎入力とする。将来MIDIを追加しても必須にはせず、同じ公開操作へ接続する。

### 3.3 MotionはCamera Performanceである

Motion Presetは位置Splineだけではない。Body、Timing、Aim、Screen Composition、Lens、Roll、Activationを同じPlayback Timeから評価する。

人とAIは同じMotion Preset Assetを作成する。AI専用Runtime経路や別データ形式を持たない。

### 3.4 PresetとScene調整を分ける

Motion Preset Assetは再利用可能な原本、生成済みShotはScene固有の適用結果とする。

- 通常のApplyはScene調整を保持する。
- RebuildだけがPreset値を既存Shotへ再適用する。
- Scene調整を再利用する場合は新しいPresetへ明示保存する。
- PresetまたはRig Profileの編集をLive中のShotへ暗黙伝播しない。

### 3.5 CinemachineをCamera Pipelineの正本とする

各Shotは専用のCinemachineCameraを持つ。Cinemachine Brain、Spline Dolly、Rotation Composer、Lensなどの標準機能を優先し、同じ機能をVLive側で再実装しない。

VLive Camera DirectorはShotの意図、Motion、lifecycle、操作を所有し、CinemachineはCamera Pipelineと最終出力を所有する。詳細は[architecture.md](architecture.md)を正本とする。

### 3.6 小さな具体型で責務を分ける

状態所有者が異なる責務は分離するが、現在使わないinterface、Service、Registry、Factory、Command Bus、DI Container、汎用Node Graphは作らない。

## 4. 現在の製品範囲

- Unity 6.3以上、Cinemachine 3
- Targetは1人
- 1台のProgram CameraとCinemachine Brain
- 1 Shotにつき1台の専用CinemachineCamera
- Inspector主導のRig Authoring
- IMGUIによる英語Custom Inspectorと日本語Tooltip
- ScriptableObject形式のMotion Preset
- 完全な3D Spline Knot、Tangent、Tangent Mode、Up
- Body、Timing、Aim、Composition、Lens、Roll、Activation
- 水平・垂直Motion ScaleとMaster Playback Speed
- Distance単位のSpline再生
- Static / Rolling Entry
- キーボードによるDirect Cut
- Speed、Reverse、Hold、Resume
- Motion Validator
- Scene上のShotから新しいPresetを保存するEditor操作
- UI ToolkitとApp UIによるGame View構図ガイド、外周フレーム、アスペクトマスク、テーマ切替付きランタイム設定パネル

## 5. 現在の対象外

- Inspector変更直後の自動生成、削除、再配置
- Preview / Take / Tally、Camera Bank、Multiview
- Pan、Tilt、Screen Position、Zoomのライブトリム
- MIDIとCamera Palette全体を扱うApp UI操作Window
- Runtime AI、Shot推薦、自動Take
- Focus、Iris、Exposureの自動演出
- 独自Aim Solver、Runtime Occlusion Solver
- RigidbodyによるCamera機材シミュレーション
- 実需のない互換wrapperと将来用抽象化

## 6. 成功条件

1. Rig Inspectorで1人のTargetと複数Shotを構成できる。
2. キーだけで明確にCutでき、追加操作なしでも各Shotの意図が伝わる。
3. StaticとRolling、機材差、Aim、Lensの意図が映像上読み取れる。
4. Speed、Reverse、Hold、Resumeで位置と速度が不連続に飛ばない。
5. 同じCinemachineCameraを別Shotとして使い回さない。
6. 通常の同期で既存のCamera、Lens、Spline、Aim調整を失わない。
7. Preset原本、Shotへ適用した設定、Runtime再生状態が混同されない。
8. 人とAIが同じPreset作成経路を利用できる。
9. 自動診断と、人による映像品質の判断を混同しない。
10. Keyboardだけの運用を将来も維持できる。
