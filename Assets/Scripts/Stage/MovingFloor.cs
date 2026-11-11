// =============================================================================
//  MovingFloor.cs
// -----------------------------------------------------------------------------
//  【ステージ部品】動く床
//    スタート位置と「スタート位置 + Move Offset」の間を行ったり来たりします。
//    上に乗った車も一緒に運ばれます。
//
//  【しくみ】
//    ・床は Rigidbody を「Is Kinematic（物理で動かさず、スクリプトで動かす）」にする
//    ・床が動いた分だけ、上に乗っている車の位置もずらす
// =============================================================================

using System.Collections.Generic;
using UnityEngine;

namespace VantanSchoolFestival2026
{
    /// <summary>2 点の間を往復する床。乗っている車も運ぶ。</summary>
    [AddComponentMenu("VantanSchoolFestival2026/ステージ/動く床 (MovingFloor)")]
    [ComponentSummary(
        "2 点の間を往復する床です。上に乗った車も一緒に運びます。",
        "Move Offset に動く距離を入れます。選択すると移動先が緑の線で表示されます。",
        "")]
    [RequireComponent(typeof(Rigidbody))]
    [DisallowMultipleComponent]
    public class MovingFloor : MonoBehaviour
    {
        [Header("動き")]
        [Tooltip("スタート位置からどれだけ動くか（ワールド座標、メートル）。")]
        [SerializeField] private Vector3 _moveOffset = new Vector3(0f, 0f, 6f);

        [Tooltip("片道にかかる時間（秒）。")]
        [Range(0.5f, 20f)]
        [SerializeField] private float _travelTime = 3f;

        [Tooltip("端に着いたら止まる時間（秒）。")]
        [Range(0f, 5f)]
        [SerializeField] private float _waitTime = 0.5f;

        private Rigidbody _body;
        private Vector3 _startPosition;
        private float _timer;

        // 上に乗っている車の Rigidbody 一覧
        private readonly HashSet<Rigidbody> _riders = new HashSet<Rigidbody>();

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
            _body.isKinematic = true;   // 物理演算に押されないようにする
            _body.interpolation = RigidbodyInterpolation.Interpolate;
            _startPosition = transform.position;
        }

        private void FixedUpdate()
        {
            _timer += Time.fixedDeltaTime;

            // 1 往復 = (移動 + 待ち) × 2。その中のどこにいるかを 0 ～ 1 で求める
            float cycle = (_travelTime + _waitTime) * 2f;
            float t = _timer % cycle;   // % … 割った余り（周期的にくり返す）

            float progress;
            if (t < _travelTime) progress = t / _travelTime;                                  // 行き
            else if (t < _travelTime + _waitTime) progress = 1f;                              // 向こうで待つ
            else if (t < _travelTime * 2f + _waitTime) progress = 1f - (t - _travelTime - _waitTime) / _travelTime; // 帰り
            else progress = 0f;                                                               // こちらで待つ

            // SmoothStep … 動き出しと止まる直前をゆっくりにする
            progress = Mathf.SmoothStep(0f, 1f, progress);

            Vector3 newPosition = _startPosition + _moveOffset * progress;
            Vector3 delta = newPosition - _body.position;

            _body.MovePosition(newPosition);

            // 乗っている車も同じだけ動かす
            foreach (Rigidbody rider in _riders)
            {
                if (rider != null) rider.position += delta;
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            // 車が乗ったら一覧に追加
            if (StageUtility.FindCar(collision.collider) != null)
            {
                _riders.Add(collision.rigidbody);
            }
        }

        private void OnCollisionExit(Collision collision)
        {
            // 車が降りたら一覧から外す
            if (collision.rigidbody != null) _riders.Remove(collision.rigidbody);
        }

        private void OnDrawGizmosSelected()
        {
            // 移動先を緑の線と箱で表示
            Vector3 start = Application.isPlaying ? _startPosition : transform.position;
            Gizmos.color = Color.green;
            Gizmos.DrawLine(start, start + _moveOffset);
            Gizmos.DrawWireCube(start + _moveOffset, transform.lossyScale);
        }
    }
}
