# セットアップ

## 必要なもの

| 項目 | バージョン・内容 |
| --- | --- |
| OS | Windows 10/11 または macOS |
| Unity Hub | 最新版 |
| Unity エディタ | **6000.3.10f1**（Unity 6.3） |
| コードエディタ（5 章で使用） | Visual Studio / Rider / VS Code のどれか |
| （任意）ゲームパッド | Xbox / PlayStation 系のコントローラー |

## 1. Unity をインストールする

1. [Unity Hub](https://unity.com/download) をインストールします。
2. Unity Hub の **Installs** → **Install Editor** から **6000.3.10f1** をインストールします。
   一覧に無いときは **Archive** タブの「download archive」から探します。

## 2. プロジェクトを手に入れる

### Git を使う場合

```bash
git clone https://github.com/HIBIKI5201/VantanSchoolFestival2026.git
```

### zip で受け取った場合

zip を展開して、日本語やスペースを含まない場所（例：`C:\Unity\VantanSchoolFestival2026`）に置きます。

## 3. プロジェクトを開く

1. Unity Hub の **Projects** → **Add** → **Add project from disk** で、プロジェクトのフォルダを選びます。
2. 一覧に出たプロジェクトをクリックして開きます。
3. **初回はインポートに数分かかります。** 進み具合のバーが消えるまで待ちましょう。

## 4. 動作を確認する

1. 初めて開いたときは、完成版のシーン `00_Sample` が自動で開きます。
   開いていないときは、メニュー **VantanSchoolFestival2026 → サンプルシーンを開く** で開けます。
2. ▶ を押して、3, 2, 1, GO! のあと W A S D で走れれば OK です。
3. メニュー **VantanSchoolFestival2026 → マニュアル・教材** で、このマニュアルが開けることも確認しましょう。

## 使っているパッケージ

`Packages/manifest.json` に書いてあり、プロジェクトを開くと自動で入ります。

| パッケージ | 使い道 |
| --- | --- |
| Universal Render Pipeline (URP) | 描画（ライト・影・画面効果） |
| Input System | キーボード・ゲームパッドの入力 |
| Cinemachine | 車を追いかけるカメラ |
| uGUI | 画面の文字（タイム・操作方法） |

---

[目次に戻る](../index.md)
