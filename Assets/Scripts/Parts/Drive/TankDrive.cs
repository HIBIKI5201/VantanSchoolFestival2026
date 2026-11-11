// =============================================================================
//  TankDrive.cs
// -----------------------------------------------------------------------------
//  【走り方】戦車（タンク）風
//    ・左右の入力で「その場で」くるっと回転できる（止まっていても回れる）
//    ・前後の入力で、向いている方向へまっすぐ進む
//    ・最高速度は遅め、でも細かい動きが得意
//
//  【ArcadeDrive との違い】
//    ArcadeDrive は「走っているときだけ」曲がれますが、
//    TankDrive は速さに関係なく曲がれます。たったこれだけで操作感が大きく変わります。
// =============================================================================

using UnityEngine;

namespace VantanSchoolFestival2026
{
    /// <summary>その場で回転できる、戦車のような駆動方式。</summary>
    [AddComponentMenu("VantanSchoolFestival2026/駆動方式/タンク (TankDrive)")]
    [ComponentSummary(
        "車の走り方を決める「駆動方式」の 1 つです。戦車のように、止まっていてもその場でくるっと回れます。",
        "RCCar に 1 つだけ付けます。細かく動きたいときに便利です。",
        "ArcadeDrive / DriftDrive")]
    public class TankDrive : DriveBase
    {
        [Header("速さ")]
        [Tooltip("前進・後退の最高速度（メートル/秒）。")]
        [Range(1f, 30f)]
        [SerializeField] private float _maxSpeed = 8f;

        [Tooltip("加速・減速の強さ（1 秒あたりに変わる速度）。")]
        [Range(1f, 100f)]
        [SerializeField] private float _acceleration = 30f;

        [Header("回転")]
        [Tooltip("1 秒あたりに回転する角度（度）。止まっていてもこの速さで回ります。")]
        [Range(10f, 360f)]
        [SerializeField] private float _turnSpeed = 140f;

        /// <summary>DriveBase から物理演算のたびに呼ばれる。</summary>
        protected override void Drive(Vector2 input, float control, float deltaTime)
        {
            SplitVelocity(out Vector3 flat, out Vector3 vertical);

            // 前後はバックも同じ速さ
            Vector3 targetVelocity = transform.forward * (input.y * _maxSpeed * Car.SpeedMultiplier);
            flat = Vector3.MoveTowards(flat, targetVelocity, _acceleration * control * deltaTime);
            Body.linearVelocity = flat + vertical;

            // 速さに関係なく回転する（ここが ArcadeDrive との違い！）
            Turn(input.x * _turnSpeed * control * deltaTime);
        }
    }
}
