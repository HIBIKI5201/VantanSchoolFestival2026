// =============================================================================
//  DocsWindow.cs   ※ Editor フォルダの中のスクリプトはゲームには含まれません
// -----------------------------------------------------------------------------
//  【役割】
//    プロジェクト直下の docs/ フォルダにある Markdown（.md）を、Unity の中で読むウィンドウ。
//    メニューの「VantanSchoolFestival2026 > マニュアル・教材」で開きます。
//
//  【Web 版と同じファイルを読んでいる】
//    docs/ は GitHub Pages でも Web ページとして公開しています。
//    このウィンドウも同じ .md ファイルを読んで表示しているので、
//    docs/ のファイルを 1 か所直せば、Web とエディタの両方が変わります。
//
//  【画面の作り】
//    ┌ ツールバー（戻る・目次・再読み込み・ブラウザで開く）──────┐
//    │ 左：目次（ページ一覧）  │ 右：ページの中身（MarkdownRenderer で作る）│
//    └──────────────────────────────────────┘
// =============================================================================

using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace VantanSchoolFestival2026.EditorTools
{
    /// <summary>docs/ の Markdown を表示するエディタウィンドウ。</summary>
    public class DocsWindow : EditorWindow
    {
        // ---------------------------------------------------------------------
        // 定数
        // ---------------------------------------------------------------------

        /// <summary>GitHub Pages で公開している Web 版の URL。</summary>
        private const string WebBaseUrl = "https://hibiki5201.github.io/VantanSchoolFestival2026/";

        /// <summary>最初に表示するページ。</summary>
        private const string IndexPage = "index.md";

        /// <summary>「初めて開いたか」を覚えておくキーの先頭。</summary>
        private const string FirstOpenKeyPrefix = "VantanSchoolFestival2026.DocsWindow.Opened.";

        /// <summary>docs フォルダの場所（Assets と同じ階層）。</summary>
        private static string DocsRoot => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "docs"));

        // ---------------------------------------------------------------------
        // 状態
        // ---------------------------------------------------------------------

        // 今表示しているページ（docs からの相対パス。例 "lesson/01_intro.md"）
        [SerializeField] private string _currentPage = IndexPage;

        // 「戻る」ボタン用の履歴
        private readonly Stack<string> _history = new Stack<string>();

        // 読み込んだ画像（ウィンドウを閉じるときに片付ける）
        private readonly Dictionary<string, Texture2D> _textures = new Dictionary<string, Texture2D>();

        // 画面の部品
        private ScrollView _contentScroll;
        private VisualElement _navList;
        private ToolbarButton _backButton;

        // ---------------------------------------------------------------------
        // ウィンドウを開く
        // ---------------------------------------------------------------------

        /// <summary>メニューから開く。</summary>
        [MenuItem("VantanSchoolFestival2026/マニュアル・教材", priority = -10)]
        public static void OpenIndex()
        {
            Open(IndexPage);
        }

        /// <summary>指定したページを開く（docs からの相対パス）。</summary>
        public static void Open(string page)
        {
            DocsWindow window = GetWindow<DocsWindow>();
            window.titleContent = new GUIContent("マニュアル・教材", EditorGUIUtility.IconContent("_Help").image);
            window.minSize = new Vector2(600, 400);
            window.Navigate(page, addHistory: false);
        }

        /// <summary>
        /// InitializeOnLoadMethod … エディタの起動時やスクリプトの再読み込み時に自動で呼ばれる。
        /// このプロジェクトを初めて開いたときだけ、マニュアルを自動で表示する。
        /// </summary>
        [InitializeOnLoadMethod]
        private static void OpenOnFirstLaunch()
        {
            // EditorPrefs はパソコン全体で共有されるので、プロジェクトの場所をキーに含める
            string key = FirstOpenKeyPrefix + Application.dataPath;
            if (EditorPrefs.GetBool(key, false)) return;
            if (Application.isBatchMode) return;   // コマンドラインで動かしているときは開かない

            EditorPrefs.SetBool(key, true);
            // delayCall … エディタの準備が終わってから実行する
            EditorApplication.delayCall += OpenIndex;
        }

        // ---------------------------------------------------------------------
        // 画面を作る（UI Toolkit）
        // ---------------------------------------------------------------------

        /// <summary>CreateGUI … ウィンドウが開いたときに 1 回呼ばれ、画面の部品を作る。</summary>
        private void CreateGUI()
        {
            // --- ツールバー ---
            var toolbar = new Toolbar();
            _backButton = new ToolbarButton(GoBack) { text = "← 戻る" };
            toolbar.Add(_backButton);
            toolbar.Add(new ToolbarButton(() => Navigate(IndexPage)) { text = "目次" });
            toolbar.Add(new ToolbarButton(Reload) { text = "再読み込み" });
            toolbar.Add(new ToolbarSpacer { flex = true });
            toolbar.Add(new ToolbarButton(OpenInBrowser) { text = "ブラウザで開く" });
            rootVisualElement.Add(toolbar);

            // --- 左右に分かれた画面 ---
            // TwoPaneSplitView … 境目をドラッグして幅を変えられる 2 分割の画面
            var split = new TwoPaneSplitView(0, 220f, TwoPaneSplitViewOrientation.Horizontal);
            split.style.flexGrow = 1;

            var navScroll = new ScrollView();
            navScroll.style.paddingTop = 6;
            _navList = new VisualElement();
            navScroll.Add(_navList);
            split.Add(navScroll);

            _contentScroll = new ScrollView(ScrollViewMode.Vertical);
            _contentScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            split.Add(_contentScroll);
            rootVisualElement.Add(split);

            Reload();
        }

        private void OnDisable()
        {
            // 読み込んだ画像を片付ける（片付けないとメモリに残り続ける）
            foreach (Texture2D texture in _textures.Values)
            {
                if (texture != null) DestroyImmediate(texture);
            }
            _textures.Clear();
        }

        // ---------------------------------------------------------------------
        // ページの移動
        // ---------------------------------------------------------------------

        /// <summary>目次とページを読み込み直す（.md を書き換えたときに押す）。</summary>
        private void Reload()
        {
            BuildNavigation();
            ShowPage(_currentPage);
        }

        /// <summary>指定したページへ移動する。</summary>
        private void Navigate(string page, bool addHistory = true)
        {
            if (addHistory && page != _currentPage) _history.Push(_currentPage);
            _currentPage = page;
            if (_contentScroll != null) ShowPage(page);
        }

        private void GoBack()
        {
            if (_history.Count == 0) return;
            _currentPage = _history.Pop();
            ShowPage(_currentPage);
        }

        /// <summary>今のページの Web 版をブラウザで開く。</summary>
        private void OpenInBrowser()
        {
            // GitHub Pages では「xxx.md」が「xxx.html」になり、index.md はフォルダの URL になる
            string url = _currentPage == IndexPage ? "" : Regex.Replace(_currentPage, @"\.md$", ".html");
            Application.OpenURL(WebBaseUrl + url);
        }

        /// <summary>ページの中身を表示する。</summary>
        private void ShowPage(string page)
        {
            _contentScroll.Clear();
            _backButton.SetEnabled(_history.Count > 0);

            string path = Path.Combine(DocsRoot, page);
            if (!File.Exists(path))
            {
                var message = new Label("ページが見つかりません：docs/" + page + "\n\ndocs フォルダがプロジェクト直下（Assets と同じ場所）にあるか確認してください。");
                message.style.whiteSpace = WhiteSpace.Normal;
                message.style.paddingLeft = 16; message.style.paddingTop = 16;
                _contentScroll.Add(message);
                return;
            }

            string directory = Path.GetDirectoryName(path);
            var renderer = new MarkdownRenderer(directory, OnLinkClicked, _textures);
            _contentScroll.Add(renderer.Render(File.ReadAllText(path)));
            _contentScroll.scrollOffset = Vector2.zero;   // 一番上から表示する

            HighlightNavigation();
        }

        /// <summary>ページの中のリンクがクリックされたとき。</summary>
        private void OnLinkClicked(string link)
        {
            // http で始まるリンクはブラウザで開く
            if (link.StartsWith("http://") || link.StartsWith("https://"))
            {
                Application.OpenURL(link);
                return;
            }

            // 「#見出し」の部分は使わない
            int hash = link.IndexOf('#');
            if (hash >= 0) link = link.Substring(0, hash);
            if (link.Length == 0) return;

            // 今のページの場所からの相対パスを、docs からのパスに直す
            string currentDir = Path.GetDirectoryName(Path.Combine(DocsRoot, _currentPage));
            string fullPath = Path.GetFullPath(Path.Combine(currentDir, link));

            if (fullPath.EndsWith(".md") && File.Exists(fullPath))
            {
                Navigate(ToDocsRelative(fullPath));
            }
            else
            {
                Debug.LogWarning("[マニュアル] リンク先が見つかりません：" + link);
            }
        }

        // ---------------------------------------------------------------------
        // 左側の目次
        // ---------------------------------------------------------------------

        /// <summary>
        /// 目次を作る。並び順は index.md に書かれているリンクの順番に合わせる
        /// （index.md を直せば、Web 版とこの目次の両方の順番が変わる）。
        /// </summary>
        private void BuildNavigation()
        {
            _navList.Clear();
            string indexPath = Path.Combine(DocsRoot, IndexPage);
            if (!File.Exists(indexPath)) return;

            AddNavItem(IndexPage, "トップ（目次）", 0);

            string section = null;
            foreach (Match m in Regex.Matches(File.ReadAllText(indexPath), @"\]\(([^)#\s]+\.md)\)"))
            {
                string page = m.Groups[1].Value;
                if (!File.Exists(Path.Combine(DocsRoot, page))) continue;

                // フォルダが変わったら見出しを入れる（lesson → 授業、manual → マニュアル）
                string folder = page.Contains("/") ? page.Substring(0, page.IndexOf('/')) : "";
                if (folder != section)
                {
                    section = folder;
                    var header = new Label(folder == "lesson" ? "授業の教材" : folder == "manual" ? "マニュアル" : folder);
                    header.style.unityFontStyleAndWeight = FontStyle.Bold;
                    header.style.marginTop = 10; header.style.marginLeft = 8; header.style.marginBottom = 2;
                    header.style.opacity = 0.7f;
                    _navList.Add(header);
                }
                if (_navList.Q<Label>(name: page) == null) AddNavItem(page, ReadTitle(page), 1);
            }
        }

        private void AddNavItem(string page, string title, int indent)
        {
            var item = new Label(title) { name = page };
            item.style.paddingLeft = 8 + indent * 10;
            item.style.paddingTop = 3; item.style.paddingBottom = 3;
            item.style.whiteSpace = WhiteSpace.Normal;
            item.RegisterCallback<ClickEvent>(evt => Navigate(page));
            // マウスを乗せたら色を変える
            item.RegisterCallback<MouseEnterEvent>(evt => { if (page != _currentPage) item.style.backgroundColor = new Color(0.5f, 0.5f, 0.5f, 0.15f); });
            item.RegisterCallback<MouseLeaveEvent>(evt => { if (page != _currentPage) item.style.backgroundColor = StyleKeyword.Null; });
            _navList.Add(item);
        }

        /// <summary>今のページを目次で目立たせる。</summary>
        private void HighlightNavigation()
        {
            if (_navList == null) return;
            _navList.Query<Label>().ForEach(label =>
            {
                if (string.IsNullOrEmpty(label.name)) return;
                bool selected = label.name == _currentPage;
                label.style.backgroundColor = selected ? new Color(0.25f, 0.5f, 0.9f, 0.35f) : StyleKeyword.Null;
                label.style.unityFontStyleAndWeight = selected ? FontStyle.Bold : FontStyle.Normal;
            });
        }

        /// <summary>ページの最初の「# 見出し」をタイトルとして読む。</summary>
        private static string ReadTitle(string page)
        {
            foreach (string line in File.ReadLines(Path.Combine(DocsRoot, page)))
            {
                if (line.StartsWith("# ")) return line.Substring(2).Trim();
            }
            return Path.GetFileNameWithoutExtension(page);
        }

        /// <summary>フルパスを「docs からの相対パス（/ 区切り）」に直す。</summary>
        private static string ToDocsRelative(string fullPath)
        {
            string root = DocsRoot.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            return fullPath.Substring(root.Length).Replace('\\', '/');
        }
    }
}
