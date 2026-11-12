// =============================================================================
//  DriveBase.cs
// -----------------------------------------------------------------------------
//  【役割】
//    「駆動方式」部品の共通の親クラス（設計図のひな形）。
//    ArcadeDrive / DriftDrive / TankDrive はすべてこのクラスを「継承」しています。
//
//  【継承(けいしょう)とは？】
//    親クラスに共通の処理を書いておき、子クラスでは「違うところだけ」を書くしくみです。
//
//        DriveBase（親）… 車本体の取得、運転禁止中は入力を 0 にする、などの共通処理
//          ├ ArcadeDrive（子）… 素直に曲がる走り方
//          ├ DriftDrive （子）… 滑る走り方
//          └ TankDrive  （子）… その場で回転できる走り方
//
//    abstract（抽象）クラスは、それ単体では使えません（Add Component に出てきません）。
//    必ず子クラスを作って、Drive() メソッドの中身を書く必要があります。
//
//  【自分で新しい駆動方式を作るには】
//    docs/advanced/02_script.md（上級 2. スクリプトを書こう）を見てください。
//    Unity のメニュー VantanSchoolFestival2026 → マニュアル・教材 からも読めます。
// =============================================================================

using UnityEngine;

namespace VantanSchoolFestival2026
{
    /// <summary>
    /// 駆動方式の親クラス。子クラスは Drive() に「どう走るか」だけを書けばよい。
    /// </summary>
    [RequireComponent(typeof(RCCar))]
    // 駆動方式は 1 台に 1 つだけ。DriveBase を継承したクラスにもこのルールが引き継がれる
    [DisallowMultipleComponent]
    public abstract class DriveBase : MonoBehaviour
    {
        [Header("共通設定")]
        [Tooltip("空中にいるときに、どのくらい操作が効くか。0 = まったく効かない / 1 = 地面と同じ。")]
        [Range(0f, 1f)]
        [SerializeField] private float _airControl = 0.2f;

        /// <summary>この部品が付いている車本体。</summary>
        protected RCCar Car { get; private set; }

        /// <summary>車の Rigidbody（物理演算）。</summary>
        protected Rigidbody Body => Car.Body;

        /// <summary>
        /// 前方向の速さ（メートル/秒）。前進ならプラス、バックならマイナス。
        /// UI 表示などに使える。
        /// </summary>
        public float ForwardSpeed => Vector3.Dot(Body.linearVelocity, Forward);

        /// <summary>
        /// 地面に沿った「前」の向き。車が段差で少し上を向いても、地面と平行な向きのまま。
        /// （車の前の向きをそのまま使うと、上を向いたときに車を空へ押し出してしまうため）
        /// </summary>
        protected Vector3 Forward => Vector3.ProjectOnPlane(transform.forward, Car.GroundNormal).normalized;

        /// <summary>
        /// virtual … 子クラスで「上書き(override)」してもよいメソッド。
        /// 子クラスで Awake を書くときは base.Awake() を必ず呼ぶこと。
        /// </summary>
        protected virtual void Awake()
        {
            Car = GetComponent<RCCar>();
        }

        /// <summary>物理演算のタイミングで、入力を整えてから Drive() を呼ぶ。</summary>
        private void FixedUpdate()
        {
            // 運転禁止中（カウントダウン中・ゴール後）は入力を 0 として扱う
            Vector2 input = Car.CanDrive ? Car.DriveInput : Vector2.zero;

            // 空中なら操作の効きを弱くする
            float control = Car.IsGrounded ? 1f : _airControl;

            // 子クラスに「どう走るか」を任せる
            Drive(input, control, Time.fixedDeltaTime);
        }

        /// <summary>
        /// 【子クラスで必ず書く】実際に車を動かす処理。
        /// </summary>
        /// <param name="input">x = ハンドル(左右), y = アクセル(前後)。-1 ～ 1。</param>
        /// <param name="control">操作の効き具合。地面なら 1、空中なら airControl。</param>
        /// <param name="deltaTime">前回からの経過時間（秒）。</param>
        protected abstract void Drive(Vector2 input, float control, float deltaTime);

        // ---------------------------------------------------------------------
        // 子クラスで使える便利メソッド
        // ---------------------------------------------------------------------

        /// <summary>
        /// 車の速度を「地面に沿った成分」と「上下の成分」に分ける。
        /// 地面に沿った成分だけをいじれば、ジャンプや落下には影響しない。
        /// </summary>
        protected void SplitVelocity(out Vector3 flat, out Vector3 vertical)
        {
            Vector3 v = Body.linearVelocity;
            // ProjectOnPlane … ベクトルを「面」に押しつぶす（地面に垂直な成分を取り除く）
            flat = Vector3.ProjectOnPlane(v, Car.GroundNormal);
            vertical = v - flat;
        }

        /// <summary>車を左右に曲げる。</summary>
        /// <param name="degrees">今回（1 回の物理演算）で曲がる角度（度）。プラスで右回り。</param>
        /// <remarks>
        /// 車の向きを直接書き換えると、段差にぶつかったときの物理の回転とぶつかり合ってしまう。
        /// そこで「1 秒あたり何度曲がるか（回転の速さ）」に直して RCCar に伝え、
        /// RCCar が物理演算の回転の速さとして設定する。
        /// </remarks>
        protected void Turn(float degrees)
        {
            Car.SetTurnRate(degrees / Time.fixedDeltaTime);
        }
    }
}
