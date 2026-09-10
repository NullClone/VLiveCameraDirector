# 製品概要

## 1. コンセプト

VLiveCameraUnitは、事前に用意した多数のカメラをライブ中に演奏するように切り替え、Cinemachineによる追従、構図、補間でオペレーターを支援するUnity完結型のライブカメラシステムである。

カメラを選択した時点で、そのShot固有の動きが始まる。切り替えだけでも意図のある画が続き、必要な場面では速度、方向、Holdなどへ手動介入できる。

## 2. 現在作る体験

1. メニューまたは簡略化されたSetup WindowからCamera Rigを1回の操作で作成する。
2. Hierarchyで`VLive Camera Rig`を選択する。
3. InspectorでPerformer Target、正面基準、構図スケール、使用するMotion Presetと順序を設定する。
4. `Apply / Sync`で不足するShotを生成し、参照と順序を同期する。
5. キーでShotを直接Cutする。
6. 移動ShotはCut後に自動で動き、必要なときだけSpeed、Reverse、Hold、Resumeを操作する。
7. 構図やSplineを初期値へ戻す必要がある場合だけ、明示的な`Rebuild From Preset`を使用する。

導入時にHierarchyやComponentを手作業で組み立てさせない。生成後の日常的な編集はRig Inspectorで完結させる。専用の確認Sceneは配布せず、実際の構図と操作感はユーザーが自身の作業用Sceneで確認する。

## 3. 製品原則

### 3.1 切り替えだけで成立する

各Shotは、選ばれた後に追加操作がなくても意図した固定画または移動画を作る。Fixed Shotは安全な戻り先として残し、すべてのShotへ無目的な微動を加えない。

### 3.2 操作すると深化する

手動操作は生のTransformを直接動かすことではない。オペレーターがタイミングや強さを決め、追従、構図、補間はCinemachineへ任せる。

### 3.3 初期作成は一操作、編集はInspector

Setup WindowとメニューはRigの初期作成だけを担当する。Target、正面基準、スケール、Shot構成の編集と同期は`VLiveCameraRig`のInspectorを正本とし、WindowとInspectorに同じ設定を重複して持たせない。

Inspectorの値を変更しただけではSceneオブジェクトを生成、削除、再配置しない。Scene変更は内容が明確なボタン操作とUndoの単位で行う。

### 3.4 AIが作れるPaletteを先に整える

Motion Presetは人とAIが同じ形式で作成できる単純なAssetとする。現在は人が選択して使用し、Runtime AIや自動推薦は実装しない。

### 3.5 現在必要なものだけを作る

Pattern検索、カテゴリ、サムネイル、評価、生成履歴、Preview、MIDIなどは、少数Presetでの運用が成立してから追加する。

### 3.6 責務を分け、状態の正本を増やさない

- RigはScene構成と順序付きShot Slotを所有する。
- SwitcherはProgram Shotの切り替えだけを行う。
- Shotは自身のCinemachineCameraと移動状態を所有する。
- Keyboard Inputは入力をSwitcherへ渡す。
- Presetは再利用可能な初期値だけを保持する。
- Editor Builderは明示的に依頼された生成、同期、再構築だけを行う。

現在必要な具体型で分離し、将来用interface、Service Locator、Command Bus、DI、汎用Editor frameworkは追加しない。

## 4. 現在の対象

- Unity 6.3以上、Cinemachine 3
- Targetは1人
- 1台のProgram CameraとCinemachine Brain
- 1 Shotにつき1台の専用CinemachineCamera
- `VLiveCameraRig`によるTarget、正面基準、スケール、Shot Slot管理
- Motion Preset Assetと6つの初期Palette
- Fixed、Push In、Pull Out、Truck Left、Truck Right、Arc Around
- キーボードによる直接Cut
- Speed、Reverse、Hold、Resume
- Off Air中の始点準備と終点Hold
- メニューまたは簡略化されたSetup WindowによるRig作成
- Inspectorの明示操作による安全な同期と再構築

## 5. 現在の対象外

- Inspector変更直後の自動生成、自動削除、自動再配置
- Preview / Take / Tally
- Camera BankとMultiview
- Pan、Tilt、Zoomのライブトリム
- MIDI
- Runtime AI、推薦、自動Take
- Pattern Importer、生成metadata、検索、カテゴリ、サムネイル
- 独自構図Solver
- Target移動に追従してSpline全体をRuntime再配置する機能

## 6. 成功条件

1. ユーザーのSceneで一操作により初期Rigを作成できる。
2. Rig Inspectorで1人のTargetと複数Shotを構成できる。
3. 同じPresetを複数Slotで使用しても、各Shotの識別と手動調整が混線しない。
4. キーだけで明確にCutでき、移動Shotは自動再生される。
5. Speed、Reverse、Hold、Resumeで位置が飛ばない。
6. 同じCinemachineCameraを別Shotとして使い回していない。
7. 通常の同期で既存の構図、Lens、Spline調整を失わない。
8. 初期Presetを基に動きの良し悪しを反復調整できる。
