using UnityEngine;
using Unity.Cinemachine;

namespace toshi.VLiveKit.Camera
{
    public class VLiveCameraSwitcher : MonoBehaviour
    {
        private const int ActivePriority = 10;
        private const int InactivePriority = 0;

        [SerializeField] private CinemachineBrain _cinemachineBrain;
        [SerializeField] private VLiveCameraShot _shotA;
        [SerializeField] private VLiveCameraShot _shotB;

        private VLiveCameraShot _currentProgramShot;

        public CinemachineBrain CinemachineBrain => _cinemachineBrain;
        public VLiveCameraShot ShotA => _shotA;
        public VLiveCameraShot ShotB => _shotB;
        public VLiveCameraShot CurrentProgramShot => _currentProgramShot;

        private void Awake()
        {
            if (_cinemachineBrain == null)
            {
                _cinemachineBrain = FindFirstObjectByType<CinemachineBrain>();
            }
        }

        private void Start()
        {
            if (_cinemachineBrain != null)
            {
                _cinemachineBrain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f);
            }

            // Initialize Program with Shot A as default
            if (_shotA != null && _shotA.CinemachineCamera != null)
            {
                _shotA.CinemachineCamera.Priority = ActivePriority;
                _shotA.CinemachineCamera.Prioritize();
                _currentProgramShot = _shotA;
                _shotA.OnEnterProgram();

                if (_shotB != null)
                {
                    if (_shotB.CinemachineCamera != null)
                    {
                        _shotB.CinemachineCamera.Priority = InactivePriority;
                    }
                    _shotB.PrepareStart();
                }

                Debug.Log($"[VLiveCameraSwitcher] Program: {_shotA.ShotName}");
            }
            else if (_shotB != null && _shotB.CinemachineCamera != null)
            {
                _shotB.CinemachineCamera.Priority = ActivePriority;
                _shotB.CinemachineCamera.Prioritize();
                _currentProgramShot = _shotB;
                _shotB.OnEnterProgram();

                Debug.Log($"[VLiveCameraSwitcher] Program: {_shotB.ShotName}");
            }
        }

        public void CutToA()
        {
            CutTo(_shotA);
        }

        public void CutToB()
        {
            CutTo(_shotB);
        }

        public void CutToShot(int shotIndex)
        {
            if (shotIndex == 1)
            {
                CutToA();
            }
            else if (shotIndex == 2)
            {
                CutToB();
            }
            // Invalid indices are ignored; current Program is maintained.
        }

        private void CutTo(VLiveCameraShot nextShot)
        {
            if (nextShot == null || nextShot == _currentProgramShot)
            {
                return;
            }

            if (nextShot.CinemachineCamera == null)
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

        public void SpeedUp()
        {
            if (_currentProgramShot != null)
            {
                _currentProgramShot.SpeedUp();
            }
        }

        public void SpeedDown()
        {
            if (_currentProgramShot != null)
            {
                _currentProgramShot.SpeedDown();
            }
        }

        public void Reverse()
        {
            if (_currentProgramShot != null)
            {
                _currentProgramShot.Reverse();
            }
        }

        public void Hold()
        {
            if (_currentProgramShot != null)
            {
                _currentProgramShot.Hold();
            }
        }

        public void Resume()
        {
            if (_currentProgramShot != null)
            {
                _currentProgramShot.Resume();
            }
        }
    }
}
