// =============================================================================
//  ArcadeDrive.cs
// -----------------------------------------------------------------------------
//  【走り方】アーケード（ゲームセンター）風
//    ・アクセルを押すと、まっすぐ素直に加速する
//    ・ハンドルを切ると、向いている方向にピタッと曲がる（横滑りしない）
//    ・止まっているときは曲がれない（本物の車と同じ）
//
//    いちばん操作しやすいので、最初はこの駆動方式を使いましょう。
// =============================================================================

using UnityEngine;

namespace VantanSchoolFestival2026
{
    /// <summary>素直に加速・旋回する、操作しやすい駆動方式。</summary>
    [AddComponentMenu("VantanSchoolFestival2026/駆動方式/アーケード (ArcadeDrive)")]
    [ComponentSummary(
        "アーケードゲーム風の駆動方式です。素直に加速し、ハンドルを切った方向にピタッと曲がります。いちばん操作しやすい走り方です。",
        "RCCar に 1 つだけ付けます。ほかの駆動方式と付け替えて違いを比べてみましょう。",
        "DriveBase（親クラス）/ DriftDrive / TankDrive")]
    public class ArcadeDrive : DriveBase
    {
        [Header("速さ")]
        [Tooltip("前進の最高速度（メートル/秒）。")]
        [Range(1f, 50f)]
        [SerializeField] private float _maxSpeed = 15f;

        [Tooltip("バックの最高速度（メートル/秒）。")]
        [Range(1f, 30f)]
        [SerializeField] private float _maxReverseSpeed = 6f;

        [Tooltip("加速の強さ（1 秒あたりに増える速度）。大きいほどすぐ最高速度になります。")]
        [Range(1f, 100f)]
        [SerializeField] private float _acceleration = 25f;

        [Tooltip("アクセルを離したときに減速する強さ。")]
        [Range(0f, 100f)]
        [SerializeField] private float _brake = 15f;

        [Header("ハンドル")]
        [Tooltip("1 秒あたりに曲がれる角度（度）。大きいほどクイックに曲がります。")]
        [Range(10f, 360f)]
        [SerializeField] private float _turnSpeed = 120f;

        /// <summary>DriveBase から物理演算のたびに呼ばれる。</summary>
        protected override void Drive(Vector2 input, float control, float deltaTime)
        {
            // --- 1. 目標の速さを決める ------------------------------------------
            // input.y が 1 なら最高速度、-1 ならバックの最高速度
            float targetSpeed = input.y >= 0f
                ? input.y * _maxSpeed * Car.SpeedMultiplier
                : input.y * _maxReverseSpeed;

            // アクセルを押しているかどうかで、加速の強さを変える
            float rate = Mathf.Approximately(input.y, 0f) ? _brake : _acceleration;

            // --- 2. 速度を目標に近づける ----------------------------------------
            SplitVelocity(out Vector3 flat, out Vector3 vertical);

            // 「車の向き × 目標の速さ」に向かって、今の速度を少しずつ近づける
            // → 横向きの速度も 0 に近づくので、横滑りしない
            Vector3 targetVelocity = transform.forward * targetSpeed;
            flat = Vector3.MoveTowards(flat, targetVelocity, rate * control * deltaTime);

            Body.linearVelocity = flat + vertical;

            // --- 3. 曲がる ------------------------------------------------------
            // 走っている速さに応じて曲がる（止まっているときは曲がらない）
            // InverseLerp(0, 3, 速さ) … 速さ 0 なら 0、3 以上なら 1 になる
            float speedFactor = Mathf.InverseLerp(0f, 3f, Mathf.Abs(ForwardSpeed));

            // バックのときはハンドルを逆にする（本物の車と同じ）
            float direction = ForwardSpeed >= 0f ? 1f : -1f;

            Turn(input.x * _turnSpeed * speedFactor * direction * control * deltaTime);
        }
    }
}
