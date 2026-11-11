// =============================================================================
//  KillZone.cs
// -----------------------------------------------------------------------------
//  【ステージ部品】落下ゾーン
//    車がここに入ると、最後のチェックポイント（またはスタート地点）に戻されます。
//    コースの下に大きく敷いておくと、落ちたときに自動でやり直せます。
//
//    見た目は不要です。MeshRenderer は外すか無効にしておきましょう。
// =============================================================================

using UnityEngine;

namespace VantanSchoolFestival2026
{
    /// <summary>入った車をリスポーンさせるエリア。</summary>
    [AddComponentMenu("VantanSchoolFestival2026/ステージ/落下ゾーン (KillZone)")]
    [ComponentSummary(
        "落ちたら戻される場所です。車が入ると、最後に通ったチェックポイント（まだ通っていなければスタート地点）に戻ります。",
        "コースの下に、大きく広げて置きます。",
        "Checkpoint / StartPoint")]
    [RequireComponent(typeof(Collider))]
    [DisallowMultipleComponent]
    public class KillZone : MonoBehaviour
    {
        [Header("デバッグ")]
        [Tooltip("ONにすると、車が落下したときに Console にログを出します。")]
        [SerializeField] private bool _logRespawn = true;

        private void Awake()
        {
            StageUtility.MakeTrigger(gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            RCCar car = StageUtility.FindCar(other);
            if (car == null) return;

            if (_logRespawn) Debug.Log("[KillZone] 落下したのでリスポーンします", this);
            car.Respawn();
        }

        private void OnDrawGizmos()
        {
            // 範囲を半透明の赤で表示
            BoxCollider box = GetComponent<BoxCollider>();
            if (box == null) return;

            Gizmos.color = new Color(1f, 0f, 0f, 0.15f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
        }
    }
}
