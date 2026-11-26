using UnityEngine;

namespace CoinRush.Infrastructure.Flow
{
    public sealed class LoadingOverlay : MonoBehaviour
    {
        public string Message { get; set; } = "Loading arena…";

        private void OnGUI()
        {
            var width = Mathf.Min(400f, Screen.width - 24f);
            GUI.Box(new Rect((Screen.width - width) * 0.5f, (Screen.height - 56f) * 0.5f, width, 56f), Message);
        }
    }
}
