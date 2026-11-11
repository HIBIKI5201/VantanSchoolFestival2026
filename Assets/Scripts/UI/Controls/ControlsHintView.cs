// =============================================================================
//  ControlsHintView.cs
// -----------------------------------------------------------------------------
//  【役割】MVP の「View（見た目）」
//    画面左下に「操作方法」を表示するだけのクラス。
//    どのキーを表示するかは ControlsHintPresenter が決めます。
//
//  【見た目を変えたいときは】
//    Canvas の中の ControlsHint を選んで、Text のフォントサイズや色、
//    RectTransform の位置を変えるだけで OK。このスクリプトを触る必要はありません。
// =============================================================================

using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace VantanSchoolFestival2026
{
    /// <summary>操作方法の一覧を表示する。</summary>
    [AddComponentMenu("VantanSchoolFestival2026/UI/操作方法 表示 (ControlsHintView)")]
    [ComponentSummary(
        "画面の左下に、操作方法（どのキーで何ができるか）を表示する係です。",
        "GameUI の ControlsHint に付いています。",
        "ControlsHintPresenter")]
    [DisallowMultipleComponent]
    public class ControlsHintView : MonoBehaviour
    {
        [Header("表示先")]
        [Tooltip("見出し（例: 操作方法 [Driving / Keyboard]）を表示する Text。空でも OK。")]
        [SerializeField] private Text _titleText;

        [Tooltip("キーと説明の一覧を表示する Text。")]
        [SerializeField] private Text _bodyText;

        [Header("見た目")]
        [Tooltip("キーの名前に付ける色。")]
        [SerializeField] private Color _keyColor = new Color(1f, 0.85f, 0.3f);

        // StringBuilder … 文字列を何回もつなげるときに使う（+ でつなぐより速い）
        private readonly StringBuilder _builder = new StringBuilder();

        /// <summary>見出しと一覧を表示する。</summary>
        public void Show(string title, IReadOnlyList<ControlsHintModel.Entry> entries)
        {
            if (_titleText != null) _titleText.text = title;
            if (_bodyText == null) return;

            // ColorUtility.ToHtmlStringRGB … Color を "FFD84C" のような文字にする
            string colorCode = ColorUtility.ToHtmlStringRGB(_keyColor);

            _builder.Clear();
            foreach (ControlsHintModel.Entry entry in entries)
            {
                // リッチテキスト：<color=#…>…</color> で一部の色を変えられる
                _builder.Append("<color=#").Append(colorCode).Append('>')
                        .Append(entry.Keys).Append("</color>  ")
                        .Append(entry.Label).Append('\n');
            }
            _bodyText.text = _builder.ToString().TrimEnd('\n');
        }

        /// <summary>表示 / 非表示を切り替える。</summary>
        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }
    }
}
