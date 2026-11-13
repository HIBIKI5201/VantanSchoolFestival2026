# 上級 4. かくれた演出を見つけよう

このプロジェクトには、授業では使わない **演出（見た目や手ざわりを良くするしかけ）** が、こっそり入っています。
どれも Unity にもともと入っている機能で作っています。見つけたら、中身をのぞいて、数字を変えて遊んでみましょう。

> 演出の部品は、すべて `Assets/Effects/`（見た目のデータ）と `Assets/Scripts/Effects/`（スクリプト）に入っています。
> 演出が無くても、ゲームはそのまま遊べます。

---

## 見つけてみよう

| どこで見られる？ | 演出 | 使っている Unity の機能 |
| --- | --- | --- |
| 車が地面を走っているとき | タイヤの後ろに砂ぼこり | **Particle System** |
| ブーストを使ったとき | マフラーから炎 | **Particle System** |
| ブーストを使ったとき | 車の後ろに光の帯 | **Trail Renderer** |
| 中間地点（チェックポイント） | 光の粒がずっと立ちのぼる | **Particle System**（ループ） |
| ゴールしたとき | 紙吹雪 | **Particle System**（1 回だけ） |
| 水に落ちたとき | 水しぶき | **Particle System** と **プレハブ** |
| 湖 | ゆらゆら動く水面 | **Shader Graph** |
| 加速床 | 前に流れる矢印 | **Shader Graph** |
| 強くぶつかったとき・高いところから着地したとき | 画面がゆれる | **Cinemachine Impulse** |
| ゴールの旗 | ゆっくりゆれる。ゴールすると大きくはねる | **Animator** と **Animation** |

---

## 1. Particle System（パーティクル）

小さな粒（パーティクル）をたくさん出して、煙・炎・紙吹雪などを表す機能です。

**見てみよう**

1. Project ウィンドウで `Assets/Prefabs/Player/RCCar` をダブルクリックして、プレハブを開きます。
2. Hierarchy で `Effects` → `Dust_RL` を選びます。
3. Scene ビューの右下に出る **Particle Effect** の窓で **Play** を押すと、粒が出るようすを見られます。

**変えてみよう**

| 項目 | 変わること |
| --- | --- |
| Start Lifetime | 粒が消えるまでの時間 |
| Start Speed | 粒が飛び出す速さ |
| Start Size | 粒の大きさ |
| Start Color | 粒の色 |
| Emission | 1 秒（または 1 メートル）あたりに出す粒の数 |
| Shape | 粒が出てくる形（円すい・箱など） |

> 砂ぼこりの量は、車の速さに合わせて `CarDustEffect` が変えています。
> 走っていないときに出ないのはそのためです。

---

## 2. Trail Renderer（光の帯）

動いたあとに、線（帯）を残すコンポーネントです。
車のプレハブの `Effects` → `BoostTrail_L` / `BoostTrail_R` に付いています。

| 項目 | 変わること |
| --- | --- |
| Time | 帯が残る時間（長くすると、帯が長くなる） |
| Width | 帯の太さ（グラフで、始めと終わりの太さを決める） |
| Color | 帯の色（右に行くほど、帯の後ろのほうの色） |

ブーストを使っている間だけ帯が出るのは、`BoostEffect` が **Emitting** の ON / OFF を切り替えているからです。

---

## 3. Shader Graph（水面・加速床）

**シェーダー** は「物の表面をどんな色で描くか」を決めるプログラムです。
**Shader Graph** を使うと、プログラムを書かずに、四角い箱（ノード）を線でつないでシェーダーを作れます。

**見てみよう**

1. `Assets/Effects/Shaders/SG_Water` をダブルクリックすると、Shader Graph の画面が開きます。
2. 左から右へ、線をたどってみましょう。
   - **Position**（場所）と **Time**（時間）から、
   - **Gradient Noise**（もやもやした模様）を作り、
   - 時間で動かして、2 つ重ねて、
   - 模様の明るいところを白っぽくして（**Smoothstep** と **Lerp**）、
   - いちばん右の **Base Color**（表面の色）につないでいます。
3. 加速床の `SG_SpeedPad` は、**UV**（表面の場所）から「へ」の字の模様を作り、**Time** で前に流しています。

**変えてみよう**

`Assets/Effects/Materials/M_Water` や `M_SpeedPad` を選ぶと、Inspector で色や速さを変えられます。

| マテリアル | 項目 | 変わること |
| --- | --- | --- |
| M_Water | Water Color / Foam Color | 水の色 / 波の白いところの色 |
| M_Water | Wave Speed / Wave Scale | 波の速さ / 波の細かさ |
| M_SpeedPad | Arrow Color | 矢印の色（光って見える） |
| M_SpeedPad | Scroll Speed / Arrow Count | 矢印の流れる速さ / 矢印の数 |

---

## 4. Cinemachine Impulse（画面のゆれ）

カメラは Cinemachine という Unity の機能で動いています。その中の **Impulse** を使うと、カメラをゆらせます。

- 車の `Effects` に付いている **CinemachineImpulseSource** が、ゆれを「出す」側です。
- `FollowCamera` に付いている **CinemachineImpulseListener** が、ゆれを「受け取る」側です。
- いつゆらすかは、`CameraShakeOnHit` が決めています（速さが急に減ったら、ぶつかったとみなす）。

`CameraShakeOnHit` の **ゆれの大きさ** を大きくすると、ぶつかったときに大きくゆれます。

---

## 5. Animator と Animation（ゴールの旗）

**Animation** は「いつ、どの値をどう変えるか」を記録したデータです。
**Animator** は、いくつかの Animation を「状態」として持ち、条件で切り替えるコンポーネントです。

**見てみよう**

1. `Assets/Prefabs/Stage/Goal` を開いて、Hierarchy で `Banner` を選びます。
2. メニュー **Window → Animation → Animator** で、Animator の画面を開きます。
   - `Idle`（ゆっくりゆれる）と `Celebrate`（大きくはねる）の 2 つの状態があります。
   - 矢印（切り替え）の条件に、`Celebrate` というトリガーが使われています。
3. メニュー **Window → Animation → Animation** で、ゆれ方のグラフを見られます。

ゴールすると `GoalEffect` が表示され、そこに付いている `AnimatorTriggerOnEnable` が、旗の Animator に `Celebrate` のトリガーを送ります。

---

## もっとやってみよう

- 砂ぼこりの色を、白い雪のような色にしてみる
- 中間地点の光の粒の色を、チェックポイントの色に合わせてみる
- `SG_SpeedPad` の **Step** ノードを **Smoothstep** に変えて、矢印のふちをぼかしてみる
- `GoalBanner_Celebrate` のアニメーションを編集して、旗を回転させてみる

---

[← 上級 3. しくみを知ろう](03_how-it-works.md) ｜ [目次](../index.md)
