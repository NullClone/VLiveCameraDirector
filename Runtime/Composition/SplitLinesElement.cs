using UnityEngine;
using UnityEngine.UIElements;

namespace VLiveKit.Camera
{
    /// <summary>
    /// Painter2Dでフレームマスク、外周線、構図ガイドを描画するUI Toolkit要素です。
    /// </summary>
    [UxmlElement]
    public partial class SplitLinesElement : VisualElement
    {
        // Fields

        private Vector4 _insets;
        private bool _letterboxEnabled = true;
        private Color _letterboxColor = Color.black;
        private float _letterboxOpacity = 0.9f;
        private bool _guidesEnabled = true;
        private SplitGuideMode _guideMode = SplitGuideMode.Thirds;
        private Color _lineColor = new Color(1f, 1f, 1f, 0.45f);
        private float _lineWidth = 1.5f;


        // Methods

        /// <summary>
        /// UI ToolkitがUXMLから要素を生成するためのコンストラクタです。
        /// </summary>
        public SplitLinesElement()
        {
            pickingMode = PickingMode.Ignore;
            generateVisualContent += OnGenerateVisualContent;
        }

        /// <summary>
        /// 次の再描画で使用する表示値を更新します。
        /// </summary>
        public void SetPresentation(
            Vector4 insets,
            bool letterboxEnabled,
            Color letterboxColor,
            float letterboxOpacity,
            bool guidesEnabled,
            SplitGuideMode guideMode,
            Color lineColor,
            float lineWidth)
        {
            if ((_insets - insets).sqrMagnitude < 0.0001f &&
                _letterboxEnabled == letterboxEnabled &&
                _letterboxColor == letterboxColor &&
                Mathf.Approximately(_letterboxOpacity, letterboxOpacity) &&
                _guidesEnabled == guidesEnabled &&
                _guideMode == guideMode &&
                _lineColor == lineColor &&
                Mathf.Approximately(_lineWidth, lineWidth))
                return;

            _insets = insets;
            _letterboxEnabled = letterboxEnabled;
            _letterboxColor = letterboxColor;
            _letterboxOpacity = Mathf.Clamp01(letterboxOpacity);
            _guidesEnabled = guidesEnabled;
            _guideMode = guideMode;
            _lineColor = lineColor;
            _lineWidth = Mathf.Max(0.5f, lineWidth);
            MarkDirtyRepaint();
        }

        private void OnGenerateVisualContent(MeshGenerationContext context)
        {
            float width = contentRect.width;
            float height = contentRect.height;
            if (width < 1f || height < 1f)
                return;

            Painter2D painter = context.painter2D;
            Rect activeRect = Rect.MinMaxRect(
                Mathf.Clamp(_insets.x, 0f, width * 0.5f),
                Mathf.Clamp(_insets.y, 0f, height * 0.5f),
                width - Mathf.Clamp(_insets.z, 0f, width * 0.5f),
                height - Mathf.Clamp(_insets.w, 0f, height * 0.5f));

            if (_letterboxEnabled && _letterboxOpacity > 0f)
                DrawFrameMask(painter, width, height, activeRect);

            if (_guidesEnabled && activeRect.width > 0f && activeRect.height > 0f)
            {
                ConfigureLinePainter(painter);
                DrawFrameBorder(painter, activeRect);
                DrawGuides(painter, activeRect);
            }
        }

        private void DrawFrameMask(Painter2D painter, float width, float height, Rect activeRect)
        {
            Color maskColor = _letterboxColor;
            maskColor.a *= _letterboxOpacity;
            painter.fillColor = maskColor;

            DrawFilledRect(painter, 0f, 0f, width, activeRect.yMin);
            DrawFilledRect(painter, 0f, activeRect.yMax, width, height - activeRect.yMax);
            DrawFilledRect(painter, 0f, activeRect.yMin, activeRect.xMin, activeRect.height);
            DrawFilledRect(painter, activeRect.xMax, activeRect.yMin, width - activeRect.xMax, activeRect.height);
        }

        private void ConfigureLinePainter(Painter2D painter)
        {
            painter.strokeColor = _lineColor;
            painter.lineWidth = _lineWidth;
            painter.lineCap = LineCap.Butt;
        }

        private void DrawFrameBorder(Painter2D painter, Rect rect)
        {
            float inset = _lineWidth * 0.5f;
            Rect borderRect = Rect.MinMaxRect(
                rect.xMin + inset,
                rect.yMin + inset,
                rect.xMax - inset,
                rect.yMax - inset);
            if (borderRect.width <= 0f || borderRect.height <= 0f)
                return;

            painter.BeginPath();
            painter.MoveTo(new Vector2(borderRect.xMin, borderRect.yMin));
            painter.LineTo(new Vector2(borderRect.xMax, borderRect.yMin));
            painter.LineTo(new Vector2(borderRect.xMax, borderRect.yMax));
            painter.LineTo(new Vector2(borderRect.xMin, borderRect.yMax));
            painter.ClosePath();
            painter.Stroke();
        }

        private void DrawGuides(Painter2D painter, Rect rect)
        {
            switch (_guideMode)
            {
                case SplitGuideMode.Symmetrical:
                    DrawVertical(painter, rect, 0.5f);
                    break;

                case SplitGuideMode.Bisection:
                    DrawVertical(painter, rect, 0.5f);
                    DrawHorizontal(painter, rect, 0.5f);
                    break;

                case SplitGuideMode.Thirds:
                case SplitGuideMode.CinemaScope:
                    DrawThirds(painter, rect);
                    break;

                case SplitGuideMode.Diagonal:
                    DrawDiagonals(painter, rect);
                    break;

                case SplitGuideMode.ThirdsAndDiagonal:
                    DrawThirds(painter, rect);
                    DrawDiagonals(painter, rect);
                    break;
            }
        }


        private static void DrawThirds(Painter2D painter, Rect rect)
        {
            DrawVertical(painter, rect, 1f / 3f);
            DrawVertical(painter, rect, 2f / 3f);
            DrawHorizontal(painter, rect, 1f / 3f);
            DrawHorizontal(painter, rect, 2f / 3f);
        }

        private static void DrawDiagonals(Painter2D painter, Rect rect)
        {
            DrawLine(painter, new Vector2(rect.xMin, rect.yMin), new Vector2(rect.xMax, rect.yMax));
            DrawLine(painter, new Vector2(rect.xMax, rect.yMin), new Vector2(rect.xMin, rect.yMax));
        }

        private static void DrawVertical(Painter2D painter, Rect rect, float normalizedPosition)
        {
            float x = Mathf.Lerp(rect.xMin, rect.xMax, normalizedPosition);
            DrawLine(painter, new Vector2(x, rect.yMin), new Vector2(x, rect.yMax));
        }

        private static void DrawHorizontal(Painter2D painter, Rect rect, float normalizedPosition)
        {
            float y = Mathf.Lerp(rect.yMin, rect.yMax, normalizedPosition);
            DrawLine(painter, new Vector2(rect.xMin, y), new Vector2(rect.xMax, y));
        }

        private static void DrawLine(Painter2D painter, Vector2 start, Vector2 end)
        {
            painter.BeginPath();
            painter.MoveTo(start);
            painter.LineTo(end);
            painter.Stroke();
        }

        private static void DrawFilledRect(Painter2D painter, float x, float y, float width, float height)
        {
            if (width <= 0f || height <= 0f)
                return;

            painter.BeginPath();
            painter.MoveTo(new Vector2(x, y));
            painter.LineTo(new Vector2(x + width, y));
            painter.LineTo(new Vector2(x + width, y + height));
            painter.LineTo(new Vector2(x, y + height));
            painter.ClosePath();
            painter.Fill();
        }
    }
}
