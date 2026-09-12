# カメラ支援仕様

## 1. 目的

カメラ支援は、オペレーターが選んだCamera Performanceを安定して再生し、意図した構図とLensを維持しながら安全に手動介入できるようにする。

システムが次のShotを判断したり、映像上の意図を推測して別の軌道へ変更したりしない。

## 2. 現在行う支援

- Distance単位によるSpline Dolly再生
- DurationとProgress CurveによるPreset固有の時間設計
- Static / Rolling Entry
- Aim ProxyによるTarget基準とShot固有Aimの分離
- Cinemachine Rotation ComposerによるScreen Position、Dead Zone、Hard Limits、Damping、Lookahead
- PresetによるLens Track
- Presetによる明示的なRoll
- Speed、Reverse、Hold、Resume時の連続性維持
- Invalid値と参照欠落時に現在のProgramを失わない処理
- Editor上のMotion Validator

可能な限りCinemachine 3とUnity Splinesの標準機能を使用する。

## 3. Motion Evaluator

Motion Evaluatorは、解決済みMotion設定とPlayback Timeから次を決定的に計算する。Runtime再生ではShotの適用済み設定を入力する。EditorでPresetを診断するときはPreset、Rig Scale、正面基準、Rig Profileをメモリ上で解決してから同じEvaluatorへ入力し、Sceneを変更しない。

- Spline Distance
- Aim Offset
- Screen Position
- Lens値
- Roll

Runtime再生とEditor Validatorは同じEvaluatorを使用する。EvaluatorはProgram選択、入力、Scene生成、Asset保存を行わない。

## 4. Aim支援

Aim Proxyは現在のTarget Poseと、Shotへ適用済みのTarget Height、基準向き、Aim Offsetから導出する。Aim Proxy自体へ追従判断や独立した設定を持たせない。

初期の追従支援はCinemachine Rotation Composerを使用する。

- Screen Position
- Dead Zone
- Hard Limits
- Horizontal / Vertical Damping
- Lookahead
- Lookahead Smoothing
- Center On Activate

Targetを常に画面中央へ固定しない一方、三分割法やHeadroomを一律の正解として自動強制しない。構図値はPresetが所有し、ユーザーのSceneで確認する。

独自の反応遅延、Overshoot、構図Solverは、Cinemachineだけでは必要な動きが作れないと映像で確認されるまで追加しない。

## 5. Rig Profileによる支援

Rig Profileは次へ使用する。

- Preset作成時の初期値
- Rebuild時にShotへ適用するSpeed、Hold、Resume、Reverseの応答
- Validatorの推奨範囲

Horizon特性と選択的なNoiseは、現在の映像を確認して必要性が認められた後にRig Character段階で追加する。

Rig ProfileはCameraをRigidbodyとしてシミュレーションしない。メーカー公称最高速度や、出典の異なる数値を業界標準として固定しない。

Rig Profile Assetの変更は生成済みShotへ暗黙伝播させず、明示的なRebuildで反映する。

## 6. 手動操作との関係

手動操作は適用済みMotionの評価結果へ非破壊に合成する。

- SpeedはPlayback Timeの進み方だけを変更する。
- Body、Aim、Composition、Lens、Rollは同じPlayback Timeを使用する。
- Holdは設定された減速で停止する。
- Resumeは現在位置から連続的に再開する。
- Reverseは減速、停止、逆方向への加速として処理する。
- 緊急の即時停止はFreezeとしてHoldと分ける。

Motion FoundationではPan、Tilt、Screen Position、Zoomのライブトリムを追加しない。実際のKeyboard運用を確認してから、同じ非破壊Trim層へ追加する。

## 7. Motion Validator

Validatorは自動テストや映像品質の採点器ではなく、破綻候補を見つけるEditor診断である。

自動診断する項目:

- 無効値
- Progress Curveの範囲と単調性
- 移動速度、加速度、Jerk
- 角速度、角加速度
- 曲率とTangentの急変
- Lens変化速度
- In / Out Pointの速度
- AimのScreen Space位置
- Horizon Roll
- Targetとの最短距離
- Near Clip侵入

人が確認する項目:

- 動きの動機
- 楽曲、振付、照明との相性
- 機材らしさ
- 有人感
- 前後Shotとのつながり
- Foreground Occlusionの意図
- 長時間視聴時の疲労

Validatorの閾値は検証開始値とし、警告を合否へ変換しない。既定ではSpline、Aim、Lens、Durationを自動修正しない。

## 8. Runtimeの安全境界

- 無効なShotをProgramへ選択しない。
- 選択失敗時は現在のProgramを維持する。
- NaNまたはInfinityをCinemachineへ適用しない。
- Target欠落時は現在状態を維持し、警告を1回表示する。
- Spline欠落時はそのMotion Shotを無効として扱う。
- 異常時に別Targetや別Shotへ自動切り替えしない。
- RuntimeでScene、Spline、Assetを生成、削除、保存しない。
- 安全処理でPresetの意図を別のCamera Performanceへ置き換えない。

## 9. 現在行わない支援

- 独自Aim Solver
- RigidbodyによるCamera機材シミュレーション
- Runtimeの軌道再生成
- 自動的な遮蔽回避
- Spline Knotの暗黙修正
- 複数Targetの自動選択
- Focus、Iris、Exposureの自動演出
- Shot推薦、楽曲解析、自動Take
- Runtime AI

設定不足を予測不能な自動探索や代替演出で補わない。

## 10. 受け入れ

1. Shotへ適用されたBody、Aim、Composition、Lensが同じPlayback Timeで評価される。
2. StaticとRollingの開始状態が壊れない。
3. 手動Speed、Hold、Resume、ReverseでMotionが不連続に飛ばない。
4. Invalid値で現在のProgramを失わない。
5. ValidatorがAssetやSceneを暗黙変更しない。
6. Validatorの数値警告と映像品質の判断が区別される。
7. 実装エージェントがCompile結果を構図やMotionの映像確認として報告しない。

構図、Motion、機材らしさ、疲労はユーザーが自身の作業用Sceneで確認する。
