// =============================================================================
//  ComponentSummaryEditor.cs   ※ Editor フォルダの中のスクリプトはゲームには含まれません
// -----------------------------------------------------------------------------
//  【役割】
//    [ComponentSummary] 属性が付いたコンポーネントの Inspector の一番上に
//    「▶ このコンポーネントについて」というプルダウン（折りたたみ）を表示します。
//    開くと「概要」「使い方」「関連」「必要なコンポーネント」が読めます。
//
//  【しくみ】
//    ・CustomEditor … 特定のコンポーネントの Inspector の見た目を自分で作る機能
//    ・typeof(MonoBehaviour), true … MonoBehaviour を継承したすべてのクラスが対象
//    ・isFallback = true … ほかに専用の Inspector があるクラスでは使わない（控えめに動く）
//
//    Inspector は UI Toolkit（VisualElement）で組み立てています。
//      Foldout … 開け閉めできる見出し
//      Label   … 文字
//    最後に InspectorElement.FillDefaultInspector で、いつもの項目を下に並べています。
//
//  【説明文を書くには】
//    コンポーネントのクラスに [ComponentSummary("概要", "使い方", "関連")] を付けるだけ。
//    （Assets/Scripts/Common/ComponentSummaryAttribute.cs を参照）
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace VantanSchoolFestival2026.EditorTools
{
    /// <summary>
    /// [ComponentSummary] の説明文を、Inspector 上部のプルダウンに表示するエディタ。
    /// </summary>
    [CustomEditor(typeof(MonoBehaviour), true, isFallback = true)]
    [CanEditMultipleObjects]   // 複数選択しても編集できるようにする
    public class ComponentSummaryEditor : Editor
    {
        // 開いている / 閉じている状態を覚えておくためのキーの先頭
        private const string PrefsKeyPrefix = "VantanSchoolFestival2026.ComponentSummary.";

        // 見た目の色
        private static readonly Color AccentColor = new Color(0.30f, 0.65f, 1.00f);
        private static readonly Color BackgroundColor = new Color(0.30f, 0.65f, 1.00f, 0.08f);

        /// <summary>
        /// CreateInspectorGUI … Inspector に表示する中身を作って返す（UI Toolkit 版）。
        /// </summary>
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            // クラスに [ComponentSummary] が付いていればプルダウンを追加
            Type type = target.GetType();
            ComponentSummaryAttribute summary = type.GetCustomAttribute<ComponentSummaryAttribute>(false);
            if (summary != null)
            {
                root.Add(CreateSummaryFoldout(type, summary));
            }

            // いつもの Inspector の項目（[SerializeField] の値など）を下に並べる
            InspectorElement.FillDefaultInspector(root, serializedObject, this);
            return root;
        }

        /// <summary>説明文のプルダウンを作る。</summary>
        private VisualElement CreateSummaryFoldout(Type type, ComponentSummaryAttribute summary)
        {
            string prefsKey = PrefsKeyPrefix + type.FullName;

            var foldout = new Foldout
            {
                text = "このコンポーネントについて",
                // EditorPrefs … エディタを閉じても値を覚えておける保存場所
                value = EditorPrefs.GetBool(prefsKey, false),
            };
            // 開け閉めしたら状態を保存する
            foldout.RegisterValueChangedCallback(evt =>
            {
                // 子要素の Foldout などから届いたイベントは無視する
                if (evt.target == foldout) EditorPrefs.SetBool(prefsKey, evt.newValue);
            });

            // --- 枠の見た目 ---
            foldout.style.marginTop = 2;
            foldout.style.marginBottom = 6;
            foldout.style.paddingLeft = 4;
            foldout.style.paddingRight = 4;
            foldout.style.paddingBottom = 4;
            foldout.style.backgroundColor = BackgroundColor;
            foldout.style.borderLeftWidth = 3;
            foldout.style.borderLeftColor = AccentColor;
            foldout.style.borderTopLeftRadius = 3;
            foldout.style.borderBottomLeftRadius = 3;

            // 見出しを太字に
            Label header = foldout.Q<Toggle>()?.Q<Label>();
            if (header != null) header.style.unityFontStyleAndWeight = FontStyle.Bold;

            // --- 中身 ---
            AddSection(foldout, "概要", summary.Summary);
            AddSection(foldout, "使い方", summary.Usage);
            AddSection(foldout, "関連", summary.Related);
            AddSection(foldout, "必要なコンポーネント（自動で付きます）", string.Join(" / ", GetRequiredComponentNames(type)));

            // スクリプトを開くボタン
            var openButton = new Button(OpenScript) { text = "スクリプトを開く" };
            openButton.style.alignSelf = Align.FlexStart;
            openButton.style.marginTop = 4;
            foldout.Add(openButton);

            return foldout;
        }

        /// <summary>小見出しと本文を追加する。本文が空なら何もしない。</summary>
        private static void AddSection(VisualElement parent, string title, string body)
        {
            if (string.IsNullOrEmpty(body)) return;

            var titleLabel = new Label(title);
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.color = AccentColor;
            titleLabel.style.marginTop = 4;
            parent.Add(titleLabel);

            var bodyLabel = new Label(body);
            bodyLabel.style.whiteSpace = WhiteSpace.Normal;   // 長い文を折り返す
            bodyLabel.style.marginLeft = 6;
            parent.Add(bodyLabel);
        }

        /// <summary>
        /// [RequireComponent] で指定されているコンポーネント名を集める（親クラスの分も含む）。
        /// </summary>
        private static IEnumerable<string> GetRequiredComponentNames(Type type)
        {
            return type.GetCustomAttributes<RequireComponent>(true)
                .SelectMany(r => new[] { r.m_Type0, r.m_Type1, r.m_Type2 })
                .Where(t => t != null)
                .Select(t => t.Name)
                .Distinct();
        }

        /// <summary>このコンポーネントのスクリプトをコードエディタで開く。</summary>
        private void OpenScript()
        {
            // MonoScript … スクリプトファイルそのものを表すアセット
            MonoScript script = MonoScript.FromMonoBehaviour((MonoBehaviour)target);
            if (script != null) AssetDatabase.OpenAsset(script);
        }
    }
}
