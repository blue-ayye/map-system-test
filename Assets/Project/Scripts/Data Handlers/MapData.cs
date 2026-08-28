namespace BP.MapSystem
{
    /// <summary>
    /// Represents the data structure for storing map-related information, including the seed used for map generation, whether a custom seed was used, and the traversal data for the map.
    /// </summary>
    [System.Serializable]
    public class MapData
    {
        public int Seed;
        public bool IsCustomSeedUsed;
        public MapTraversalData MapTraversalData;
    }
}