// =============================================================================
//  AnimatorTriggerOnEnable.cs
// -----------------------------------------------------------------------------
//  【演出】表示されたらアニメーションを切り替える
//    このオブジェクトが表示（Active）になったときに、
//    指定した Animator に「トリガー」を送って、アニメーションを切り替えます。
//    例：ゴールしたとき（ゴールの演出が表示されたとき）に、旗を大きく揺らす
//
//  【使っている Unity の機能】
//    Animator   … アニメーションの「状態」と、状態の切り替え方を決めるコンポーネント
//    SetTrigger … Animator のパラメーター（トリガー）を ON にして、状態を切り替える
// =============================================================================

using UnityEngine;

namespace VantanSchoolFestival2026
{
    /// <summary>表示されたときに、Animator にトリガーを送る。</summary>
    [AddComponentMenu("VantanSchoolFestival2026/演出/表示でアニメ切り替え (AnimatorTriggerOnEnable)")]
    [ComponentSummary(
        "このオブジェクトが表示されたときに、Animator のアニメーションを切り替えます。",
        "ゴールの演出など、あとから表示されるオブジェクトに付けて、切り替えたい Animator とトリガーの名前を入れます。",
        "Animator / Goal")]
    public class AnimatorTriggerOnEnable : MonoBehaviour
    {
        [Header("アニメーション")]
        [Tooltip("トリガーを送る Animator。")]
        [SerializeField] private Animator _animator;

        [Tooltip("送るトリガーの名前（Animator の Parameters に書いた名前）。")]
        [SerializeField] private string _triggerName = "Celebrate";

        private void OnEnable()
        {
            if (_animator != null && !string.IsNullOrEmpty(_triggerName)) _animator.SetTrigger(_triggerName);
        }
    }
}
