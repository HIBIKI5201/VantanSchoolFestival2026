// =============================================================================
//  WaterSplash.cs
// -----------------------------------------------------------------------------
//  【演出】水しぶき
//    車が水面にふれたとき、その場所に水しぶきを出します。
//    水面の少し上に「トリガー（通り抜けられる当たり判定）」を置いて使います。
//    （下にある KillZone で車が戻される前に、しぶきを出すため）
//
//  【使っている Unity の機能】
//    Instantiate … プレハブ（ここでは水しぶきのパーティクル）をシーンに作る
//    Particle System の Stop Action = Destroy … 出し終わったら自動で消える
// =============================================================================

using UnityEngine;

namespace VantanSchoolFestival2026
{
    /// <summary>車が水面にふれたら、水しぶきを出す。</summary>
    [AddComponentMenu("VantanSchoolFestival2026/演出/水しぶき (WaterSplash)")]
    [ComponentSummary(
        "車が水面にふれたとき、その場所に水しぶきを出します。",
        "水面の少し上に、トリガーの Collider と一緒に付けます。しぶきのプレハブを下の欄に入れます。",
        "KillZone / Particle System")]
    [RequireComponent(typeof(Collider))]
    [DisallowMultipleComponent]
    public class WaterSplash : MonoBehaviour
    {
        [Header("水しぶき")]
        [Tooltip("出す水しぶきのプレハブ（Particle System）。")]
        [SerializeField] private ParticleSystem _splashPrefab;

        [Tooltip("続けて何度も出さないための待ち時間（秒）。")]
        [Range(0f, 3f)]
        [SerializeField] private float _cooldown = 0.5f;

        private float _lastSplashTime = -100f;

        private void Awake()
        {
            StageUtility.MakeTrigger(gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_splashPrefab == null) return;

            RCCar car = StageUtility.FindCar(other);
            if (car == null) return;
            if (Time.time - _lastSplashTime < _cooldown) return;
            _lastSplashTime = Time.time;

            // 車の真下の、トリガーの上の面の高さに出す
            Bounds bounds = GetComponent<Collider>().bounds;
            Vector3 position = car.transform.position;
            position.y = bounds.max.y;

            Instantiate(_splashPrefab, position, Quaternion.identity);
        }
    }
}
