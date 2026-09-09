using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace toshi.VLiveKit.Camera
{
    public class VLiveCameraKeyboardInput : MonoBehaviour
    {
        [SerializeField] private VLiveCameraSwitcher _switcher;

#if ENABLE_INPUT_SYSTEM
        [Header("Keyboard Bindings (Input System)")]
        [SerializeField] private Key _cutAKey = Key.Digit1;
        [SerializeField] private Key _cutBKey = Key.Digit2;
        [SerializeField] private Key _speedUpKey = Key.UpArrow;
        [SerializeField] private Key _speedDownKey = Key.DownArrow;
        [SerializeField] private Key _reverseKey = Key.R;
        [SerializeField] private Key _holdKey = Key.H;
        [SerializeField] private Key _resumeKey = Key.Space;
#endif

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

            // Cut selection (deterministic priority if both pressed in same frame)
            if (IsPressed(keyboard, _cutAKey) || IsPressed(keyboard, Key.Numpad1))
            {
                _switcher.CutToA();
            }
            else if (IsPressed(keyboard, _cutBKey) || IsPressed(keyboard, Key.Numpad2))
            {
                _switcher.CutToB();
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
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
            {
                _switcher.CutToA();
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
            {
                _switcher.CutToB();
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
