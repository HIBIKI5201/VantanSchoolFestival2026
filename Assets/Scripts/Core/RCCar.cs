// =============================================================================
//  RCCar.cs
// -----------------------------------------------------------------------------
//  【役割】
//    ラジコンカーの「本体（土台）」。
//    このスクリプト自体は車を動かしません！
//    動かすのは、別に付ける「駆動方式」の部品（ArcadeDrive など）です。
//
//  【部品の考え方 ― コンポーネント指向】
//      RCCar (本体)        … 地面に触れているか・リスポーン位置・速度倍率などを管理
//      ○○Drive (駆動方式)  … どうやって走るか（1つだけ付ける）
//      ○○Parts (追加パーツ)… ジャンプ・ブーストなど（好きなだけ付ける）
//
//    Inspector で駆動方式を付け替えるだけで、走り方がガラッと変わります。
//    これが Unity の「コンポーネントを組み合わせて作る」考え方です。
//
//  【車と入力を分けている理由】
//    車は「キーボードで操作されている」ことを知りません。
//    外から次のメソッドで「命令」を受け取るだけです。
//
//        SetDriveInput(移動量)   … アクセルとハンドル
//        SetBoost(押しているか)   … ブースト
//        RequestJump()           … ジャンプ
//        RequestRespawn()        … やり直し
//
//    プレイヤーの操作は別のオブジェクト「PlayerController」の
//    PlayerCarInput がこれらを呼んでいます。
//    こうしておくと、同じ車を「AI」や「リプレイ」で動かすこともできます。
//
//  【ほかのスクリプトからの使い方】
//      RCCar car = other.attachedRigidbody.GetComponent<RCCar>();
//      car.Respawn();          // 最後のチェックポイントへ戻す
// =============================================================================

using System;
using UnityEngine;

namespace VantanSchoolFestival2026
{
    /// <summary>
    /// ラジコンカー本体。駆動方式や追加パーツが共通で使う情報をまとめて持つ。
    /// </summary>
    [AddComponentMenu("VantanSchoolFestival2026/Core/RCカー本体 (RCCar)")]
    [ComponentSummary(
        "ラジコンカーの本体です。これだけでは走りません。",
        "「駆動方式」の部品を 1 つ付けると走れるようになります。ジャンプなどの「追加パーツ」は、いくつでも付けられます。",
        "ArcadeDrive / DriftDrive / TankDrive / JumpParts / BoostParts")]
    [RequireComponent(typeof(Rigidbody))]
    [DisallowMultipleComponent]
    public class RCCar : MonoBehaviour
    {
        // ---------------------------------------------------------------------
        // Inspector に表示する設定
        // ---------------------------------------------------------------------

        [Header("接地判定（地面に触れているか）")]
        [Tooltip("車の中心から下に向かって、どのくらいの距離まで地面を探すか（メートル）。車を大きくしたら増やしてください。")]
        [Range(0.1f, 3f)]
        [SerializeField] private float _groundCheckDistance = 0.6f;

        [Tooltip("地面として扱うレイヤー。通常は Everything のままで OK です。")]
        [SerializeField] private LayerMask _groundLayers = ~0;   // ~0 は「すべてのレイヤー」という意味

        [Header("安定性")]
        [Tooltip("車の重さの中心の位置。Y をマイナスにするほど転びにくくなります（タイヤの下まで下げると、ほとんど転ばなくなります）。")]
        [SerializeField] private Vector3 _centerOfMass = new Vector3(0f, -0.5f, 0f);

        [Tooltip("車をまっすぐ（地面に沿った向き）に保とうとする強さ。大きいほど、段差やジャンプで傾いてもすぐ戻ります。0 にすると戻しません。")]
        [Range(0f, 30f)]
        [SerializeField] private float _uprightStrength = 12f;

        [Header("リスポーン")]
        [Tooltip("リスポーン時、目標地点からどれだけ上に出現させるか（メートル）。床にめり込むのを防ぎます。")]
        [Range(0f, 3f)]
        [SerializeField] private float _respawnHeight = 0.5f;

        // ---------------------------------------------------------------------
        // 外から受け取る「命令」（入力）
        // ---------------------------------------------------------------------

        /// <summary>移動の命令。x = ハンドル(左右), y = アクセル(前後)。-1 ～ 1。</summary>
        public Vector2 DriveInput { get; private set; }

        /// <summary>ブーストの命令が出ている間 true。</summary>
        public bool BoostRequested { get; private set; }

        /// <summary>ジャンプの命令が来たときに呼ばれるイベント（JumpParts が受け取る）。</summary>
        public event Action JumpRequested;

        /// <summary>やり直しの命令が来たときに呼ばれるイベント（GameManager が受け取る）。</summary>
        public event Action RespawnRequested;

        // ---------------------------------------------------------------------
        // ほかのスクリプトから読む値（プロパティ）
        // ---------------------------------------------------------------------

        /// <summary>物理演算をしてくれる Rigidbody。</summary>
        public Rigidbody Body { get; private set; }

        /// <summary>地面に触れているなら true。FixedUpdate ごとに更新される。</summary>
        public bool IsGrounded { get; private set; }

        /// <summary>
        /// 車の真下の地面の向き（地面に垂直な向き）。空中では真上。
        /// 駆動方式は、この向きに垂直な方向（＝地面に沿った方向）に車を押す。
        /// </summary>
        public Vector3 GroundNormal { get; private set; } = Vector3.up;

        /// <summary>
        /// 乗り越えられる段差の高さの目安（メートル）。ステージチェッカーの表示に使う。
        /// タイヤ（丸い当たり判定）の半径の半分くらいまでなら、転がるように乗り越えられる。
        /// </summary>
        public float MaxStepHeight
        {
            get
            {
                float radius = 0f;
                foreach (SphereCollider wheel in GetComponentsInChildren<SphereCollider>())
                {
                    // 大きさ（スケール）も考えた、本当の半径
                    Vector3 s = wheel.transform.lossyScale;
                    radius = Mathf.Max(radius, wheel.radius * Mathf.Max(s.x, Mathf.Max(s.y, s.z)));
                }
                return radius > 0f ? radius * 0.5f : 0.05f;
            }
        }

        /// <summary>
        /// 運転してよいか。GameManager がカウントダウン中やゴール後に false にする。
        /// false の間、駆動方式の部品は入力を無視する。
        /// </summary>
        public bool CanDrive { get; set; } = true;

        /// <summary>ブーストパーツが設定する速度倍率（1 = 通常）。</summary>
        public float BoostMultiplier { get; set; } = 1f;

        /// <summary>
        /// 最終的な速度倍率。駆動方式はこの値を最高速度に掛け算する。
        /// （ブースト × 加速床）
        /// </summary>
        public float SpeedMultiplier
        {
            get
            {
                // 加速床の効果時間が残っていれば倍率を掛ける
                float pad = Time.time < _speedUpEndTime ? _speedUpMultiplier : 1f;
                return BoostMultiplier * pad;
            }
        }

        // ---------------------------------------------------------------------
        // 内部で使う変数
        // ---------------------------------------------------------------------

        // リスポーンする位置と向き（スタート地点やチェックポイントで更新される）
        private Vector3 _respawnPosition;
        private Quaternion _respawnRotation;

        // 加速床による一時的な速度アップ
        private float _speedUpMultiplier = 1f;
        private float _speedUpEndTime = 0f;

        // 駆動方式から伝えられた「曲がる速さ」（度 / 秒）
        private float _turnRate;

        // ---------------------------------------------------------------------
        // Unity のイベント
        // ---------------------------------------------------------------------

        /// <summary>
        /// Awake … シーン開始時、ほかのどの処理よりも先に 1 回だけ呼ばれる。
        /// 自分自身の準備（コンポーネントの取得など）はここで行う。
        /// </summary>
        private void Awake()
        {
            Body = GetComponent<Rigidbody>();

            // 重心を下げて転びにくくする
            Body.centerOfMass = _centerOfMass;

            // すり抜け防止：速い物体でも当たり判定を正確にする
            Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            // 補間をONにすると、カメラで追いかけたときにカクカクしにくい
            Body.interpolation = RigidbodyInterpolation.Interpolate;

            // 最初は「今いる場所」をリスポーン地点にしておく
            SetRespawnPoint(transform.position, transform.rotation);
        }

        /// <summary>
        /// FixedUpdate … 物理演算のタイミングで一定間隔(標準 0.02 秒)ごとに呼ばれる。
        /// Rigidbody を扱う処理は Update ではなくここに書くのが基本。
        /// </summary>
        private void FixedUpdate()
        {
            // 車の中心から真下に「見えない光線(Ray)」を飛ばして、地面に当たるか調べる
            IsGrounded = Physics.Raycast(
                transform.position,             // 光線のスタート地点
                -transform.up,                  // 方向（車から見て下）
                out RaycastHit groundHit,       // 当たった場所の情報がここに入る
                _groundCheckDistance,           // 長さ
                _groundLayers,                  // 対象レイヤー
                QueryTriggerInteraction.Ignore  // トリガー(ゴールなど)は地面扱いしない
            );
            GroundNormal = IsGrounded ? groundHit.normal : Vector3.up;

            // 曲がる速さを、物理演算の回転に反映する（ぶつかって勝手に回る動きもここで消える）
            ApplyTurn();

            // 傾きすぎないように戻す（地面では地面の傾きに、空中では水平に合わせる）
            if (_uprightStrength > 0f) KeepUpright(IsGrounded ? GroundNormal : Vector3.up);
        }

        /// <summary>
        /// 車の上の向きを targetUp に近づける。前後・左右に傾く勢いも弱めて、
        /// 段差やジャンプのあとにぐらぐら揺れ続けたり、ひっくり返ったりしないようにする。
        /// </summary>
        /// <param name="targetUp">目標の上の向き（地面の上なら地面の向き、空中なら真上）</param>
        private void KeepUpright(Vector3 targetUp)
        {
            Vector3 up = transform.up;

            // Cross … 「今の上の向き」から「目標の上の向き」へ回すための回転の軸を求める
            Vector3 toUpright = Vector3.Cross(up, targetUp);
            Body.AddTorque(toUpright * _uprightStrength, ForceMode.Acceleration);

            // 前後・左右に傾く勢いだけを弱める（曲がる向きの回転は駆動方式に任せる）
            Vector3 angular = Body.angularVelocity;
            Vector3 yaw = up * Vector3.Dot(angular, up);
            Vector3 tilt = angular - yaw;
            Body.angularVelocity = yaw + tilt * (1f - Mathf.Clamp01(_uprightStrength * 0.3f * Time.fixedDeltaTime));
        }

        /// <summary>
        /// 車の「上の向き」を軸にした回転（左右に曲がる動き）を、駆動方式が決めた速さにする。
        /// </summary>
        /// <remarks>
        /// 段差や壁にぶつかると、車に「くるくる回る勢い」が残ることがある。
        /// 上の向きを軸にした回転は、駆動方式が決めた速さだけにするので、
        /// ぶつかった後に勝手に向きが変わったり、回り続けたりしなくなる。
        /// 前後・左右に傾く回転は物理演算に任せるので、坂や段差では自然に傾く。
        /// </remarks>
        private void ApplyTurn()
        {
            Vector3 up = transform.up;
            Vector3 angular = Body.angularVelocity;

            // 今の回転の勢いから「上の向きを軸にした成分」を取り除いて、決めた速さに置きかえる
            Vector3 tilt = angular - up * Vector3.Dot(angular, up);
            Body.angularVelocity = tilt + up * (_turnRate * Mathf.Deg2Rad);

            // 次の物理演算で駆動方式がまた伝えるまで 0 にしておく（駆動方式が無ければ曲がらない）
            _turnRate = 0f;
        }

        // ---------------------------------------------------------------------
        // 命令を受け取るメソッド（入力側から呼ばれる）
        // ---------------------------------------------------------------------

        /// <summary>アクセルとハンドルの量を設定する。</summary>
        /// <param name="input">x = ハンドル(左右), y = アクセル(前後)。-1 ～ 1。</param>
        public void SetDriveInput(Vector2 input)
        {
            // ClampMagnitude … 長さが 1 を超えないようにする（斜め入力で速くなりすぎないように）
            DriveInput = Vector2.ClampMagnitude(input, 1f);
        }

        /// <summary>
        /// 曲がる速さ（度 / 秒）を伝える。プラスで右回り。駆動方式が毎回の物理演算で呼ぶ。
        /// </summary>
        public void SetTurnRate(float degreesPerSecond)
        {
            _turnRate = degreesPerSecond;
        }

        /// <summary>ブーストする / やめる。</summary>
        public void SetBoost(bool boost)
        {
            BoostRequested = boost;
        }

        /// <summary>ジャンプを頼む（JumpParts が付いていなければ何も起きない）。</summary>
        public void RequestJump()
        {
            // ?.Invoke() … 誰も登録していなければ何もしない、という安全な呼び方
            JumpRequested?.Invoke();
        }

        /// <summary>やり直しを頼む（GameManager が受け取って処理する）。</summary>
        public void RequestRespawn()
        {
            if (RespawnRequested != null)
            {
                RespawnRequested.Invoke();
            }
            else
            {
                // GameManager がいないシーンでも、最低限リスポーンはできるようにする
                Respawn();
            }
        }

        // ---------------------------------------------------------------------
        // ほかのスクリプトから呼ぶメソッド
        // ---------------------------------------------------------------------

        /// <summary>リスポーン地点を設定する（スタート地点やチェックポイントから呼ばれる）。</summary>
        public void SetRespawnPoint(Vector3 position, Quaternion rotation)
        {
            _respawnPosition = position;
            _respawnRotation = rotation;
        }

        /// <summary>最後に設定されたリスポーン地点へ車を戻す。</summary>
        public void Respawn()
        {
            // 勢いを止める（止めないと、戻った瞬間に飛んでいってしまう）
            Body.linearVelocity = Vector3.zero;
            Body.angularVelocity = Vector3.zero;

            // 位置と向きを戻す。Rigidbody 付きのオブジェクトは
            // transform ではなく Body.position / rotation を使うと安全
            Vector3 position = _respawnPosition + Vector3.up * _respawnHeight;
            Body.position = position;
            Body.rotation = _respawnRotation;
            transform.SetPositionAndRotation(position, _respawnRotation);

            // 加速床の効果も解除
            _speedUpEndTime = 0f;
        }

        /// <summary>一定時間だけ速度倍率を上げる（加速床から呼ばれる）。</summary>
        /// <param name="multiplier">速度倍率（2 なら 2 倍）</param>
        /// <param name="duration">効果時間（秒）</param>
        public void AddTemporarySpeedUp(float multiplier, float duration)
        {
            _speedUpMultiplier = multiplier;
            _speedUpEndTime = Time.time + duration;
        }

        // ---------------------------------------------------------------------
        // エディタ専用：Scene ビューに補助線を描く
        // ---------------------------------------------------------------------

        /// <summary>
        /// OnDrawGizmosSelected … オブジェクトを選択しているとき、Scene ビューに図形を描く。
        /// ゲームの見た目には影響しない、開発用の目印。
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            // 接地判定の光線を表示（地面に触れていれば緑、離れていれば赤）
            Gizmos.color = IsGrounded ? Color.green : Color.red;
            Gizmos.DrawLine(transform.position, transform.position - transform.up * _groundCheckDistance);

            // 重心の位置を黄色い球で表示
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(transform.TransformPoint(_centerOfMass), 0.08f);
        }
    }
}
