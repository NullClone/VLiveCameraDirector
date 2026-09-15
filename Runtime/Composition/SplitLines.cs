using System.Collections.Generic;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.UIElements;
using AppButton = Unity.AppUI.UI.Button;
using AppDropdown = Unity.AppUI.UI.Dropdown;
using AppIconButton = Unity.AppUI.UI.IconButton;
using AppPanel = Unity.AppUI.UI.Panel;
using AppToggle = Unity.AppUI.UI.Toggle;

namespace VLiveKit.Camera
{
    /// <summary>
    /// Game Viewへ構図ガイド、アスペクトマスク、ランタイム設定パネルを表示します。
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class SplitLines : MonoBehaviour
    {
        // Fields

        private const float MinAspectRatio = 0.5f;
        private const float MaxAspectRatio = 4f;
        private const float ClosedDrawerPadding = 24f;


        [SerializeField]
        [HideInInspector]
        private VisualTreeAsset _visualTree;

        [SerializeField]
        [HideInInspector]
        private PanelSettings _panelSettings;

        [Tooltip("指定したアスペクト比の外側へフレームマスクを表示するか。")]
        [SerializeField]
        private bool _letterboxEnabled = true;

        [Tooltip("フレームマスク内側のアスペクト比。CameraやCinemachineのアスペクト比は変更しません。")]
        [Range(MinAspectRatio, MaxAspectRatio)]
        [SerializeField]
        private float _aspectRatio = 2.35f;

        [Tooltip("フレームマスクの色。透明度はOpacityで個別に調整します。")]
        [SerializeField]
        private Color _letterboxColor = Color.black;

        [Tooltip("フレームマスクの透明度。")]
        [Range(0f, 1f)]
        [SerializeField]
        private float _letterboxOpacity = 0.9f;

        [Tooltip("再生開始時に通常画面からフレームマスクを動かして表示するか。")]
        [SerializeField]
        private bool _animateOnStart = true;

        [Tooltip("アスペクト比とフレームマスクが目標サイズへ追従する時間。")]
        [Range(0.01f, 1f)]
        [SerializeField]
        private float _frameResponseTime = 0.22f;

        [Header("Composition Guides")]
        [Tooltip("表示する構図ガイドの種類。")]
        [SerializeField]
        private SplitGuideMode _guideMode = SplitGuideMode.Thirds;

        [Tooltip("有効フレームの外周線と構図ガイドを表示するか。")]
        [SerializeField]
        private bool _drawGuides = true;

        [Tooltip("有効フレームの外周線と構図ガイドの色と透明度。")]
        [SerializeField]
        private Color _lineColor = new Color(1f, 1f, 1f, 0.45f);

        [Tooltip("有効フレームの外周線と構図ガイドの線幅。単位はピクセルです。")]
        [Range(0.5f, 10f)]
        [SerializeField]
        private float _lineWidth = 1.5f;

        [Header("Runtime Panel")]
        [Tooltip("再生開始時から右側の設定パネルを開いておくか。")]
        [SerializeField]
        private bool _showSettingsOnStart;

        [Tooltip("右側の設定パネルが開閉する追従時間。")]
        [Range(0.01f, 1f)]
        [SerializeField]
        private float _settingsResponseTime = 0.16f;

        [Tooltip("Game View設定パネルへ適用するApp UI標準テーマ。")]
        [SerializeField]
        private CompositionPanelTheme _panelTheme = CompositionPanelTheme.Dark;

        [Tooltip("Game View設定パネルへ適用するApp UI標準スケール。")]
        [SerializeField]
        private CompositionPanelScale _panelScale = CompositionPanelScale.Medium;


        private UIDocument _uiDocument;
        private AppPanel _appUiPanel;
        private SplitLinesElement _overlay;
        private VisualElement _runtimeControls;
        private VisualElement _settingsDrawer;
        private AppIconButton _openButton;
        private AppIconButton _closeButton;
        private AppButton _resetButton;
        private AppButton _presetFourThreeButton;
        private AppButton _presetSixteenNineButton;
        private AppButton _presetOneEightyFiveButton;
        private AppButton _presetTwoButton;
        private AppButton _presetTwoThirtyFiveButton;
        private AppButton _presetTwoThirtyNineButton;
        private AppButton _themeDarkButton;
        private AppButton _themeLightButton;
        private AppButton _themeEditorDarkButton;
        private AppButton _themeEditorLightButton;
        private AppButton _scaleSmallButton;
        private AppButton _scaleMediumButton;
        private AppButton _scaleLargeButton;
        private AppToggle _letterboxToggle;
        private AppToggle _guidesToggle;
        private AppDropdown _guideModeDropdown;
        private SliderFloat _aspectSlider;
        private SliderFloat _opacitySlider;
        private SliderFloat _frameResponseSlider;
        private SliderFloat _lineWidthSlider;
        private ColorField _letterboxColorField;
        private ColorField _lineColorField;

        private bool _uiBound;
        private bool _runtimeInitialized;
        private bool _settingsOpen;
        private float _runtimeAspectRatio;
        private float _runtimeOpacity;
        private float _runtimeFrameResponseTime;
        private float _runtimeLineWidth;
        private Color _runtimeLetterboxColor;
        private Color _runtimeLineColor;
        private bool _runtimeLetterboxEnabled;
        private bool _runtimeGuidesEnabled;
        private SplitGuideMode _runtimeGuideMode;
        private CompositionPanelTheme _runtimePanelTheme;
        private CompositionPanelScale _runtimePanelScale;
        private Vector4 _currentInsets;
        private Vector4 _insetVelocity;
        private float _drawerOffset;
        private float _drawerVelocity;


        private static readonly string[] GuideModeLabels =
        {
            "Symmetrical",
            "Bisection",
            "Thirds",
            "Diagonal",
            "Thirds + Diagonal",
            "CinemaScope"
        };


        // Methods

        /// <summary>
        /// 現在のランタイム設定をInspectorで指定した初期値へ戻します。
        /// </summary>
        public void ResetRuntimeSettings()
        {
            CopyDefaultsToRuntime();
            PushRuntimeValuesToControls();
        }

        /// <summary>
        /// ランタイム設定パネルを開閉します。
        /// </summary>
        /// <param name="open">開く場合はtrue。</param>
        public void SetSettingsOpen(bool open)
        {
            _settingsOpen = open;
            if (_settingsDrawer != null)
                _settingsDrawer.style.display = DisplayStyle.Flex;
        }


        private void Reset()
        {
            _uiDocument = GetComponent<UIDocument>();
            _uiDocument.panelSettings = _panelSettings;
            _uiDocument.visualTreeAsset = _visualTree;
            _uiDocument.sortingOrder = 1000;
        }

        private void OnEnable()
        {
            _uiDocument ??= GetComponent<UIDocument>();
            _uiBound = false;
            _runtimeInitialized = false;

            TryBindUi();
        }

        private void Start()
        {
            InitializeRuntimeState();
        }

        private void OnDisable()
        {
            UnbindUi();
        }

        private void Update()
        {
            if (!TryBindUi())
                return;

            if (!_runtimeInitialized)
                InitializeRuntimeState();

            UpdateFrame(Time.unscaledDeltaTime);
            UpdateDrawer(Time.unscaledDeltaTime);
        }

        private bool TryBindUi()
        {
            if (_uiBound)
                return true;

            if (_uiDocument == null)
                _uiDocument = GetComponent<UIDocument>();

            VisualElement root = _uiDocument != null ? _uiDocument.rootVisualElement : null;
            if (root == null)
                return false;

            _appUiPanel = root.Q<AppPanel>("composition-panel");
            _overlay = root.Q<SplitLinesElement>("composition-overlay");
            _runtimeControls = root.Q<VisualElement>("runtime-controls");
            _settingsDrawer = root.Q<VisualElement>("settings-drawer");
            _openButton = root.Q<AppIconButton>("settings-open-button");
            _closeButton = root.Q<AppIconButton>("settings-close-button");
            _resetButton = root.Q<AppButton>("reset-button");
            _presetFourThreeButton = root.Q<AppButton>("preset-4-3");
            _presetSixteenNineButton = root.Q<AppButton>("preset-16-9");
            _presetOneEightyFiveButton = root.Q<AppButton>("preset-1-85");
            _presetTwoButton = root.Q<AppButton>("preset-2-00");
            _presetTwoThirtyFiveButton = root.Q<AppButton>("preset-2-35");
            _presetTwoThirtyNineButton = root.Q<AppButton>("preset-2-39");
            _themeDarkButton = root.Q<AppButton>("theme-dark");
            _themeLightButton = root.Q<AppButton>("theme-light");
            _themeEditorDarkButton = root.Q<AppButton>("theme-editor-dark");
            _themeEditorLightButton = root.Q<AppButton>("theme-editor-light");
            _scaleSmallButton = root.Q<AppButton>("scale-small");
            _scaleMediumButton = root.Q<AppButton>("scale-medium");
            _scaleLargeButton = root.Q<AppButton>("scale-large");
            _letterboxToggle = root.Q<AppToggle>("letterbox-toggle");
            _guidesToggle = root.Q<AppToggle>("guides-toggle");
            _guideModeDropdown = root.Q<AppDropdown>("guide-mode-dropdown");
            _aspectSlider = root.Q<SliderFloat>("aspect-slider");
            _opacitySlider = root.Q<SliderFloat>("opacity-slider");
            _frameResponseSlider = root.Q<SliderFloat>("frame-response-slider");
            _lineWidthSlider = root.Q<SliderFloat>("line-width-slider");
            _letterboxColorField = root.Q<ColorField>("letterbox-color-field");
            _lineColorField = root.Q<ColorField>("line-color-field");

            if (_appUiPanel == null || _overlay == null || _runtimeControls == null || _settingsDrawer == null ||
                _openButton == null || _closeButton == null || _resetButton == null ||
                _presetFourThreeButton == null || _presetSixteenNineButton == null ||
                _presetOneEightyFiveButton == null || _presetTwoButton == null ||
                _presetTwoThirtyFiveButton == null || _presetTwoThirtyNineButton == null ||
                _themeDarkButton == null || _themeLightButton == null ||
                _themeEditorDarkButton == null || _themeEditorLightButton == null ||
                _scaleSmallButton == null || _scaleMediumButton == null || _scaleLargeButton == null ||
                _letterboxToggle == null || _guidesToggle == null || _guideModeDropdown == null ||
                _aspectSlider == null ||
                _opacitySlider == null || _frameResponseSlider == null ||
                _lineWidthSlider == null || _letterboxColorField == null || _lineColorField == null)
                return false;

            _guideModeDropdown.bindItem = BindGuideModeItem;
            _guideModeDropdown.sourceItems = GuideModeLabels;

            _openButton.clickable.clicked += OpenSettings;
            _closeButton.clickable.clicked += CloseSettings;
            _resetButton.clickable.clicked += ResetRuntimeSettings;
            _presetFourThreeButton.clickable.clicked += SetFourThree;
            _presetSixteenNineButton.clickable.clicked += SetSixteenNine;
            _presetOneEightyFiveButton.clickable.clicked += SetOneEightyFive;
            _presetTwoButton.clickable.clicked += SetTwo;
            _presetTwoThirtyFiveButton.clickable.clicked += SetTwoThirtyFive;
            _presetTwoThirtyNineButton.clickable.clicked += SetTwoThirtyNine;
            _themeDarkButton.clickable.clicked += SetDarkTheme;
            _themeLightButton.clickable.clicked += SetLightTheme;
            _themeEditorDarkButton.clickable.clicked += SetEditorDarkTheme;
            _themeEditorLightButton.clickable.clicked += SetEditorLightTheme;
            _scaleSmallButton.clickable.clicked += SetSmallScale;
            _scaleMediumButton.clickable.clicked += SetMediumScale;
            _scaleLargeButton.clickable.clicked += SetLargeScale;
            _letterboxToggle.RegisterValueChangedCallback(OnLetterboxChanged);
            _guidesToggle.RegisterValueChangedCallback(OnGuidesChanged);
            _guideModeDropdown.RegisterValueChangedCallback(OnGuideModeChanged);
            RegisterSliderCallbacks();
            _letterboxColorField.RegisterValueChangingCallback(OnLetterboxColorChanging);
            _letterboxColorField.RegisterValueChangedCallback(OnLetterboxColorChanged);
            _lineColorField.RegisterValueChangingCallback(OnLineColorChanging);
            _lineColorField.RegisterValueChangedCallback(OnLineColorChanged);

            _runtimeControls.style.display = Application.isPlaying ? DisplayStyle.Flex : DisplayStyle.None;
            _uiBound = true;
            PushRuntimeValuesToControls();
            return true;
        }

        private void RegisterSliderCallbacks()
        {
            _aspectSlider.RegisterValueChangingCallback(OnAspectChanging);
            _aspectSlider.RegisterValueChangedCallback(OnAspectChanged);
            _opacitySlider.RegisterValueChangingCallback(OnOpacityChanging);
            _opacitySlider.RegisterValueChangedCallback(OnOpacityChanged);
            _frameResponseSlider.RegisterValueChangingCallback(OnFrameResponseChanging);
            _frameResponseSlider.RegisterValueChangedCallback(OnFrameResponseChanged);
            _lineWidthSlider.RegisterValueChangingCallback(OnLineWidthChanging);
            _lineWidthSlider.RegisterValueChangedCallback(OnLineWidthChanged);
        }

        private void UnbindUi()
        {
            if (!_uiBound)
                return;

            _openButton.clickable.clicked -= OpenSettings;
            _closeButton.clickable.clicked -= CloseSettings;
            _resetButton.clickable.clicked -= ResetRuntimeSettings;
            _presetFourThreeButton.clickable.clicked -= SetFourThree;
            _presetSixteenNineButton.clickable.clicked -= SetSixteenNine;
            _presetOneEightyFiveButton.clickable.clicked -= SetOneEightyFive;
            _presetTwoButton.clickable.clicked -= SetTwo;
            _presetTwoThirtyFiveButton.clickable.clicked -= SetTwoThirtyFive;
            _presetTwoThirtyNineButton.clickable.clicked -= SetTwoThirtyNine;
            _themeDarkButton.clickable.clicked -= SetDarkTheme;
            _themeLightButton.clickable.clicked -= SetLightTheme;
            _themeEditorDarkButton.clickable.clicked -= SetEditorDarkTheme;
            _themeEditorLightButton.clickable.clicked -= SetEditorLightTheme;
            _scaleSmallButton.clickable.clicked -= SetSmallScale;
            _scaleMediumButton.clickable.clicked -= SetMediumScale;
            _scaleLargeButton.clickable.clicked -= SetLargeScale;
            _letterboxToggle.UnregisterValueChangedCallback(OnLetterboxChanged);
            _guidesToggle.UnregisterValueChangedCallback(OnGuidesChanged);
            _guideModeDropdown.UnregisterValueChangedCallback(OnGuideModeChanged);
            _guideModeDropdown.bindItem = null;
            _guideModeDropdown.sourceItems = null;
            UnregisterSliderCallbacks();
            _letterboxColorField.UnregisterValueChangingCallback(OnLetterboxColorChanging);
            _letterboxColorField.UnregisterValueChangedCallback(OnLetterboxColorChanged);
            _lineColorField.UnregisterValueChangingCallback(OnLineColorChanging);
            _lineColorField.UnregisterValueChangedCallback(OnLineColorChanged);
            _uiBound = false;
        }

        private void UnregisterSliderCallbacks()
        {
            _aspectSlider.UnregisterValueChangingCallback(OnAspectChanging);
            _aspectSlider.UnregisterValueChangedCallback(OnAspectChanged);
            _opacitySlider.UnregisterValueChangingCallback(OnOpacityChanging);
            _opacitySlider.UnregisterValueChangedCallback(OnOpacityChanged);
            _frameResponseSlider.UnregisterValueChangingCallback(OnFrameResponseChanging);
            _frameResponseSlider.UnregisterValueChangedCallback(OnFrameResponseChanged);
            _lineWidthSlider.UnregisterValueChangingCallback(OnLineWidthChanging);
            _lineWidthSlider.UnregisterValueChangedCallback(OnLineWidthChanged);
        }

        private void InitializeRuntimeState()
        {
            CopyDefaultsToRuntime();
            _settingsOpen = _showSettingsOnStart;
            _drawerVelocity = 0f;

            Vector4 targetInsets = CalculateTargetInsets();
            _currentInsets = Application.isPlaying && _animateOnStart ? Vector4.zero : targetInsets;
            _insetVelocity = Vector4.zero;

            float closedOffset = GetClosedDrawerOffset();
            _drawerOffset = closedOffset;
            if (_settingsDrawer != null)
            {
                _settingsDrawer.style.display = _settingsOpen ? DisplayStyle.Flex : DisplayStyle.None;
                _settingsDrawer.style.translate = new Translate(_drawerOffset, 0f);
            }

            _runtimeInitialized = true;
            PushRuntimeValuesToControls();
        }

        private void CopyDefaultsToRuntime()
        {
            SanitizeSerializedValues();
            _runtimeLetterboxEnabled = _letterboxEnabled;
            _runtimeAspectRatio = _aspectRatio;
            _runtimeLetterboxColor = _letterboxColor;
            _runtimeOpacity = _letterboxOpacity;
            _runtimeFrameResponseTime = _frameResponseTime;
            _runtimeGuidesEnabled = _drawGuides;
            _runtimeGuideMode = _guideMode;
            _runtimeLineColor = _lineColor;
            _runtimeLineWidth = _lineWidth;
            _runtimePanelTheme = _panelTheme;
            _runtimePanelScale = _panelScale;
        }

        private void UpdateFrame(float deltaTime)
        {
            Vector4 targetInsets = CalculateTargetInsets();
            if (Application.isPlaying)
            {
                float smoothTime = Mathf.Max(0.01f, _runtimeFrameResponseTime);
                _currentInsets.x = Mathf.SmoothDamp(_currentInsets.x, targetInsets.x, ref _insetVelocity.x, smoothTime, Mathf.Infinity, deltaTime);
                _currentInsets.y = Mathf.SmoothDamp(_currentInsets.y, targetInsets.y, ref _insetVelocity.y, smoothTime, Mathf.Infinity, deltaTime);
                _currentInsets.z = Mathf.SmoothDamp(_currentInsets.z, targetInsets.z, ref _insetVelocity.z, smoothTime, Mathf.Infinity, deltaTime);
                _currentInsets.w = Mathf.SmoothDamp(_currentInsets.w, targetInsets.w, ref _insetVelocity.w, smoothTime, Mathf.Infinity, deltaTime);
            }
            else
            {
                _currentInsets = targetInsets;
                _insetVelocity = Vector4.zero;
            }

            bool drawFrameMask = _runtimeLetterboxEnabled || _currentInsets.sqrMagnitude > 0.01f;
            _overlay.SetPresentation(
                _currentInsets,
                drawFrameMask,
                _runtimeLetterboxColor,
                _runtimeOpacity,
                _runtimeGuidesEnabled,
                _runtimeGuideMode,
                _runtimeLineColor,
                _runtimeLineWidth);
        }

        private Vector4 CalculateTargetInsets()
        {
            if (_overlay == null || !_runtimeLetterboxEnabled)
                return Vector4.zero;

            Rect rect = _overlay.contentRect;
            if (rect.width <= 0f || rect.height <= 0f)
                return Vector4.zero;

            float targetAspect = Mathf.Clamp(_runtimeAspectRatio, MinAspectRatio, MaxAspectRatio);
            float viewportAspect = rect.width / rect.height;
            if (targetAspect >= viewportAspect)
            {
                float imageHeight = rect.width / targetAspect;
                float inset = Mathf.Max(0f, (rect.height - imageHeight) * 0.5f);
                return new Vector4(0f, inset, 0f, inset);
            }

            float imageWidth = rect.height * targetAspect;
            float horizontalInset = Mathf.Max(0f, (rect.width - imageWidth) * 0.5f);
            return new Vector4(horizontalInset, 0f, horizontalInset, 0f);
        }

        private void UpdateDrawer(float deltaTime)
        {
            if (!Application.isPlaying || _settingsDrawer == null)
                return;

            float closedOffset = GetClosedDrawerOffset();
            float targetOffset = _settingsOpen ? 0f : closedOffset;
            if (_settingsOpen)
                _settingsDrawer.style.display = DisplayStyle.Flex;

            _drawerOffset = Mathf.SmoothDamp(
                _drawerOffset,
                targetOffset,
                ref _drawerVelocity,
                Mathf.Max(0.01f, _settingsResponseTime),
                Mathf.Infinity,
                deltaTime);
            _settingsDrawer.style.translate = new Translate(_drawerOffset, 0f);

            if (!_settingsOpen && Mathf.Abs(_drawerOffset - closedOffset) < 0.5f)
                _settingsDrawer.style.display = DisplayStyle.None;
        }

        private float GetClosedDrawerOffset()
        {
            if (_settingsDrawer == null)
                return 384f;

            float width = _settingsDrawer.resolvedStyle.width;
            if (float.IsNaN(width) || float.IsInfinity(width) || width <= 0f)
                width = 360f;
            return width + ClosedDrawerPadding;
        }

        private void PushRuntimeValuesToControls()
        {
            if (!_uiBound)
                return;

            _letterboxToggle.SetValueWithoutNotify(_runtimeLetterboxEnabled);
            _guidesToggle.SetValueWithoutNotify(_runtimeGuidesEnabled);
            _guideModeDropdown.SetEnabled(_runtimeGuidesEnabled);
            _guideModeDropdown.SetValueWithoutNotify(new[] { (int)_runtimeGuideMode });
            _aspectSlider.SetValueWithoutNotify(_runtimeAspectRatio);
            _opacitySlider.SetValueWithoutNotify(_runtimeOpacity);
            _frameResponseSlider.SetValueWithoutNotify(_runtimeFrameResponseTime);
            _lineWidthSlider.SetValueWithoutNotify(_runtimeLineWidth);
            _letterboxColorField.SetValueWithoutNotify(_runtimeLetterboxColor);
            _lineColorField.SetValueWithoutNotify(_runtimeLineColor);
            ApplyPanelPresentation();
        }


        private void OpenSettings() => SetSettingsOpen(true);
        private void CloseSettings() => SetSettingsOpen(false);
        private void SetFourThree() => SetAspectRatio(4f / 3f);
        private void SetSixteenNine() => SetAspectRatio(16f / 9f);
        private void SetOneEightyFive() => SetAspectRatio(1.85f);
        private void SetTwo() => SetAspectRatio(2f);
        private void SetTwoThirtyFive() => SetAspectRatio(2.35f);
        private void SetTwoThirtyNine() => SetAspectRatio(2.39f);
        private void SetDarkTheme() => SetPanelTheme(CompositionPanelTheme.Dark);
        private void SetLightTheme() => SetPanelTheme(CompositionPanelTheme.Light);
        private void SetEditorDarkTheme() => SetPanelTheme(CompositionPanelTheme.EditorDark);
        private void SetEditorLightTheme() => SetPanelTheme(CompositionPanelTheme.EditorLight);
        private void SetSmallScale() => SetPanelScale(CompositionPanelScale.Small);
        private void SetMediumScale() => SetPanelScale(CompositionPanelScale.Medium);
        private void SetLargeScale() => SetPanelScale(CompositionPanelScale.Large);

        private void SetAspectRatio(float value)
        {
            _runtimeAspectRatio = Mathf.Clamp(value, MinAspectRatio, MaxAspectRatio);
            if (_aspectSlider != null)
                _aspectSlider.SetValueWithoutNotify(_runtimeAspectRatio);
        }

        private void SetPanelTheme(CompositionPanelTheme theme)
        {
            _runtimePanelTheme = theme;
            ApplyPanelPresentation();
        }

        private void SetPanelScale(CompositionPanelScale scale)
        {
            _runtimePanelScale = scale;
            ApplyPanelPresentation();
        }

        private static void BindGuideModeItem(DropdownItem item, int index)
        {
            if (index >= 0 && index < GuideModeLabels.Length)
                item.label = GuideModeLabels[index];
        }

        private void ApplyPanelPresentation()
        {
            if (_appUiPanel == null)
                return;

            _appUiPanel.theme = _runtimePanelTheme switch
            {
                CompositionPanelTheme.Light => "light",
                CompositionPanelTheme.EditorDark => "editor-dark",
                CompositionPanelTheme.EditorLight => "editor-light",
                _ => "dark"
            };
            _appUiPanel.scale = _runtimePanelScale switch
            {
                CompositionPanelScale.Small => "small",
                CompositionPanelScale.Large => "large",
                _ => "medium"
            };

            _themeDarkButton.variant = _runtimePanelTheme == CompositionPanelTheme.Dark ? ButtonVariant.Accent : ButtonVariant.Default;
            _themeLightButton.variant = _runtimePanelTheme == CompositionPanelTheme.Light ? ButtonVariant.Accent : ButtonVariant.Default;
            _themeEditorDarkButton.variant = _runtimePanelTheme == CompositionPanelTheme.EditorDark ? ButtonVariant.Accent : ButtonVariant.Default;
            _themeEditorLightButton.variant = _runtimePanelTheme == CompositionPanelTheme.EditorLight ? ButtonVariant.Accent : ButtonVariant.Default;
            _scaleSmallButton.variant = _runtimePanelScale == CompositionPanelScale.Small ? ButtonVariant.Accent : ButtonVariant.Default;
            _scaleMediumButton.variant = _runtimePanelScale == CompositionPanelScale.Medium ? ButtonVariant.Accent : ButtonVariant.Default;
            _scaleLargeButton.variant = _runtimePanelScale == CompositionPanelScale.Large ? ButtonVariant.Accent : ButtonVariant.Default;
        }

        private void OnLetterboxChanged(ChangeEvent<bool> evt) => _runtimeLetterboxEnabled = evt.newValue;

        private void OnGuidesChanged(ChangeEvent<bool> evt)
        {
            _runtimeGuidesEnabled = evt.newValue;
            _guideModeDropdown.SetEnabled(evt.newValue);
        }

        private void OnGuideModeChanged(ChangeEvent<IEnumerable<int>> evt)
        {
            int index = _guideModeDropdown.selectedIndex;
            if (index >= 0 && index < GuideModeLabels.Length)
                _runtimeGuideMode = (SplitGuideMode)index;
        }

        private void OnAspectChanging(ChangingEvent<float> evt) => SetAspectRatio(evt.newValue);
        private void OnAspectChanged(ChangeEvent<float> evt) => SetAspectRatio(evt.newValue);
        private void OnOpacityChanging(ChangingEvent<float> evt) => _runtimeOpacity = Mathf.Clamp01(evt.newValue);
        private void OnOpacityChanged(ChangeEvent<float> evt) => _runtimeOpacity = Mathf.Clamp01(evt.newValue);
        private void OnFrameResponseChanging(ChangingEvent<float> evt) => _runtimeFrameResponseTime = Mathf.Clamp(evt.newValue, 0.01f, 1f);
        private void OnFrameResponseChanged(ChangeEvent<float> evt) => _runtimeFrameResponseTime = Mathf.Clamp(evt.newValue, 0.01f, 1f);
        private void OnLineWidthChanging(ChangingEvent<float> evt) => _runtimeLineWidth = Mathf.Clamp(evt.newValue, 0.5f, 10f);
        private void OnLineWidthChanged(ChangeEvent<float> evt) => _runtimeLineWidth = Mathf.Clamp(evt.newValue, 0.5f, 10f);
        private void OnLetterboxColorChanging(ChangingEvent<Color> evt) => _runtimeLetterboxColor = evt.newValue;
        private void OnLetterboxColorChanged(ChangeEvent<Color> evt) => _runtimeLetterboxColor = evt.newValue;
        private void OnLineColorChanging(ChangingEvent<Color> evt) => _runtimeLineColor = evt.newValue;
        private void OnLineColorChanged(ChangeEvent<Color> evt) => _runtimeLineColor = evt.newValue;

        private void SanitizeSerializedValues()
        {
            _aspectRatio = Mathf.Clamp(_aspectRatio, MinAspectRatio, MaxAspectRatio);
            _letterboxOpacity = Mathf.Clamp01(_letterboxOpacity);
            _frameResponseTime = Mathf.Clamp(_frameResponseTime, 0.01f, 1f);
            _lineWidth = Mathf.Clamp(_lineWidth, 0.5f, 10f);
            _settingsResponseTime = Mathf.Clamp(_settingsResponseTime, 0.01f, 1f);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            SanitizeSerializedValues();
            if (!Application.isPlaying)
            {
                CopyDefaultsToRuntime();
                PushRuntimeValuesToControls();
            }
        }
#endif
    }

    /// <summary>
    /// Game Viewへ表示する構図ガイドの種類です。
    /// </summary>
    public enum SplitGuideMode
    {
        Symmetrical,
        Bisection,
        Thirds,
        Diagonal,
        ThirdsAndDiagonal,
        CinemaScope
    }

    /// <summary>
    /// Game View設定パネルへ適用するApp UI標準テーマです。
    /// </summary>
    public enum CompositionPanelTheme
    {
        Dark,
        Light,
        EditorDark,
        EditorLight
    }

    /// <summary>
    /// Game View設定パネルへ適用するApp UI標準スケールです。
    /// </summary>
    public enum CompositionPanelScale
    {
        Small,
        Medium,
        Large
    }
}
