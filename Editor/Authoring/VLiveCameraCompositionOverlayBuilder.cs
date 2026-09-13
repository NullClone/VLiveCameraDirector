using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace VLiveKit.Camera.Editor
{
    /// <summary>
    /// 構図オーバーレイ用GameObjectとUI Documentを明示操作で構成します。
    /// </summary>
    public static class VLiveCameraCompositionOverlayBuilder
    {
        // Fields

        private const string PackageRoot = "Packages/com.toshi.vlivekit.camera-director/";
        private const string SourceRoot = "Assets/toshi.VLiveKit/VLiveCameraDirector/";
        private const string UxmlRelativePath = "Runtime/UI/Main.uxml";
        private const string PanelSettingsRelativePath = "Runtime/UI/DefaultSettings.asset";


        // Methods

        [MenuItem("GameObject/VLiveKit/Composition Overlay", false, 31)]
        private static void CreateCompositionOverlay()
        {
            VisualTreeAsset visualTree = LoadPackageAsset<VisualTreeAsset>(UxmlRelativePath);
            PanelSettings panelSettings = LoadPackageAsset<PanelSettings>(PanelSettingsRelativePath);
            if (visualTree == null || panelSettings == null)
            {
                Debug.LogError("VLive Camera Director composition overlay assets could not be loaded.");
                return;
            }

            var gameObject = new GameObject("Composition Overlay");
            Undo.RegisterCreatedObjectUndo(gameObject, "Create Composition Overlay");
            GameObjectUtility.SetParentAndAlign(gameObject, Selection.activeGameObject);

            UIDocument document = Undo.AddComponent<UIDocument>(gameObject);
            SplitLines splitLines = Undo.AddComponent<SplitLines>(gameObject);
            ConfigureDocument(document, visualTree, panelSettings);

            Selection.activeGameObject = gameObject;
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
            EditorGUIUtility.PingObject(splitLines);
        }

        /// <summary>
        /// 既存のSplitLinesに必要なUI Toolkit Assetを割り当てます。
        /// </summary>
        /// <param name="splitLines">構成するComponent。</param>
        public static void Configure(SplitLines splitLines)
        {
            if (splitLines == null)
                return;

            VisualTreeAsset visualTree = LoadPackageAsset<VisualTreeAsset>(UxmlRelativePath);
            PanelSettings panelSettings = LoadPackageAsset<PanelSettings>(PanelSettingsRelativePath);
            if (visualTree == null || panelSettings == null)
            {
                Debug.LogError("VLive Camera Director composition overlay assets could not be loaded.", splitLines);
                return;
            }

            UIDocument document = splitLines.GetComponent<UIDocument>();
            if (document == null)
                document = Undo.AddComponent<UIDocument>(splitLines.gameObject);

            ConfigureDocument(document, visualTree, panelSettings);
            EditorSceneManager.MarkSceneDirty(splitLines.gameObject.scene);
        }

        private static void ConfigureDocument(
            UIDocument document,
            VisualTreeAsset visualTree,
            PanelSettings panelSettings)
        {
            Undo.RecordObject(document, "Configure Composition Overlay");
            document.panelSettings = panelSettings;
            document.visualTreeAsset = visualTree;
            document.sortingOrder = 1000;
            EditorUtility.SetDirty(document);
        }

        private static T LoadPackageAsset<T>(string relativePath) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(PackageRoot + relativePath);
            return asset != null
                ? asset
                : AssetDatabase.LoadAssetAtPath<T>(SourceRoot + relativePath);
        }
    }
}
