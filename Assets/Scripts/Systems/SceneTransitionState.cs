namespace TerrasHeart.Systems
{
    /// <summary>
    /// Static state carrier for scene transitions.
    /// Holds the target spawn ID between scene load and PlayerSpawnPoint resolution.
    /// No MonoBehaviour — survives scene changes automatically.
    /// </summary>
    public static class SceneTransitionState
    {
        public static string TargetSpawnID { get; set; } = string.Empty;
    }
}