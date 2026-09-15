# Roadmap

## 1. この文書の役割

現在成立している基盤、次に完成させる縦切り、その後の順序だけを管理する。完了済み作業の詳細はGit履歴へ残し、この文書へStep履歴を蓄積しない。

## 2. Current — Camera Director Foundation

2026-09-14時点の基盤:

- Unity 6.3、Cinemachine 3.1.7、Input System 1.19.0
- 製品名`VLive Camera Director`、Package ID`com.toshi.vlivekit.camera-director`
- `VLiveKit.Camera.Runtime`と`VLiveKit.Camera.Editor` assembly
- Rigと順序付きShot SlotによるScene Authoring
- 1 Shotにつき1つの専用CinemachineCamera
- Rig共通の単独・複数`VLivePerformer`とShotごとの任意Override
- HumanoidのHead、UpperChestまたはChest、Hips自動認識
- Cinemachine Target Group、Rotation Composer、Group FramingによるActor構図
- Wide、Full、BustUp、CloseUp、FaceUpのShot Size
- 全ShotとProgram CameraのPhysical Camera化
- RigによるSensor Size、Gate Fit、Lens Shift、Near / Far Clip Plane一括設定
- KeyboardによるDirect Cut
- Cameraを生成しないGameObject Menu
- Setup WindowとStep UIを使用しないInspector主導のAuthoring
- Apply、Rebuild、Camera Settings一括適用、削除の明示的な分離
- Identity、Body、Timing、Aim、Lens、Roll、Activation形式のMotion Preset
- 完全な3D Spline Knot、水平・垂直Scale、Master Playback Speed
- Motion EvaluatorとMotion Player
- Static / Rolling Entry
- Speed、Reverse、Hold、Resume
- 10種の初期3D Motion Palette
- 標準IMGUIを使った英語Custom Inspector
- Editor Motion Validator
- Runtime、Editor、Presetの責務別フォルダ構成
- Game View用のApp UI構図パネル、テーマ切替、アニメーション付きアスペクトマスク、外周フレーム、Split Line

ユーザーの作業用SceneでRig作成と基本的な切り替えは確認済みである。新しいActor構図とPhysical Camera基盤はUnity CompileとConsoleまで確認済みであり、実際のFaceUp、BustUp、複数Actorの画面構図は未評価である。初期Presetは評価候補であり、Gold Masterではない。

## 3. Next — Framing and Physical Camera Acceptance

次の1縦切りでは機能を増やさず、ユーザーの作業用SceneでActor構図とPhysical Cameraの基準値を確定する。

- 1人のActorでBustUp、CloseUp、FaceUpのHeadroomと切れ位置を確認する。
- 身長と体格が異なる複数ActorでGroup Framingの余白と中心を確認する。
- Actorの移動、屈み、腕振り中にHead、Bust、Body Radiusが過不足なく機能するか確認する。
- FixedとSpline ShotでDolly Onlyの補正量とMotion意図が競合しないか確認する。
- 既定Sensor Size 36 x 24mm、Near 0.1m、Far 1000m、Dolly Range -5mから+5mをScene条件に合わせて評価する。
- Shot SizeごとのGroup Framing SizeとActor Radiusを、代表Sceneの基準値として確定する。
- ValidatorのBounding Sphere距離とNear Clip警告が実映像の問題と一致するか確認する。

数値は映像比較から調整し、Eyes推定、顔ランドマーク、独自Aim Solver、Occlusion Solverを先に追加しない。

## 4. Then — Motion Quality and Gold Masters

FramingとPhysical Cameraの基準値確定後、ユーザーのSceneで現在のPaletteを評価する。

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

- App UIによるCamera Palette全体とProgram表示
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
