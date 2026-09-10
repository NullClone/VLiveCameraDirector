using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

namespace toshi.VLiveKit.Camera
{
    /// <summary>
    /// 登録された複数のShotを管理し、番号指定または直接指定でCut切り替えを行うスイッチャー。
    /// </summary>
    [DisallowMultipleComponent]
    public class VLiveCameraSwitcher : MonoBehaviour
    {
        // Fields

        private const int ActivePriority = 10;
        private const int InactivePriority = 0;

        [Header("Cinemachine Output (出力設定)")]
        [Tooltip("Program出力を行うCinemachineBrain。")]
        [SerializeField]
        private CinemachineBrain _cinemachineBrain;

        [Header("Shot List (ショット一覧)")]
        [Tooltip("管理対象のショット一覧。インデックス0（1番目）がキー1に対応します。")]
        [SerializeField]
        private List<VLiveCameraShot> _shots = new List<VLiveCameraShot>();

        private VLiveCameraShot _currentProgramShot;


        // Properties

        /// <summary>
        /// 出力用のCinemachineBrainを取得します。
        /// </summary>
        public CinemachineBrain CinemachineBrain => _cinemachineBrain;

        /// <summary>
        /// 登録されているショット一覧を取得します。
        /// </summary>
        public IReadOnlyList<VLiveCameraShot> Shots => _shots;

        /// <summary>
        /// 登録されているショット数を取得します。
        /// </summary>
        public int ShotCount => _shots != null ? _shots.Count : 0;

        /// <summary>
        /// 現在ProgramとしてLive出力中のShotを取得します。
        /// </summary>
        public VLiveCameraShot CurrentProgramShot => _currentProgramShot;


        // Methods

        private void Awake()
        {
            if (_cinemachineBrain == null)
            {
                _cinemachineBrain = FindFirstObjectByType<CinemachineBrain>(FindObjectsInactive.Include);
            }
        }

        private void Start()
        {
            if (_cinemachineBrain != null)
            {
                _cinemachineBrain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f);
            }

            VLiveCameraShot initialShot = null;
            if (_shots != null)
            {
                for (int i = 0; i < _shots.Count; i++)
                {
                    if (_shots[i] != null && _shots[i].IsValid)
                    {
                        initialShot = _shots[i];
                        break;
                    }
                }

                for (int i = 0; i < _shots.Count; i++)
                {
                    VLiveCameraShot shot = _shots[i];
                    if (shot != null)
                    {
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
        /// 指定されたショット番号（1〜9、1始まり）へ直接Cutします。
        /// 無効な番号や未設定スロットの場合は現在のProgramを維持します。
        /// </summary>
        /// <param name="shotNumber">1〜9のショット番号。</param>
        public void CutToShot(int shotNumber)
        {
            if (_shots == null)
            {
                return;
            }

            int index = shotNumber - 1;
            if (index < 0 || index >= _shots.Count)
            {
                return;
            }

            CutTo(_shots[index]);
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
        /// ショット一覧を設定します。
        /// </summary>
        public void SetShots(List<VLiveCameraShot> shots)
        {
            _shots = shots;
        }

        /// <summary>
        /// CinemachineBrain参照を設定します。
        /// </summary>
        public void SetCinemachineBrain(CinemachineBrain brain)
        {
            _cinemachineBrain = brain;
        }
    }
}
