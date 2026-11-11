// =============================================================================
//  BoostParts.cs
// -----------------------------------------------------------------------------
//  【追加パーツ】ブースト（加速装置）
//    付けると、左 Shift キー（ゲームパッドは RT）を押している間スピードアップします。
//    使うとエネルギーが減り、離すと少しずつ回復します。
//
//  【ほかの部品との関係】
//    このパーツは車を直接動かしません。
//    RCCar.BoostMultiplier（速度倍率）を書き換えるだけで、
//    駆動方式の部品がその倍率を読んで速く走ります。
//    → 駆動方式を付け替えても、ブーストはそのまま使えます！
// =============================================================================

using UnityEngine;

namespace VantanSchoolFestival2026
{
    /// <summary>ボタンを押している間スピードアップする追加パーツ。</summary>
    [AddComponentMenu("VantanSchoolFestival2026/追加パーツ/ブースト (BoostParts)")]
    [ComponentSummary(
        "ボタンを押している間スピードアップする追加パーツです。使うとエネルギーが減り、離すと回復します。",
        "RCCar に付けると、ブーストボタン（左 Shift など）で加速できます。画面にゲージが表示されます。",
        "RCCar（BoostMultiplier）/ HudPresenter")]
    [RequireComponent(typeof(RCCar))]
    [DisallowMultipleComponent]
    public class BoostParts : MonoBehaviour
    {
        [Header("ブースト")]
        [Tooltip("ブースト中の速度倍率。1.5 なら 1.5 倍の速さになります。")]
        [Range(1f, 3f)]
        [SerializeField] private float _speedMultiplier = 1.6f;

        [Header("エネルギー")]
        [Tooltip("エネルギーの最大量。満タンからこの秒数だけブーストできます。")]
        [Range(0.5f, 10f)]
        [SerializeField] private float _maxEnergy = 2f;

        [Tooltip("1 秒あたりに回復するエネルギー量。")]
        [Range(0f, 5f)]
        [SerializeField] private float _rechargeRate = 0.5f;

        private RCCar _car;
        private float _energy;

        /// <summary>エネルギー残量を 0 ～ 1 で表したもの（UI のゲージ用）。</summary>
        public float Energy01 => _maxEnergy > 0f ? _energy / _maxEnergy : 0f;

        /// <summary>今ブースト中か。</summary>
        public bool IsBoosting { get; private set; }

        private void Awake()
        {
            _car = GetComponent<RCCar>();
            _energy = _maxEnergy;   // 最初は満タン
        }

        private void Update()
        {
            // ブーストできる条件：ボタンを押している && 運転できる && エネルギーが残っている
            IsBoosting = _car.BoostRequested && _car.CanDrive && _energy > 0f;

            if (IsBoosting)
            {
                _energy -= Time.deltaTime;
            }
            else
            {
                _energy += _rechargeRate * Time.deltaTime;
            }

            // エネルギーを 0 ～ 最大値 に収める
            _energy = Mathf.Clamp(_energy, 0f, _maxEnergy);

            // 車本体に速度倍率を伝える
            _car.BoostMultiplier = IsBoosting ? _speedMultiplier : 1f;
        }

        private void OnDisable()
        {
            // パーツを外したら倍率を元に戻す（戻さないと速いままになる）
            if (_car != null) _car.BoostMultiplier = 1f;
            IsBoosting = false;
        }
    }
}
