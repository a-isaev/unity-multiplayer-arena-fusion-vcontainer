using UnityEngine;

namespace CoinRush.Gameplay
{
    public static class ArenaScreenLayout
    {
        private const float Gap = 12f;

        public static void GetRects(out Rect field, out Rect room, out Rect score, out float roomScale)
        {
            var screen = Screen.safeArea;
            var safe = new Rect(screen.x, Screen.height - screen.yMax, screen.width, screen.height);
            roomScale = Application.isMobilePlatform
                ? Mathf.Min(1.5f, safe.width / 340f, safe.height / 188f)
                : 1f;

            var touchSpace = Application.isMobilePlatform ? ArenaTouchControls.Size * 1.2f : 0f;
            var roomHeight = 164f * roomScale;
            var scoreHeight = Mathf.Clamp(safe.height * 0.24f, 132f, 178f);
            var topSpace = roomHeight + 2f * Gap;
            var bottomSpace = touchSpace + scoreHeight + 2f * Gap;
            var rail = 320f * roomScale + 2f * Gap;
            var sideFieldWidth = safe.width - 2f * rail;
            var stackedFieldHeight = safe.height - topSpace - bottomSpace;

            if (sideFieldWidth > Mathf.Max(1f, stackedFieldHeight))
            {
                room = new Rect(safe.xMax - rail + Gap, safe.yMin + Gap, rail - 2f * Gap, roomHeight);
                score = new Rect(safe.xMin + Gap, safe.yMin + Gap, rail - 2f * Gap, scoreHeight);
                field = new Rect(safe.xMin + rail, safe.yMin + Gap, sideFieldWidth, safe.height - 2f * Gap);
                return;
            }

            var roomWidth = Mathf.Min(400f * roomScale, safe.width - 2f * Gap);
            room = new Rect(safe.center.x - roomWidth * 0.5f, safe.yMin + Gap, roomWidth, roomHeight);
            score = new Rect(safe.xMin + Gap, safe.yMax - touchSpace - scoreHeight - Gap,
                Mathf.Min(340f, safe.width - 2f * Gap), scoreHeight);
            field = new Rect(safe.xMin + Gap, safe.yMin + topSpace, safe.width - 2f * Gap,
                Mathf.Max(1f, stackedFieldHeight));
        }
    }
}
