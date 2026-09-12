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

        [Header("Target Switcher")]
        [Tooltip("切り替え命令を送信するVLiveCameraSwitcher。")]
        [SerializeField]
        private VLiveCameraSwitcher _switcher;


#if ENABLE_INPUT_SYSTEM
        [Header("Keyboard Bindings - Shot Cut")]
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

        [Header("Keyboard Bindings - Motion Control")]
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

        [Tooltip("緊急即時停止キー（既定では未割り当て）。")]
        [SerializeField]
        private Key _freezeKey = Key.None;


        public static readonly Key[] ImplicitNumpadCutKeys = new Key[]
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

        public static readonly Key[] ImplicitSpeedUpKeys = new Key[]
        {
            Key.Equals,
            Key.NumpadPlus
        };

        public static readonly Key[] ImplicitSpeedDownKeys = new Key[]
        {
            Key.Minus,
            Key.NumpadMinus
        };
#endif


        // Properties

        /// <summary>
        /// 制御対象のスイッチャーを取得します。
        /// </summary>
        public VLiveCameraSwitcher Switcher => _switcher;


        // Methods

        /// <summary>
        /// 制御対象のスイッチャー参照を設定します。
        /// </summary>
        public void SetSwitcher(VLiveCameraSwitcher switcher)
        {
            _switcher = switcher;
        }


#if ENABLE_INPUT_SYSTEM
        /// <summary>
        /// 指定されたキーがランタイム暗黙 Cut キー（テンキー1〜9）であるかを判定します。
        /// </summary>
        public static bool IsImplicitCutKey(Key key, out int slotIndex)
        {
            for (int i = 0; i < ImplicitNumpadCutKeys.Length; i++)
            {
                if (ImplicitNumpadCutKeys[i] == key)
                {
                    slotIndex = i;
                    return true;
                }
            }

            slotIndex = -1;
            return false;
        }

        /// <summary>
        /// 指定されたキーがランタイム暗黙 Speed Up キー（Equals / NumpadPlus）であるかを判定します。
        /// </summary>
        public static bool IsImplicitSpeedUpKey(Key key)
        {
            for (int i = 0; i < ImplicitSpeedUpKeys.Length; i++)
            {
                if (ImplicitSpeedUpKeys[i] == key)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 指定されたキーがランタイム暗黙 Speed Down キー（Minus / NumpadMinus）であるかを判定します。
        /// </summary>
        public static bool IsImplicitSpeedDownKey(Key key)
        {
            for (int i = 0; i < ImplicitSpeedDownKeys.Length; i++)
            {
                if (ImplicitSpeedDownKeys[i] == key)
                {
                    return true;
                }
            }

            return false;
        }


        private void Awake()
        {
            if (_switcher == null)
            {
                _switcher = GetComponent<VLiveCameraSwitcher>();
                if (_switcher == null)
                {
                    _switcher = GetComponentInParent<VLiveCameraSwitcher>();
                }
            }
        }

        private static bool IsPressed(Keyboard keyboard, Key key)
        {
            if (key == Key.None)
            {
                return false;
            }

            var control = keyboard[key];
            return control != null && control.wasPressedThisFrame;
        }

        private static bool IsAnyPressed(Keyboard keyboard, Key[] keys)
        {
            if (keys == null)
            {
                return false;
            }

            for (int i = 0; i < keys.Length; i++)
            {
                if (IsPressed(keyboard, keys[i]))
                {
                    return true;
                }
            }

            return false;
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

            // Cut selection (deterministic priority: lowest number wins, bounded by cut keys and shot count)
            int shotCount = _switcher.ShotCount;
            int cutKeyCount = _cutKeys != null ? _cutKeys.Length : 0;
            int maxSlots = Mathf.Min(Mathf.Max(cutKeyCount, ImplicitNumpadCutKeys.Length), shotCount);

            for (int i = 0; i < maxSlots; i++)
            {
                bool mainPressed = (i < cutKeyCount) && _cutKeys != null && IsPressed(keyboard, _cutKeys[i]);
                bool numpadPressed = (i < ImplicitNumpadCutKeys.Length) && IsPressed(keyboard, ImplicitNumpadCutKeys[i]);
                if (mainPressed || numpadPressed)
                {
                    _switcher.CutToShot(i + 1);
                    break;
                }
            }

            // Speed adjustment
            if (IsPressed(keyboard, _speedUpKey) || IsAnyPressed(keyboard, ImplicitSpeedUpKeys))
            {
                _switcher.SpeedUp();
            }
            else if (IsPressed(keyboard, _speedDownKey) || IsAnyPressed(keyboard, ImplicitSpeedDownKeys))
            {
                _switcher.SpeedDown();
            }

            // Direction reverse
            if (IsPressed(keyboard, _reverseKey))
            {
                _switcher.Reverse();
            }

            // Hold / Resume / Freeze (Freeze takes highest priority, then Hold, then Resume)
            if (IsPressed(keyboard, _freezeKey))
            {
                _switcher.Freeze();
            }
            else if (IsPressed(keyboard, _holdKey))
            {
                _switcher.Hold();
            }
            else if (IsPressed(keyboard, _resumeKey))
            {
                _switcher.Resume();
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            int shotCount = _switcher.ShotCount;
            int maxCut = Mathf.Min(9, shotCount);
            for (int i = 0; i < maxCut; i++)
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
    }
}
