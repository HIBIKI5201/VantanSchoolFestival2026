# エディタ拡張

**エディタ拡張** は、Unity のエディタに自分で機能を足す仕組みです。
このプロジェクトには次の 3 つがあります。どれも `Assets/Editor/` にあり、ゲームには含まれません。

## 1. マニュアル・教材ビューア

メニュー **VantanSchoolFestival2026 → マニュアル・教材** で開きます。

- プロジェクト直下の `docs/` フォルダにある Markdown（`.md`）ファイルを、Unity の中で読めます。
- GitHub Pages で公開している Web 版と **同じファイル** を表示しています。どちらかを直せば両方が変わります。
- 左の目次から読みたいページを選びます。ページ内のリンクをクリックすると、そのページに移動します。
- 「ブラウザで開く」ボタンで、同じページの Web 版を開けます。
- プロジェクトを初めて開いたときは、自動でこのウィンドウが開きます。

スクリプト：`Assets/Editor/DocsWindow.cs`

## 2. ステージチェッカー

メニュー **VantanSchoolFestival2026 → ステージチェッカー** で開きます。
今のシーンが「ゲームとして遊べる状態か」を調べます。

| チェック内容 | 例 |
| --- | --- |
| プレイヤー | 車が 1 台だけあるか、駆動方式が 1 つ付いているか |
| 入力 | PlayerController があるか、PlayerInput の設定 |
| スタート・ゴール | StartPoint の下に床があるか、Goal がトリガーか |
| カメラ | CinemachineBrain・追従カメラの対象 |
| 落下ゾーン・場外判定 | KillZone、CourseArea と OutOfBoundsJudge |
| 画面表示・画面効果 | HUD・操作方法表示・Volume |
| Missing Script | 見つからないスクリプトが付いていないか |

- **選択** ボタン：問題のオブジェクトを Hierarchy で選ぶ
- **修正** ボタン：自動で直す（Ctrl + Z で元に戻せる）
- メニュー **VantanSchoolFestival2026 → 全シーンをチェック** で、Build Settings のすべてのシーンの結果を Console に出せます（先生向け）。

スクリプト：`Assets/Editor/StageCheckerWindow.cs`

## 3. コンポーネントの説明（プルダウン）

`[ComponentSummary]` 属性が付いたコンポーネントは、Inspector の一番上に **「▶ このコンポーネントについて」** が出ます。
開くと「概要」「使い方」「関連」「必要なコンポーネント」と、スクリプトを開くボタンが表示されます。

```csharp
[ComponentSummary(
    "何をする部品か",
    "どう使うか（省略可）",
    "関係する部品（省略可）")]
public class MyParts : MonoBehaviour { }
```

スクリプト：`Assets/Editor/ComponentSummaryEditor.cs`、`Assets/Scripts/Common/ComponentSummaryAttribute.cs`

---

[目次に戻る](../index.md)
