using UnityEngine;
using UnityEngine.InputSystem;

namespace CoinRush.Gameplay
{
    internal sealed class ArenaTouchControls
    {
        private int? _stickFinger;
        private int? _dashFinger;
        private bool _dashPressed;

        public Vector2 Move { get; private set; }

        internal static float Size => Mathf.Clamp(Mathf.Min(Screen.width, Screen.height) * 0.24f, 64f, 180f);

        private static Vector2 StickCenter => new(Screen.safeArea.xMin + Size * 0.65f,
            Screen.safeArea.yMin + Size * 0.65f);

        private static Vector2 DashCenter => new(Screen.safeArea.xMax - Size * 0.65f,
            Screen.safeArea.yMin + Size * 0.65f);

        public void Update()
        {
            var screen = Touchscreen.current;
            if (!Application.isFocused || screen == null)
            {
                _stickFinger = null;
                _dashFinger = null;
                _dashPressed = false;
                Move = Vector2.zero;
                return;
            }

            var stickActive = false;
            var dashActive = false;
            var radius = Size * 0.5f;
            foreach (var touch in screen.touches)
            {
                var pressedNow = touch.press.wasPressedThisFrame;
                if (!touch.press.isPressed && !pressedNow)
                    continue;

                var id = touch.touchId.ReadValue();
                var position = touch.position.ReadValue();
                var start = touch.startPosition.ReadValue();
                if (_stickFinger == null && pressedNow && touch.press.isPressed &&
                    ControlRect(StickCenter, Size).Contains(start))
                    _stickFinger = id;

                if (_dashFinger == null && pressedNow && ControlRect(DashCenter, Size).Contains(start))
                {
                    if (touch.press.isPressed)
                        _dashFinger = id;

                    _dashPressed = true;
                }

                if (_stickFinger == id && touch.press.isPressed)
                {
                    stickActive = true;
                    var offset = (position - StickCenter) / radius;
                    Move = offset.sqrMagnitude < 0.01f ? Vector2.zero : Vector2.ClampMagnitude(offset, 1f);
                }

                if (_dashFinger == id && touch.press.isPressed)
                    dashActive = true;
            }

            if (!stickActive)
            {
                _stickFinger = null;
                Move = Vector2.zero;
            }

            if (!dashActive)
                _dashFinger = null;
        }

        public bool ConsumeDash()
        {
            var pressed = Application.isFocused && (_dashPressed || _dashFinger.HasValue);
            _dashPressed = false;
            return pressed;
        }

        public void Draw()
        {
            DrawControl(StickCenter, Size, "Move");
            DrawControl(StickCenter + Move * (Size * 0.26f), Size * 0.32f, "");
            DrawControl(DashCenter, Size, "Dash");
        }

        private static Rect ControlRect(Vector2 center, float size) =>
            new(center.x - size / 2f, center.y - size / 2f, size, size);

        private static void DrawControl(Vector2 center, float size, string label)
        {
            var rect = ControlRect(center, size);
            rect.y = Screen.height - rect.yMax;
            GUI.Box(rect, label);
        }
    }
}
