// =============================================================================
//  HudView.cs
// -----------------------------------------------------------------------------
//  【役割】MVP の「View（見た目）」
//    タイム・カウントダウン・チェックポイント数・ブーストゲージを「表示するだけ」。
//    ゲームのルールや状態は何も知りません。
//    「何を表示するか」は HudPresenter が決めて、このクラスのメソッドを呼びます。
//
//  【MVP（Model - View - Presenter）とは？】
//    画面まわりのプログラムを 3 つの役割に分ける考え方です。
//
//      Model     … データとルール      （GameManager, RCCar, BoostParts）
//      View      … 見た目（表示だけ）  （HudView ← このファイル）
//      Presenter … 2 つをつなぐ係      （HudPresenter）
//
//    View がゲームのことを知らないので、
//    見た目を作り直しても（文字の場所を変えても）ゲーム側のコードは変えなくて済みます。
// =============================================================================

using UnityEngine;
using UnityEngine.UI;

namespace VantanSchoolFestival2026
{
    /// <summary>HUD（画面上の情報表示）の見た目。値を受け取って表示するだけ。</summary>
    [AddComponentMenu("VantanSchoolFestival2026/UI/HUD 表示 (HudView)")]
    [ComponentSummary(
        "画面にタイム・カウントダウン・チェックポイントの数・燃料ゲージを表示する係です。",
        "GameUI に付いています。文字の大きさや場所を変えたいときは、TimerText などの子どもオブジェクトを選んで変えます。",
        "HudPresenter / GameManager")]
    [DisallowMultipleComponent]
    public class HudView : MonoBehaviour
    {
        [Header("表示先（空のものは表示しません）")]
        [Tooltip("走行タイムを表示する Text。")]
        [SerializeField] private Text _timerText;

        [Tooltip("カウントダウンや「ゴール！」を大きく表示する Text。")]
        [SerializeField] private Text _messageText;

        [Tooltip("チェックポイントの通過数を表示する Text。")]
        [SerializeField] private Text _checkpointText;

        [Tooltip("燃料ゲージの外枠。ブーストのパーツが無いときは、この外枠ごと隠します。")]
        [SerializeField] private Image _boostGauge;

        [Tooltip("燃料ゲージの中身のバー。燃料の量に合わせて横の長さが変わります。")]
        [SerializeField] private RectTransform _boostFill;

        /// <summary>走行タイム（秒）を表示する。</summary>
        public void SetTime(float seconds)
        {
            // ToString("F2") … 小数点以下 2 桁で文字にする
            SetText(_timerText, "TIME " + seconds.ToString("F2"));
        }

        /// <summary>画面中央のメッセージを表示する。空文字なら消える。</summary>
        public void SetMessage(string message)
        {
            SetText(_messageText, message);
        }

        /// <summary>チェックポイントの通過数を表示する。total が 0 なら消える。</summary>
        public void SetCheckpoints(int passed, int total)
        {
            SetText(_checkpointText, total > 0 ? "CP " + passed + " / " + total : "");
        }

        /// <summary>ブーストゲージを表示する。visible が false なら隠す。</summary>
        /// <param name="visible">表示するか</param>
        /// <param name="amount01">残量（0 ～ 1）</param>
        public void SetBoost(bool visible, float amount01)
        {
            if (_boostGauge == null) return;

            // 表示 / 非表示が変わるときだけ SetActive する（毎フレーム呼ぶと無駄なため）
            if (_boostGauge.gameObject.activeSelf != visible)
            {
                _boostGauge.gameObject.SetActive(visible);
            }

            if (_boostFill == null) return;

            // 中身のバーの右端を、燃料の量に合わせて動かす（0 で左端、1 で右端）
            // Image Type を Sliced（9 スライス）にしているので、長さが変わっても丸い端は伸びない
            Vector2 anchorMax = _boostFill.anchorMax;
            anchorMax.x = amount01;
            _boostFill.anchorMax = anchorMax;

            // ほとんど空のときは、つぶれた形が見えないように隠す
            bool hasEnergy = amount01 > 0.03f;
            if (_boostFill.gameObject.activeSelf != hasEnergy)
            {
                _boostFill.gameObject.SetActive(hasEnergy);
            }
        }

        /// <summary>Text が設定されていれば文字を書き換える（null チェックをまとめたもの）。</summary>
        private static void SetText(Text text, string value)
        {
            if (text != null && text.text != value) text.text = value;
        }
    }
}
