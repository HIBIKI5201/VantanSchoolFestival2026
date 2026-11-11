// =============================================================================
//  Goal.cs
// -----------------------------------------------------------------------------
//  【ステージ部品】ゴール
//    車がこのオブジェクトの当たり判定（トリガー）に入るとゴールになります。
//
//  【必要なもの】
//    ・Collider（Box Collider など）… Is Trigger を ON にする
//      （このスクリプトが開始時に自動で ON にしてくれますが、
//        ステージチェッカーでは「OFF のままだよ」と警告されます）
// =============================================================================

using UnityEngine;

namespace VantanSchoolFestival2026
{
    /// <summary>車が入るとゴールになるエリア。</summary>
    [AddComponentMenu("VantanSchoolFestival2026/ステージ/ゴール (Goal)")]
    [ComponentSummary(
        "車が入るとゴールになるエリアです。",
        "Collider の Is Trigger を ON にして置きます。GameManager がタイムを確定します。",
        "GameManager / Checkpoint")]
    [RequireComponent(typeof(Collider))]
    [DisallowMultipleComponent]
    public class Goal : MonoBehaviour
    {
        [Header("演出")]
        [Tooltip("ゴールしたときに表示（Active に）するオブジェクト。紙吹雪のパーティクルなど。空でも OK。")]
        [SerializeField] private GameObject _goalEffect;

        private void Awake()
        {
            // 当たり判定をトリガーにする（ぶつからずに通り抜けられるように）
            StageUtility.MakeTrigger(gameObject);

            // 演出は最初は隠しておく
            if (_goalEffect != null) _goalEffect.SetActive(false);
        }

        /// <summary>
        /// OnTriggerEnter … トリガーに何かが入った瞬間に呼ばれる。
        /// other には「入ってきた相手の Collider」が入っている。
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            // 入ってきたのが車でなければ無視
            RCCar car = StageUtility.FindCar(other);
            if (car == null) return;

            if (GameManager.Instance == null)
            {
                Debug.Log("[Goal] ゴールに到着！（GameManager が無いのでタイムは計測されていません）", this);
                return;
            }

            // ゴールできる状態かを GameManager に聞いてから処理する
            bool canFinish = GameManager.Instance.CanFinish();
            GameManager.Instance.ReachGoal();

            if (canFinish && _goalEffect != null) _goalEffect.SetActive(true);
        }
    }
}
