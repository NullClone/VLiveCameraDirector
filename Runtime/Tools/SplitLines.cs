using UnityEngine;
using UnityEngine.Serialization;

namespace VLiveKit.Camera
{
    /// <summary>
    /// Game Viewへ構図ガイドと任意のレターボックスを低負荷で描画します。
    /// </summary>
    [ExecuteAlways]
    public class SplitLines : MonoBehaviour
    {
        // Fields

        [Tooltip("表示する構図ガイドの種類。")]
        [FormerlySerializedAs("m_splitMode")]
        [SerializeField]
        private SplitGuideMode _guideMode = SplitGuideMode.Thirds;

        [Tooltip("構図ガイドを表示するか。")]
        [FormerlySerializedAs("m_isDraw")]
        [SerializeField]
        private bool _drawGuides = true;

        [Tooltip("構図ガイドの色と透明度。")]
        [FormerlySerializedAs("m_lineColor")]
        [SerializeField]
        private Color _lineColor = new Color(1f, 1f, 1f, 0.45f);

        [Tooltip("構図ガイドの線幅。単位はピクセルです。")]
        [FormerlySerializedAs("m_lineWidth")]
        [Range(0.5f, 10f)]
        [SerializeField]
        private float _lineWidth = 1.5f;

        [Tooltip("上下のレターボックスを表示するか。")]
        [FormerlySerializedAs("m_enableLetterbox")]
        [SerializeField]
        private bool _letterboxEnabled;

        [Tooltip("レターボックスの色と透明度。")]
        [FormerlySerializedAs("m_letterboxColor")]
        [SerializeField]
        private Color _letterboxColor = Color.black;

        [Tooltip("画面高に対する片側レターボックスの比率。")]
        [FormerlySerializedAs("m_letterboxRatio")]
        [Range(0f, 0.5f)]
        [SerializeField]
        private float _letterboxRatio = 0.1f;


        // Methods

        private void OnGUI()
        {
            if (_letterboxEnabled)
            {
                DrawLetterbox();
            }

            if (!_drawGuides)
            {
                return;
            }

            switch (_guideMode)
            {
                case SplitGuideMode.Symmetrical:
                    DrawVertical(Screen.width * 0.5f, 0f, Screen.height);
                    break;

                case SplitGuideMode.Bisection:
                    DrawVertical(Screen.width * 0.5f, 0f, Screen.height);
                    DrawHorizontal(Screen.height * 0.5f, 0f, Screen.width);
                    break;

                case SplitGuideMode.Thirds:
                    DrawThirds(0f, Screen.height);
                    break;

                case SplitGuideMode.Diagonal:
                    DrawDiagonals();
                    break;

                case SplitGuideMode.ThirdsAndDiagonal:
                    DrawThirds(0f, Screen.height);
                    DrawDiagonals();
                    break;

                case SplitGuideMode.CinemaScope:
                    DrawCinemaScopeThirds();
                    break;
            }
        }

        private void DrawLetterbox()
        {
            float height = Screen.height * _letterboxRatio;
            DrawRect(new Rect(0f, 0f, Screen.width, height), _letterboxColor);
            DrawRect(new Rect(0f, Screen.height - height, Screen.width, height), _letterboxColor);
        }

        private void DrawThirds(float top, float height)
        {
            DrawVertical(Screen.width / 3f, top, height);
            DrawVertical(Screen.width * 2f / 3f, top, height);
            DrawHorizontal(top + height / 3f, 0f, Screen.width);
            DrawHorizontal(top + height * 2f / 3f, 0f, Screen.width);
        }

        private void DrawCinemaScopeThirds()
        {
            float imageHeight = Mathf.Min(Screen.height, Screen.width / 2.35f);
            float top = (Screen.height - imageHeight) * 0.5f;
            DrawThirds(top, imageHeight);
        }

        private void DrawDiagonals()
        {
            DrawLine(Vector2.zero, new Vector2(Screen.width, Screen.height));
            DrawLine(new Vector2(Screen.width, 0f), new Vector2(0f, Screen.height));
        }

        private void DrawVertical(float x, float top, float height)
        {
            DrawRect(new Rect(x - _lineWidth * 0.5f, top, _lineWidth, height), _lineColor);
        }

        private void DrawHorizontal(float y, float left, float width)
        {
            DrawRect(new Rect(left, y - _lineWidth * 0.5f, width, _lineWidth), _lineColor);
        }

        private void DrawLine(Vector2 start, Vector2 end)
        {
            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousColor = GUI.color;
            float angle = Mathf.Atan2(end.y - start.y, end.x - start.x) * Mathf.Rad2Deg;
            float length = Vector2.Distance(start, end);

            GUI.color = _lineColor;
            GUIUtility.RotateAroundPivot(angle, start);
            GUI.DrawTexture(new Rect(start.x, start.y - _lineWidth * 0.5f, length, _lineWidth), Texture2D.whiteTexture);
            GUI.matrix = previousMatrix;
            GUI.color = previousColor;
        }

        private static void DrawRect(Rect rect, Color color)
        {
            Color previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previousColor;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _lineWidth = Mathf.Clamp(_lineWidth, 0.5f, 10f);
            _letterboxRatio = Mathf.Clamp(_letterboxRatio, 0f, 0.5f);
        }
#endif
    }
}
