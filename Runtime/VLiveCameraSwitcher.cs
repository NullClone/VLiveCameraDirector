using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

namespace VLiveKit.Camera
{
    /// <summary>
    /// VLiveCameraRigを参照し、番号指定または直接指定でCut切り替えを行うスイッチャー。
    /// Program切り替えと再生状態の伝達のみを担当します。
    /// </summary>
    [DisallowMultipleComponent]
    public class VLiveCameraSwitcher : MonoBehaviour
    {
        // Fields

        private const int ActivePriority = 10;
        private const int InactivePriority = 0;

        [Header("Target Rig (対象リグ)")]
        [Tooltip("管理対象のVLiveCameraRig参照。Shotスロット一覧およびProgram CameraはRigから取得します。")]
        [SerializeField]
        private VLiveCameraRig _rig;

        private VLiveCameraShot _currentProgramShot;


        // Properties

        /// <summary>
        /// 参照しているVLiveCameraRigを取得します。
        /// </summary>
        public VLiveCameraRig Rig => _rig;

        /// <summary>
        /// 出力用のCinemachineBrainを取得します。
        /// </summary>
        public CinemachineBrain CinemachineBrain => _rig != null ? _rig.CinemachineBrain : null;

        /// <summary>
        /// Rigに登録されているショットスロット一覧を取得します。
        /// </summary>
        public IReadOnlyList<VLiveCameraShotSlot> Slots => _rig != null ? _rig.Slots : null;

        /// <summary>
        /// Rigに登録されているショットスロット数を取得します。
        /// </summary>
        public int ShotCount => (_rig != null && _rig.Slots != null) ? _rig.Slots.Count : 0;

        /// <summary>
        /// 現在ProgramとしてLive出力中のShotを取得します。
        /// </summary>
        public VLiveCameraShot CurrentProgramShot => _currentProgramShot;


        // Methods

        private void Awake()
        {
            if (_rig == null)
            {
                _rig = GetComponent<VLiveCameraRig>();
                if (_rig == null)
                {
                    _rig = GetComponentInParent<VLiveCameraRig>();
                }
            }
        }

        private void Start()
        {
            CinemachineBrain brain = CinemachineBrain;
            if (brain != null)
            {
                brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f);
            }

            VLiveCameraShot initialShot = null;
            if (_rig != null && _rig.Slots != null)
            {
                for (int i = 0; i < _rig.Slots.Count; i++)
                {
                    VLiveCameraShotSlot slot = _rig.Slots[i];
                    if (slot != null && slot.Shot != null && slot.Shot.IsValid)
                    {
                        initialShot = slot.Shot;
                        break;
                    }
                }

                for (int i = 0; i < _rig.Slots.Count; i++)
                {
                    VLiveCameraShotSlot slot = _rig.Slots[i];
                    if (slot != null && slot.Shot != null)
                    {
                        VLiveCameraShot shot = slot.Shot;
                        if (shot.CinemachineCamera != null)
                        {
                            shot.CinemachineCamera.Priority = InactivePriority;
                        }

                        shot.PrepareStart();
                    }
                }
            }

            if (initialShot != null)
            {
                initialShot.CinemachineCamera.Priority = ActivePriority;
                initialShot.CinemachineCamera.Prioritize();
                _currentProgramShot = initialShot;
                initialShot.OnEnterProgram();

                Debug.Log($"[VLiveCameraSwitcher] Program: {initialShot.ShotName}");
            }
        }

        /// <summary>
        /// 指定されたショット番号（1始まり）へ直接Cutします。
        /// 無効な番号や未設定スロットの場合は現在のProgramを維持します。
        /// </summary>
        /// <param name="shotNumber">1始まりのショット番号。</param>
        public void CutToShot(int shotNumber)
        {
            if (_rig == null || _rig.Slots == null)
            {
                return;
            }

            int index = shotNumber - 1;
            if (index < 0 || index >= _rig.Slots.Count)
            {
                return;
            }

            VLiveCameraShotSlot slot = _rig.Slots[index];
            if (slot == null || slot.Shot == null)
            {
                return;
            }

            CutTo(slot.Shot);
        }

        /// <summary>
        /// 指定されたShotへ直接Cutします。
        /// 現在のProgramと同一のShotである場合や無効な参照の場合は何もしません。
        /// </summary>
        /// <param name="nextShot">切り替え先のShot。</param>
        public void CutTo(VLiveCameraShot nextShot)
        {
            if (nextShot == null || nextShot == _currentProgramShot || !nextShot.IsValid)
            {
                return;
            }

            VLiveCameraShot previousShot = _currentProgramShot;

            nextShot.CinemachineCamera.Priority = ActivePriority;
            nextShot.CinemachineCamera.Prioritize();

            if (previousShot != null && previousShot.CinemachineCamera != null)
            {
                previousShot.CinemachineCamera.Priority = InactivePriority;
            }

            _currentProgramShot = nextShot;

            if (previousShot != null)
            {
                previousShot.OnExitProgram();
            }

            nextShot.OnEnterProgram();

            Debug.Log($"[VLiveCameraSwitcher] Program: {nextShot.ShotName}");
        }

        /// <summary>
        /// 現在のProgram ShotのSpline進行速度を1段階上げます。
        /// </summary>
        public void SpeedUp()
        {
            if (_currentProgramShot != null)
            {
                _currentProgramShot.SpeedUp();
            }
        }

        /// <summary>
        /// 現在のProgram ShotのSpline進行速度を1段階下げます。
        /// </summary>
        public void SpeedDown()
        {
            if (_currentProgramShot != null)
            {
                _currentProgramShot.SpeedDown();
            }
        }

        /// <summary>
        /// 現在のProgram Shotの進行方向を反転します。
        /// </summary>
        public void Reverse()
        {
            if (_currentProgramShot != null)
            {
                _currentProgramShot.Reverse();
            }
        }

        /// <summary>
        /// 現在のProgram Shotの進行を一時停止します。
        /// </summary>
        public void Hold()
        {
            if (_currentProgramShot != null)
            {
                _currentProgramShot.Hold();
            }
        }

        /// <summary>
        /// 一時停止中のProgram Shotの進行を再開します。
        /// </summary>
        public void Resume()
        {
            if (_currentProgramShot != null)
            {
                _currentProgramShot.Resume();
            }
        }

        /// <summary>
        /// 現在のProgram Shotの進行を即時停止します。
        /// </summary>
        public void Freeze()
        {
            if (_currentProgramShot != null)
            {
                _currentProgramShot.Freeze();
            }
        }

        /// <summary>
        /// 管理対象のVLiveCameraRig参照を設定します。
        /// </summary>
        /// <param name="rig">設定するVLiveCameraRig。</param>
        public void SetRig(VLiveCameraRig rig)
        {
            _rig = rig;
        }
    }
}
