// =============================================================================
//  ComponentSummaryAttribute.cs
// -----------------------------------------------------------------------------
//  【役割】
//    コンポーネントに「説明文」を付けるための属性（Attribute）。
//    この属性を付けたコンポーネントは、Inspector の一番上に
//    「▶ このコンポーネントについて」というプルダウンが表示されます。
//    （表示しているのは Assets/Editor/ComponentSummaryEditor.cs です）
//
//  【属性(Attribute)とは？】
//    クラスやフィールドの上に [ ] で書く「付せん」のようなものです。
//    [Tooltip("…")] や [AddComponentMenu("…")] も属性です。
//    属性そのものは何もしませんが、エディタなどが付せんを読んで動きを変えます。
//
//  【使い方】
//      [ComponentSummary(
//          "何をする部品か",
//          "どう使うか（省略可）",
//          "関係する部品（省略可）")]
//      public class MyParts : MonoBehaviour { ... }
// =============================================================================

using System;

namespace VantanSchoolFestival2026
{
    /// <summary>
    /// コンポーネントの説明文。Inspector のプルダウンに表示される。
    /// </summary>
    // AttributeUsage … この属性を「どこに」付けられるかを決める（ここではクラスだけ）
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class ComponentSummaryAttribute : Attribute
    {
        /// <summary>何をするコンポーネントか。</summary>
        public string Summary { get; }

        /// <summary>使い方。空なら表示しない。</summary>
        public string Usage { get; }

        /// <summary>関係するコンポーネントなど。空なら表示しない。</summary>
        public string Related { get; }

        /// <param name="summary">何をするコンポーネントか</param>
        /// <param name="usage">使い方（省略可）</param>
        /// <param name="related">関係するコンポーネントなど（省略可）</param>
        public ComponentSummaryAttribute(string summary, string usage = "", string related = "")
        {
            Summary = summary;
            Usage = usage;
            Related = related;
        }
    }
}
