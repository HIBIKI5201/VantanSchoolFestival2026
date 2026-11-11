// =============================================================================
//  Bumper.cs
// -----------------------------------------------------------------------------
//  【ステージ部品】バンパー（はね返し）
//    ぶつかった車を勢いよくはじき飛ばします。ピンボールのバンパーのイメージです。
//
//  【トリガーとの違い】
//    Goal や SpeedPad は「通り抜ける」トリガーでしたが、
//    Bumper は「実際にぶつかる」普通の Collider を使います。
//    そのため OnTriggerEnter ではなく OnCollisionEnter を使っています。
// =============================================================================

using UnityEngine;

namespace VantanSchoolFestival2026
{
    /// <summary>ぶつかった車をはじき飛ばす障害物。</summary>
    [AddComponentMenu("VantanSchoolFestival2026/ステージ/バンパー (Bumper)")]
    [ComponentSummary(
        "ぶつかった車をはじき飛ばす障害物です。",
        "普通の Collider（Is Trigger は OFF）と一緒に置きます。Bounce Power ではじく強さを変えられます。",
        "")]
    [RequireComponent(typeof(Collider))]
    [DisallowMultipleComponent]
    public class Bumper : MonoBehaviour
    {
        [Header("はね返し")]
        [Tooltip("はじき飛ばす強さ（メートル/秒）。")]
        [Range(0f, 50f)]
        [SerializeField] private float _bouncePower = 12f;

        [Tooltip("はじくときに少し上向きにする量。0 で真横、1 で斜め 45 度くらい。")]
        [Range(0f, 1f)]
        [SerializeField] private float _upward = 0.2f;

        /// <summary>
        /// OnCollisionEnter … 普通の Collider 同士がぶつかった瞬間に呼ばれる。
        /// </summary>
        private void OnCollisionEnter(Collision collision)
        {
            RCCar car = StageUtility.FindCar(collision.collider);
            if (car == null) return;

            // バンパーの中心 → 車 の向きにはじく
            Vector3 direction = car.transform.position - transform.position;
            direction.y = 0f;                       // まずは水平方向だけにする
            direction = direction.normalized;       // 長さを 1 にそろえる
            direction += Vector3.up * _upward;      // 少し上向きを足す

            car.Body.AddForce(direction.normalized * _bouncePower, ForceMode.VelocityChange);
        }
    }
}
