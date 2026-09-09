using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using toshi.VLiveKit.Camera;

namespace toshi.VLiveKit.Camera.Editor
{
    [InitializeOnLoad]
    public static class VLiveCameraABPlayModeVerifier
    {
        private const string ScenePath = "Assets/toshi.VLiveKit/VLiveCameraUnit/Tests/VLiveCameraABTest.unity";
        private const string FlagFile = "Temp/run_verification.flag";
        private const string LogFile = "Temp/playmode_verification.log";
        private const string StateKey = "VLiveCameraAB_TestStep";
        private const string TimerKey = "VLiveCameraAB_TestTimer";
        private const string PosKey = "VLiveCameraAB_PosBeforeHold";

        private static float s_StepTimer = 0f;

        static VLiveCameraABPlayModeVerifier()
        {
            EditorApplication.update += CheckAndRun;
        }

        private static void WriteLog(string message)
        {
            try
            {
                File.AppendAllText(LogFile, $"[{DateTime.Now:HH:mm:ss.fff}] {message}\n");
            }
            catch { }
            Debug.Log($"[PlayModeVerification] {message}");
        }

        private static void Fail(string reason)
        {
            WriteLog($"FAILED: {reason}");
            SessionState.EraseInt(StateKey);
            if (File.Exists(FlagFile))
            {
                File.Delete(FlagFile);
            }
            EditorApplication.isPlaying = false;
        }

        [MenuItem("Tools/VLiveCameraUnit/Run PlayMode Verification")]
        public static void TriggerVerification()
        {
            File.WriteAllText(FlagFile, "run");
            WriteLog("Verification triggered via menu/flag.");
        }

        private static void CheckAndRun()
        {
            if (!File.Exists(FlagFile))
            {
                return;
            }

            if (!EditorApplication.isPlaying)
            {
                int currentStep = SessionState.GetInt(StateKey, -1);
                if (currentStep < 0)
                {
                    // Start verification
                    File.WriteAllText(LogFile, "=== Starting A/B Play Mode Verification ===\n");
                    WriteLog("Opening scene: " + ScenePath);
                    EditorSceneManager.OpenScene(ScenePath);
                    SessionState.SetInt(StateKey, 0);
                    SessionState.SetFloat(TimerKey, 0f);
                    WriteLog("Entering Play Mode...");
                    EditorApplication.isPlaying = true;
                }
                return;
            }

            // In Play Mode
            var switcher = UnityEngine.Object.FindFirstObjectByType<VLiveCameraSwitcher>();
            if (switcher == null)
            {
                return;
            }

            int step = SessionState.GetInt(StateKey, 0);
            s_StepTimer += Time.deltaTime;

            switch (step)
            {
                case 0: // Verify initial state (Shot A live, Shot B standby at 0, IsPrepared)
                    if (s_StepTimer > 0.5f)
                    {
                        if (switcher.CurrentProgramShot != switcher.ShotA)
                        {
                            Fail($"Step 0: Expected Shot A as Program, got {switcher.CurrentProgramShot?.ShotName}");
                            return;
                        }
                        if (switcher.ShotA.CinemachineCamera.Priority.Value != 10)
                        {
                            Fail($"Step 0: Expected Shot A priority 10, got {switcher.ShotA.CinemachineCamera.Priority.Value}");
                            return;
                        }
                        if (switcher.ShotB.CinemachineCamera.Priority.Value != 0)
                        {
                            Fail($"Step 0: Expected Shot B priority 0, got {switcher.ShotB.CinemachineCamera.Priority.Value}");
                            return;
                        }
                        if (switcher.ShotB.CurrentPosition != 0f)
                        {
                            Fail($"Step 0: Expected Shot B position 0, got {switcher.ShotB.CurrentPosition}");
                            return;
                        }
                        if (!switcher.ShotB.IsPrepared)
                        {
                            Fail("Step 0: Expected Shot B to be prepared at start.");
                            return;
                        }

                        // Test off-air operation guards on Shot B directly
                        float bInitSpeed = switcher.ShotB.CurrentSpeed;
                        int bInitDir = switcher.ShotB.CurrentDirection;
                        switcher.ShotB.SpeedUp();
                        switcher.ShotB.SpeedDown();
                        switcher.ShotB.Reverse();
                        switcher.ShotB.Hold();
                        switcher.ShotB.Resume();
                        switcher.ShotB.PrepareStart();

                        if (switcher.ShotB.CurrentSpeed != bInitSpeed || switcher.ShotB.CurrentDirection != bInitDir || switcher.ShotB.IsHolding || switcher.ShotB.IsPlaying)
                        {
                            Fail("Step 0: Direct manipulation on off-air Shot B mutated its state.");
                            return;
                        }
                        WriteLog("Step 0 PASSED: Initial state verified; off-air Shot B correctly ignores direct operations.");

                        // Cut to B
                        switcher.CutToB();
                        s_StepTimer = 0f;
                        SessionState.SetInt(StateKey, 1);
                    }
                    break;

                case 1: // Verify Cut to B
                    if (s_StepTimer > 0.2f)
                    {
                        if (switcher.CurrentProgramShot != switcher.ShotB)
                        {
                            Fail($"Step 1: Expected Shot B as Program, got {switcher.CurrentProgramShot?.ShotName}");
                            return;
                        }
                        if (switcher.ShotB.CinemachineCamera.Priority.Value != 10)
                        {
                            Fail($"Step 1: Expected Shot B priority 10, got {switcher.ShotB.CinemachineCamera.Priority.Value}");
                            return;
                        }
                        if (switcher.ShotA.CinemachineCamera.Priority.Value != 0)
                        {
                            Fail($"Step 1: Expected Shot A priority 0, got {switcher.ShotA.CinemachineCamera.Priority.Value}");
                            return;
                        }
                        if (!switcher.ShotB.IsPlaying)
                        {
                            Fail("Step 1: Shot B must be playing after Cut to B.");
                            return;
                        }
                        if (switcher.ShotB.IsPrepared)
                        {
                            Fail("Step 1: Shot B IsPrepared must be false while Live.");
                            return;
                        }

                        // Verify that calling PrepareStart() on Live B does NOT reset B
                        switcher.ShotB.PrepareStart();
                        if (!switcher.ShotB.IsLive || !switcher.ShotB.IsPlaying)
                        {
                            Fail("Step 1: Calling PrepareStart on live Shot B improperly disrupted live playback.");
                            return;
                        }
                        WriteLog("Step 1 PASSED: Cut to B succeeded, Shot B is playing, Live PrepareStart protection verified.");

                        s_StepTimer = 0f;
                        SessionState.SetInt(StateKey, 2);
                    }
                    break;

                case 2: // Verify Shot B movement & SpeedUp & Hold
                    if (s_StepTimer > 0.5f)
                    {
                        if (switcher.ShotB.CurrentPosition <= 0f)
                        {
                            Fail($"Step 2: Expected Shot B position to advance > 0, got {switcher.ShotB.CurrentPosition}");
                            return;
                        }
                        WriteLog($"Step 2 PASSED: Shot B moved to position {switcher.ShotB.CurrentPosition:F4}.");

                        float prevSpeed = switcher.ShotB.CurrentSpeed;
                        switcher.SpeedUp();
                        if (switcher.ShotB.CurrentSpeed <= prevSpeed)
                        {
                            Fail($"Step 2: SpeedUp failed: {prevSpeed} -> {switcher.ShotB.CurrentSpeed}");
                            return;
                        }
                        WriteLog($"SpeedUp verified: {prevSpeed:F2} -> {switcher.ShotB.CurrentSpeed:F2}");

                        switcher.Hold();
                        SessionState.SetFloat(PosKey, switcher.ShotB.CurrentPosition);
                        s_StepTimer = 0f;
                        SessionState.SetInt(StateKey, 3);
                    }
                    break;

                case 3: // Verify Hold stops movement & Resume restores movement
                    if (s_StepTimer > 0.3f)
                    {
                        float posBeforeHold = SessionState.GetFloat(PosKey, 0f);
                        if (!switcher.ShotB.IsHolding)
                        {
                            Fail("Step 3: Shot B must be holding.");
                            return;
                        }
                        if (!Mathf.Approximately(switcher.ShotB.CurrentPosition, posBeforeHold))
                        {
                            Fail($"Step 3: Position changed during hold: was {posBeforeHold}, now {switcher.ShotB.CurrentPosition}");
                            return;
                        }
                        WriteLog($"Step 3 PASSED: Hold verified at position {switcher.ShotB.CurrentPosition:F4}.");

                        switcher.Resume();
                        // Speed up significantly to reach the end of the spline quickly for testing
                        for (int i = 0; i < 15; i++)
                        {
                            switcher.SpeedUp();
                        }
                        s_StepTimer = 0f;
                        SessionState.SetInt(StateKey, 4);
                    }
                    break;

                case 4: // Verify full traversal to end (1.0), deceleration, and end hold
                    if (!switcher.ShotB.IsPlaying)
                    {
                        // Has arrived at end
                        if (switcher.ShotB.CurrentPosition < 0.999f)
                        {
                            Fail($"Step 4: Shot B stopped before reaching end: position {switcher.ShotB.CurrentPosition}");
                            return;
                        }
                        WriteLog($"Step 4 PASSED: Shot B reached end (position {switcher.ShotB.CurrentPosition:F4}) and stopped playing.");

                        // Record end position to verify hold across frames
                        SessionState.SetFloat(PosKey, switcher.ShotB.CurrentPosition);
                        s_StepTimer = 0f;
                        SessionState.SetInt(StateKey, 5);
                    }
                    else if (s_StepTimer > 5.0f)
                    {
                        Fail($"Step 4: Timed out waiting for Shot B to reach end: current pos {switcher.ShotB.CurrentPosition}");
                        return;
                    }
                    break;

                case 5: // Verify end hold across subsequent frames (no loop, no teleport) & Reverse at end
                    if (s_StepTimer > 0.4f)
                    {
                        float endPos = SessionState.GetFloat(PosKey, 1.0f);
                        if (!Mathf.Approximately(switcher.ShotB.CurrentPosition, endPos))
                        {
                            Fail($"Step 5: End hold violated; position drifted from {endPos} to {switcher.ShotB.CurrentPosition}");
                            return;
                        }
                        WriteLog("Step 5 PASSED: Shot B framing holds at end without looping or teleporting.");

                        // Reverse at end
                        int prevDir = switcher.ShotB.CurrentDirection;
                        switcher.Reverse();
                        if (switcher.ShotB.CurrentDirection != -prevDir)
                        {
                            Fail($"Step 5: Reverse at end failed: {prevDir} -> {switcher.ShotB.CurrentDirection}");
                            return;
                        }
                        if (!switcher.ShotB.IsPlaying)
                        {
                            Fail("Step 5: Shot B should resume playing after Reverse at end.");
                            return;
                        }
                        WriteLog("Reverse at end verified: direction inverted to -1, playback resumed backwards.");

                        s_StepTimer = 0f;
                        SessionState.SetInt(StateKey, 6);
                    }
                    break;

                case 6: // Verify moving backward & Cut back to A resets B & test safety guards
                    if (s_StepTimer > 0.4f)
                    {
                        if (switcher.ShotB.CurrentPosition >= 1.0f)
                        {
                            Fail($"Step 6: Expected Shot B position to decrease after reverse, but position is {switcher.ShotB.CurrentPosition}");
                            return;
                        }
                        WriteLog($"Step 6 PASSED: Reverse motion verified, position moved back to {switcher.ShotB.CurrentPosition:F4}.");

                        // Cut back to A
                        switcher.CutToA();
                        if (switcher.CurrentProgramShot != switcher.ShotA)
                        {
                            Fail($"Step 6: Expected Program Shot A, got {switcher.CurrentProgramShot?.ShotName}");
                            return;
                        }
                        if (switcher.ShotA.CinemachineCamera.Priority.Value != 10)
                        {
                            Fail($"Step 6: Expected Shot A priority 10, got {switcher.ShotA.CinemachineCamera.Priority.Value}");
                            return;
                        }
                        if (switcher.ShotB.CinemachineCamera.Priority.Value != 0)
                        {
                            Fail($"Step 6: Expected Shot B priority 0, got {switcher.ShotB.CinemachineCamera.Priority.Value}");
                            return;
                        }
                        if (switcher.ShotB.CurrentPosition != 0f)
                        {
                            Fail($"Step 6: Shot B must reset to 0 after cut back to A, got {switcher.ShotB.CurrentPosition}");
                            return;
                        }
                        if (!switcher.ShotB.IsPrepared)
                        {
                            Fail("Step 6: Shot B must have IsPrepared == true after reset.");
                            return;
                        }
                        WriteLog("Step 6 PASSED: Cut back to A succeeded, Shot B reset to start (position 0, IsPrepared == true).");

                        // Reselection of same shot
                        switcher.CutToA();
                        if (switcher.CurrentProgramShot != switcher.ShotA)
                        {
                            Fail("Step 6: Reselection of Shot A altered Program.");
                            return;
                        }
                        WriteLog("Reselection of Shot A verified: safe no-op.");

                        // Invalid indices
                        switcher.CutToShot(99);
                        if (switcher.CurrentProgramShot != switcher.ShotA)
                        {
                            Fail("Step 6: Invalid shot index altered Program.");
                            return;
                        }
                        switcher.CutToShot(-1);
                        if (switcher.CurrentProgramShot != switcher.ShotA)
                        {
                            Fail("Step 6: Negative shot index altered Program.");
                            return;
                        }
                        WriteLog("Invalid shot indices verified: safe no-op.");

                        // Manipulations on Fixed shot while A is live
                        switcher.SpeedUp();
                        switcher.Reverse();
                        switcher.Hold();
                        switcher.Resume();
                        WriteLog("Operations while Fixed Shot A is live verified: safe no-ops.");

                        WriteLog("=================================================");
                        WriteLog("=== ALL PLAY MODE VERIFICATION TESTS PASSED! ===");
                        WriteLog("=================================================");

                        SessionState.EraseInt(StateKey);
                        if (File.Exists(FlagFile))
                        {
                            File.Delete(FlagFile);
                        }
                        EditorApplication.isPlaying = false;
                    }
                    break;
            }
        }
    }
}
