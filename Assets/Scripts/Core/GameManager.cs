// =============================================================================
//  GameManager.cs
// -----------------------------------------------------------------------------
//  【役割】
//    ゲーム全体の進行を管理する「司会者」。
//      1. 開始時、プレイヤーをスタート地点に置く
//      2. カウントダウン（3, 2, 1, GO!）
//      3. 走行中はタイムを計る
//      4. ゴールしたらタイムを確定し、リスポーンボタンでやり直し
//
//  【状態(State)の流れ】
//      Ready(準備) ──カウントダウン終了──▶ Playing(走行中) ──ゴール──▶ Goal(ゴール)
//                                                                      │
//                                         リスポーンボタンでシーン再読込 ◀┘
//
//  【シングルトン】
//    GameManager はシーンに 1 つだけ置きます。
//    どこからでも GameManager.Instance で呼び出せるようにしてあります。
//      例） GameManager.Instance.ReachGoal();
// =============================================================================

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VantanSchoolFestival2026
{
    /// <summary>ゲームの状態。</summary>
    public enum GameState
    {
        /// <summary>カウントダウン中。まだ走れない。</summary>
        Ready,
        /// <summary>走行中。タイム計測中。</summary>
        Playing,
        /// <summary>ゴールした。</summary>
        Goal,
    }

    /// <summary>
    /// ゲームの進行（スタート・タイム計測・ゴール・やり直し）を管理する。
    /// </summary>
    [AddComponentMenu("VantanSchoolFestival2026/Core/ゲーム進行 (GameManager)")]
    [ComponentSummary(
        "ゲームの進行係です。スタートの合図（3, 2, 1, GO!）、タイム計測、ゴールの判定をします。",
        "シーンに 1 つだけ置きます。Require All Checkpoints に✓を入れると、チェックポイントを全部通らないとゴールできなくなります。",
        "StartPoint / Checkpoint / Goal")]
    [DisallowMultipleComponent]
    public class GameManager : MonoBehaviour
    {
        // ---------------------------------------------------------------------
        // シングルトン（どこからでも GameManager.Instance で使えるようにする）
        // ---------------------------------------------------------------------

        /// <summary>シーン内の GameManager。無ければ null。</summary>
        public static GameManager Instance { get; private set; }

        // ---------------------------------------------------------------------
        // Inspector に表示する設定
        // ---------------------------------------------------------------------

        [Header("参照（空なら自動で探します）")]
        [Tooltip("操作するラジコンカー。空のままなら、シーンにある RCCar を自動で探します。")]
        [SerializeField] private RCCar _player;

        [Tooltip("スタート地点。空のままなら、シーンにある StartPoint を自動で探します。")]
        [SerializeField] private StartPoint _startPoint;

        [Header("ルール")]
        [Tooltip("走り出すまでのカウントダウン秒数。0 にするとすぐに走れます。")]
        [Range(0, 10)]
        [SerializeField] private int _countdownSeconds = 3;

        [Tooltip("ONにすると、すべてのチェックポイントを通過しないとゴールできません。")]
        [SerializeField] private bool _requireAllCheckpoints = false;

        // ---------------------------------------------------------------------
        // ほかのスクリプトから読む値
        // ---------------------------------------------------------------------

        /// <summary>今の状態。</summary>
        public GameState State { get; private set; } = GameState.Ready;

        /// <summary>走行タイム（秒）。</summary>
        public float ElapsedTime { get; private set; }

        /// <summary>カウントダウンの残り秒数（Ready 中のみ意味がある）。</summary>
        public float CountdownRemaining { get; private set; }

        /// <summary>操作中のラジコンカー。</summary>
        public RCCar Player => _player;   // => は「この値を返す」短い書き方

        /// <summary>シーン内のチェックポイントの総数。</summary>
        public int CheckpointCount { get; private set; }

        /// <summary>通過済みのチェックポイント数。</summary>
        public int PassedCheckpointCount => _passedCheckpoints.Count;

        /// <summary>
        /// チェックポイントが残っていてゴールできなかった時刻（Time.time）。
        /// 画面に「チェックポイントが残っています」と出すために使う。まだ無ければ -100。
        /// </summary>
        public float GoalRefusedTime { get; private set; } = -100f;

        // 通過済みのチェックポイント（同じものを 2 回数えないように HashSet を使う）
        private readonly HashSet<Checkpoint> _passedCheckpoints = new HashSet<Checkpoint>();

        // ---------------------------------------------------------------------
        // Unity のイベント
        // ---------------------------------------------------------------------

        private void Awake()
        {
            // 2 つ目の GameManager があったら警告（シーンに 1 つだけ置くルール）
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[GameManager] GameManager がシーンに 2 つ以上あります。1 つにしてください。", this);
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            // シーンを抜けるときに自分を登録解除
            if (Instance == this) Instance = null;

            // イベントの登録も解除しておく（-= は += の反対）
            if (_player != null)
            {
                _player.RespawnRequested -= OnRespawnPressed;
            }
        }

        /// <summary>
        /// Start … Awake の後、最初のフレームの直前に 1 回だけ呼ばれる。
        /// ほかのオブジェクトを探す処理は、全員の Awake が終わったここで行うと安全。
        /// </summary>
        private void Start()
        {
            // 参照が空なら自動で探す
            // FindFirstObjectByType … シーンから指定した型のコンポーネントを 1 つ探す
            if (_player == null) _player = FindFirstObjectByType<RCCar>();
            if (_startPoint == null) _startPoint = FindFirstObjectByType<StartPoint>();

            // チェックポイントの数を数えておく
            CheckpointCount = FindObjectsByType<Checkpoint>(FindObjectsSortMode.None).Length;

            if (_player == null)
            {
                Debug.LogError("[GameManager] シーンに RCCar（プレイヤー）がありません！ メニュー VantanSchoolFestival2026 > ステージチェッカー で確認してください。", this);
                return;
            }

            // 車に「やり直し」の命令が来たら OnRespawnPressed を呼んでもらう
            _player.RespawnRequested += OnRespawnPressed;

            // スタート地点へ移動
            if (_startPoint != null)
            {
                _player.SetRespawnPoint(_startPoint.transform.position, _startPoint.transform.rotation);
                _player.Respawn();
            }
            else
            {
                Debug.LogWarning("[GameManager] StartPoint がありません。車は置いた場所からスタートします。", this);
            }

            // カウントダウン開始
            State = GameState.Ready;
            CountdownRemaining = _countdownSeconds;
            _player.CanDrive = false;
        }

        /// <summary>
        /// Update … 毎フレーム呼ばれる。タイマーなど「時間で進む処理」を書く。
        /// </summary>
        private void Update()
        {
            // プレイヤーがいないシーンでは何もしない（エラーで止まらないように）
            if (_player == null) return;

            switch (State)
            {
                case GameState.Ready:
                    // Time.deltaTime … 前のフレームからの経過秒数
                    CountdownRemaining -= Time.deltaTime;
                    if (CountdownRemaining <= 0f)
                    {
                        StartRace();
                    }
                    break;

                case GameState.Playing:
                    ElapsedTime += Time.deltaTime;
                    break;

                case GameState.Goal:
                    // ゴール後は何もしない（リスポーンボタン待ち）
                    break;
            }
        }

        // ---------------------------------------------------------------------
        // ほかのスクリプトから呼ぶメソッド
        // ---------------------------------------------------------------------

        /// <summary>チェックポイントを通過した（Checkpoint から呼ばれる）。</summary>
        public void ReachCheckpoint(Checkpoint checkpoint)
        {
            // Add は「新しく追加できたら true」を返す
            if (_passedCheckpoints.Add(checkpoint))
            {
                Debug.Log("[GameManager] チェックポイント通過 " + PassedCheckpointCount + " / " + CheckpointCount, checkpoint);
            }
        }

        /// <summary>今ゴールできる状態か。</summary>
        public bool CanFinish()
        {
            if (State != GameState.Playing) return false;
            if (_requireAllCheckpoints && PassedCheckpointCount < CheckpointCount) return false;
            return true;
        }

        /// <summary>ゴールに到達した（Goal から呼ばれる）。</summary>
        public void ReachGoal()
        {
            if (!CanFinish())
            {
                if (State == GameState.Playing)
                {
                    GoalRefusedTime = Time.time;
                    Debug.Log("[GameManager] まだ通過していないチェックポイントがあります！");
                }
                return;
            }

            State = GameState.Goal;
            _player.CanDrive = false;
            // ToString("F2") … 小数点以下 2 桁で文字にする
            Debug.Log("[GameManager] ゴール！ タイム: " + ElapsedTime.ToString("F2") + " 秒");
        }

        /// <summary>今のシーンを最初から読み込み直す。</summary>
        public void RestartScene()
        {
            Scene current = SceneManager.GetActiveScene();
            SceneManager.LoadScene(current.buildIndex >= 0 ? current.buildIndex : 0);
        }

        // ---------------------------------------------------------------------
        // 内部の処理
        // ---------------------------------------------------------------------

        /// <summary>レース開始。</summary>
        private void StartRace()
        {
            State = GameState.Playing;
            CountdownRemaining = 0f;
            ElapsedTime = 0f;
            _player.CanDrive = true;
            Debug.Log("[GameManager] スタート！");
        }

        /// <summary>リスポーンボタンが押されたとき。</summary>
        private void OnRespawnPressed()
        {
            if (State == GameState.Goal)
            {
                // ゴール後ならシーンを最初からやり直す
                RestartScene();
            }
            else
            {
                // 走行中なら最後のチェックポイントへ戻る
                _player.Respawn();
            }
        }
    }
}
