namespace PowerPulse.Core
{
    /// <summary>
    /// Contract for scene transitions.
    /// </summary>
    public interface ISceneLoader : IGameService
    {
        /// <summary>Loads a scene by name with a fade in/out transition.</summary>
        /// <param name="sceneName">Name of the scene as set in Build Settings.</param>
        void LoadScene(string sceneName);
    }
}
