// =============================================================================
//  HudPresenter.cs
// -----------------------------------------------------------------------------
//  【役割】MVP の「Presenter（つなぐ係）」
//    Model（GameManager・車のブーストパーツ）の状態を読んで、
//    View（HudView）に「これを表示して」と伝えます。
//
//        GameManager ─┐
//        BoostParts  ─┼─▶ HudPresenter ──▶ HudView ──▶ 画面
//
//    「状態をどんな文字で見せるか（3, 2, 1, GO! など）」を決めるのもここです。
// =============================================================================

using UnityEngine;

namespace VantanSchoolFestival2026
{
    /// <summary>GameManager などの状態を HudView に表示させる。</summary>
    [AddComponentMenu("VantanSchoolFestival2026/UI/HUD つなぎ役 (HudPresenter)")]
    [ComponentSummary(
        "ゲームの様子（タイムやゴールしたかどうか）を調べて、画面に出す文章を決める係です。",
        "GameUI に付いています。Goal Format などで、画面に出す文章を変えられます。",
        "HudView / GameManager")]
    [DisallowMultipleComponent]
    public class HudPresenter : MonoBehaviour
    {
        [Header("参照（空なら自動で探します）")]
        [Tooltip("表示を担当する HudView。空なら同じ GameObject から探します。")]
        [SerializeField] private HudView _view;

        [Tooltip("ブーストゲージを読む車。空ならシーンの RCCar を探します。")]
        [SerializeField] private RCCar _car;

        [Header("文言")]
        [Tooltip("スタート直後に表示する文字。")]
        [SerializeField] private string _goText = "GO!";

        [Tooltip("ゴール後に表示する案内文。{0} にタイム(秒)が入ります。<size=24>…</size> で文字の大きさを変えられます。")]
        [TextArea(2, 4)]
        [SerializeField] private string _goalFormat = "GOAL!\n{0} 秒\n<size=24>やり直しボタンでもう一度</size>";

        [Tooltip("コースアウト中に表示する文字。{0} にリスポーンまでの残り秒数が入ります。")]
        [TextArea(2, 3)]
        [SerializeField] private string _courseOutFormat = "コースアウト！\n<size=28>コースに戻ろう… {0}</size>";

        // 車のブーストパーツ（付いていなければ null）
        private BoostParts _boost;

        // 場外判定係（シーンに無ければ null）
        private OutOfBoundsJudge _judge;

        private void Awake()
        {
            if (_view == null) _view = GetComponent<HudView>();
        }

        private void Start()
        {
            if (_car == null) _car = FindFirstObjectByType<RCCar>();
            if (_car != null) _boost = _car.GetComponent<BoostParts>();
            _judge = FindFirstObjectByType<OutOfBoundsJudge>();
        }

        /// <summary>毎フレーム、Model の状態を View に反映する。</summary>
        private void Update()
        {
            if (_view == null) return;

            GameManager gm = GameManager.Instance;
            if (gm == null)
            {
                _view.SetMessage("GameManager がありません");
                return;
            }

            _view.SetTime(gm.ElapsedTime);
            _view.SetCheckpoints(gm.PassedCheckpointCount, gm.CheckpointCount);
            _view.SetMessage(GetMessage(gm));

            // ブーストパーツは途中で付け外しされるかもしれないので、有効かどうかも見る
            bool hasBoost = _boost != null && _boost.isActiveAndEnabled;
            _view.SetBoost(hasBoost, hasBoost ? _boost.Energy01 : 0f);
        }

        /// <summary>ゲームの状態から、中央に出すメッセージを決める。</summary>
        private string GetMessage(GameManager gm)
        {
            switch (gm.State)
            {
                case GameState.Ready:
                    // Mathf.CeilToInt … 小数を切り上げて整数にする (2.3 → 3)
                    return Mathf.CeilToInt(gm.CountdownRemaining).ToString();

                case GameState.Playing:
                    // コースの外に 0.5 秒以上いたら警告を出す（ジャンプ中の一瞬は出さない）
                    if (_judge != null && _judge.OutTime > 0.5f)
                    {
                        return string.Format(_courseOutFormat, Mathf.CeilToInt(_judge.TimeLeft));
                    }
                    // スタート直後の 1 秒だけ GO! を出す
                    return gm.ElapsedTime < 1f ? _goText : "";

                case GameState.Goal:
                    // string.Format … {0} の場所に値を入れて文字を作る
                    return string.Format(_goalFormat, gm.ElapsedTime.ToString("F2"));

                default:
                    return "";
            }
        }
    }
}
