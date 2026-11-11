// =============================================================================
//  ActionMapSwitcher.cs
// -----------------------------------------------------------------------------
//  【役割】
//    ゲーム中に「操作方法(ActionMap)」を切り替える。
//    Tab キー（ゲームパッドは Start）を押すたびに
//        Driving (WASD)  ⇔  DrivingAlt (矢印キー)
//    のように入れ替わります。
//
//  【ActionMap とは？】
//    「アクション(移動・ジャンプなど)とキーの組み合わせ」をひとまとめにしたものです。
//    例えば「車に乗っているとき用」「メニュー画面用」のように
//    場面ごとに ActionMap を用意して切り替えるのが Input System の基本的な使い方です。
//
//  【試してみよう】
//    ・PlayerInput の「Default Map」を DrivingAlt に変えて再生すると、
//      最初から矢印キー操作になります（コードを書かずに変更できる！）
//    ・このコンポーネントを外すと、Tab キーで切り替わらなくなります。
// =============================================================================

using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace VantanSchoolFestival2026
{
    /// <summary>
    /// SwitchMap アクションが押されるたびに、PlayerInput の ActionMap を順番に切り替える。
    /// </summary>
    [AddComponentMenu("VantanSchoolFestival2026/Core/操作切り替え (ActionMapSwitcher)")]
    [ComponentSummary(
        "Tab キーを押すたびに、操作に使うキーを「W A S D」と「矢印キー」で切り替えます。",
        "PlayerController に付いています。外すと Tab キーで切り替わらなくなります。",
        "PlayerCarInput")]
    [RequireComponent(typeof(PlayerInput))]
    [DisallowMultipleComponent]
    public class ActionMapSwitcher : MonoBehaviour
    {
        [Header("切り替える ActionMap")]
        [Tooltip("切り替える操作のセット（ActionMap）の名前。RCInputActions にある名前を、大文字・小文字も同じに書いてください。")]
        [SerializeField] private string[] _mapNames = { "Driving", "DrivingAlt" };

        // 今、_mapNames の何番目を使っているか
        private int _currentIndex = 0;

        // 同じ GameObject の PlayerInput（Awake で取得してとっておく）
        private PlayerInput _playerInput;

        /// <summary>ActionMap が切り替わったときに呼ばれるイベント（引数は新しい ActionMap 名）。</summary>
        public event Action<string> MapChanged;

        /// <summary>今使っている ActionMap の名前（UI 表示用）。</summary>
        public string CurrentMapName
        {
            get
            {
                // PlayerInput がまだ準備できていないときは空文字を返す
                if (_playerInput == null || _playerInput.currentActionMap == null) return "";
                return _playerInput.currentActionMap.name;
            }
        }

        private void Awake()
        {
            // GetComponent … 同じ GameObject に付いているコンポーネントを探して取ってくる
            _playerInput = GetComponent<PlayerInput>();
        }

        private void Start()
        {
            // 開始時の ActionMap が配列の何番目かを調べておく
            // （Default Map を Inspector で変えていても正しく切り替わるように）
            for (int i = 0; i < _mapNames.Length; i++)
            {
                if (_mapNames[i] == CurrentMapName)
                {
                    _currentIndex = i;
                    break;
                }
            }
        }

        /// <summary>
        /// SwitchMap アクションが押されたときに PlayerInput (Send Messages) から呼ばれる。
        /// </summary>
        private void OnSwitchMap(InputValue value)
        {
            if (!value.isPressed) return;
            if (_mapNames == null || _mapNames.Length == 0) return;

            // 次の番号へ。最後まで行ったら 0 番に戻る（% は「割った余り」）
            _currentIndex = (_currentIndex + 1) % _mapNames.Length;
            string nextMap = _mapNames[_currentIndex];

            // 指定した名前の ActionMap が存在するか確認してから切り替える
            if (_playerInput.actions.FindActionMap(nextMap) == null)
            {
                Debug.LogWarning("[ActionMapSwitcher] ActionMap \"" + nextMap + "\" が見つかりません。名前を確認してください。", this);
                return;
            }

            _playerInput.SwitchCurrentActionMap(nextMap);
            MapChanged?.Invoke(nextMap);
            Debug.Log("[ActionMapSwitcher] 操作方法を " + nextMap + " に切り替えました", this);
        }
    }
}
