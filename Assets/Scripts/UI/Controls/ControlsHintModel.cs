// =============================================================================
//  ControlsHintModel.cs
// -----------------------------------------------------------------------------
//  【役割】MVP の「Model（データ）」
//    「今どのキーで何ができるか」の一覧を作ります。
//    MonoBehaviour ではない普通の C# クラスなので、GameObject には付けません。
//    （ControlsHintPresenter が new して使います）
//
//  【キーの名前はどこから？】
//    Input System の InputAction.GetBindingDisplayString() を使うと、
//    「W/A/S/D」「Space」「Left Stick」のような表示用の名前を取り出せます。
//    RCInputActions でキー割り当てを変えると、画面の表示も自動で変わります。
// =============================================================================

using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace VantanSchoolFestival2026
{
    /// <summary>操作方法の一覧（どのキーで何ができるか）を作るデータクラス。</summary>
    public class ControlsHintModel
    {
        /// <summary>一覧の 1 行分。</summary>
        public readonly struct Entry
        {
            /// <summary>何ができるか（例: ジャンプ）</summary>
            public readonly string Label;

            /// <summary>どのキーか（例: Space）</summary>
            public readonly string Keys;

            public Entry(string label, string keys)
            {
                Label = label;
                Keys = keys;
            }
        }

        /// <summary>アクション名と、画面に出す日本語の対応表。上から順に表示される。</summary>
        private static readonly (string action, string label)[] ActionLabels =
        {
            ("Move", "移動"),
            ("Jump", "ジャンプ"),
            ("Boost", "ブースト"),
            ("Respawn", "やり直し"),
            ("SwitchMap", "操作切り替え"),
        };

        private readonly PlayerInput _playerInput;
        private readonly RCCar _car;

        /// <param name="playerInput">入力を管理している PlayerInput</param>
        /// <param name="car">操作している車（付いているパーツで表示を変える）。null でもよい</param>
        public ControlsHintModel(PlayerInput playerInput, RCCar car)
        {
            _playerInput = playerInput;
            _car = car;
        }

        /// <summary>今の ActionMap 名（例: Driving）。</summary>
        public string MapName =>
            _playerInput != null && _playerInput.currentActionMap != null ? _playerInput.currentActionMap.name : "";

        /// <summary>今のコントロールスキーム名（例: Keyboard / Gamepad）。</summary>
        public string ControlScheme =>
            _playerInput != null ? _playerInput.currentControlScheme ?? "" : "";

        /// <summary>
        /// 今使えるアクションの組み合わせを 1 つの数字にして返す。
        /// この数字が変わったら「車の部品が付け外しされた」と分かる。
        /// </summary>
        public int GetAvailabilityKey()
        {
            int key = 0;
            for (int i = 0; i < ActionLabels.Length; i++)
            {
                // 使えるアクションの番号のビットを立てる（例：0 番と 2 番が使える → 101）
                if (IsAvailable(ActionLabels[i].action)) key |= 1 << i;
            }
            return key;
        }

        /// <summary>今の操作方法の一覧を作って返す。</summary>
        public List<Entry> BuildEntries()
        {
            var entries = new List<Entry>();
            if (_playerInput == null || _playerInput.currentActionMap == null) return entries;

            InputActionMap map = _playerInput.currentActionMap;

            // コントロールスキーム（Keyboard / Gamepad）が決まっていれば、それに合うキーだけ表示する
            string group = string.IsNullOrEmpty(ControlScheme) ? null : ControlScheme;

            foreach ((string actionName, string label) in ActionLabels)
            {
                // その車で使えないアクションは表示しない（パーツが付いていない など）
                if (!IsAvailable(actionName)) continue;

                InputAction action = map.FindAction(actionName);
                if (action == null) continue;

                // 表示用のキー名を取り出す（例: "W/A/S/D"）
                string keys = GetKeysText(action, group);
                if (string.IsNullOrEmpty(keys)) continue;

                entries.Add(new Entry(label, keys));
            }
            return entries;
        }

        /// <summary>
        /// アクションに割り当てられたキーの表示名を作る。
        /// </summary>
        /// <remarks>
        /// WASD のような「複合バインディング(Composite)」は、
        /// 親(2DVector)にはグループ(Keyboard など)が付いておらず、子(up/down/…)にだけ付いている。
        /// そのため「子のどれかがグループに合うか」を自分で調べてから、親の表示名を使っている。
        /// </remarks>
        private static string GetKeysText(InputAction action, string group)
        {
            var parts = new List<string>();
            // MaskByGroup … 指定したグループに属するバインディングだけを通す「ふるい」
            InputBinding? mask = group != null ? InputBinding.MaskByGroup(group) : (InputBinding?)null;

            for (int i = 0; i < action.bindings.Count; i++)
            {
                InputBinding binding = action.bindings[i];

                if (binding.isComposite)
                {
                    // 子(パーツ)のどれかがグループに合えば、複合バインディング全体を表示する
                    bool match = mask == null;
                    for (int j = i + 1; j < action.bindings.Count && action.bindings[j].isPartOfComposite; j++)
                    {
                        if (mask != null && mask.Value.Matches(action.bindings[j])) match = true;
                    }
                    if (match) parts.Add(action.GetBindingDisplayString(i));
                }
                else if (!binding.isPartOfComposite)
                {
                    // 普通のバインディング（Space など）
                    if (mask == null || mask.Value.Matches(binding)) parts.Add(action.GetBindingDisplayString(i));
                }
            }
            // 同じアクションに複数のキーがあれば " / " でつなぐ
            return string.Join(" / ", parts);
        }

        /// <summary>そのアクションが今の車で使えるか。</summary>
        private bool IsAvailable(string actionName)
        {
            if (_car == null) return true;

            switch (actionName)
            {
                case "Jump": return HasEnabled<JumpParts>();
                case "Boost": return HasEnabled<BoostParts>();
                default: return true;
            }
        }

        /// <summary>車に指定の部品が付いていて、有効になっているか。</summary>
        private bool HasEnabled<T>() where T : UnityEngine.Behaviour
        {
            T parts = _car.GetComponent<T>();
            return parts != null && parts.enabled;
        }
    }
}
