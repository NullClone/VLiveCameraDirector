# 次作業エージェント向け実装プロンプト

以下をそのままAntigravityのGemini Flash 3.8 highへ渡す。

---

Unityパッケージ`VLiveCameraUnit`のA/Bカメラ最小版を、調査、旧コード整理、実装、Unity検証、コミットまで一度で完成させてください。途中の細かな判断でユーザー確認を挟まず、仕様内で最も単純な方法を選んでください。

## 作業場所

`E:\Unity\Project\MMD\Assets\toshi.VLiveKit\VLiveCameraUnit`

## 仕様

最初に`AGENTS.md`と、`Docs/architecture.md`、`Docs/spec-camera.md`、`Docs/spec-operation.md`、`Docs/spec-switching.md`、`Docs/phases.md`を読んでください。このプロンプトが今回の範囲であり、文書中の将来構想は実装しません。

プロジェクト本体はUnity `6000.3.19f1`、Cinemachine `3.1.7`です。パッケージ側にはUnity 2022.3 / Cinemachine 2.9.7指定と旧API依存コードが残っています。旧版とのAPI、SerializedField、Prefab、Scene互換性は不要です。

## 完成条件

- 出力は1台のUnity CameraとCinemachine Brainを使用する。
- Shot AとShot Bは、別々のCinemachineCameraを持つ。
- Aは正面ミドルを想定した安定したFixed Shotとする。
- BはSpline始点から終点へ移動し、減速して終点でHoldする。
- キー1 / 2でA/BをCinemachineのCutとして切り替える。
- AがLiveの間にBを始点へ準備し、BへCutした後に移動を開始する。
- Live中のBはResetせず、Aへ戻ってから次の使用へ向けて始点へ戻す。
- BのSpeed Up / Down、Reverse、Hold、Resumeを操作できる。
- 同じShotの再選択は何もせず、無効な参照や入力でも現在のProgramを維持する。
- Cut成功時にProgram Shot名を1回だけLogする。

主要なRuntime型は原則`VLiveCameraShot`、`VLiveCameraSwitcher`、`VLiveCameraKeyboardInput`の3つとし、`toshi.VLiveKit.Camera` namespaceへ置いてください。正確なCinemachine API、フィールド構成、更新方法は、インストール済みPackage sourceを確認して決めて構いません。

## 旧コードの整理

Cinemachine 2へ依存する旧カメラRuntime、専用Editor、専用Test、直接依存するDevelop試作コードは、A/B最小版に不要なら削除または置換して構いません。`package.json`をUnity 6.3 / Cinemachine 3.1.7へ合わせ、Runtime / Editor asmdefも必要な参照とplatformへ整理してください。

削除前にGUIDと参照を確認し、PrefabへMissing Scriptを残さないでください。不要な試作Prefabは依存確認後に一緒に削除して構いません。`.meta`も対応させてください。

次は今回変更しません。

- `Runtime/LivePerformer/`
- `Runtime/LiveTimeline/`
- `Runtime/MirrorCamera/`
- `Runtime/SplitLines/`
- `Runtime/LetterBox/`
- `Develop/`内の無関係な素材、モデル、マテリアル、コード
- 既存の本番Scene

新しいSwitcherは既存の`Runtime/Switching/`へ置いてください。今回保持するコードのnamespace統一や、タスク外のリファクタリングは行いません。

## 確認用Scene

既存Sceneを変更せず、`Tests/`配下へA/B確認用Sceneを作成してください。A/Bの構図差を明確にし、Cut、Bの始点から終点までの移動、終点Hold、Speed、Reverse、Hold、ResumeをPlay ModeとGame Viewで確認できる状態にしてください。必要なTargetや床はPrimitiveで構いません。

## 非目標

同じCinemachineCameraへの別Shot設定の上書き、A/B汎用Slot、Preview、Take、Tally、Bank、Multiview、MIDI、AI、Recommendation、Pattern Asset、独自Solver、Command Bus、DI、互換wrapper、将来用interfaceは実装しません。

## 検証と完了

`git diff --check`、旧Cinemachine 2 APIの残存、Missing Script、Unity Import、Domain Reload、Runtime / Editor Compile、Play Mode、Game Viewを確認してください。A/B Cut、Bの一連の移動、手動操作、同一Shot再選択、無効参照時の安全性を実際に確認してください。長時間確認を行わない場合は未確認と明記してください。

既存の作業ツリー変更を上書きせず、今回の変更だけをステージしてください。検証後、`feat: A/Bカメラ切り替え基盤を実装`でコミットし、Branch作成とPushは行わないでください。

完了報告には、実装した挙動、変更・削除ファイル、検証結果、未確認事項、Commit IDを記載してください。仕様内で解決できない重大な障害がある場合だけ、変更を最小限に保って原因と必要な判断を報告してください。

---
