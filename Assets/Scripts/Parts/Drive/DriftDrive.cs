// =============================================================================
//  DriftDrive.cs
// -----------------------------------------------------------------------------
//  【走り方】ドリフト（横滑り）
//    ・アクセルで前に「押す力」をかける（すぐには最高速度にならない）
//    ・ハンドルを切っても、勢いが残ってツーッと横に滑る
//    ・Grip(グリップ) を大きくするほど滑りにくくなる
//
//  【ArcadeDrive との違い】
//    ArcadeDrive … 速度を「直接」目標に合わせる → 横滑りしない
//    DriftDrive  … 「横向きの速度」をゆっくり消していく → 消えるまで滑る
//
//  【試してみよう】
//    Grip を 0.5 くらいにすると氷の上、10 くらいにするとほぼ ArcadeDrive と同じになります。
// =============================================================================

using UnityEngine;

namespace VantanSchoolFestival2026
{
    /// <summary>勢いが残って横に滑る駆動方式。</summary>
    [AddComponentMenu("VantanSchoolFestival2026/駆動方式/ドリフト (DriftDrive)")]
    [ComponentSummary(
        "車の走り方を決める「駆動方式」の 1 つです。曲がっても勢いが残って、ツーッと横に滑ります。",
        "RCCar に 1 つだけ付けます。Grip を小さくすると、氷の上のようによく滑ります。",
        "ArcadeDrive / TankDrive")]
    public class DriftDrive : DriveBase
    {
        [Header("速さ")]
        [Tooltip("前進の最高速度（メートル/秒）。")]
        [Range(1f, 50f)]
        [SerializeField] private float _maxSpeed = 18f;

        [Tooltip("アクセルの強さ（1 秒あたりに増える速度）。")]
        [Range(1f, 100f)]
        [SerializeField] private float _acceleration = 20f;

        [Tooltip("アクセルを離したときに自然に減速する強さ。")]
        [Range(0f, 50f)]
        [SerializeField] private float _drag = 4f;

        [Header("ハンドルとグリップ")]
        [Tooltip("1 秒あたりに曲がれる角度（度）。")]
        [Range(10f, 360f)]
        [SerializeField] private float _turnSpeed = 150f;

        [Tooltip("タイヤのグリップ（横滑りを止める強さ）。小さいほどよく滑ります。")]
        [Range(0f, 20f)]
        [SerializeField] private float _grip = 2.5f;

        /// <summary>DriveBase から物理演算のたびに呼ばれる。</summary>
        protected override void Drive(Vector2 input, float control, float deltaTime)
        {
            SplitVelocity(out Vector3 flat, out Vector3 vertical);

            // --- 1. 速度を「前向き」と「横向き」に分ける -------------------------
            // Dot … 2 つのベクトルがどれだけ同じ向きか（＝前向きの速さ）
            float forwardSpeed = Vector3.Dot(flat, Forward);
            Vector3 sideways = flat - Forward * forwardSpeed;

            // --- 2. 前向きの速さをアクセルで変える -------------------------------
            float maxSpeed = _maxSpeed * Car.SpeedMultiplier;
            if (Mathf.Approximately(input.y, 0f))
            {
                // アクセルを離している → 少しずつ 0 に近づける
                forwardSpeed = Mathf.MoveTowards(forwardSpeed, 0f, _drag * control * deltaTime);
            }
            else
            {
                forwardSpeed += input.y * _acceleration * control * deltaTime;
                // Clamp … 値を「最小 ～ 最大」の範囲に収める（バックは半分の速さまで）
                forwardSpeed = Mathf.Clamp(forwardSpeed, -maxSpeed * 0.5f, maxSpeed);
            }

            // --- 3. 横向きの速度をグリップで少しずつ消す --------------------------
            // ここが「滑る」ポイント。一気に 0 にせず、ゆっくり減らす
            sideways = Vector3.Lerp(sideways, Vector3.zero, _grip * control * deltaTime);

            Body.linearVelocity = Forward * forwardSpeed + sideways + vertical;

            // --- 4. 曲がる ------------------------------------------------------
            float speedFactor = Mathf.InverseLerp(0f, 3f, Mathf.Abs(forwardSpeed));
            float direction = forwardSpeed >= 0f ? 1f : -1f;
            Turn(input.x * _turnSpeed * speedFactor * direction * control * deltaTime);
        }
    }
}
