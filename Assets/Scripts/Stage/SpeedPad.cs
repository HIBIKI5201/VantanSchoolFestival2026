// =============================================================================
//  SpeedPad.cs
// -----------------------------------------------------------------------------
//  【ステージ部品】加速床
//    上を通った車を、床の向き（青い矢印 = Z 軸）に押し出し、
//    しばらくの間スピードアップさせます。
//    回転させて置けば、好きな方向に加速させられます。
// =============================================================================

using UnityEngine;

namespace VantanSchoolFestival2026
{
    /// <summary>通った車を加速させる床。</summary>
    [AddComponentMenu("VantanSchoolFestival2026/ステージ/加速床 (SpeedPad)")]
    [ComponentSummary(
        "通るとスピードアップする床です。",
        "置いて向きを回すと、矢印の向きに加速します。",
        "RCCar")]
    [RequireComponent(typeof(Collider))]
    [DisallowMultipleComponent]
    public class SpeedPad : MonoBehaviour
    {
        [Header("加速")]
        [Tooltip("乗った瞬間に加える前向きの速度（メートル/秒）。")]
        [Range(0f, 50f)]
        [SerializeField] private float _pushPower = 10f;

        [Tooltip("効果時間中の速度倍率。")]
        [Range(1f, 3f)]
        [SerializeField] private float _speedMultiplier = 1.5f;

        [Tooltip("効果時間（秒）。")]
        [Range(0f, 10f)]
        [SerializeField] private float _duration = 1.5f;

        private void Awake()
        {
            StageUtility.MakeTrigger(gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            RCCar car = StageUtility.FindCar(other);
            if (car == null) return;

            // 床の前方向に押し出す
            car.Body.AddForce(transform.forward * _pushPower, ForceMode.VelocityChange);

            // しばらく最高速度を上げる
            car.AddTemporarySpeedUp(_speedMultiplier, _duration);
        }

        private void OnDrawGizmos()
        {
            // 加速する向きを水色の矢印で表示
            Gizmos.color = Color.cyan;
            Vector3 start = transform.position + Vector3.up * 0.2f;
            Vector3 end = start + transform.forward * 2f;
            Gizmos.DrawLine(start, end);
            Gizmos.DrawLine(end, end - transform.forward * 0.5f + transform.right * 0.4f);
            Gizmos.DrawLine(end, end - transform.forward * 0.5f - transform.right * 0.4f);
        }
    }
}
