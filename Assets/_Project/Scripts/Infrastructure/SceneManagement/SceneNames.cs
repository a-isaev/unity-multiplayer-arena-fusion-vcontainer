namespace CoinRush.Infrastructure.SceneManagement
{
    /// <summary>
    /// Scene names in one place, so a rename breaks only this file and not string literals all over the code.
    /// Bootstrap and Menu are in Build Settings. Arena will be loaded through Addressables (stage 4).
    /// </summary>
    public static class SceneNames
    {
        public const string Bootstrap = "Bootstrap";
        public const string Menu = "Menu";
    }
}
