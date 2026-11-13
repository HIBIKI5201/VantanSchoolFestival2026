# コンポーネント一覧

このプロジェクトで作った部品（コンポーネント）の一覧です。
Inspector の **Add Component** → **VantanSchoolFestival2026** の下に、種類ごとに並んでいます。
Unity の中では、それぞれの部品の **「▶ このコンポーネントについて」** でも同じ説明が読めます。

## 車の本体とゲームの進行

| 部品 | 何をするか | よく変える数字 |
| --- | --- | --- |
| **RCCar** | ラジコンカーの本体。これだけでは走らない。丸いタイヤの当たり判定で、低い段差は自然に乗り越える | Center Of Mass（重さの中心。低いほど転びにくい）、Upright Strength（傾いたときに戻す強さ） |
| **GameManager** | ゲームの進行係。スタートの合図・タイム・ゴールの判定 | Countdown Seconds（スタートまでの秒数）、Require All Checkpoints（チェックポイントを全部通らないとゴールできない） |

## 操作（PlayerController に付いている）

| 部品 | 何をするか | よく変える数字 |
| --- | --- | --- |
| **PlayerCarInput** | キーボードやゲームパッドの操作を受け取って、車を動かす | Target Car（動かす車） |
| **ActionMapSwitcher** | Tab キーで、操作のキーを W A S D ⇔ 矢印キーに切り替える | — |

## 駆動方式（車に 1 つだけ付ける）

| 部品 | 走り方 | よく変える数字 |
| --- | --- | --- |
| **ArcadeDrive** | 素直に加速して、ピタッと曲がる | Max Speed（いちばん速いスピード）、Turn Speed（曲がる速さ） |
| **DriftDrive** | 勢いが残って、横に滑る | Grip（小さいほど滑る） |
| **TankDrive** | 止まっていても、その場で回れる | Turn Speed（回る速さ） |

## 追加パーツ（いくつでも付けられる）

| 部品 | できるようになること | よく変える数字 |
| --- | --- | --- |
| **JumpParts** | Space キーでジャンプ | Jump Power（ジャンプの強さ）、Air Jumps（空中でジャンプできる回数） |
| **BoostParts** | 左 Shift キーを押している間スピードアップ | Speed Multiplier（何倍の速さか）、Max Energy（燃料の量） |
| **ColorParts** | 車の色を変える | Body Color（色）、Targets（色を変える部分） |

## コースの部品

| 部品 | 何をするか | よく変える数字 |
| --- | --- | --- |
| **StartPoint** | スタートの場所と向き。シーンに 1 つ | — |
| **Goal** | 入るとゴール | — |
| **Checkpoint** | 中間地点。通ると、落ちたときにここから再開できる | — |
| **KillZone** | 入ると前の場所に戻される（落下・水） | — |
| **SpeedPad** | 通るとスピードアップ | Push Power（押し出す強さ）、Duration（速くなる時間） |
| **Bumper** | ぶつかると、はじき飛ばされる | Bounce Power（はじく強さ） |
| **MovingFloor** | 行ったり来たりする床。乗った車も運ぶ | Move Offset（どこまで動くか）、Travel Time（片道の時間） |
| **CourseArea** | 走ってよい範囲を表す見えない箱。ここから出るとコースアウト | — |
| **OutOfBoundsJudge** | コースアウトを見張る係 | Grace Time（コースの外にいられる秒数） |

## 画面の表示（GameUI に付いている）

| 部品 | 何を表示するか |
| --- | --- |
| **HudView** / **HudPresenter** | タイム・カウントダウン・チェックポイントの数・燃料ゲージ・「コースアウト」 |
| **ControlsHintView** / **ControlsHintPresenter** | 画面左下の操作方法 |

> 💡 プログラムがどんな考え方で作られているかは、[上級 3. しくみを知ろう](../advanced/03_how-it-works.md) で説明しています。

## 演出（プレハブにはじめから入っている）

授業では使いませんが、見た目を良くするための部品です。外しても、ゲームはそのまま遊べます。

| コンポーネント | 役割 | 付いている場所 |
| --- | --- | --- |
| **CarDustEffect** | 走っているときに砂ぼこりを出す | RCCar の子の `Effects` |
| **BoostEffect** | ブースト中に炎と光の帯を出す | RCCar の子の `Effects` |
| **CameraShakeOnHit** | 強くぶつかったら画面をゆらす | RCCar の子の `Effects` |
| **WaterSplash** | 水に落ちたら水しぶきを出す | Water の子の `SplashTrigger` |
| **AnimatorTriggerOnEnable** | 表示されたらアニメーションを切り替える | Goal の子の `GoalEffect` |

> 💡 くわしくは [上級 4. かくれた演出を見つけよう](../advanced/04_hidden-effects.md) を見てください。

---

[目次に戻る](../index.md)
