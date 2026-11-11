# プロジェクトの構成

## フォルダ

```
VantanSchoolFestival2026/
├─ docs/              … このマニュアルと授業の教材（GitHub Pages と Unity の両方で読む）
├─ Assets/
│   ├─ Scenes/        … シーン（00 が完成版、01〜03 が練習用）
│   ├─ Scripts/       … ゲームのスクリプト
│   │   ├─ Common/    … いろいろな部品で使う共通のもの
│   │   ├─ Core/      … 車本体・ゲーム進行
│   │   ├─ Input/     … プレイヤーの入力
│   │   ├─ UI/        … 画面の表示（タイム・操作方法）
│   │   ├─ Parts/
│   │   │   ├─ Drive/   … 駆動方式（付け替えると走り方が変わる）
│   │   │   └─ Options/ … 追加パーツ（ジャンプ・ブースト・色）
│   │   └─ Stage/     … ステージ部品（ゴール・加速床・場外判定など）
│   ├─ Editor/        … エディタ拡張（ゲームには含まれない）
│   ├─ Prefabs/       … 置くだけで使える部品
│   │   ├─ Player/    … ラジコンカー
│   │   ├─ Stage/     … 床・道路・壁・柵・坂・ゴールなど
│   │   ├─ Nature/    … 木・岩・水
│   │   └─ System/    … GameManager・PlayerController・カメラ・Volume・UI
│   ├─ Materials/     … 色や質感
│   ├─ Models/        … 木や岩などの形
│   └─ Settings/      … URP の設定・入力設定・ポストプロセス
├─ .editorconfig      … 文字コードや書き方の設定
└─ README.md
```

## シーン

| シーン | 内容 | 使う章 |
| --- | --- | --- |
| `00_Sample` | 完成版。自然の中を走る約 1 km のコース | [1 章](../lesson/01_intro.md) |
| `01_Lesson_Parts` | 平らな広場。部品の付け替えを試す | [2 章](../lesson/02_components.md)・[上級 2](../advanced/02_script.md) |
| `02_Lesson_Stage` | 床とスタート地点だけ。コースを作る | [3 章](../lesson/03_stage.md)・[4 章](../lesson/04_finish.md) |
| `03_Lesson_Debug` | わざと壊してある。問題を見つけて直す | [上級 1](../advanced/01_debug.md) |

## シーンに置くもの（基本セット）

どのシーンにも、次のオブジェクトを置きます。`Assets/Prefabs/System/` にあります。

| オブジェクト | 役割 |
| --- | --- |
| GameManager | ゲームの進行（カウントダウン・タイム・ゴール） |
| PlayerController | キーボード・ゲームパッドの入力を受け取って、車に命令する |
| MainCamera | 画面を映すカメラ |
| FollowCamera | 車を追いかけて、MainCamera を動かす係（Cinemachine） |
| GameUI | タイム・メッセージ・操作方法の表示 |
| GlobalVolume | 画面全体の色味（ポストプロセス） |

足りないものがあると、[ステージチェッカー](editor-tools.md) が教えてくれます。

## 00_Sample のコース

| エリア | 内容 |
| --- | --- |
| 01_スタート〜03_加速ストレート | カーブと加速床で操作に慣れる |
| 04_川と細い橋 | 川にかかる細い橋。落ちたら川へ |
| 05_丘と谷ジャンプ | 丘を登って、谷をジャンプ台で飛び越える |
| 06_バンパー広場 | バンパーにはじかれないように進む |
| 07_ヘアピン | 急カーブ |
| 08_湖と動く床 | 湖を渡る動く床。床が来るのを待って乗る |
| 09_ワインディング〜11_ゴール | 登り下りのカーブ、小川の連続ジャンプ、最後の直線 |

- 道路は無く、**木と岩の列** で進路を示しています。
- 走ってよい範囲は **Course → CourseAreas** の見えない箱（CourseArea）で決めています。
- 地面は **Terrain**、水面は **Water** と、その少し下の **KillZone_Water** でできています。

---

[目次に戻る](../index.md)
