// =============================================================================
//  ControlsHintPresenter.cs
// -----------------------------------------------------------------------------
//  【役割】MVP の「Presenter（つなぐ係）」
//    入力（PlayerInput）が変わったら、Model に一覧を作らせて View に表示させます。
//
//        PlayerInput（キーボード ⇔ ゲームパッド が変わった）─┐
//        ActionMapSwitcher（Driving ⇔ DrivingAlt）        ─┼─▶ Presenter ─▶ Model で一覧作成 ─▶ View に表示
//
//  【イベントで動く】
//    毎フレーム調べるのではなく「変わったときだけ」更新しています。
//    （PlayerInput.onControlsChanged や ActionMapSwitcher.MapChanged に登録）
// =============================================================================

using UnityEngine;
using UnityEngine.InputSystem;

namespace VantanSchoolFestival2026
{
    /// <summary>入力の状態を ControlsHintView に表示させる。</summary>
    [AddComponentMenu("VantanSchoolFestival2026/UI/操作方法 つなぎ役 (ControlsHintPresenter)")]
    [ComponentSummary(
        "今の操作方法（ActionMap とキーボード/ゲームパッド）を調べ、ControlsHintView に表示させます（MVP の Presenter）。キー割り当てを変えると表示も自動で変わります。",
        "View と同じ GameObject に付けます。Player Input が空なら、シーンの PlayerInput を自動で探します。",
        "ControlsHintView / ControlsHintModel / PlayerInput / ActionMapSwitcher")]
    [DisallowMultipleComponent]
    public class ControlsHintPresenter : MonoBehaviour
    {
        [Header("参照（空なら自動で探します）")]
        [Tooltip("表示を担当する ControlsHintView。空なら同じ GameObject から探します。")]
        [SerializeField] private ControlsHintView _view;

        [Tooltip("入力を管理している PlayerInput（PlayerController に付いています）。空ならシーンから探します。")]
        [SerializeField] private PlayerInput _playerInput;

        [Header("文言")]
        [Tooltip("見出しの書式。{0} に ActionMap 名、{1} にコントロールスキーム名が入ります。")]
        [SerializeField] private string _titleFormat = "操作方法  [{0} / {1}]";

        private ControlsHintModel _model;
        private ActionMapSwitcher _switcher;

        private void Awake()
        {
            if (_view == null) _view = GetComponent<ControlsHintView>();
            if (_playerInput == null) _playerInput = FindFirstObjectByType<PlayerInput>();
        }

        private void OnEnable()
        {
            if (_playerInput == null) return;

            // 「変わったら教えて」と登録
            _playerInput.onControlsChanged += OnControlsChanged;
            _switcher = _playerInput.GetComponent<ActionMapSwitcher>();
            if (_switcher != null) _switcher.MapChanged += OnMapChanged;
        }

        private void OnDisable()
        {
            if (_playerInput != null) _playerInput.onControlsChanged -= OnControlsChanged;
            if (_switcher != null) _switcher.MapChanged -= OnMapChanged;
        }

        private void Start()
        {
            if (_playerInput == null) return;

            // 操作する車は PlayerCarInput から教えてもらう（付いているパーツで表示を変えるため）
            // ※ PlayerCarInput は Awake で車を探すので、全員の Awake が終わった Start で読む
            PlayerCarInput carInput = _playerInput.GetComponent<PlayerCarInput>();
            RCCar car = carInput != null ? carInput.TargetCar : null;
            _model = new ControlsHintModel(_playerInput, car);

            // 最初の表示
            Refresh();
        }

        /// <summary>キーボード ⇔ ゲームパッド が切り替わったとき。</summary>
        private void OnControlsChanged(PlayerInput input) => Refresh();

        /// <summary>ActionMap が切り替わったとき。</summary>
        private void OnMapChanged(string mapName) => Refresh();

        /// <summary>Model から一覧を作り直して View に表示させる。</summary>
        public void Refresh()
        {
            if (_view == null || _model == null) return;

            string scheme = string.IsNullOrEmpty(_model.ControlScheme) ? "-" : _model.ControlScheme;
            _view.Show(string.Format(_titleFormat, _model.MapName, scheme), _model.BuildEntries());
        }
    }
}
