// =============================================================================
//  OutOfBoundsJudge.cs
// -----------------------------------------------------------------------------
//  【ステージ部品】場外判定係
//    車がコースエリア（CourseArea）の外に出ていないかを見張ります。
//    どのコースエリアにも入っていない時間が「猶予時間」を超えたら、
//    最後のチェックポイント（またはスタート地点）へ車を戻します。
//
//  【猶予時間がある理由】
//    ジャンプ中や、エリアの境目をまたいだ一瞬だけ外に出ることがあります。
//    すぐに戻してしまうとイライラするので、少し待ってから判定しています。
//
//  【シーンに CourseArea が 1 つも無いとき】
//    「ずっと場外」になってしまうので、このコンポーネントは自動で休みます。
//    （練習用シーンなどで誤動作しないように）
// =============================================================================

using System.Collections.Generic;
using UnityEngine;

namespace VantanSchoolFestival2026
{
    /// <summary>車がコースエリアの外に出たら、少し待ってからリスポーンさせる。</summary>
    [AddComponentMenu("VantanSchoolFestival2026/ステージ/場外判定 (OutOfBoundsJudge)")]
    [ComponentSummary(
        "コースアウトを見張る係です。車が走ってよい範囲（CourseArea）の外に出たまま少したつと、最後のチェックポイントに戻します。",
        "シーンに 1 つだけ置きます。Grace Time を長くすると、戻されるまでの時間が長くなります。",
        "CourseArea")]
    [DisallowMultipleComponent]
    public class OutOfBoundsJudge : MonoBehaviour
    {
        /// <summary>シーン内の場外判定係。無ければ null。</summary>
        public static OutOfBoundsJudge Instance { get; private set; }

        [Header("判定")]
        [Tooltip("コースエリアの外にいられる時間（秒）。これを超えるとリスポーンします。ジャンプの滞空時間より長くしてください。")]
        [Range(0.5f, 10f)]
        [SerializeField] private float _graceTime = 2.5f;

        [Header("参照（空なら自動で探します）")]
        [Tooltip("見張る車。空のままなら、シーンにある RCCar を自動で探します。")]
        [SerializeField] private RCCar _car;

        [Header("デバッグ")]
        [Tooltip("ONにすると、コースアウトでリスポーンしたときに Console にログを出します。")]
        [SerializeField] private bool _logRespawn = true;

        // 今、車が入っているコースエリアの一覧（重なった場所では 2 つ以上になる）
        private readonly HashSet<CourseArea> _inside = new HashSet<CourseArea>();

        // コースエリアの外にいる時間（秒）
        private float _outTime;

        // シーンにコースエリアがあるか（無ければ判定しない）
        private bool _hasAreas;

        /// <summary>今コースエリアの外にいる時間（秒）。中にいれば 0。</summary>
        public float OutTime => _outTime;

        /// <summary>リスポーンまでの残り時間（秒）。</summary>
        public float TimeLeft => Mathf.Max(0f, _graceTime - _outTime);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[OutOfBoundsJudge] 場外判定がシーンに 2 つ以上あります。1 つにしてください。", this);
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            if (_car == null) _car = FindFirstObjectByType<RCCar>();

            _hasAreas = FindObjectsByType<CourseArea>(FindObjectsSortMode.None).Length > 0;
            if (!_hasAreas)
            {
                Debug.LogWarning("[OutOfBoundsJudge] CourseArea が 1 つもないので、場外判定はお休みします。", this);
            }
        }

        private void Update()
        {
            if (_car == null || !_hasAreas) return;

            // カウントダウン中やゴール後は判定しない
            if (GameManager.Instance != null && GameManager.Instance.State != GameState.Playing)
            {
                _outTime = 0f;
                return;
            }

            // 消えたコースエリアが一覧に残らないように掃除する
            _inside.RemoveWhere(area => area == null);

            if (_inside.Count > 0)
            {
                _outTime = 0f;   // 中にいる
                return;
            }

            _outTime += Time.deltaTime;
            if (_outTime >= _graceTime)
            {
                if (_logRespawn) Debug.Log("[OutOfBoundsJudge] コースアウトしたのでリスポーンします", this);
                _car.Respawn();
                _outTime = 0f;
            }
        }

        /// <summary>車がコースエリアに入った（CourseArea から呼ばれる）。</summary>
        public void EnterArea(CourseArea area)
        {
            _inside.Add(area);
        }

        /// <summary>車がコースエリアから出た（CourseArea から呼ばれる）。</summary>
        public void ExitArea(CourseArea area)
        {
            _inside.Remove(area);
        }
    }
}
