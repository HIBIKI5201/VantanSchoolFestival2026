# VantanSchoolFestival2026 ― はじめての Unity ラジコン

Unity を初めて触る人が、**ラジコンカーのゲーム**を通して Unity の基本操作に慣れるためのプロジェクトです。

- **コンポーネントを付け替える** と、車の走り方や機能が変わる
- **オブジェクトを置く** だけで、ステージ（コース）が作れる
- **専用のエディタウィンドウ** で、シーンに足りないもの（プレイヤー・ゴールなど）を調べられる
- Inspector の **「このコンポーネントについて」** プルダウンで、各部品の説明が読める
- カメラは **Cinemachine**、画面効果は **Volume（ポストプロセス）** と、Unity 公式の機能を使っている

持ち帰って、あとからゆっくり読み返せるように、スクリプトにはすべて日本語で詳しくコメントを書いています。

---

## 動作環境

| 項目 | バージョン |
| --- | --- |
| Unity | 6000.3.10f1（Unity 6.3） |
| レンダーパイプライン | URP (Universal Render Pipeline) |
| 入力 | Input System（PlayerInput コンポーネント） |
| カメラ | Cinemachine 3.1 |
| 名前空間 | `VantanSchoolFestival2026` |

## まず遊んでみよう

1. Unity Hub からこのプロジェクトを開く
2. Project ウィンドウで `Assets/Scenes/00_Sample` をダブルクリック
3. 画面上の ▶（再生）ボタンを押す

### 操作方法

| 操作 | キーボード (Driving) | キーボード (DrivingAlt) | ゲームパッド |
| --- | --- | --- | --- |
| アクセル / バック | W / S | ↑ / ↓ | 左スティック（Alt: 十字キー） |
| ハンドル | A / D | ← / → | 左スティック（Alt: 十字キー） |
| ジャンプ ※1 | Space | 右 Ctrl | A / ×（Alt: B / ○） |
| ブースト ※1 | 左 Shift | 右 Shift | RT（Alt: LT） |
| やり直し（ゴール後はリスタート） | R | Backspace | Select / View |
| 操作方法を切り替える | Tab | Tab | Start / Menu |

※1 … 車に `JumpParts` / `BoostParts` が付いているときだけ使えます。

## フォルダ構成

```
Assets/
├─ Scenes/        … シーン（00 が完成版、01～03 が練習用）
├─ Scripts/       … ゲームのスクリプト
│   ├─ Common/    … 共通（ComponentSummary 属性）
│   ├─ Core/      … 車本体・ゲーム進行
│   ├─ Input/     … プレイヤーの入力（PlayerCarInput・ActionMapSwitcher）
│   ├─ UI/        … 画面表示（MVP: Hud / Controls）
│   ├─ Parts/
│   │   ├─ Drive/   … 駆動方式（付け替えると走り方が変わる）
│   │   └─ Options/ … 追加パーツ（ジャンプ・ブースト・色）
│   └─ Stage/     … ステージ部品（ゴール・加速床など）
├─ Editor/        … エディタ拡張（ステージチェッカー・コンポーネント説明）※ゲームには含まれない
├─ Prefabs/       … 置くだけで使える部品
│   ├─ Player/    … ラジコンカー
│   ├─ Stage/     … 床・道路・壁・柵・坂・ゴールなど
│   ├─ Nature/    … 木・岩・水（プリミティブで作ったローポリ素材）
│   └─ System/    … GameManager・PlayerController・カメラ・Volume・UI
├─ Materials/     … 色や質感の設定（Nature/ に地面の色テクスチャと Terrain レイヤー）
├─ Models/        … スクリプトで作った簡単なメッシュ（円すい・岩など）
├─ Settings/      … URP の設定・入力設定 (RCInputActions)・ポストプロセス (PostProcessProfile)
└─ ThirdParty/    … 外部から取得したアセット（ライセンスは docs/manual/license.md）
```

## 説明書

| ファイル | 内容 |
| --- | --- |
| [docs/index.md](docs/index.md) | 目次（70 分授業の教材・上級者向け・マニュアル） |
| [docs/lesson/](docs/lesson/00_teacher-guide.md) | 授業の教材（先生用の進め方と 1〜4 章・まとめ） |
| [docs/advanced/](docs/advanced/01_debug.md) | 上級者向け（デバッグ・スクリプト・しくみ） |
| [docs/manual/](docs/manual/setup.md) | マニュアル（セットアップ・部品一覧・規約・ライセンスなど） |

同じ内容を次の 2 か所で読めます。

- **Web**：GitHub Pages（https://hibiki5201.github.io/VantanSchoolFestival2026/）
- **Unity エディタ**：メニュー **VantanSchoolFestival2026 → マニュアル・教材**

## 文字コードについて

すべてのテキストファイルは **UTF-8** です（C# スクリプトは UTF-8 BOM 付き）。
設定は [.editorconfig](.editorconfig) に書いてあり、Visual Studio / Rider / VS Code は自動でこの設定に従います。
