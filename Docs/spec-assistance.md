# カメラ支援仕様

## 1. 目的

初期支援は、オペレーターが選んだShotをCinemachine 3で安定して成立させることに限定する。システムが次のShotを判断したり、独自計算で構図を作り直したりしない。

## 2. 現在行う支援

- CinemachineCameraによる1人のTarget追従
- Composerによる画面内構図
- Dampingによる位置と回転の平滑化
- Spline DollyによるPreset移動
- Cut後の自動再生
- Speed、Reverse、Hold、Resume時の連続性維持
- 始点から終点までの移動、減速、終点Hold
- 無効参照時に現在のProgramを失わない処理

可能な限りCinemachine 3とUnity Splinesの標準機能を使用する。

## 3. 切り替えだけで成立する条件

各Presetは制作時に次を決める。

- 開始位置
- Tracking Target
- 画面内のTarget位置
- Field of View
- Damping
- Spline形状と初期速度
- 終端への減速とHold

Fixedは安全な戻り先、移動Shotは「止め、動き、止め」を基本とする。無限に漂わせない。

## 4. 手動操作との関係

- Speed、Reverse、Hold、Resumeは現在のSpline進行だけを変更する。
- Hold中もCinemachineのTrackingとAimを継続できる。
- 手動Transform入力と自動移動を合成する汎用機構は作らない。
- Pan、Tilt、Zoomを追加するときに、実際の競合を見て合成方法を決める。

## 5. 行わない支援

- 独自Assistance Solver
- 自動的な遮蔽回避
- 複数Targetの自動選択
- LensやFocusの自動演出
- Shot推薦、楽曲解析、自動Take
- Runtime AI

設定不足を予測不能な自動探索や代替演出で補わない。

## 6. 異常時

- 無効なShotはProgramへ選択しない。
- 選択失敗時は現在のProgramを維持する。
- Target欠落時は現在状態を維持し、警告を1回表示する。
- Spline欠落時はその移動Shotを無効として扱う。
- 異常時に別Targetや別Shotへ自動切り替えしない。

## 7. 受け入れ

構図、動き、プロらしさはユーザーが自身の作業用Sceneで確認する。実装エージェントはCompileと簡易Reviewだけで見た目を確認済みと報告しない。

初期Paletteが成立した後、Pan、Tilt、Zoom、Timeline Cue、推薦、半自動Takeを順に検討する。これらのinterfaceや空設定を先に追加しない。
