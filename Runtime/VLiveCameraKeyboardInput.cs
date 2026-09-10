using UnityEngine;
using UnityEngine.InputSystem;

namespace VLiveKit.Camera
{
    /// <summary>
    /// キーボード入力を受け付け、VLiveCameraSwitcherへのCutおよび手動操作命令を発行するコンポーネント。
    /// </summary>
    [DisallowMultipleComponent]
    public class VLiveCameraKeyboardInput : MonoBehaviour
    {
        // Fields

        [Header("Target Switcher (制御対象)")]
        [Tooltip("切り替え命令を送信するVLiveCameraSwitcher。")]
        [SerializeField]
        private VLiveCameraSwitcher _switcher;

#if ENABLE_INPUT_SYSTEM
        private static readonly Key[] NumpadCutKeys = new Key[]
        {
            Key.Numpad1,
            Key.Numpad2,
            Key.Numpad3,
            Key.Numpad4,
            Key.Numpad5,
            Key.Numpad6,
            Key.Numpad7,
            Key.Numpad8,
            Key.Numpad9
        };

        [Header("Keyboard Bindings - Shot Cut (ショット切り替えキー)")]
        [Tooltip("ショット1〜9へ直接Cutするためのキー割り当て一覧。")]
        [SerializeField]
        private Key[] _cutKeys = new Key[]
        {
            Key.Digit1,
            Key.Digit2,
            Key.Digit3,
            Key.Digit4,
            Key.Digit5,
            Key.Digit6,
            Key.Digit7,
            Key.Digit8,
            Key.Digit9
        };

        [Header("Keyboard Bindings - Motion Control (移動制御キー)")]
        [Tooltip("Spline進行速度を上げるキー。")]
        [SerializeField]
        private Key _speedUpKey = Key.UpArrow;

        [Tooltip("Spline進行速度を下げるキー。")]
        [SerializeField]
        private Key _speedDownKey = Key.DownArrow;

        [Tooltip("Spline進行方向を反転するキー。")]
        [SerializeField]
        private Key _reverseKey = Key.R;

        [Tooltip("Spline進行を一時停止するキー。")]
        [SerializeField]
        private Key _holdKey = Key.H;

        [Tooltip("Spline進行を再開するキー。")]
        [SerializeField]
        private Key _resumeKey = Key.Space;
#endif


        // Properties

        /// <summary>
        /// 制御対象のスイッチャーを取得します。
        /// </summary>
        public VLiveCameraSwitcher Switcher => _switcher;


        // Methods

        private void Awake()
        {
            if (_switcher == null)
            {
                _switcher = FindFirstObjectByType<VLiveCameraSwitcher>();
            }
        }

#if ENABLE_INPUT_SYSTEM
        private static bool IsPressed(Keyboard keyboard, Key key)
        {
            if (key == Key.None)
            {
                return false;
            }

            return keyboard[key].wasPressedThisFrame;
        }
#endif

        private void Update()
        {
            if (_switcher == null)
            {
                return;
            }

#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            // Cut selection (1 to 9, deterministic priority: lowest number wins)
            for (int i = 0; i < 9; i++)
            {
                bool mainPressed = _cutKeys != null && i < _cutKeys.Length && IsPressed(keyboard, _cutKeys[i]);
                bool numpadPressed = i < NumpadCutKeys.Length && IsPressed(keyboard, NumpadCutKeys[i]);
                if (mainPressed || numpadPressed)
                {
                    _switcher.CutToShot(i + 1);
                    break;
                }
            }

            // Speed adjustment
            if (IsPressed(keyboard, _speedUpKey) || IsPressed(keyboard, Key.Equals) || IsPressed(keyboard, Key.NumpadPlus))
            {
                _switcher.SpeedUp();
            }
            else if (IsPressed(keyboard, _speedDownKey) || IsPressed(keyboard, Key.Minus) || IsPressed(keyboard, Key.NumpadMinus))
            {
                _switcher.SpeedDown();
            }

            // Direction reverse
            if (IsPressed(keyboard, _reverseKey))
            {
                _switcher.Reverse();
            }

            // Hold / Resume (mutually exclusive, Hold takes priority if both pressed in same frame)
            if (IsPressed(keyboard, _holdKey))
            {
                _switcher.Hold();
            }
            else if (IsPressed(keyboard, _resumeKey))
            {
                _switcher.Resume();
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            for (int i = 0; i < 9; i++)
            {
                KeyCode alphaKey = KeyCode.Alpha1 + i;
                KeyCode keypadKey = KeyCode.Keypad1 + i;
                if (Input.GetKeyDown(alphaKey) || Input.GetKeyDown(keypadKey))
                {
                    _switcher.CutToShot(i + 1);
                    break;
                }
            }

            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadPlus))
            {
                _switcher.SpeedUp();
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
            {
                _switcher.SpeedDown();
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                _switcher.Reverse();
            }

            if (Input.GetKeyDown(KeyCode.H))
            {
                _switcher.Hold();
            }
            else if (Input.GetKeyDown(KeyCode.Space))
            {
                _switcher.Resume();
            }
#endif
        }

        /// <summary>
        /// 制御対象のスイッチャー参照を設定します。
        /// </summary>
        public void SetSwitcher(VLiveCameraSwitcher switcher)
        {
            _switcher = switcher;
        }
    }
}
