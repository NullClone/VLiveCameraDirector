using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;
using Unity.Cinemachine;
using toshi.VLiveKit.Camera;

namespace toshi.VLiveKit.Camera.Editor
{
    public static class VLiveCameraABTestSceneBuilder
    {
        private const string ScenePath = "Assets/toshi.VLiveKit/VLiveCameraUnit/Tests/VLiveCameraABTest.unity";

        [MenuItem("Tools/VLiveCameraUnit/Create A-B Test Scene")]
        public static void CreateTestScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Directional Light
            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // 2. Stage Floor
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Stage Floor";
            floor.transform.position = Vector3.zero;
            floor.transform.localScale = new Vector3(2f, 1f, 2f);

            // 3. Performer Target
            var targetRoot = new GameObject("Target");
            targetRoot.transform.position = new Vector3(0f, 1f, 0f);

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "PerformerBody";
            body.transform.SetParent(targetRoot.transform, false);
            body.transform.localPosition = Vector3.zero;

            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(targetRoot.transform, false);
            head.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            head.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

            // 4. Spline Path for Shot B
            var splineGo = new GameObject("SplinePath_ShotB");
            var splineContainer = splineGo.AddComponent<SplineContainer>();
            var spline = splineContainer.Spline;
            spline.Clear();
            spline.Add(new BezierKnot((float3)new Vector3(-4f, 1.2f, -3f)));
            spline.Add(new BezierKnot((float3)new Vector3(0f, 1.8f, -4.5f)));
            spline.Add(new BezierKnot((float3)new Vector3(3.5f, 2.2f, -2.5f)));

            // 5. Main Camera with CinemachineBrain
            var mainCameraGo = new GameObject("Main Camera");
            mainCameraGo.tag = "MainCamera";
            mainCameraGo.AddComponent<UnityEngine.Camera>();
            var brain = mainCameraGo.AddComponent<CinemachineBrain>();
            brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f);
            mainCameraGo.AddComponent<AudioListener>();

            // 6. Shot A (Fixed)
            var shotAGo = new GameObject("Shot A (Fixed)");
            shotAGo.transform.position = new Vector3(0f, 1.5f, -5f);
            shotAGo.transform.rotation = Quaternion.Euler(5f, 0f, 0f);
            var cmCamA = shotAGo.AddComponent<CinemachineCamera>();
            cmCamA.Target.TrackingTarget = targetRoot.transform;
            cmCamA.Target.LookAtTarget = targetRoot.transform;
            cmCamA.Priority = 10;
            var composerA = shotAGo.AddComponent<CinemachineRotationComposer>();
            composerA.TargetOffset = new Vector3(0f, 0.5f, 0f);

            var shotA = shotAGo.AddComponent<VLiveCameraShot>();
            var soA = new SerializedObject(shotA);
            soA.FindProperty("_shotName").stringValue = "Shot A";
            soA.FindProperty("_shotType").enumValueIndex = (int)VLiveCameraShot.ShotType.Fixed;
            soA.FindProperty("_cinemachineCamera").objectReferenceValue = cmCamA;
            soA.ApplyModifiedPropertiesWithoutUndo();

            // 7. Shot B (Spline)
            var shotBGo = new GameObject("Shot B (Spline)");
            var cmCamB = shotBGo.AddComponent<CinemachineCamera>();
            cmCamB.Target.TrackingTarget = targetRoot.transform;
            cmCamB.Target.LookAtTarget = targetRoot.transform;
            cmCamB.Priority = 0;

            var dolly = shotBGo.AddComponent<CinemachineSplineDolly>();
            dolly.Spline = splineContainer;
            dolly.PositionUnits = PathIndexUnit.Normalized;
            dolly.CameraPosition = 0f;

            var composerB = shotBGo.AddComponent<CinemachineRotationComposer>();
            composerB.TargetOffset = new Vector3(0f, 0.5f, 0f);

            var shotB = shotBGo.AddComponent<VLiveCameraShot>();
            var soB = new SerializedObject(shotB);
            soB.FindProperty("_shotName").stringValue = "Shot B";
            soB.FindProperty("_shotType").enumValueIndex = (int)VLiveCameraShot.ShotType.Spline;
            soB.FindProperty("_cinemachineCamera").objectReferenceValue = cmCamB;
            soB.FindProperty("_splineDolly").objectReferenceValue = dolly;
            soB.FindProperty("_initialSpeed").floatValue = 0.2f;
            soB.FindProperty("_minSpeed").floatValue = 0.05f;
            soB.FindProperty("_maxSpeed").floatValue = 1.0f;
            soB.FindProperty("_speedStep").floatValue = 0.05f;
            soB.FindProperty("_initialDirection").intValue = 1;
            soB.FindProperty("_startPosition").floatValue = 0f;
            soB.FindProperty("_endPosition").floatValue = 1f;
            soB.FindProperty("_decelerationDistance").floatValue = 0.25f;
            soB.ApplyModifiedPropertiesWithoutUndo();

            // 8. Switcher & Keyboard Input
            var switcherGo = new GameObject("VLiveCameraSwitcher");
            var switcher = switcherGo.AddComponent<VLiveCameraSwitcher>();
            var keyboardInput = switcherGo.AddComponent<VLiveCameraKeyboardInput>();

            var soSwitcher = new SerializedObject(switcher);
            soSwitcher.FindProperty("_cinemachineBrain").objectReferenceValue = brain;
            soSwitcher.FindProperty("_shotA").objectReferenceValue = shotA;
            soSwitcher.FindProperty("_shotB").objectReferenceValue = shotB;
            soSwitcher.ApplyModifiedPropertiesWithoutUndo();

            var soInput = new SerializedObject(keyboardInput);
            soInput.FindProperty("_switcher").objectReferenceValue = switcher;
            soInput.ApplyModifiedPropertiesWithoutUndo();

            // Save scene
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[VLiveCameraABTestSceneBuilder] Created test scene at {ScenePath}");
        }
    }
}
