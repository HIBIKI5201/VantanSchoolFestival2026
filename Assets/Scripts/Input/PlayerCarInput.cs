// =============================================================================
//  PlayerCarInput.cs
// -----------------------------------------------------------------------------
//  【役割】
//    プレイヤーの「入力」を受け取って、車(RCCar)へ「命令」として伝える。
//    車とは別の GameObject（PlayerController）に付けて使います。
//
//        [PlayerController]                      [RCCar]
//          PlayerInput  ──OnMove など──▶ PlayerCarInput ──SetDriveInput など──▶ RCCar
//
//  【しくみ（大事！）】
//    同じ GameObject に付いている「PlayerInput」コンポーネントが、
//    キーボードやゲームパッドの操作を監視しています。
//    PlayerInput の Behavior を「Send Messages」にしておくと、
//    アクションが動いたときに「On + アクション名」という名前のメソッドを
//    自動で呼んでくれます。
//
//        アクション "Move"    → OnMove(InputValue value)
//        アクション "Jump"    → OnJump(InputValue value)
//        アクション "Boost"   → OnBoost(InputValue value)
//        アクション "Respawn" → OnRespawn(InputValue value)
//
//    アクションの一覧とキー割り当ては
//      Assets/Settings/RCInputActions.inputactions
//    をダブルクリックすると確認・変更できます。
//
//  【入力と車を分けるとうれしいこと】
//    ・Target Car を別の車に差し替えるだけで、操作する車を変えられる
//    ・PlayerCarInput の代わりに「AI が命令を出すスクリプト」を作れば自動運転になる
// =============================================================================

using UnityEngine;
using UnityEngine.InputSystem;

namespace VantanSchoolFestival2026
{
    /// <summary>
    /// PlayerInput から届く入力を、RCCar への命令に変換するコンポーネント。
    /// </summary>
    // AddComponentMenu … Inspector の「Add Component」ボタンで表示される場所を決める
    [AddComponentMenu("VantanSchoolFestival2026/入力/プレイヤー入力 (PlayerCarInput)")]
    [ComponentSummary(
        "キーボードやゲームパッドの操作を受け取って、車を動かす係です。",
        "PlayerController に付いています。Target Car に動かしたい車を入れます。空のままなら、シーンにある車を自動で見つけます。",
        "PlayerInput / RCCar")]
    // RequireComponent … このコンポーネントを付けると PlayerInput も自動で付く
    [RequireComponent(typeof(PlayerInput))]
    // DisallowMultipleComponent … 同じ GameObject に 2 つ付けられないようにする
    [DisallowMultipleComponent]
    public class PlayerCarInput : MonoBehaviour
    {
        // ---------------------------------------------------------------------
        // Inspector に表示する設定
        // ---------------------------------------------------------------------

        [Header("操作する車")]
        [Tooltip("操作する車。空のままなら、シーンにある RCCar を自動で探します。")]
        [SerializeField] private RCCar _targetCar;

        [Header("デバッグ")]
        [Tooltip("ONにすると、ボタンが押されるたびに Console にログを出します。入力が届いているか確かめたいときに使います。")]
        [SerializeField] private bool _logInput = false;

        /// <summary>今操作している車。</summary>
        public RCCar TargetCar => _targetCar;

        // ---------------------------------------------------------------------
        // Unity のイベント
        // ---------------------------------------------------------------------

        private void Awake()
        {
            // 車が指定されていなければシーンから探す
            // FindFirstObjectByType … シーンから指定した型のコンポーネントを 1 つ探す
            if (_targetCar == null) _targetCar = FindFirstObjectByType<RCCar>();

            if (_targetCar == null)
            {
                Debug.LogWarning("[PlayerCarInput] 操作する RCCar が見つかりません。", this);
            }
        }

        /// <summary>
        /// コンポーネントが無効になったとき、命令を止める。
        /// （押しっぱなしのまま無効にすると、車が走り続けてしまうのを防ぐため）
        /// </summary>
        private void OnDisable()
        {
            if (_targetCar == null) return;
            _targetCar.SetDriveInput(Vector2.zero);
            _targetCar.SetBoost(false);
        }

        // ---------------------------------------------------------------------
        // PlayerInput (Send Messages) から自動で呼ばれるメソッド
        //   ※ 名前を変えると呼ばれなくなるので注意！
        // ---------------------------------------------------------------------

        /// <summary>Move アクションの値が変わったときに呼ばれる。</summary>
        private void OnMove(InputValue value)
        {
            if (_targetCar == null) return;
            // Vector2 として値を取り出して車に渡す
            _targetCar.SetDriveInput(value.Get<Vector2>());
        }

        /// <summary>Jump アクションが押されたときに呼ばれる。</summary>
        private void OnJump(InputValue value)
        {
            // isPressed … ボタンが押されている状態なら true
            if (!value.isPressed || _targetCar == null) return;

            Log("Jump");
            _targetCar.RequestJump();
        }

        /// <summary>
        /// Boost アクションが押された / 離されたときに呼ばれる。
        /// </summary>
        /// <remarks>
        /// ふつうのボタンは「押した瞬間」しか呼ばれないので、離したことが分からない。
        /// そこで RCInputActions の Boost には Interactions に「Press（Press And Release）」を付けて、
        /// 離したときにも呼ばれるようにしている（value.isPressed が false で届く）。
        /// </remarks>
        private void OnBoost(InputValue value)
        {
            if (_targetCar == null) return;

            Log("Boost = " + value.isPressed);
            _targetCar.SetBoost(value.isPressed);
        }

        /// <summary>Respawn アクションが押されたときに呼ばれる。</summary>
        private void OnRespawn(InputValue value)
        {
            if (!value.isPressed || _targetCar == null) return;

            Log("Respawn");
            _targetCar.RequestRespawn();
        }

        /// <summary>デバッグ用のログ出力。_logInput が ON のときだけ出す。</summary>
        private void Log(string message)
        {
            if (_logInput)
            {
                // 第2引数に this を渡すと、Console のログをクリックしたとき
                // このオブジェクトが Hierarchy で選択される
                Debug.Log("[PlayerCarInput] " + message, this);
            }
        }
    }
}
