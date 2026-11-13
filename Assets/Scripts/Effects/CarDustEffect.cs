// =============================================================================
//  CarDustEffect.cs
// -----------------------------------------------------------------------------
//  【演出】タイヤの砂ぼこり
//    車が地面を走っているときだけ、タイヤの後ろから砂ぼこりを出します。
//    速く走るほど、たくさん出ます。
//
//  【使っている Unity の機能】
//    Particle System … 小さな粒（パーティクル）をたくさん出して、煙や火花などを表すしくみ
//    emission.rateOverDistance … 「動いた距離 1 メートルごとに何個出すか」の設定
// =============================================================================

using UnityEngine;

namespace VantanSchoolFestival2026
{
    /// <summary>地面を走っているときだけ、タイヤから砂ぼこりを出す。</summary>
    [AddComponentMenu("VantanSchoolFestival2026/演出/砂ぼこり (CarDustEffect)")]
    [ComponentSummary(
        "車が地面を走っているときだけ、タイヤから砂ぼこりを出します。速いほどたくさん出ます。",
        "RCCar に付けて、砂ぼこりの Particle System をタイヤの後ろに置き、下のリストに入れます。",
        "RCCar / Particle System")]
    [DisallowMultipleComponent]
    public class CarDustEffect : MonoBehaviour
    {
        [Header("砂ぼこり")]
        [Tooltip("砂ぼこりを出す Particle System（後ろのタイヤに 1 つずつなど）。")]
        [SerializeField] private ParticleSystem[] _dustParticles = new ParticleSystem[0];

        [Tooltip("この速さ（メートル/秒）より遅いときは出さない。")]
        [Range(0f, 10f)]
        [SerializeField] private float _minSpeed = 2f;

        [Tooltip("いちばん速いときに、1 メートル進むごとに出す粒の数。")]
        [Range(0f, 20f)]
        [SerializeField] private float _maxAmountPerMeter = 4f;

        [Tooltip("この速さ（メートル/秒）で、いちばんたくさん出る。")]
        [Range(1f, 50f)]
        [SerializeField] private float _fullSpeed = 15f;

        private RCCar _car;

        private void Awake()
        {
            _car = GetComponentInParent<RCCar>();
        }

        private void Update()
        {
            if (_car == null || _car.Body == null) return;

            float speed = _car.Body.linearVelocity.magnitude;

            // 地面にいて、ある程度の速さがあるときだけ出す
            float amount = 0f;
            if (_car.IsGrounded && speed > _minSpeed)
            {
                // 速さを 0〜1 にして、出す量を決める
                float t = Mathf.InverseLerp(_minSpeed, _fullSpeed, speed);
                amount = _maxAmountPerMeter * t;
            }

            foreach (ParticleSystem dust in _dustParticles)
            {
                if (dust == null) continue;
                // emission … 粒の出し方の設定。rateOverDistance は「1 メートル進むごとの数」
                ParticleSystem.EmissionModule emission = dust.emission;
                emission.rateOverDistance = amount;
            }
        }
    }
}
