# 製品概要

## 1. コンセプト

VLiveCameraUnitは、事前に用意した多数のカメラをライブ中に演奏するように切り替え、破綻しない計算とCinemachineによる追従、構図、Lens、補間でオペレーターを支援するUnity完結型のライブカメラシステムである。

各Shotは専用のCinemachineCameraとCamera Performance形式のMotion Presetを持つ。Shotを選ぶだけで、Presetが定めたStaticまたはRollingの開始状態から、位置、Aim、構図、Lensが意図どおりに進行する。必要な場面では速度、方向、Holdなどへ手動介入できる。

## 2. 目指す操作体験

1. `GameObject/VLiveKit/Virtual Camera`からCameraを含まないRig骨格を1回の操作で作成する。
2. Hierarchyで`VLive Camera Rig`を選択する。
3. InspectorでPerformer Target、正面基準、構図スケール、使用するMotion Presetと順序を設定する。
4. `Apply / Sync`で不足するShotを生成し、参照と順序を同期する。
5. キーでShotを直接Cutする。
6. 各ShotはPresetから適用されたIn Pointから自動で成立し、Rolling Shotは最初の表示区間から動いている。
7. 必要なときだけSpeed、Reverse、Hold、Resumeを操作する。
8. Scene上でCamera、Spline、Aim、Lensを調整し、必要なら明示操作で新しいPresetとして保存する。

導入時にHierarchyやComponentを手作業で組み立てさせない。生成後の日常的な編集はRig InspectorとShotのScene Authoringで完結させる。

専用の確認Sceneは配布せず、実際の構図と操作感はユーザーが自身の作業用Sceneで確認する。

## 3. 製品原則

### 3.1 切り替えだけで成立する

各Shotは追加操作がなくても、意図した固定画または移動画を作る。Fixed Shotは安全な戻り先として残し、すべてのShotへ無目的な微動を加えない。

動きのあるShotは、Cameraが必ず停止状態から発進する必要はない。PresetからShotへ適用されたIn Pointへ準備し、StaticまたはRollingの開始状態をShotごとに選ぶ。

### 3.2 操作すると深化する

手動操作は生のTransformやPreset Assetを直接書き換えない。オペレーターがタイミングと強さを決め、Preset評価、機材特性、構図補間、最終Camera出力はMotion PlayerとCinemachineへ任せる。

### 3.3 MotionはCamera Performanceである

Motion Presetは位置Splineだけではない。

- Body
- Timing
- Aim
- Screen Composition
- Lens
- Roll
- Activation

これらを同じ時刻から独立して評価する。Dolly、Crane、Gimbalなどの機材差をRig Profileで共有する。HumanizationとNoiseは現在の契約へ含めず、映像確認で必要性が認められた機材だけへ後から追加する。

### 3.4 初期作成はGameObject Menu、編集はInspector

独立したSetup Windowは持たない。GameObject MenuはRigの初期作成だけを担当する。Target、正面基準、Scale、Shot構成の編集と同期は`VLiveCameraRig`のInspectorを正本とする。

GameObject Menuによる初期作成はUnity CameraやCinemachine Brainを生成、探索、割り当て、変更しない。Program CameraはRig Inspectorでユーザーが明示的に指定する。

Inspectorの値を変更しただけではSceneオブジェクトやAssetを生成、削除、再配置、保存しない。変更は内容が明確なボタン操作とUndoの単位で行う。

Custom InspectorはIMGUIを使用し、UnityとCinemachineの標準Componentに近い簡潔な外観とする。固定表示は英語、Tooltipは日本語とし、独自Banner、絵文字、色付きBadge、装飾目的のBoxを使用しない。App UIは将来のライブ操作Windowに使用する。

### 3.5 Preset AssetとScene調整を分ける

Motion Preset Assetは再利用可能な原本、生成済みShotはScene固有の派生結果とする。

- 通常のApplyはScene調整を保持する。
- RebuildだけがPreset値を再適用する。
- Scene調整を再利用する場合は新しいPresetとして明示保存する。
- AIもUnity Editor APIを通して人と同じPreset形式を作る。

### 3.6 責務を分け、正本を増やさない

- RigはScene構成と順序付きShot Slotを所有する。
- SwitcherはProgram Shotの切り替えだけを行う。
- Shotは専用Camera、Spline、Aim Proxy、適用済みMotion設定とlifecycleを所有する。
- Motion Playerは再生状態だけを所有する。
- Motion EvaluatorはShotに適用済みの解決済み設定から値を決定的に計算する。
- Keyboard InputはSwitcherの公開操作だけを呼ぶ。
- Presetは再利用可能な設定だけを保持する。
- Editor Builderは明示的な生成、同期、再構築だけを行う。

現在必要な具体型で分離し、Command Bus、DI、Service Locator、汎用Node Graphは追加しない。

## 4. 現在の開発対象

- Unity 6.3以上、Cinemachine 3
- Targetは1人
- 1台のProgram CameraとCinemachine Brain
- 1 Shotにつき1台の専用CinemachineCamera
- Inspector主導のRig Authoring
- 標準IMGUIによる英語Custom Inspector
- ScriptableObject形式のMotion Preset
- Identity / Body / Timing / Aim / Lens / Roll / Activationへ分割されたCamera Performanceデータ
- 完全な3D Spline Knot、Tangent、Up
- 水平Motion Scale、垂直Motion Scale、Master Playback Speed
- Distance単位のSpline再生
- DurationとProgress Curve
- Aim ProxyとCinemachine Rotation Composer
- 自動Lens Track
- Static / Rolling Entry
- Fixed Shotと、切り替え直後から動作中に入れるRolling移動Shot
- キーボードによる直接Cut
- 連続的なSpeed、Reverse、Hold、Resume
- Editor上の簡易Motion Validator
- Scene上のShotから新しいPresetを明示保存する操作

## 5. 現在の対象外

- Inspector変更直後の自動生成、自動削除、自動再配置
- Preview / Take / Tally
- Camera BankとMultiview
- Pan、Tilt、Zoomのライブトリム
- MIDI
- Runtime AI、推薦、自動Take
- 大量Presetの検索、カテゴリUI、サムネイル
- Focus、Iris、Exposureの自動演出
- 独自Aim Solver
- Runtime Occlusion Solverと軌道自動修正
- 剛体カメラ物理
- RuntimeでのSpline全体のTarget追従

## 6. 将来の方向

Motion FoundationとGold MasterがユーザーのSceneで成立した後、次へ進む。

1. 現在の10種3D PaletteをユーザーのSceneで調整し、Gold Master候補を増やす
2. App UIによるCamera PaletteとProgram表示
3. Preview / Take / Tally
4. Camera Bank
5. Pan、Tilt、Zoomのライブトリム
6. MIDIとSoft Takeover
7. Timeline / BPM Cue
8. AIによるPreset候補生成
9. 次Shot推薦と明示的な半自動Take

キーボード運用を残し、MIDIを必須にしない。AIは人と同じPreset Assetを生成し、Runtimeで別のCamera制御経路を持たない。

## 7. 成功条件

1. ユーザーのSceneで一操作により初期Rigを作成できる。
2. Rig Inspectorで1人のTargetと複数Shotを構成できる。
3. キーだけで明確にCutでき、追加操作なしでも各Shotの意図が伝わる。
4. StaticとRollingの開始差、機材差、Aim、Lensの意図が映像上読み取れる。
5. Speed、Reverse、Hold、Resumeで位置と速度が不連続に飛ばない。
6. 同じCinemachineCameraを別Shotとして使い回していない。
7. 通常の同期で既存のCamera、Lens、Spline、Aim調整を失わない。
8. Preset原本、Shotの適用済み設定、Motion PlayerのRuntime状態が混同されていない。
9. AIが同じPreset形式を安全に生成できる。
10. 自動診断の数値と、ユーザーによる映像品質の判断を混同しない。
11. Preset、Rig Profile、Rig Scale、Slot参照の変更が生成済みShotへ暗黙伝播しない。
