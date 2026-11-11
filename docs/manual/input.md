# 入力と操作方法

## 操作方法

| 操作 | キーボード（Driving） | キーボード（DrivingAlt） | ゲームパッド |
| --- | --- | --- | --- |
| 進む・下がる | W / S | ↑ / ↓ | 左スティック（Alt: 十字キー） |
| 曲がる | A / D | ← / → | 左スティック（Alt: 十字キー） |
| ジャンプ ※ | Space | 右 Ctrl | A / ×（Alt: B / ○） |
| ブースト ※ | 左 Shift | 右 Shift | RT（Alt: LT） |
| やり直し（ゴール後はリスタート） | R | Backspace | Select / View |
| 操作方法の切り替え | Tab | Tab | Start / Menu |

※ 車に JumpParts / BoostParts が付いているときだけ使えます。

## 入力が車に届くまで

```
キーボード / ゲームパッド
   ↓
PlayerInput（Unity の Input System）
   ↓ OnMove / OnJump / OnBoost / OnRespawn を自動で呼ぶ（Send Messages）
PlayerCarInput
   ↓ SetDriveInput / RequestJump / SetBoost / RequestRespawn
RCCar（車）
```

- 入力を受け取るのは車ではなく **PlayerController** というオブジェクトです。
- 車（RCCar）は「誰が命令しているか」を知りません。そのため、PlayerCarInput の代わりに AI のスクリプトを作れば自動運転にできます。

## ActionMap（操作方法のセット）

キー割り当ては `Assets/Settings/RCInputActions` に入っています。ダブルクリックで編集画面が開きます。

| ActionMap | 内容 |
| --- | --- |
| Driving | W A S D を使う操作 |
| DrivingAlt | 矢印キーを使う操作 |

- 再生中に **Tab** を押すと、ActionMapSwitcher が Driving ⇔ DrivingAlt を切り替えます。
- 最初の操作方法は、PlayerController の **Player Input → Default Map** で決めます。
- キーを変えたら、編集画面の **Save Asset** を押してください。画面左下の操作方法の表示も自動で変わります。

---

[目次に戻る](../index.md)
