# 実装順序と現在地

## 1. この文書の役割

現在実装との差分と、次に完成させる1段階を管理する。将来フェーズの詳細は先に設計しない。

## 2. 現在の実装

2026-09-10時点:

- `package.json`はUnity 6000.3、Cinemachine 3.1.7、Splines 2.0.0、Input System 1.19.0を指定している。
- `VLiveCameraShot`、`VLiveCameraSwitcher`、`VLiveCameraKeyboardInput`がある。
- A/Bは別々のCinemachineCameraを持ち、キー1 / 2でCutできる。
- BはSpline移動、Speed、Reverse、Hold、Resumeを持つ。
- A/B用の試作Scene Builder、Verifier、Tests Sceneが残っている。
- Setup Window、Motion Preset Asset、複数Shot一覧、専用CustomEditorは未実装である。

次の実装では既存Tests Sceneを変更、削除、ステージしない。ユーザーは自身の作業用Sceneで受け入れ確認を行う。

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

## 5. Step 2 — 現在の実装対象

### 目的

ユーザーの作業用Sceneで、1つのWindowから1 Targetと複数の基本カメラワークを設定し、キーボードだけで使用できる状態を作る。

### 実装範囲

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

## 6. Step 3 — 手動運用の改善

Step 2をユーザーが確認した後、不足したものだけを追加する。

- Presetの追加と値調整
- Pan、Tilt、Zoomの手動トリム
- Program状態の見やすさ
- Palette検索やカテゴリが実際に必要かの確認

## 7. Step 4 — 現場操作

- Preview / Take / Tally
- Camera Bank
- MIDIとSoft Takeover
- 必要になったTransition

キーボード運用を維持し、MIDIを必須にしない。

## 8. 将来

- AIによるMotion Preset Asset生成
- Timeline / BPM Cue
- 次Shot候補の推薦
- 明示的に許可された半自動Take
- Multiviewと大規模Palette

AIはStep 2と同じPreset形式を作成し、専用Runtime経路を持たない。

## 9. 実装順序の規則

- Step 2は独立した1つの縦切り実装として一括で依頼できる。
- 実装中の通常判断はエージェントに任せる。
- 製品挙動、データ所有権、公開API、範囲外削除を変える場合だけ確認する。
- 将来機能のための空コードや拡張ポイントを作らない。
- ユーザーの既存Sceneと無関係な変更を保持する。
