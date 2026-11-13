// =============================================================================
//  BoostEffect.cs
// -----------------------------------------------------------------------------
//  【演出】ブーストの炎と光の帯
//    ブースト（BoostParts）を使っている間だけ、
//    マフラーから炎を出し、車の後ろに光の帯（Trail）を引きます。
//    BoostParts が付いていない車では、何も出ません。
//
//  【使っている Unity の機能】
//    Particle System … 炎の粒を出す
//    Trail Renderer  … 動いたあとに線（帯）を残すコンポーネント。emitting で描く / 描かないを切り替える
// =============================================================================

using UnityEngine;

namespace VantanSchoolFestival2026
{
    /// <summary>ブースト中だけ、炎と光の帯を出す。</summary>
    [AddComponentMenu("VantanSchoolFestival2026/演出/ブーストの炎 (BoostEffect)")]
    [ComponentSummary(
        "ブーストを使っている間だけ、マフラーから炎を出し、車の後ろに光の帯を引きます。",
        "RCCar に付けて、炎の Particle System と Trail Renderer を下の欄に入れます。BoostParts が無い車では何も出ません。",
        "BoostParts / Particle System / Trail Renderer")]
    [DisallowMultipleComponent]
    public class BoostEffect : MonoBehaviour
    {
        [Header("炎")]
        [Tooltip("ブースト中に出す炎の Particle System。")]
        [SerializeField] private ParticleSystem[] _flames = new ParticleSystem[0];

        [Header("光の帯")]
        [Tooltip("ブースト中に線を引く Trail Renderer。")]
        [SerializeField] private TrailRenderer[] _trails = new TrailRenderer[0];

        private RCCar _car;
        private bool _wasBoosting;
        private Vector3 _lastPosition;

        private void Awake()
        {
            _car = GetComponentInParent<RCCar>();
            SetBoosting(false);
        }

        private void Update()
        {
            // BoostParts は途中で付けたり外したりできるので、毎回さがす
            BoostParts boost = _car != null ? _car.GetComponent<BoostParts>() : null;
            bool boosting = boost != null && boost.enabled && boost.IsBoosting;

            // 変わったときだけ切り替える
            if (boosting != _wasBoosting) SetBoosting(boosting);
        }

        private void LateUpdate()
        {
            if (_car == null) return;

            // やり直し（リスポーン）でワープしたときに、帯が長く伸びないように消す
            // 1 フレームで 5 メートル以上動いたら、ワープしたとみなす
            Vector3 position = _car.transform.position;
            if ((position - _lastPosition).sqrMagnitude > 25f)
            {
                foreach (TrailRenderer trail in _trails)
                {
                    if (trail != null) trail.Clear();
                }
            }
            _lastPosition = position;
        }

        private void SetBoosting(bool boosting)
        {
            _wasBoosting = boosting;

            foreach (ParticleSystem flame in _flames)
            {
                if (flame == null) continue;
                if (boosting) flame.Play();
                else flame.Stop(true, ParticleSystemStopBehavior.StopEmitting);   // 出ている炎は自然に消す
            }

            foreach (TrailRenderer trail in _trails)
            {
                if (trail == null) continue;
                trail.emitting = boosting;
            }
        }
    }
}
