// =============================================================================
//  ColorParts.cs
// -----------------------------------------------------------------------------
//  【追加パーツ】ボディカラー
//    Inspector で色を選ぶだけで、車の色が変わります。
//    再生していなくても（エディタ上でも）すぐに反映されます。
//
//  【MaterialPropertyBlock とは？】
//    マテリアル（見た目の設定ファイル）を直接書き換えると、
//    同じマテリアルを使っている「ほかのオブジェクト」まで色が変わってしまいます。
//    MaterialPropertyBlock を使うと「このオブジェクトだけ」色を変えられます。
// =============================================================================

using UnityEngine;

namespace VantanSchoolFestival2026
{
    /// <summary>指定した Renderer の色を変える追加パーツ。</summary>
    [AddComponentMenu("VantanSchoolFestival2026/追加パーツ/ボディカラー (ColorParts)")]
    [ComponentSummary(
        "車の色を変える追加パーツです。再生していなくても色が変わります。",
        "Targets に色を変えたい Renderer（車体など）をドラッグし、Body Color を選びます。",
        "")]
    [DisallowMultipleComponent]
    // ExecuteAlways … 再生していないときもスクリプトを動かす（色をすぐ見られるように）
    [ExecuteAlways]
    public class ColorParts : MonoBehaviour
    {
        [Header("色")]
        [Tooltip("ボディの色。")]
        [SerializeField] private Color _bodyColor = new Color(0.9f, 0.2f, 0.2f);

        [Header("対象")]
        [Tooltip("色を変える Renderer（見た目の部品）。車体のメッシュをドラッグして入れてください。")]
        [SerializeField] private Renderer[] _targets = new Renderer[0];

        // シェーダーの色プロパティの名前
        //   URP の Lit シェーダー → "_BaseColor"
        //   古い Standard シェーダー → "_Color"
        // Shader.PropertyToID で文字列を数値にしておくと処理が速い
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private MaterialPropertyBlock _block;

        /// <summary>今のボディカラー。スクリプトから変えたいときは SetColor を使う。</summary>
        public Color BodyColor => _bodyColor;

        private void OnEnable()
        {
            Apply();
        }

        /// <summary>
        /// OnValidate … Inspector で値を変えたときに呼ばれる（エディタ専用）。
        /// </summary>
        private void OnValidate()
        {
            Apply();
        }

        /// <summary>スクリプトから色を変える。</summary>
        public void SetColor(Color color)
        {
            _bodyColor = color;
            Apply();
        }

        /// <summary>対象の Renderer すべてに色を反映する。</summary>
        private void Apply()
        {
            if (_targets == null) return;
            if (_block == null) _block = new MaterialPropertyBlock();

            foreach (Renderer target in _targets)
            {
                if (target == null) continue;   // 空の欄は飛ばす

                target.GetPropertyBlock(_block);
                _block.SetColor(BaseColorId, _bodyColor);
                _block.SetColor(ColorId, _bodyColor);
                target.SetPropertyBlock(_block);
            }
        }
    }
}
