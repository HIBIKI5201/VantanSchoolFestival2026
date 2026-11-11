# 入力と操作方法

## 操作方法

| 操作 | キーボード（ふつう） | キーボード（矢印キー） | ゲームパッド |
| --- | --- | --- | --- |
| 進む・下がる | W / S | ↑ / ↓ | 左スティック（矢印キーのときは十字キー） |
| 曲がる | A / D | ← / → | 左スティック（矢印キーのときは十字キー） |
| ジャンプ ※ | Space | 右 Ctrl | A / ×（矢印キーのときは B / ○） |
| スピードアップ ※ | 左 Shift | 右 Shift | RT（矢印キーのときは LT） |
| やり直し（ゴール後はもう一度） | R | Backspace | Select / View |
| 操作のキーを切り替える | Tab | Tab | Start / Menu |

※ 車に JumpParts / BoostParts が付いているときだけ使えます。

## だれが操作を受け取っているの？

キーボードやゲームパッドの操作を受け取っているのは、車ではなく **PlayerController** というオブジェクトです。
PlayerController が受け取った操作を、車に「進め」「ジャンプ」と伝えています。

- PlayerController の **Player Car Input** → **Target Car** に別の車を入れると、その車を動かせます。
- もっと詳しいしくみは [上級 3. しくみを知ろう](../advanced/03_how-it-works.md) を見てください。

## キーの割り当てを変える

キーの割り当ては `Assets/Settings/RCInputActions` に入っています。ダブルクリックすると、変えるための画面が開きます。

| 操作のセット（ActionMap） | 内容 |
| --- | --- |
| Driving | W A S D を使う操作 |
| DrivingAlt | 矢印キーを使う操作 |

1. 左の **Driving** を選び、真ん中の **Jump** を開きます。
2. **Space [Keyboard]** をクリックし、右の **Path** の欄で別のキー（例：J）を選びます。
3. 上の **Save Asset** を押して保存します。画面左下の操作方法の表示も、自動で変わります。

- 再生中に **Tab** を押すと、Driving と DrivingAlt が切り替わります。
- 最初にどちらを使うかは、PlayerController の **Player Input** → **Default Map** で決めます。

---

[目次に戻る](../index.md)
