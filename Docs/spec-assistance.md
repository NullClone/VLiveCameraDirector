# カメラ支援仕様

## 1. 目的

初期版の支援は、オペレーターが選んだShotをCinemachine 3で安定して成立させることに限定する。システムが次のShotを判断したり、独自計算で構図を作り直したりしない。

## 2. 初期版で行う支援

- CinemachineCameraによる対象追従
- Composerによる画面内構図
- Dampingによる位置と回転の平滑化
- Spline Dollyによるレーン移動
- Speed、Reverse、Hold、Resume時の連続性維持
- Bの始点から終点までの加速、移動、減速、収束
- 無効参照時に現在のProgramを失わない処理

これらは可能な限りCinemachine 3標準機能を使用する。

## 3. 行わない支援

初期版では次を実装しない。

- 独自Assistance Solver
- 自動的な遮蔽回避
- 複数Targetの自動選択
- LensやFocusの自動演出
- Shotの自動推薦
- 楽曲解析
- 自動Take
- 実行時AI

設定不足を補うために、予測不能な自動探索や代替演出を行わない。

## 4. 切り替えだけで成立する条件

初期版ではAを安定した戻り先、Bを移動による変化として制作する。各Shotは制作時に次を調整する。

- 開始時の位置と向き
- Tracking Target
- 画面内の対象位置
- Damping
- Spline速度と方向
- Fixed Shotとして保持するか
- 移動の始点、終点、加減速、終点Hold

Programへ選択した後、追加操作がなくてもその設定で画が成立することを、ShotごとにGame Viewで確認する。Bは「止め→動き→止め」を基本とし、無限に漂わせない。

## 5. 手動操作との関係

- Speed、Reverse、Hold、ResumeはSpline進行だけを変更する。
- Hold中もCinemachineのTrackingと構図補正は継続できる。
- 手動操作のための汎用チャンネル合成機構は作らない。
- Pan、Tilt、Zoomを追加するときに、実際の競合を見て合成方法を決める。

## 6. 異常時

- 無効なShotはProgramへ選択しない。
- 選択処理に失敗した場合は現在のProgramを維持する。
- Targetが欠落した場合はCinemachineCameraの現在状態を維持し、警告を1回表示する。
- Splineが欠落したSpline Shotは無効として扱う。
- 異常時に別Targetや別Shotへ自動切り替えしない。

## 7. 将来の支援

初期版とProgram / Preview運用が安定した後、必要性を確認して次を検討する。

1. Pan、Tilt、Zoomの手動トリム
2. Pattern CueとTimeline / BPM同期
3. 次Shot候補のPreview提示
4. 明示的に許可された範囲での半自動化

これは現在の実装契約ではない。将来機能のためのinterfaceや空設定を先に追加しない。

## 8. 初期受け入れ条件

1. 操作なしでも各Shotの構図と動きが成立する。
2. Dampingによって追従が滑らかである。
3. Aが視聴者の理解を戻せる安定構図になっている。
4. Bが予備動作、加速、減速、終点Holdを持ち、機械的な無限移動に見えない。
5. Hold中も必要なTrackingが継続する。
6. TargetやSpline欠落でカメラが無関係な方向へ飛ばない。
7. 独自Solverや自動推薦が実装されていない。
