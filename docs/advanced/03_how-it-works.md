# 上級 3. しくみを知ろう

> **このページでやること**：このゲームのプログラムが、どんな考え方で作られているかを知る。
> スクリプトを読んだり、自分で部品を作ったりしたい人向けのページです。

## 1. 走り方の部品は「ひな形」から作る（継承）

ArcadeDrive・DriftDrive・TankDrive は、どれも `DriveBase` という **ひな形** をもとに作られています。

```
DriveBase（ひな形）… 車本体を探す、運転できない間は入力を 0 にする、などの共通の処理
  ├ ArcadeDrive … 素直に曲がる
  ├ DriftDrive  … 横に滑る
  └ TankDrive   … その場で回れる
```

- ひな形をもとに新しいクラスを作ることを **継承** といいます。
- 子どもの側は「どう走るか」を書いた `Drive()` だけを書けばよく、共通の部分は書かなくて済みます。
- 自分で作る方法は [上級 2. スクリプトを書こう](02_script.md) を見てください。

## 2. 車と操作を分けている

キーボードの入力を受け取るのは車ではなく、**PlayerController** という別のオブジェクトです。

```
キーボード / ゲームパッド
   ↓
PlayerInput（Unity の Input System の部品）
   ↓ OnMove / OnJump などの名前のメソッドを自動で呼ぶ（Behavior = Send Messages）
PlayerCarInput
   ↓ SetDriveInput / RequestJump / SetBoost / RequestRespawn
RCCar（車）
```

- 車（RCCar）は「誰が命令しているか」を知りません。外から命令を受け取るだけです。
- だから PlayerCarInput の代わりに「AI のスクリプト」を作れば、同じ車を自動で運転させることができます。
- キーの割り当ては `Assets/Settings/RCInputActions` に入っています。詳しくは [入力と操作方法](../manual/input.md) を見てください。

## 3. 画面の表示は 3 つの係に分けている（MVP）

画面にタイムや操作方法を出すプログラムは、**MVP** という考え方で 3 つの役割に分けています。

| 役割 | 何をするか | このプロジェクトの例 |
| --- | --- | --- |
| Model（データ） | ゲームの状態やルール。画面のことは知らない | GameManager、RCCar、ControlsHintModel |
| View（見た目） | 言われたものを表示するだけ。ゲームのことは知らない | HudView、ControlsHintView |
| Presenter（つなぎ役） | Model を読んで、View に表示させる | HudPresenter、ControlsHintPresenter |

- 見た目を作り直すときは View とプレハブだけを触れば済み、ゲームのルールの部分は変えなくてよくなります。
- 操作方法の表示は、PlayerInput の「キーボード ⇔ ゲームパッドが変わった」という知らせや、Tab での切り替えの知らせを受けて更新しています（毎フレーム調べずに、変わったときだけ更新する）。

## 4. 場外判定のしくみ

```
[CourseArea] [CourseArea] [CourseArea] …  ← コースに沿って並べる見えない箱
      └── 入った / 出た ──▶ OutOfBoundsJudge ──▶ 外にいる時間が Grace Time を超えたら Respawn
```

- コースの「外側」をふさぐ代わりに、「走ってよい内側」に箱を並べています。曲がりくねったコースでも置き忘れに気づきやすいからです。
- ジャンプする谷の上や、動く床の通り道も CourseArea でおおっておきます。
- CourseArea が 1 つも無いシーンでは、OutOfBoundsJudge は自動で休みます。

## 5. 部品の説明を出すしくみ（属性とエディタ拡張）

Inspector の「▶ このコンポーネントについて」は、クラスに付けた `[ComponentSummary]` という **属性**（付せんのようなもの）を、
エディタ拡張のスクリプトが読んで表示しています。

```csharp
[ComponentSummary(
    "何をする部品か",
    "どう使うか（省略可）",
    "関係する部品（省略可）")]
public class MyParts : MonoBehaviour { }
```

- 属性を表すスクリプト：`Assets/Scripts/Common/ComponentSummaryAttribute.cs`
- 表示しているエディタ拡張：`Assets/Editor/ComponentSummaryEditor.cs`
- このマニュアルを Unity の中で表示している仕組み：`Assets/Editor/DocsWindow.cs`、`Assets/Editor/MarkdownRenderer.cs`

## 次に読むとよいページ

- [コーディング規約](../manual/coding-rules.md)：スクリプトを書くときのルール
- [エディタ拡張](../manual/editor-tools.md)：このプロジェクトにある便利な道具

---

[← 上級 2. スクリプトを書こう](02_script.md) ｜ [目次](../index.md) ｜ [上級 4. かくれた演出を見つけよう →](04_hidden-effects.md)
