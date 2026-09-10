# 実装順序と現在地

## 1. この文書の役割

現在実装との差分と、次に完成させる1段階を管理する。将来フェーズの詳細は先に設計しない。

## 2. 現在の実装

2026-09-10時点:

- `package.json`はUnity 6000.3、Cinemachine 3.1.7、Splines 2.0.0、Input System 1.19.0を指定している。
- `VLiveCameraShot`、`VLiveCameraSwitcher`、`VLiveCameraKeyboardInput`がある。
- `VLiveCameraMotionPreset`、6つの標準Preset、Setup Windowがある。
- Setup Windowから1 Target、Program Camera、複数Shotを生成、更新できる。
- 各Shotは別々のCinemachineCameraを持ち、FixedまたはSplineとして動作する。
- Switcherは順序付きShot一覧を持ち、キー1〜9でCutできる。
- Spline ShotはSpeed、Reverse、Hold、Resumeを持つ。
- Switcher、Shot、Keyboard Inputに専用CustomEditorがある。
- A/B専用の試作Builder、Verifier、Testsは削除済みである。
- Runtimeは`VLiveKit.Camera`、Editorは`VLiveKit.Camera.Editor` namespaceを使用する。

現在のSetupはPreset参照で既存Shotを照合し、Target Transform Scaleを含む`TransformPoint`で配置する。Setup Windowが設定とScene変更の両方を担っているため、次の実装でRig Inspector主導へ整理する。

## 3. Step 0 — 仕様整理

状態: 完了。

- 手動優先、1 Shot 1 CinemachineCamera
- Unity 6.3+、Cinemachine 3
- 旧版互換なし
- Agent Skills、コードとInspectorスタイル

## 4. Step 1 — A/B成立確認

状態: Runtime実装済み。最終的な見た目はユーザー確認。

- 1台のProgram CameraとBrain
- Fixed Shot AとSpline Shot B
- キー1 / 2の直接Cut
- Speed、Reverse、Hold、Resume
- Off Air準備と終点Hold

A/Bは製品の最終操作数ではなく、独立したカメラ切り替えが成立することを確認する土台とする。

## 5. Step 2 — Motion Paletteと複数Shot

状態: 実装済み。最終的な構図と操作感はユーザー確認。

### 実装した体験

ユーザーの作業用Sceneで、1つのWindowから1 Targetと複数の基本カメラワークを設定し、キーボードだけで使用できる状態を作る。

### 実装内容

- `VLiveCameraMotionPreset` ScriptableObject
- `VLiveCameraSetupWindow` EditorWindow
- Target、Output Camera、Preset選択
- `Create / Update Camera Rig`
- SwitcherのA/B固定参照を順序付きShot一覧へ変更
- キー1〜9の直接Cut
- 6つの初期Preset
  - Fixed Medium
  - Push In
  - Pull Out
  - Truck Left
  - Truck Right
  - Arc Around
- 生成される各Shot専用のCinemachineCameraとSpline
- Speed、Reverse、Hold、Resumeの継続
- 今回触るユーザー向けMonoBehaviourのCustomEditor
- 複数Shot化で不要になるA/B専用Scene BuilderとPlayMode Verifierの削除

### 対象外

- 専用テストSceneの新規作成または既存Tests Sceneの変更、削除
- Preview / Take / Tally
- Camera Bank、Multiview、Blend、映像Transition
- Pan、Tilt、Zoomのライブトリム
- MIDI
- Runtime AI、推薦、自動Take
- Pattern検索、カテゴリ、サムネイル、Importer、AI metadata
- 独自Solver、Command Bus、DI、汎用Editor framework

### エージェント完了条件

1. Unity ImportとCompileで新しいエラーがない。
2. Setup WindowがMenuから開ける。
3. diffを簡易Reviewし、参照、Undo、重複生成防止、所有権に明白な問題がない。
4. Package直下READMEとTests Sceneを変更していない。
5. 実施していないScene動作確認を完了扱いにしていない。

### ユーザー受け入れ

1. 作業用SceneでTargetを指定し、1回の操作でRigを作成できる。
2. キー1〜6で複数ShotへCutできる。
3. 各移動ShotがCut後に分かりやすく動く。
4. Speed、Reverse、Hold、Resumeを操作できる。
5. 再度Setupしても重複せず、Undoできる。

## 6. Step 3 — 現在の実装対象: Rig Inspector Authoring

### 目的

初期Rigを一操作で作成し、その後のTarget、正面、スケール、Shot構成をRig Inspectorで安全に編集する。現在の動作を維持しながら、Rig、Switcher、Shot、Input、Editor生成処理の責務と状態の正本を分離する。

### 実装範囲

- `VLiveCameraRig`と順序付きShot Slot
- Rigを唯一のShot順とScene Authoring設定の正本にする
- `VLiveCameraRigEditor`
- Editor専用`VLiveCameraRigBuilder`
- GameObject Menuと簡略化されたSetup Windowからの一操作作成
- Target Forward、World +Z、World -Z、Custom Reference
- Target Height、Distance Scale、Motion Scale
- +Zを正面側とする6つの同梱Preset更新
- 明示的な`Apply / Sync`
- 明示的なSelected / Allの`Rebuild From Preset`
- Slotから外す操作と生成物削除の分離
- 同じPresetを複数Slotで使える安定したShot参照
- キーボードCut処理の9固定撤廃とInspectorのキー競合表示
- 現在触るユーザー向けMonoBehaviourのCustomEditor

### 対象外

- Inspector変更時の自動生成、自動削除、自動再配置
- 専用テストSceneと自動テストの新規作成
- Preview / Take / Tally
- Camera Bank、Multiview、Blend、映像Transition
- Pan、Tilt、Zoomのライブトリム
- MIDI
- Runtime AI、推薦、自動Take
- QWERTYや記号キーの自動割り当て
- Pattern検索、カテゴリ、サムネイル、Importer、AI metadata
- RuntimeでのSpline全体のTarget追従
- 独自Solver、Command Bus、DI、汎用Editor framework

### エージェント完了条件

1. Unity ImportとCompileで新しいエラーがない。
2. メニューまたはSetup Windowから初期Rigを作成できる入口がある。
3. Rig Inspector、Builder、Switcher、Shot、Inputの所有権が仕様どおり分かれている。
4. `Apply / Sync`とRebuild、削除が別操作になっている。
5. diffを簡易Reviewし、Undo、Prefab Override、Scene Dirty、同一Presetの複数Slot、手動調整保持に明白な問題がない。
6. Package直下READMEと無関係なToolsを変更していない。
7. 実施していないScene動作確認を完了扱いにしていない。

### ユーザー受け入れ

1. 作業用Sceneで一操作により初期Rigを作成できる。
2. Targetと正面基準を設定すると、+Z基準の正面側から初期Shotが作成される。
3. Distance ScaleとMotion Scaleが別の意味で反映される。
4. Slot追加、複製、並び替え後にApplyしてもShot参照と手動調整が混線しない。
5. 通常のApplyで既存のLens、Spline、Camera調整が保持される。
6. Rebuildと生成物削除をUndoできる。
7. キー1〜6でCutし、移動操作を継続できる。

## 7. Step 4 — 手動運用の改善

Step 3をユーザーが確認した後、不足したものだけを追加する。

- Presetの追加と値調整
- Pan、Tilt、Zoomの手動トリム
- Program状態の見やすさ
- Palette検索やカテゴリが実際に必要かの確認

## 8. Step 5 — 現場操作

- Preview / Take / Tally
- Camera Bank
- MIDIとSoft Takeover
- 必要になったTransition

キーボード運用を維持し、MIDIを必須にしない。

## 9. 将来

- AIによるMotion Preset Asset生成
- Timeline / BPM Cue
- 次Shot候補の推薦
- 明示的に許可された半自動Take
- Multiviewと大規模Palette

AIは手動作成と同じPreset形式を作成し、専用Runtime経路を持たない。

## 10. 実装順序の規則

- Step 3は独立した1つの縦切り実装として一括で依頼できる。
- 実装中の通常判断はエージェントに任せる。
- 製品挙動、データ所有権、公開API、範囲外削除を変える場合だけ確認する。
- 将来機能のための空コードや拡張ポイントを作らない。
- ユーザーの既存Sceneと無関係な変更を保持する。
