# カメラ・画面・地形

このプロジェクトでは、カメラや画面の色合いを自分で作らずに、**Unity にもとからある機能** を使っています。

## カメラ（Cinemachine）

| オブジェクト | 役割 |
| --- | --- |
| MainCamera | 実際に画面を映すカメラ |
| FollowCamera | 車を追いかけて、MainCamera を動かす係 |

- FollowCamera の **Tracking Target** に車を入れると、その車を追いかけます。
- **Follow Offset** で、車から見たカメラの位置を変えられます（例：Y を 8 にすると、上から見下ろす）。
- **Position Damping** を大きくすると、カメラがふわっと遅れてついてきます。

> 💡 Cinemachine（シネマシーン）は、Unity のカメラを動かすための公式の道具です。

## 画面の色合い（Volume）

**GlobalVolume** というオブジェクトで、画面全体の見た目を整えています。
設定は `Assets/Settings/PostProcessProfile` に入っています。

| 効果 | 内容 |
| --- | --- |
| Tonemapping | 明るすぎる所や暗すぎる所を、見やすく整える |
| Bloom | 明るい所を、ふわっと光らせる |
| Vignette | 画面の四すみを少し暗くする |
| Color Adjustments | はっきりさ・色の濃さ |

それぞれの左のチェックを外すと、その効果が消えます。どれがどの見た目を作っているか、比べてみましょう。

## 地形（Terrain）と自然

00_Sample の地面は **Terrain（テレイン）** という、地形を作るための部品でできています。
Terrain を選ぶと、Inspector に筆のマークの道具が出ます。

| 道具 | できること |
| --- | --- |
| Raise or Lower Terrain | 地面を盛り上げる・へこませる |
| Paint Texture | 草・土・砂・岩の色を塗る |
| Paint Trees | 木や岩を植える（Ctrl を押しながらで消す） |

- 木や岩は `Assets/Prefabs/Nature/` にあります。Unity にもとからある形（四角・円柱など）を組み合わせて作りました。
- 水面は **Water**、その少し下に **KillZone_Water** を置いて、「水に落ちたらやり直し」にしています。
- 遠くがかすんで見えるのは **霧（Fog）** です。メニューの Window → Rendering → Lighting の **Environment** で変えられます。

---

[目次に戻る](../index.md)
