# 製品概要

## 1. コンセプト

VLiveCameraUnitは、事前に用意した多数のカメラをライブ中に演奏するように切り替え、Cinemachineによる追従、構図、補間でオペレーターを支援するUnity完結型のライブカメラシステムである。

カメラを選択した時点で、そのShot固有の動きが始まる。切り替えだけでも意図のある画が続き、必要な場面では速度、方向、Holdなどへ手動介入できる。

## 2. 現在作る体験

1. `VLive Camera Setup` Windowを開く。
2. Scene内のPerformer Targetを1人指定する。
3. Motion Paletteから使用するPresetを選ぶ。
4. `Create / Update Camera Rig`を押す。
5. SceneにProgram Camera、Cinemachine Brain、Switcher、入力、複数のShotが作成される。
6. キー1〜9でShotを直接Cutする。
7. 移動ShotはCut後に自動で動き、必要なときだけSpeed、Reverse、Hold、Resumeを操作する。

専用の確認Sceneは配布しない。実際の構図と操作感は、ユーザーが自身の作業用Sceneで確認する。

## 3. 製品原則

### 3.1 切り替えだけで成立する

各Shotは、選ばれた後に追加操作がなくても意図した固定画または移動画を作る。Fixed Shotは安全な戻り先として残し、すべてのShotへ無目的な微動を加えない。

### 3.2 操作すると深化する

手動操作は生のTransformを直接動かすことではない。オペレーターがタイミングや強さを決め、追従、構図、補間はCinemachineへ任せる。

### 3.3 Setupは1つのWindowで完結する

導入時にHierarchyやComponentを手作業で組み立てさせない。WindowでTargetとPresetを選び、1回の操作で現在のSceneへ必要な構成を作る。

### 3.4 AIが作れるPaletteを先に整える

Motion Presetは人とAIが同じ形式で作成できる単純なAssetとする。現在は人が選択して使用し、Runtime AIや自動推薦は実装しない。

### 3.5 現在必要なものだけを作る

Pattern検索、カテゴリ、サムネイル、評価、生成履歴、Preview、MIDIなどは、少数Presetでの運用が成立してから追加する。

## 4. 現在の対象

- Unity 6.3以上、Cinemachine 3
- Targetは1人
- 1台のProgram CameraとCinemachine Brain
- 1 Shotにつき1台の専用CinemachineCamera
- Motion Preset AssetとWindow内の簡易Palette
- Fixed、Push In、Pull Out、Truck Left、Truck Right、Arc Around
- キー1〜9による直接Cut
- Speed、Reverse、Hold、Resume
- Off Air中の始点準備と終点Hold
- Setup Windowによる生成と更新

## 5. 現在の対象外

- Preview / Take / Tally
- Camera BankとMultiview
- Pan、Tilt、Zoomのライブトリム
- MIDI
- Runtime AI、推薦、自動Take
- Pattern Importer、生成metadata、検索、カテゴリ、サムネイル
- 独自構図Solver

## 6. 成功条件

1. ユーザーのSceneでWindowから一式を作成できる。
2. 1人のTargetに対して複数Shotが構成される。
3. キーだけで明確にCutでき、移動Shotは自動再生される。
4. Speed、Reverse、Hold、Resumeで位置が飛ばない。
5. 同じCinemachineCameraを別Shotとして使い回していない。
6. 初期Presetを基に動きの良し悪しを反復調整できる。
