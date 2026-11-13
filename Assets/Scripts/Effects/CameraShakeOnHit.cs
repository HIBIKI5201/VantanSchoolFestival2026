// =============================================================================
//  CameraShakeOnHit.cs
// -----------------------------------------------------------------------------
//  【演出】ぶつかったときの画面の揺れ
//    車が強くぶつかったとき（バンパー・壁・ジャンプの着地など）に、カメラを揺らします。
//    ぶつかる強さが大きいほど、大きく揺れます。
//
//  【しくみ】
//    毎回の物理の計算（FixedUpdate）で、車の速さをおぼえておき、
//    「進んでいた向きの速さが、急にどれだけ減ったか」を、ぶつかった強さとして使います。
//    （加速床やジャンプのように速さが増えるときや、横向きに動くときは揺らしません）
//
//  【使っている Unity の機能】
//    Cinemachine Impulse … カメラを揺らすための、Cinemachine の機能
//      ・CinemachineImpulseSource   … 揺れを「出す」側（このオブジェクトに付ける）
//      ・CinemachineImpulseListener … 揺れを「受け取る」側（カメラに付ける）
// =============================================================================

using Unity.Cinemachine;
using UnityEngine;

namespace VantanSchoolFestival2026
{
    /// <summary>車が強くぶつかったら、カメラを揺らす。</summary>
    [AddComponentMenu("VantanSchoolFestival2026/演出/画面の揺れ (CameraShakeOnHit)")]
    [ComponentSummary(
        "車が強くぶつかったとき（バンパー・壁・着地など）に、画面を揺らします。",
        "RCCar か、その子オブジェクトに付けます。カメラ側には CinemachineImpulseListener が必要です。",
        "RCCar / CinemachineImpulseSource / CinemachineImpulseListener")]
    [RequireComponent(typeof(CinemachineImpulseSource))]
    [DisallowMultipleComponent]
    public class CameraShakeOnHit : MonoBehaviour
    {
        [Header("揺れ")]
        [Tooltip("速さがこれ以上（メートル/秒）急に減ったら、ぶつかったとみなして揺らす。")]
        [Range(0f, 20f)]
        [SerializeField] private float _minImpactSpeed = 5f;

        [Tooltip("ぶつかる速さ 1 メートル/秒ごとに増える、揺れの大きさ。")]
        [Range(0f, 0.2f)]
        [SerializeField] private float _strength = 0.04f;

        [Tooltip("揺れの大きさの上限。")]
        [Range(0f, 2f)]
        [SerializeField] private float _maxStrength = 0.6f;

        private CinemachineImpulseSource _impulse;
        private RCCar _car;
        private Vector3 _lastVelocity;
        private Vector3 _lastPosition;

        private void Awake()
        {
            _impulse = GetComponent<CinemachineImpulseSource>();
            _car = GetComponentInParent<RCCar>();
        }

        private void FixedUpdate()
        {
            if (_car == null || _car.Body == null) return;

            Vector3 velocity = _car.Body.linearVelocity;
            Vector3 position = _car.Body.position;

            // やり直し（リスポーン）でワープしたときは、ぶつかったとみなさない
            bool warped = (position - _lastPosition).sqrMagnitude > 25f;

            if (!warped && _lastVelocity.sqrMagnitude > 0.01f)
            {
                // 進んでいた向きの速さが、どれだけ減ったか
                Vector3 change = velocity - _lastVelocity;
                float impact = -Vector3.Dot(change, _lastVelocity.normalized);

                if (impact >= _minImpactSpeed)
                {
                    float power = Mathf.Min((impact - _minImpactSpeed + 1f) * _strength, _maxStrength);
                    // ぶつかった向きに揺らす
                    _impulse.GenerateImpulseWithVelocity(-_lastVelocity.normalized * power);
                }
            }

            _lastVelocity = velocity;
            _lastPosition = position;
        }
    }
}
