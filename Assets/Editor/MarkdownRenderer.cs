// =============================================================================
//  MarkdownRenderer.cs   ※ Editor フォルダの中のスクリプトはゲームには含まれません
// -----------------------------------------------------------------------------
//  【役割】
//    Markdown（.md）の文章を、Unity の UI Toolkit の部品（Label など）に変換する。
//    DocsWindow（マニュアル・教材ビューア）が使っています。
//
//  【対応している書き方】
//    # 見出し（# ～ ####）        段落
//    - 箇条書き / 1. 番号付き       > 引用
//    | 表 |                       ``` コード ```
//    --- 区切り線                  ![画像](images/xxx.png)
//    文中の **太字**・`コード`・[リンク](xxx.md)
//
//    GitHub Pages で使っている Markdown の中から、この教材で使う書き方だけに
//    しぼって対応しています（全部の書き方には対応していません）。
//
//  【しくみ】
//    1 行ずつ読んで「今どの種類のブロック（見出し・表・コードなど）か」を判断し、
//    ブロックごとに VisualElement を作って並べていきます。
//    文中の太字やコードは、Label の「リッチテキスト」（<b> や <color> のタグ）に置き換えます。
//    リンクはクリックを受け取るため、別の Label に分けて横に並べます。
// =============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace VantanSchoolFestival2026.EditorTools
{
    /// <summary>Markdown を UI Toolkit の VisualElement に変換する。</summary>
    public class MarkdownRenderer
    {
        // ---------------------------------------------------------------------
        // 見た目の設定
        // ---------------------------------------------------------------------
        private static readonly float[] HeadingSizes = { 24f, 19f, 15f, 13f };
        private static Color AccentColor => EditorGUIUtility.isProSkin ? new Color(0.35f, 0.65f, 1f) : new Color(0.1f, 0.4f, 0.8f);
        private static Color CodeBackground => EditorGUIUtility.isProSkin ? new Color(0f, 0f, 0f, 0.35f) : new Color(0f, 0f, 0f, 0.07f);
        private static Color BorderColor => EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.15f) : new Color(0f, 0f, 0f, 0.15f);
        private static string CodeColorHex => EditorGUIUtility.isProSkin ? "#E8A87C" : "#B04A12";

        // 文中の書き方を見つけるための正規表現
        private static readonly Regex InlineCode = new Regex(@"`([^`]+)`");
        private static readonly Regex Bold = new Regex(@"\*\*(.+?)\*\*");
        private static readonly Regex Link = new Regex(@"\[([^\]]+)\]\(([^)\s]+)\)");
        private static readonly Regex ImageLine = new Regex(@"^!\[([^\]]*)\]\(([^)\s]+)\)\s*$");
        private static readonly Regex ListItem = new Regex(@"^(\s*)([-*]|\d+\.)\s+(.*)$");

        private readonly string _baseDirectory;          // 画像を探すフォルダ（今の .md があるフォルダ）
        private readonly Action<string> _onLinkClicked;  // リンクがクリックされたときに呼ぶ処理
        private readonly Dictionary<string, Texture2D> _textureCache;

        /// <param name="baseDirectory">今表示している .md ファイルがあるフォルダ</param>
        /// <param name="onLinkClicked">リンクがクリックされたときに、リンク先の文字列を受け取る処理</param>
        /// <param name="textureCache">読み込んだ画像を使い回すための入れ物</param>
        public MarkdownRenderer(string baseDirectory, Action<string> onLinkClicked, Dictionary<string, Texture2D> textureCache)
        {
            _baseDirectory = baseDirectory;
            _onLinkClicked = onLinkClicked;
            _textureCache = textureCache;
        }

        // ---------------------------------------------------------------------
        // ブロック（見出し・段落・表など）の組み立て
        // ---------------------------------------------------------------------

        /// <summary>Markdown の文章全体を VisualElement に変換する。</summary>
        public VisualElement Render(string markdown)
        {
            var root = new VisualElement();
            root.style.paddingLeft = 16; root.style.paddingRight = 16;
            root.style.paddingTop = 8; root.style.paddingBottom = 24;

            // 改行コードをそろえて、1 行ずつに分ける
            string[] lines = markdown.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
            int i = SkipFrontMatter(lines);

            var paragraph = new List<string>();
            while (i < lines.Length)
            {
                string line = lines[i];
                string trimmed = line.Trim();

                // 空行 … 段落の区切り
                if (trimmed.Length == 0) { FlushParagraph(root, paragraph); i++; continue; }

                // ``` コードブロック
                if (trimmed.StartsWith("```"))
                {
                    FlushParagraph(root, paragraph);
                    var code = new List<string>();
                    i++;
                    while (i < lines.Length && !lines[i].Trim().StartsWith("```")) { code.Add(lines[i]); i++; }
                    i++; // 閉じの ``` を飛ばす
                    root.Add(CreateCodeBlock(string.Join("\n", code)));
                    continue;
                }

                // # 見出し
                if (trimmed.StartsWith("#"))
                {
                    FlushParagraph(root, paragraph);
                    int level = 0;
                    while (level < trimmed.Length && trimmed[level] == '#') level++;
                    root.Add(CreateHeading(trimmed.Substring(level).Trim(), Mathf.Clamp(level, 1, 4)));
                    i++;
                    continue;
                }

                // --- 区切り線
                if (trimmed == "---" || trimmed == "***")
                {
                    FlushParagraph(root, paragraph);
                    root.Add(CreateRule());
                    i++;
                    continue;
                }

                // | 表 |
                if (trimmed.StartsWith("|"))
                {
                    FlushParagraph(root, paragraph);
                    var rows = new List<string>();
                    while (i < lines.Length && lines[i].Trim().StartsWith("|")) { rows.Add(lines[i].Trim()); i++; }
                    root.Add(CreateTable(rows));
                    continue;
                }

                // > 引用
                if (trimmed.StartsWith(">"))
                {
                    FlushParagraph(root, paragraph);
                    var quote = new List<string>();
                    while (i < lines.Length && lines[i].Trim().StartsWith(">")) { quote.Add(lines[i].Trim().Substring(1).Trim()); i++; }
                    root.Add(CreateQuote(quote));
                    continue;
                }

                // ![画像](パス)
                Match image = ImageLine.Match(trimmed);
                if (image.Success)
                {
                    FlushParagraph(root, paragraph);
                    root.Add(CreateImage(image.Groups[2].Value, image.Groups[1].Value));
                    i++;
                    continue;
                }

                // - 箇条書き / 1. 番号付き
                Match item = ListItem.Match(line);
                if (item.Success)
                {
                    FlushParagraph(root, paragraph);
                    i = ReadList(root, lines, i);
                    continue;
                }

                // それ以外 … 段落の 1 行
                paragraph.Add(trimmed);
                i++;
            }
            FlushParagraph(root, paragraph);
            return root;
        }

        /// <summary>先頭の「--- ～ ---」（Jekyll の設定欄）があれば読み飛ばす。</summary>
        private static int SkipFrontMatter(string[] lines)
        {
            if (lines.Length == 0 || lines[0].Trim() != "---") return 0;
            for (int i = 1; i < lines.Length; i++)
            {
                if (lines[i].Trim() == "---") return i + 1;
            }
            return 0;
        }

        /// <summary>たまっている段落の行をまとめて 1 つの Label にする。</summary>
        private void FlushParagraph(VisualElement root, List<string> paragraph)
        {
            if (paragraph.Count == 0) return;

            // Markdown では、段落の中の改行は「つなげて表示」が決まり
            var builder = new StringBuilder();
            foreach (string line in paragraph)
            {
                if (builder.Length > 0 && !IsCjk(builder[builder.Length - 1]) && !IsCjk(line[0])) builder.Append(' ');
                builder.Append(line);
            }
            paragraph.Clear();

            VisualElement label = CreateText(builder.ToString());
            label.style.marginTop = 4; label.style.marginBottom = 6;
            root.Add(label);
        }

        /// <summary>箇条書きを読み進めて、1 項目ずつ行を作る。読み終わった次の行番号を返す。</summary>
        private int ReadList(VisualElement root, string[] lines, int i)
        {
            var box = new VisualElement();
            box.style.marginTop = 2; box.style.marginBottom = 6;

            while (i < lines.Length)
            {
                Match item = ListItem.Match(lines[i]);
                if (!item.Success) break;

                int indent = item.Groups[1].Value.Replace("\t", "    ").Length / 2;   // 字下げの深さ
                string marker = item.Groups[2].Value;
                var text = new StringBuilder(item.Groups[3].Value.Trim());
                i++;

                // 次の行が「字下げされた続きの文章」なら、同じ項目として改行してつなげる
                while (i < lines.Length && lines[i].StartsWith("  ") && lines[i].Trim().Length > 0 && !ListItem.IsMatch(lines[i]))
                {
                    text.Append('\n').Append(lines[i].Trim());
                    i++;
                }

                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.marginLeft = 8 + indent * 16;
                row.style.marginBottom = 2;

                var bullet = new Label(char.IsDigit(marker[0]) ? marker : "・");
                bullet.style.minWidth = char.IsDigit(marker[0]) ? 22 : 14;
                bullet.style.unityTextAlign = TextAnchor.UpperRight;
                bullet.style.marginRight = 4;
                row.Add(bullet);

                VisualElement body = CreateText(text.ToString());
                body.style.flexShrink = 1; body.style.flexGrow = 1;
                row.Add(body);
                box.Add(row);
            }
            root.Add(box);
            return i;
        }

        private VisualElement CreateHeading(string text, int level)
        {
            VisualElement label = CreateText(text);
            label.style.fontSize = HeadingSizes[level - 1];
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.marginTop = level == 1 ? 4 : 14;
            label.style.marginBottom = 6;

            if (level <= 2)
            {
                // 大きい見出しには下線を引く
                label.style.paddingBottom = 4;
                label.style.borderBottomWidth = 1;
                label.style.borderBottomColor = BorderColor;
            }
            return label;
        }

        private static VisualElement CreateRule()
        {
            var rule = new VisualElement();
            rule.style.height = 1;
            rule.style.backgroundColor = BorderColor;
            rule.style.marginTop = 12; rule.style.marginBottom = 12;
            return rule;
        }

        private static VisualElement CreateCodeBlock(string code)
        {
            // コードはタグとして解釈させず、そのまま表示する（コピーもできるようにする）
            var label = new Label(code) { enableRichText = false };
            label.selection.isSelectable = true;
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.backgroundColor = CodeBackground;
            label.style.paddingLeft = 10; label.style.paddingRight = 10;
            label.style.paddingTop = 8; label.style.paddingBottom = 8;
            label.style.marginTop = 4; label.style.marginBottom = 8;
            label.style.borderTopLeftRadius = 4; label.style.borderTopRightRadius = 4;
            label.style.borderBottomLeftRadius = 4; label.style.borderBottomRightRadius = 4;
            return label;
        }

        private VisualElement CreateQuote(List<string> lines)
        {
            var box = new VisualElement();
            box.style.borderLeftWidth = 4;
            box.style.borderLeftColor = AccentColor;
            box.style.backgroundColor = CodeBackground;
            box.style.paddingLeft = 10; box.style.paddingRight = 8;
            box.style.paddingTop = 6; box.style.paddingBottom = 6;
            box.style.marginTop = 4; box.style.marginBottom = 8;

            // 引用の中は、1 行ずつ改行して表示する
            box.Add(CreateText(string.Join("\n", lines)));
            return box;
        }

        private VisualElement CreateTable(List<string> rows)
        {
            var table = new VisualElement();
            table.style.marginTop = 4; table.style.marginBottom = 10;
            table.style.borderTopWidth = 1; table.style.borderLeftWidth = 1;
            table.style.borderTopColor = BorderColor; table.style.borderLeftColor = BorderColor;

            bool header = true;
            foreach (string row in rows)
            {
                string[] cells = SplitRow(row);

                // 2 行目の「| --- | --- |」は区切りなので飛ばす
                if (Array.TrueForAll(cells, c => Regex.IsMatch(c.Trim(), @"^:?-{3,}:?$"))) continue;

                var line = new VisualElement();
                line.style.flexDirection = FlexDirection.Row;
                if (header) line.style.backgroundColor = CodeBackground;

                foreach (string cell in cells)
                {
                    VisualElement label = CreateText(cell.Trim());
                    label.style.flexGrow = 1; label.style.flexBasis = 0; label.style.flexShrink = 1;
                    label.style.paddingLeft = 6; label.style.paddingRight = 6;
                    label.style.paddingTop = 4; label.style.paddingBottom = 4;
                    label.style.borderRightWidth = 1; label.style.borderBottomWidth = 1;
                    label.style.borderRightColor = BorderColor; label.style.borderBottomColor = BorderColor;
                    if (header) label.style.unityFontStyleAndWeight = FontStyle.Bold;
                    line.Add(label);
                }
                table.Add(line);
                header = false;
            }
            return table;
        }

        /// <summary>「| a | b |」を ["a", "b"] に分ける（`コード` の中の | では分けない）。</summary>
        private static string[] SplitRow(string row)
        {
            string inner = row.Trim();
            if (inner.StartsWith("|")) inner = inner.Substring(1);
            if (inner.EndsWith("|")) inner = inner.Substring(0, inner.Length - 1);

            var cells = new List<string>();
            var current = new StringBuilder();
            bool inCode = false;
            foreach (char ch in inner)
            {
                if (ch == '`') inCode = !inCode;
                if (ch == '|' && !inCode) { cells.Add(current.ToString()); current.Clear(); continue; }
                current.Append(ch);
            }
            cells.Add(current.ToString());
            return cells.ToArray();
        }

        private VisualElement CreateImage(string relativePath, string alt)
        {
            string path = Path.GetFullPath(Path.Combine(_baseDirectory, relativePath));
            Texture2D texture = LoadTexture(path);
            if (texture == null) return CreateText("（画像が見つかりません：" + relativePath + "）");

            var image = new Image { image = texture, scaleMode = ScaleMode.ScaleToFit, tooltip = alt };
            image.style.marginTop = 6; image.style.marginBottom = 10;
            image.style.maxWidth = texture.width;

            // 横幅に合わせて、縦横比を保ったまま高さを決める
            image.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                float height = image.resolvedStyle.width * texture.height / texture.width;
                if (Mathf.Abs(image.resolvedStyle.height - height) > 1f) image.style.height = height;
            });
            return image;
        }

        private Texture2D LoadTexture(string path)
        {
            if (_textureCache.TryGetValue(path, out Texture2D cached) && cached != null) return cached;
            if (!File.Exists(path)) return null;

            var texture = new Texture2D(2, 2) { hideFlags = HideFlags.HideAndDontSave };
            // LoadImage … PNG / JPG のデータから画像を作る
            if (!texture.LoadImage(File.ReadAllBytes(path))) return null;
            _textureCache[path] = texture;
            return texture;
        }

        // ---------------------------------------------------------------------
        // 文中の書き方（太字・コード・リンク）
        // ---------------------------------------------------------------------

        /// <summary>
        /// 文中の書き方を変換した文字の部品を作る。
        /// リンクが無ければ Label 1 つ、リンクがあれば「文字」と「リンク」を横に並べた入れ物を返す。
        /// </summary>
        /// <remarks>
        /// Unity 6.3 では、Label の中の一部分（リンク）だけのクリックを受け取る仕組みが
        /// 外から使えないため、リンクは別の Label に分けてクリックを受け取っている。
        /// </remarks>
        private VisualElement CreateText(string markdown)
        {
            if (!Link.IsMatch(markdown)) return CreateRun(markdown);

            // 文字とリンクを左から順に並べ、はみ出したら次の行へ折り返す
            var flow = new VisualElement();
            flow.style.flexDirection = FlexDirection.Row;
            flow.style.flexWrap = Wrap.Wrap;
            flow.style.alignItems = Align.FlexStart;

            int last = 0;
            foreach (Match m in Link.Matches(markdown))
            {
                if (m.Index > last) flow.Add(CreateRun(markdown.Substring(last, m.Index - last)));
                flow.Add(CreateLink(m.Groups[1].Value, m.Groups[2].Value));
                last = m.Index + m.Length;
            }
            if (last < markdown.Length) flow.Add(CreateRun(markdown.Substring(last)));
            return flow;
        }

        /// <summary>リンクを含まない文字の部分（太字・コードはリッチテキストにする）。</summary>
        private static Label CreateRun(string markdown)
        {
            var label = new Label(ToRichText(markdown)) { enableRichText = true };
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.flexShrink = 1;
            label.style.marginLeft = 0; label.style.marginRight = 0;
            label.style.paddingLeft = 0; label.style.paddingRight = 0;
            return label;
        }

        /// <summary>クリックできるリンクの部分。</summary>
        private Label CreateLink(string text, string url)
        {
            var label = new Label("<u>" + ToRichText(text) + "</u>") { enableRichText = true, tooltip = url };
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.flexShrink = 1;
            label.style.color = AccentColor;
            label.style.marginLeft = 0; label.style.marginRight = 0;
            label.style.paddingLeft = 0; label.style.paddingRight = 0;

            // クリックされたらリンク先を知らせる。マウスを乗せたら少し薄くする
            label.RegisterCallback<ClickEvent>(evt => _onLinkClicked?.Invoke(url));
            label.RegisterCallback<MouseEnterEvent>(evt => label.style.opacity = 0.7f);
            label.RegisterCallback<MouseLeaveEvent>(evt => label.style.opacity = 1f);
            return label;
        }

        /// <summary>**太字**・`コード` を Label のタグに置き換える（リンクは CreateText で別に扱う）。</summary>
        private static string ToRichText(string text)
        {
            // 1. `コード` を先に取り出しておく（中の ** や [ ] を太字やリンクにしないため）
            var codes = new List<string>();
            text = InlineCode.Replace(text, m =>
            {
                codes.Add(m.Groups[1].Value);
                return "\u0001" + (codes.Count - 1) + "\u0002";
            });

            // 2. 本文の < がタグと間違われないようにする（< の後ろに見えない文字をはさむ）
            text = EscapeTags(text);

            // 3. 太字
            text = Bold.Replace(text, "<b>$1</b>");

            // 4. 取り出しておいたコードを色付きで戻す
            for (int i = 0; i < codes.Count; i++)
            {
                text = text.Replace("\u0001" + i + "\u0002", "<color=" + CodeColorHex + ">" + EscapeTags(codes[i]) + "</color>");
            }
            return text;
        }

        private static string EscapeTags(string text) => text.Replace("<", "<​");

        /// <summary>日本語・中国語などの文字か（段落をつなぐときに空白を入れるか判断する）。</summary>
        private static bool IsCjk(char c) => c >= 0x3000 && c <= 0x9FFF || c >= 0xFF00 && c <= 0xFFEF;
    }
}
