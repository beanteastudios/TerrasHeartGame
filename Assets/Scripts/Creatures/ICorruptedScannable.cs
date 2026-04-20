namespace TerrasHeart.Creatures
{
    /// <summary>
    /// Marker interface. Implement on any creature AI whose successful scan
    /// should apply CorruptedRestoration to biome health rather than ScanRestoration.
    /// The scan window on these creatures only opens in the restored state,
    /// so any successful scan of an ICorruptedScannable is always a post-restoration event.
    /// </summary>
    public interface ICorruptedScannable { }
}