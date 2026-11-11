// =============================================================================
//  Checkpoint.cs
// -----------------------------------------------------------------------------
//  【ステージ部品】チェックポイント（中間地点）
//    車が通過すると、そこがリスポーン地点になります。
//    コースから落ちても、最後に通ったチェックポイントから再スタートできます。
//
//    GameManager の「Require All Checkpoints」を ON にすると、
//    すべてのチェックポイントを通らないとゴールできなくなります。
// =============================================================================

using UnityEngine;

namespace VantanSchoolFestival2026
{
    /// <summary>通過するとリスポーン地点が更新される中間地点。</summary>
    [AddComponentMenu("VantanSchoolFestival2026/ステージ/チェックポイント (Checkpoint)")]
    [ComponentSummary(
        "中間地点です。通過すると、そこがリスポーン（やり直し）地点になります。",
        "コースの途中に置きます。黄色い線の向きが、リスポーンしたときの車の向きになります。",
        "GameManager / KillZone")]
    [RequireComponent(typeof(Collider))]
    [DisallowMultipleComponent]
    public class Checkpoint : MonoBehaviour
    {
        [Header("見た目")]
        [Tooltip("通過前に表示するオブジェクト（旗など）。空でも OK。")]
        [SerializeField] private GameObject _inactiveVisual;

        [Tooltip("通過後に表示するオブジェクト（光る旗など）。空でも OK。")]
        [SerializeField] private GameObject _activeVisual;

        /// <summary>もう通過したか。</summary>
        public bool IsPassed { get; private set; }

        private void Awake()
        {
            StageUtility.MakeTrigger(gameObject);
            UpdateVisual();
        }

        private void OnTriggerEnter(Collider other)
        {
            RCCar car = StageUtility.FindCar(other);
            if (car == null) return;

            // リスポーン地点をこのチェックポイントに更新
            car.SetRespawnPoint(transform.position, transform.rotation);

            IsPassed = true;
            UpdateVisual();

            // GameManager があれば通過を知らせる
            if (GameManager.Instance != null) GameManager.Instance.ReachCheckpoint(this);
        }

        /// <summary>通過済みかどうかで見た目を切り替える。</summary>
        private void UpdateVisual()
        {
            if (_inactiveVisual != null) _inactiveVisual.SetActive(!IsPassed);
            if (_activeVisual != null) _activeVisual.SetActive(IsPassed);
        }

        private void OnDrawGizmos()
        {
            // 出現する向きを黄色い線で表示
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * 2f);
        }
    }
}
