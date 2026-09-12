# Roadmap

## 1. この文書の役割

現在成立している基盤、次に完成させる縦切り、その後の順序だけを管理する。完了済み作業の詳細はGit履歴へ残し、この文書へStep履歴を蓄積しない。

## 2. Current — Motion Foundation

2026-09-12時点の基盤:

- Unity 6.3、Cinemachine 3.1.7、Input System 1.19.0
- Rigと順序付きShot SlotによるScene Authoring
- 1 Shotにつき1つの専用CinemachineCamera
- KeyboardによるDirect Cut
- Cameraを生成しないGameObject Menu
- Setup WindowとStep UIを使用しないInspector主導のAuthoring
- Apply、Rebuild、削除、Preset保存の明示的な分離
- Identity、Body、Timing、Aim、Lens、Roll、Activation形式のMotion Preset
- 完全な3D Spline Knot、水平・垂直Scale、Master Playback Speed
- Motion EvaluatorとMotion Player
- Static / Rolling Entry
- Speed、Reverse、Hold、Resume
- 10種の初期3D Motion Palette
- 標準IMGUIを使った英語Custom Inspector
- Editor Motion Validator
- Runtime、Editor、Presetの責務別フォルダ構成

ユーザーの作業用SceneでRig作成と基本的な切り替えは確認済みである。初期Presetは評価候補であり、Gold Masterではない。

## 3. Next — Camera Director Consolidation

次の1縦切りでは、機能追加より先に製品名、フォルダ、assembly、Cinemachine境界を揃える。

- 製品表示名を`VLive Camera Director`へ変更する。
- Package IDを`com.toshi.vlivekit.camera-director`へ変更する。
- ルートフォルダを`VLiveCameraDirector`へ変更する。
- asmdefを`VLiveKit.Camera.Runtime`と`VLiveKit.Camera.Editor`へ変更する。
- namespaceと`VLiveCamera`型接頭辞は維持する。
- `.cs`、Asset、フォルダと各`.meta`を対で移動しGUIDを維持する。
- Preset CreatorとBakerの固定パスを新構成へ更新する。
- Menu、CreateAssetMenu、文書、表示名から旧製品名を除去する。
- Cinemachineの標準ComponentとPipelineを優先し、重複する独自処理がないか確認する。
- 旧Package ID、asmdef、フォルダ名の互換wrapperを残さない。
- Unity Import、Compile、Consoleとscoped diffを確認する。

この段階ではCamera挙動、Preset値、ユーザーScene、Package直下READMEを変更しない。

## 4. Then — Motion Quality and Gold Masters

Consolidation後、ユーザーのSceneで現在のPaletteを評価する。

- Y移動、旋回、距離、Lens、Rolling Entryの映像確認
- Dolly、Fluid Head、Crane、Gimbal、Handheld、RoboticのRig Character
- Rig別のSpeed、Hold、Reverse応答
- Roll、Horizon、必要な機材だけへのCinemachine Noise
- Validatorの角速度、曲率、Horizon、Near Clip診断
- Gold、Experimental、Reject分類
- 代表的なGold Master候補の確立

数値だけでGold Master認定せず、ユーザーの作業用Sceneで判断する。

## 5. After — Palette Authoring

Gold Masterから安全にVariationを増やす。

- 左右、距離、Duration、Lens、Energy Variant
- AIによるMotion Preset候補生成
- ValidatorによるCandidate診断
- 明示的な採用、調整、却下
- 実数が増えた時点で検索、Filter、カテゴリ、サムネイルを設計する

AIはUnity Editor APIから人と同じMotion Preset Assetを作る。YAML直接編集やAI専用Runtime形式を使用しない。

## 6. Later — Live Operation

- App UIによるCamera PaletteとProgram表示
- Preview / Take / Tally
- Camera Bank
- Pan、Tilt、Screen Position、Zoomの非破壊ライブトリム
- MIDIとSoft Takeover
- 必要性が確認されたTransition

Keyboard Direct Cutは残し、MIDIを必須にしない。

## 7. Future

- Focus、Iris、Exposure Track
- Timeline / BPM Cue
- Multiview
- 次Shot候補の推薦
- 明示的に許可された半自動Take
- 必要性が映像で確認されたTrajectoryまたはAim拡張

## 8. Gate

- CurrentをユーザーのSceneで確認してから次の映像表現へ進む。
- 1回の実装依頼は独立してCompileできる縦切りにする。
- Runtime、Editor、Presetが同じ契約を完成させる場合は一括変更してよい。
- 将来機能の空interface、Adapter、folder、serviceを先に追加しない。
- 製品挙動、データ所有権、公開API、破壊的範囲が仕様を越える場合だけユーザーへ確認する。
