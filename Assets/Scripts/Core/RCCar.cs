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
        [Tooltip("車の重さの中心の位置。Y をマイナスにするほど転びにくくなります。")]
        [SerializeField] private Vector3 _centerOfMass = new Vector3(0f, -0.3f, 0f);

        [Header("段差を乗り越える")]
        [Tooltip("この高さ（メートル）までの段差なら、ぶつからずに自動で乗り越えます。0 にすると乗り越えません。")]
        [Range(0f, 1f)]
        [SerializeField] private float _maxStepHeight = 0.3f;

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

        /// <summary>自動で乗り越えられる段差の高さ（メートル）。ステージチェッカーの表示にも使う。</summary>
        public float MaxStepHeight => _maxStepHeight;

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

        // 車の当たり判定（段差を調べるときに、車の大きさを知るために使う）
        private BoxCollider _box;

        // 段差の左右の調べる位置（毎回作らないように、ここで用意しておく）
        private static readonly float[] StepProbeSides = { 0f, 0.8f, -0.8f };

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
            _box = GetComponent<BoxCollider>();

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
                _groundCheckDistance,           // 長さ
                _groundLayers,                  // 対象レイヤー
                QueryTriggerInteraction.Ignore  // トリガー(ゴールなど)は地面扱いしない
            );

            // 低い段差があれば乗り越える
            if (_maxStepHeight > 0f) TryStepUp();
        }

        /// <summary>
        /// 進む先の足元に低い段差があれば、車を少し持ち上げて乗り越えさせる。
        /// </summary>
        /// <remarks>
        /// 車の当たり判定は四角い箱なので、数センチの段差でも前の角が引っかかって止まってしまう。
        /// そこで、次の 3 つを光線(Ray)で調べて、全部 OK なら段差の高さぶん車を持ち上げる。
        ///   1. 足元のすぐ前に、何かの面がある（＝段差か壁がある）
        ///   2. 「乗り越えられる高さ」の上には何もない（＝高い壁ではない）
        ///   3. 段差の上に、乗れる面がある
        /// </remarks>
        private void TryStepUp()
        {
            if (_box == null || !IsGrounded || !CanDrive) return;

            Vector3 up = transform.up;

            // 進む向き：走っていればその向き、止まっていればアクセルの向き
            Vector3 flatVelocity = Vector3.ProjectOnPlane(Body.linearVelocity, up);
            Vector3 dir;
            if (flatVelocity.sqrMagnitude > 0.25f) dir = flatVelocity.normalized;
            else if (Mathf.Abs(DriveInput.y) > 0.1f) dir = transform.forward * Mathf.Sign(DriveInput.y);
            else return;

            // 車の箱の大きさ（スケールも考える）と、底の真ん中の位置
            Vector3 half = Vector3.Scale(_box.size * 0.5f, transform.lossyScale);
            Vector3 bottom = transform.TransformPoint(_box.center) - up * half.y;

            // 車の中心から、進む向きの端までの距離 + 少し先まで調べる
            float reach = Mathf.Abs(Vector3.Dot(dir, transform.forward)) * half.z
                        + Mathf.Abs(Vector3.Dot(dir, transform.right)) * half.x;
            float look = reach + 0.25f + flatVelocity.magnitude * Time.fixedDeltaTime;
            Vector3 sideAxis = Vector3.Cross(up, dir).normalized * half.x;

            float bestRise = 0f;
            foreach (float side in StepProbeSides)   // 車の真ん中・右・左の 3 か所で調べる
            {
                Vector3 low = bottom + up * 0.04f + sideAxis * side;

                // 1. 足元のすぐ前に面があるか
                if (!Physics.Raycast(low, dir, out RaycastHit wall, look, _groundLayers, QueryTriggerInteraction.Ignore)) continue;
                // ゆるい坂は段差ではない（坂は駆動方式がそのまま登る）
                if (Vector3.Dot(wall.normal, up) > 0.7f) continue;

                // 2. 乗り越えられる高さの上に、何もないか（あれば高い壁）
                Vector3 high = low + up * _maxStepHeight;
                if (Physics.Raycast(high, dir, look, _groundLayers, QueryTriggerInteraction.Ignore)) continue;

                // 3. 段差の上の面の高さを、上から光線を当てて調べる
                Vector3 above = high + dir * (wall.distance + 0.1f);
                if (!Physics.Raycast(above, -up, out RaycastHit top, _maxStepHeight + 0.05f, _groundLayers, QueryTriggerInteraction.Ignore)) continue;
                if (Vector3.Dot(top.normal, up) < 0.7f) continue;   // 上の面が急すぎる

                float rise = Vector3.Dot(top.point - bottom, up);
                if (rise > 0.005f && rise <= _maxStepHeight) bestRise = Mathf.Max(bestRise, rise);
            }

            if (bestRise <= 0f) return;

            // 段差の高さぶん持ち上げる（少しだけ余分に上げて、角に引っかからないようにする）
            Body.position += up * (bestRise + 0.02f);
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
