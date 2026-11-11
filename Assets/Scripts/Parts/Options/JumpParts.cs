// =============================================================================
//  JumpParts.cs
// -----------------------------------------------------------------------------
//  【追加パーツ】ジャンプ
//    付けると、Space キー（ゲームパッドは A / ×）でジャンプできるようになります。
//    外すとジャンプできなくなります。コードを変えずに「機能を足し引き」できる例です。
//
//  【しくみ】
//    RCCar の JumpRequested イベントに「自分の Jump メソッド」を登録しています。
//    ジャンプボタンが押されると、入力側が RCCar.RequestJump() を呼び、
//    RCCar が JumpRequested を発生させ、それを受けて Jump が動きます。
//    （登録 += は OnEnable、解除 -= は OnDisable で行うのが定番の書き方）
// =============================================================================

using UnityEngine;

namespace VantanSchoolFestival2026
{
    /// <summary>ボタンでジャンプできるようにする追加パーツ。</summary>
    [AddComponentMenu("VantanSchoolFestival2026/追加パーツ/ジャンプ (JumpParts)")]
    [ComponentSummary(
        "ジャンプできるようになる追加パーツです。",
        "RCCar に付けると、Space キーでジャンプできます。Air Jumps を増やすと、空中でもう一度ジャンプできます。",
        "RCCar")]
    [RequireComponent(typeof(RCCar))]
    [DisallowMultipleComponent]
    public class JumpParts : MonoBehaviour
    {
        [Header("ジャンプ")]
        [Tooltip("ジャンプの強さ（上向きに加える速度 メートル/秒）。")]
        [Range(1f, 20f)]
        [SerializeField] private float _jumpPower = 6f;

        [Tooltip("空中で追加ジャンプできる回数。0 なら地面にいるときだけジャンプできます。")]
        [Range(0, 5)]
        [SerializeField] private int _airJumps = 0;

        private RCCar _car;
        private int _airJumpsLeft;   // 残りの空中ジャンプ回数

        private void Awake()
        {
            _car = GetComponent<RCCar>();
        }

        /// <summary>OnEnable … コンポーネントが有効になったときに呼ばれる。</summary>
        private void OnEnable()
        {
            // 車に「ジャンプ」の命令が来たら、Jump を呼んでもらうよう登録
            _car.JumpRequested += Jump;
        }

        /// <summary>OnDisable … コンポーネントが無効になったとき（外したときも）に呼ばれる。</summary>
        private void OnDisable()
        {
            // 登録を解除（忘れると、外したあともジャンプしてしまう）
            _car.JumpRequested -= Jump;
        }

        private void FixedUpdate()
        {
            // 地面にいる間は空中ジャンプの回数を回復
            if (_car.IsGrounded) _airJumpsLeft = _airJumps;
        }

        /// <summary>ジャンプする。</summary>
        private void Jump()
        {
            if (!_car.CanDrive) return;

            if (_car.IsGrounded)
            {
                // 地面からのジャンプ
            }
            else if (_airJumpsLeft > 0)
            {
                // 空中ジャンプ（回数を 1 減らす）
                _airJumpsLeft--;
            }
            else
            {
                // ジャンプできない
                return;
            }

            // いまの上下の速度を消してから、上向きの速度を加える
            // （落ちている途中でもしっかり飛べるように）
            Vector3 v = _car.Body.linearVelocity;
            v.y = 0f;
            _car.Body.linearVelocity = v;

            // ForceMode.VelocityChange … 重さに関係なく「速度」を直接足す
            _car.Body.AddForce(Vector3.up * _jumpPower, ForceMode.VelocityChange);
        }
    }
}
