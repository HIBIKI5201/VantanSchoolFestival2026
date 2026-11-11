# コンポーネント一覧

このプロジェクトで作ったコンポーネントの一覧です。
**Add Component** → **VantanSchoolFestival2026** の下に、分類ごとに並んでいます。
Unity の中では、各コンポーネントの **「▶ このコンポーネントについて」** でも同じ説明が読めます。

## Core（中心になる部品）

| コンポーネント | 役割 | 主な設定 |
| --- | --- | --- |
| **RCCar** | 車の本体。接地判定・リスポーン地点・速度倍率を管理する。これだけでは動かない | Ground Check Distance（地面を探す距離）、Center Of Mass（重心） |
| **GameManager** | ゲームの進行。カウントダウン・タイム・チェックポイント・ゴール・やり直し | Countdown Seconds、Require All Checkpoints（全部通らないとゴールできない） |

## 入力

| コンポーネント | 役割 | 主な設定 |
| --- | --- | --- |
| **PlayerCarInput** | PlayerInput から届いた入力を、車への命令に変える | Target Car（操作する車。空なら自動で探す） |
| **ActionMapSwitcher** | Tab キーで操作方法（ActionMap）を切り替える | Map Names（切り替える順番） |

詳しくは [入力と操作方法](input.md) を見てください。

## 駆動方式（車に 1 つだけ付ける）

| コンポーネント | 走り方 | 主な設定 |
| --- | --- | --- |
| **ArcadeDrive** | 素直に加速して、ピタッと曲がる | Max Speed、Acceleration、Turn Speed |
| **DriftDrive** | 勢いが残って横に滑る | Grip（小さいほど滑る） |
| **TankDrive** | 止まっていてもその場で回転できる | Turn Speed |

すべて `DriveBase`（駆動方式のひな形）を継承しています。自分で作る方法は [5 章](../lesson/05_script.md) を見てください。

## 追加パーツ（好きなだけ付けられる）

| コンポーネント | できるようになること | 主な設定 |
| --- | --- | --- |
| **JumpParts** | ジャンプ | Jump Power、Air Jumps（空中で追加ジャンプできる回数） |
| **BoostParts** | 押している間スピードアップ | Speed Multiplier、Max Energy |
| **ColorParts** | 車の色を変える | Body Color、Targets（色を変える Renderer） |

## ステージ部品

| コンポーネント | 役割 | 主な設定 |
| --- | --- | --- |
| **StartPoint** | スタート位置と向き。シーンに 1 つ | — |
| **Goal** | 入るとゴール | Goal Effect（ゴール時に表示するもの） |
| **Checkpoint** | 中間地点。通るとリスポーン地点になる | Inactive / Active Visual |
| **KillZone** | 入ると最後のチェックポイントへ戻す（落下・水） | — |
| **SpeedPad** | 通ると加速する床 | Push Power、Speed Multiplier、Duration |
| **Bumper** | ぶつかるとはじき飛ばす | Bounce Power |
| **MovingFloor** | 2 点の間を往復する床。乗った車も運ぶ | Move Offset、Travel Time、Wait Time |
| **CourseArea** | 走ってよい範囲（見えない箱）。コースに沿って並べる | — |
| **OutOfBoundsJudge** | 場外判定係。CourseArea の外にいる時間が続くと戻す | Grace Time（外にいられる秒数） |

### 場外判定のしくみ

```
[CourseArea] [CourseArea] [CourseArea] …  ← コースに沿って並べる
      └── 入った / 出た ──▶ OutOfBoundsJudge ──▶ Grace Time を超えたら Respawn
```

- ジャンプする谷の上や、動く床の通り道も CourseArea でおおっておきます。
- 外に出ている間は、画面に「コースアウト！」と残り秒数が表示されます。
- CourseArea が 1 つも無いシーンでは、OutOfBoundsJudge は自動で休みます。

## UI（画面表示）

画面のスクリプトは **MVP** という形で、役割を 3 つに分けています。

| 役割 | 何をするか | コンポーネント |
| --- | --- | --- |
| Model | データとルール | GameManager、RCCar、ControlsHintModel |
| View | 表示するだけ | **HudView**、**ControlsHintView** |
| Presenter | Model を読んで View に表示させる | **HudPresenter**、**ControlsHintPresenter** |

| コンポーネント | 表示するもの |
| --- | --- |
| HudView / HudPresenter | タイム、カウントダウン、チェックポイント数、ブーストゲージ、コースアウト |
| ControlsHintView / ControlsHintPresenter | 画面左下の操作方法（キー割り当てを変えると自動で変わる） |

---

[目次に戻る](../index.md)
