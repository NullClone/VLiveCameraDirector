# カメラ・パターン仕様

## 1. 目的

本仕様は、ライブで呼び出す Shot、カメラリグ、レーン、Motion Pattern、手動介入のデータモデルを定義する。

## 2. すべてのショットはレーンを持つ

すべての Shot は、位置と向きの時間変化を定義するレーンを持つ。レーンは必ずしも移動を伴わない。

- Fixed Shot は長さ 0 または一定位置を保持する Hold Lane として表現する。
- Dolly、Crane、Orbit 等は Spline またはパラメトリックなレーンを使用する。
- Handheld は基準レーンへ制約された微細運動を重ねる。
- Tracking は対象に追従しつつ、基準となる構図レーンを維持する。

これにより、静止と移動を別系統にせず、選択、再生、Hold、Reverse、時間同期を共通化する。

## 3. Camera Shot

Camera Shot は本番で選択される最小単位であり、少なくとも次を保持する。

- 永続的で重複しない Shot ID
- 表示名、Bank、並び順、タグ
- 使用する Cinemachine Rig と Lane
- Motion Pattern
- Tracking Target の解決規則
- Framing、Lens、Focus の初期値と制限
- Take 時の開始ポリシー
- 推奨 Transition
- Multiview 用サムネイルと短い説明
- バージョン、作者、生成元、検証状態

Scene 固有の対象は Target Resolver で解決し、再利用可能な Shot アセットへ不安定な Scene 参照を固定しない。

## 4. Motion Pattern の位相

パターンは次の位相を任意に持つ。

| 位相 | 目的 |
| --- | --- |
| Entry | Take 直後に画を成立させ、Main へ導入する |
| Main / Loop | ショットの主運動を継続する |
| Exit | 次の遷移へ向けて画を整える |
| Hold | 意図的に位置、構図、レンズを保持する |

位相の省略は可能だが、Take 直後の状態は必ず定義する。Loop 境界で位置、速度、回転、レンズに不連続が生じないよう Editor 検証を行う。

## 5. Take 時の開始ポリシー

| ポリシー | 動作 | 主な用途 |
| --- | --- | --- |
| `RestartOnTake` | Entry の先頭から開始 | 初期既定。確実な演出開始 |
| `ShowTimeSync` | ライブ共通時間から位相を計算 | 周期運動の同期 |
| `BeatSync` | 次の拍または指定拍位置へ同期 | 楽曲に合わせた移動 |
| `Resume` | 前回の再生位置から再開 | 継続する空間運動 |

初期実装では `RestartOnTake` を既定とし、他ポリシーは時間モデルと Preview 再現性を検証した後に追加する。

## 6. カメラ文法

Motion Pattern は次の軸を組み合わせて表現する。

| 軸 | 代表値 |
| --- | --- |
| Rig | Fixed、Dolly、Crane、Orbit、Handheld、Tracking |
| Framing | Wide、Full、Medium、CloseUp、Detail |
| Subject | Solo、Duo、Group、Stage、Audience、Prop |
| Movement | Push、Pull、Truck、Pedestal、Arc、Orbit、Hold |
| Timing | Duration、Speed Curve、Beat、Entry、Exit、Loop |
| Aim | Target、Bone、Group Center、Screen Position、Offset、Damping |
| Lens | Focal Length、Zoom Curve、Focus、Aperture、Dutch |
| Character | Smooth、Energetic、Heavy、Floating、Handheld |
| Variation | Amplitude、Direction、Seed、Intensity Range |

文法はタグだけでなく、Runtime が解釈できる型付きデータとして保存する。自由記述値だけで運動を決定しない。

## 7. Lane と進行

- Lane Position は原則として正規化値と実距離の両方を参照できる。
- 速度は Lane の長さに依存する値と、ショット所要時間に依存する値を区別する。
- Ease Curve は位置曲線と速度曲線の意味を混同しない。
- Reverse は同じレーンを逆評価し、注視やレンズの方向依存カーブも規則に従って反転する。
- Hold は時間源を失わずに進行だけを停止できる。
- Resume 時の追いつき、時間同期維持、ローカル再開をポリシーとして定義する。
- Spline 変更後は Shot の開始・終了・ループ・構図を再検証する。

## 8. 構図とターゲット

Shot は、対象を単なる Transform 参照ではなく意味で指定できる。

- Performer ID または Role
- Humanoid Bone または明示 Anchor
- 複数対象の Group Center
- Stage Anchor、Audience Anchor、Prop Anchor
- 画面内の目標位置と余白

ターゲットが一時的に失われた場合は、最後の有効な構図を短時間保持する。代替対象の選択は Shot に明示されている場合だけ行い、無関係な対象へ自動で向けない。復帰時は Damping を通して再取得する。

## 9. レンズとフォーカス

Lens と Focus は位置運動と同じ Pattern 内で同期できるが、独立した制御チャンネルを持つ。

- 焦点距離、フォーカス距離、絞り、Dutch の既定値と許容範囲を保存する。
- Dolly と Zoom の組み合わせはプリセット化できる。
- Focus Target が無効な場合は定義済み距離を保持する。
- 物理カメラ設定の有無を Shot ごとに曖昧にせず、プロジェクト方針で統一する。
- 手動 Zoom / Focus からの復帰は画面上の急変を起こさない。

## 10. 手動トリム

Pattern の出力を基準値とし、[spec-operation.md](spec-operation.md) のモードで手動値を合成する。トリムは原則として Shot アセットそのものを書き換えず、本番セッション状態として保持する。

Shot を再選択したときにトリムを Reset、Resume、セッション保持のどれにするかをチャンネル別に指定できる。初期既定は、Rail Hold と方向は Reset、画角トリムは滑らかに Reset とする。

## 11. AI 生成契約

AI は Camera Shot または Motion Pattern のデータ候補を生成する。生成物は次を満たすまで本番利用可能にしない。

1. スキーマ、範囲、必須参照の静的検証を通る。
2. Lane の開始、終了、Loop 連続性を検証する。
3. 対象別の Preview Scene で衝突、遮蔽、構図、速度を確認する。
4. 作者、生成モデルまたは生成経路、Seed、バージョンを記録する。
5. 人が承認済み状態へ変更する。

AI に固有 MonoBehaviour や専用 Runtime 分岐を量産させない。表現できない動きは、まず共通プリミティブとして設計・検証し、その後にデータ生成へ開放する。

## 12. ライフサイクル

- Standby は再生位置と必要最小限の論理状態だけを保持する。
- Preview 選択時にターゲット、Spline、Lens、必要な Renderer を Prewarm する。
- Take 時に Preview と異なる初期化結果を出さない。
- Program から外れた Shot は Exit / Hold / Suspend の設定に従う。
- アセットの Hot Reload は Editor 開発用とし、本番 Player では不完全な差し替えを許可しない。

## 13. 受け入れ条件

1. Fixed を含むすべての Shot が共通 Lane モデルで動作する。
2. Take 直後、操作なしでも定義された画と運動が成立する。
3. Entry、Loop、Exit、Reverse、Hold、Resume で不連続がない。
4. Preview と同一条件の Take で、開始位置、構図、レンズが再現される。
5. ターゲット消失時に無関係な方向へカメラが飛ばない。
6. AI 生成を含むパターンをコード追加なしで検証・登録できる。
