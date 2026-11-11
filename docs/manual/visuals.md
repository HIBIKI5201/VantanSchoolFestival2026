# カメラ・画面・地形

このプロジェクトでは、カメラや画面効果を自分で作らず、**Unity の公式機能** を使っています。

## カメラ（Cinemachine）

| オブジェクト | 付いているもの | 役割 |
| --- | --- | --- |
| MainCamera | Camera、CinemachineBrain | 実際に画面を映すカメラ |
| FollowCamera | CinemachineCamera、CinemachineFollow、CinemachineRotationComposer | 「どこから・何を映すか」を決める仮想カメラ |

- **Tracking Target**（FollowCamera）に車を入れると、車を追いかけます。
- **Cinemachine Follow → Follow Offset** でカメラの位置を変えられます（例：Y を 8 にすると上から見下ろす）。
- **Position Damping** を大きくすると、カメラがふわっと遅れてついてきます。

## 画面効果（Volume / ポストプロセス）

**GlobalVolume** の Volume Profile（`Assets/Settings/PostProcessProfile`）で、画面全体の見た目を調整しています。

| 効果 | 内容 |
| --- | --- |
| Tonemapping | 明るさの幅を整える |
| Bloom | 明るい部分を光らせる |
| Vignette | 画面の四隅を少し暗くする |
| Color Adjustments | コントラスト・彩度 |

チェックを外すと効果が消えます。どれがどの見た目を作っているか、比べてみましょう。
メインカメラの **Post Processing** が OFF だと効果が出ません。

## 地形（Terrain）と自然

00_Sample の地面は **Terrain** でできています。Terrain を選ぶと、Inspector に筆のアイコンが出ます。

| ツール | できること |
| --- | --- |
| Raise or Lower Terrain | 地面を盛り上げる・へこませる |
| Paint Texture | 草・土・砂・岩の色を塗る |
| Paint Trees | 木や岩を植える（Ctrl を押しながらで消す） |

- 木と岩は `Assets/Prefabs/Nature/` のプレハブを、Terrain の **Trees** として登録しています。
- 木や岩はすべて、Unity 標準の形（Cube・Cylinder など）とスクリプトで作った形でできています。外部の素材は使っていません。
- 水面は **Water**、その少し下の **KillZone_Water** で「水に落ちたらやり直し」にしています。
- 遠くがかすんで見えるのは **霧（Fog）** です。Window → Rendering → Lighting → Environment で設定できます。

---

[目次に戻る](../index.md)
